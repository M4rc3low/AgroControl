namespace AgroControl.Application.Sync;

public sealed record OfflineSyncStatusDto(
    Guid FarmId,
    string FarmName,
    int ProtocolVersion,
    int LocalSchemaVersion,
    IReadOnlyList<string> EntityKinds,
    DateTime ServerTimeUtc,
    DateTime FarmUpdatedAtUtc);
