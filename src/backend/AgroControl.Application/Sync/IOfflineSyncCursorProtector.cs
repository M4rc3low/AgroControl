namespace AgroControl.Application.Sync;

public sealed record OfflineSyncCursorValidation(
    bool IsValid,
    long Sequence,
    string? Error)
{
    public static OfflineSyncCursorValidation Valid(long sequence) => new(true, sequence, null);
    public static OfflineSyncCursorValidation Invalid(string error) => new(false, 0, error);
}

public interface IOfflineSyncCursorProtector
{
    string Protect(
        Guid organizationId,
        Guid farmId,
        long sequence,
        DateTime issuedAtUtc);

    OfflineSyncCursorValidation Validate(
        string cursor,
        Guid expectedOrganizationId,
        Guid expectedFarmId,
        DateTime nowUtc);
}
