using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgroControl.Application.Sync;
using Microsoft.Extensions.Configuration;

namespace AgroControl.Infrastructure.Security;

public sealed class OfflineSyncCursorProtector : IOfflineSyncCursorProtector
{
    private const int CursorVersion = 1;
    private static readonly TimeSpan CursorLifetime = TimeSpan.FromDays(30);
    private static readonly byte[] AdditionalAuthenticatedData =
        Encoding.UTF8.GetBytes("AgroControl.OfflineSync.Cursor.v1");

    private readonly byte[] _encryptionKey;

    public OfflineSyncCursorProtector(IConfiguration configuration)
    {
        var rootSecret = configuration["Jwt:Key"] ?? string.Empty;
        if (rootSecret.Length < 32)
            throw new InvalidOperationException("Jwt:Key must contain at least 32 characters before sync cursors can be protected.");

        using var derivation = new HMACSHA256(Encoding.UTF8.GetBytes(rootSecret));
        _encryptionKey = derivation.ComputeHash(AdditionalAuthenticatedData);
    }

    public string Protect(
        Guid organizationId,
        Guid farmId,
        long sequence,
        DateTime issuedAtUtc)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (farmId == Guid.Empty) throw new ArgumentException("Farm id is required.", nameof(farmId));
        if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));

        var payload = new CursorPayload(
            CursorVersion,
            organizationId,
            farmId,
            sequence,
            NormalizeUtc(issuedAtUtc));
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(_encryptionKey, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AdditionalAuthenticatedData);
        }

        var token = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, token, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, token, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, token, nonce.Length + ciphertext.Length, tag.Length);
        return Base64UrlEncode(token);
    }

    public OfflineSyncCursorValidation Validate(
        string cursor,
        Guid expectedOrganizationId,
        Guid expectedFarmId,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return OfflineSyncCursorValidation.Invalid("Sync cursor is required.");
        if (expectedOrganizationId == Guid.Empty || expectedFarmId == Guid.Empty)
            return OfflineSyncCursorValidation.Invalid("Sync cursor scope is invalid.");

        try
        {
            var token = Base64UrlDecode(cursor);
            if (token.Length <= 28)
                return OfflineSyncCursorValidation.Invalid("Sync cursor is invalid.");

            var nonce = token.AsSpan(0, 12);
            var tag = token.AsSpan(token.Length - 16, 16);
            var ciphertext = token.AsSpan(12, token.Length - 28);
            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(_encryptionKey, tag.Length))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext, AdditionalAuthenticatedData);
            }

            var payload = JsonSerializer.Deserialize<CursorPayload>(plaintext);
            if (payload is null || payload.Version != CursorVersion || payload.Sequence < 0)
                return OfflineSyncCursorValidation.Invalid("Sync cursor is invalid.");
            if (payload.OrganizationId != expectedOrganizationId || payload.FarmId != expectedFarmId)
                return OfflineSyncCursorValidation.Invalid("Sync cursor does not belong to the requested farm scope.");

            var now = NormalizeUtc(nowUtc);
            var issuedAt = NormalizeUtc(payload.IssuedAtUtc);
            if (issuedAt > now.AddMinutes(5))
                return OfflineSyncCursorValidation.Invalid("Sync cursor issue time is invalid.");
            if (now - issuedAt > CursorLifetime)
                return OfflineSyncCursorValidation.Invalid("Sync cursor expired. Prepare the farm for offline use again.");

            return OfflineSyncCursorValidation.Valid(payload.Sequence);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException or ArgumentException)
        {
            return OfflineSyncCursorValidation.Invalid("Sync cursor is invalid.");
        }
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            0 => base64,
            2 => base64 + "==",
            3 => base64 + "=",
            _ => throw new FormatException("Invalid base64url length.")
        };
        return Convert.FromBase64String(base64);
    }

    private sealed record CursorPayload(
        int Version,
        Guid OrganizationId,
        Guid FarmId,
        long Sequence,
        DateTime IssuedAtUtc);
}
