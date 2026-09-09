using AgroControl.Application.Production;

namespace AgroControl.Application.Sync;

public sealed record OfflineSyncStatusDto(
    Guid FarmId,
    string FarmName,
    int ProtocolVersion,
    int LocalSchemaVersion,
    IReadOnlyList<string> EntityKinds,
    DateTime ServerTimeUtc,
    DateTime FarmUpdatedAtUtc);

public sealed record OfflineBootstrapDto(
    FarmDto Farm,
    IReadOnlyList<FieldDto> Fields,
    IReadOnlyList<CropDto> Crops,
    IReadOnlyList<SeasonDto> Seasons,
    int ProtocolVersion,
    int LocalSchemaVersion,
    DateTime ServerTimeUtc,
    string Cursor,
    long WatermarkSequence);

public sealed record OfflinePullChangeDto(
    long Sequence,
    string EntityKind,
    Guid EntityId,
    string ChangeType,
    object? Payload,
    DateTime OccurredAtUtc);

public sealed record OfflinePullDto(
    Guid FarmId,
    IReadOnlyList<OfflinePullChangeDto> Changes,
    string Cursor,
    long LastSequence,
    bool HasMore,
    DateTime ServerTimeUtc);
