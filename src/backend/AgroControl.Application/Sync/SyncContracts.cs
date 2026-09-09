using System.Text.Json;
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

public sealed record OfflinePushRequestDto(
    Guid FarmId,
    IReadOnlyList<OfflinePushOperationDto> Operations);

public sealed record OfflinePushOperationDto(
    Guid OperationId,
    string EntityKind,
    Guid EntityId,
    string Operation,
    string? BaseServerVersion,
    JsonElement? Payload);

public sealed record OfflinePushOperationResultDto(
    Guid OperationId,
    string EntityKind,
    Guid EntityId,
    string Status,
    string? ServerVersion,
    object? ServerEntity,
    string? ErrorCode,
    string? Message,
    bool Replayed);

public sealed record OfflinePushDto(
    Guid FarmId,
    IReadOnlyList<OfflinePushOperationResultDto> Results,
    DateTime ServerTimeUtc);

public sealed record OfflineFieldMutationPayload(
    Guid FarmId,
    string Name,
    decimal AreaHectares);

public sealed record OfflineSeasonCreatePayload(
    Guid FieldId,
    Guid CropId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ExpectedYieldPerHectare);

public sealed record OfflineSeasonUpdatePayload(
    Guid FieldId,
    Guid CropId,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ExpectedYieldPerHectare,
    decimal? ActualYieldPerHectare,
    string Status);
