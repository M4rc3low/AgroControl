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
    DateTime ServerTimeUtc);
