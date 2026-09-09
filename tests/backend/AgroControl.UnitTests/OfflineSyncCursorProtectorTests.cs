using AgroControl.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace AgroControl.UnitTests;

public sealed class OfflineSyncCursorProtectorTests
{
    private const string SigningRoot = "agrocontrol-test-jwt-key-with-more-than-32-characters-2026";

    [Fact]
    public void ProtectAndValidate_ForSameScope_RestoresSequence()
    {
        var protector = CreateProtector();
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 9, 1, 0, 0, DateTimeKind.Utc);

        var cursor = protector.Protect(organizationId, farmId, 1234, now);
        var result = protector.Validate(cursor, organizationId, farmId, now.AddHours(1));

        Assert.True(result.IsValid);
        Assert.Equal(1234, result.Sequence);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Validate_ForDifferentFarm_IsRejected()
    {
        var protector = CreateProtector();
        var organizationId = Guid.NewGuid();
        var farmA = Guid.NewGuid();
        var farmB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cursor = protector.Protect(organizationId, farmA, 10, now);
        var result = protector.Validate(cursor, organizationId, farmB, now);

        Assert.False(result.IsValid);
        Assert.Equal(0, result.Sequence);
    }

    [Fact]
    public void Validate_ForTamperedCiphertext_IsRejected()
    {
        var protector = CreateProtector();
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cursor = protector.Protect(organizationId, farmId, 10, now);
        var replacement = cursor[^1] == 'A' ? 'B' : 'A';
        var tampered = cursor[..^1] + replacement;

        var result = protector.Validate(tampered, organizationId, farmId, now);

        Assert.False(result.IsValid);
        Assert.Equal(0, result.Sequence);
    }

    [Fact]
    public void Validate_ForExpiredCursor_RequiresNewBootstrap()
    {
        var protector = CreateProtector();
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var issuedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var cursor = protector.Protect(organizationId, farmId, 10, issuedAt);

        var result = protector.Validate(cursor, organizationId, farmId, issuedAt.AddDays(31));

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static OfflineSyncCursorProtector CreateProtector()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = SigningRoot
            })
            .Build();
        return new OfflineSyncCursorProtector(configuration);
    }
}
