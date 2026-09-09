using System.Globalization;
using System.Text.Json;
using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.Application.Sync;

public sealed class OfflinePushService(
    IFarmAccessScope farmAccessScope,
    IProductionRepository productionRepository,
    FieldService fieldService,
    SeasonService seasonService,
    IOfflineSyncIdempotencyRepository idempotencyRepository,
    IOfflineSyncConcurrencyRepository concurrencyRepository,
    IOfflineSyncTransaction transaction)
{
    private const int MaxBatchSize = 100;
    private static readonly TimeSpan IdempotencyRetention = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<OperationResult<OfflinePushDto>> PushAsync(
        Guid organizationId,
        Guid userId,
        OfflinePushRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || request.FarmId == Guid.Empty)
            return OperationResult<OfflinePushDto>.Validation("FarmId is required for offline push.");
        if (request.Operations is null || request.Operations.Count is < 1 or > MaxBatchSize)
            return OperationResult<OfflinePushDto>.Validation($"Offline push requires between 1 and {MaxBatchSize} operations.");
        if (request.Operations.Any(item => item.OperationId == Guid.Empty || item.EntityId == Guid.Empty))
            return OperationResult<OfflinePushDto>.Validation("Every offline operation requires non-empty operationId and entityId.");
        if (request.Operations.Select(item => item.OperationId).Distinct().Count() != request.Operations.Count)
            return OperationResult<OfflinePushDto>.Validation("Offline push contains duplicate operationId values.");

        if (!await farmAccessScope.CanAccessFarmAsync(organizationId, userId, request.FarmId, cancellationToken))
            return OperationResult<OfflinePushDto>.Forbidden("Farm access is not available for offline synchronization.");

        var farm = await productionRepository.GetFarmAsync(organizationId, request.FarmId, false, cancellationToken);
        if (farm is null || !farm.IsActive)
            return OperationResult<OfflinePushDto>.NotFound("Farm not found or inactive.");

        var results = new List<OfflinePushOperationResultDto>(request.Operations.Count);
        foreach (var operation in request.Operations)
        {
            if (!await farmAccessScope.CanAccessFarmAsync(organizationId, userId, request.FarmId, cancellationToken))
            {
                results.Add(Result(
                    operation,
                    "Forbidden",
                    errorCode: "FarmAccessRevoked",
                    message: "Farm access was revoked before this operation could be synchronized."));
                continue;
            }

            results.Add(await ProcessOperationSafelyAsync(
                organizationId,
                userId,
                request.FarmId,
                operation,
                cancellationToken));
        }

        return OperationResult<OfflinePushDto>.Success(new OfflinePushDto(
            request.FarmId,
            results,
            DateTime.UtcNow));
    }

    private async Task<OfflinePushOperationResultDto> ProcessOperationSafelyAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        OfflinePushOperationDto operation,
        CancellationToken cancellationToken)
    {
        var entityKind = operation.EntityKind?.Trim().ToLowerInvariant() ?? string.Empty;
        var operationType = operation.Operation?.Trim().ToLowerInvariant() ?? string.Empty;
        if (entityKind is not ("field" or "season"))
            return Result(operation, "ValidationError", errorCode: "UnsupportedEntity", message: "Only field and season are enabled for offline mutations.");
        if (operationType is not ("create" or "update" or "delete"))
            return Result(operation, "ValidationError", errorCode: "UnsupportedOperation", message: "Offline operation must be create, update, or delete.");

        var requestHash = OfflineSyncRequestHasher.Compute(farmId, operation);
        var nowUtc = DateTime.UtcNow;

        try
        {
            return await transaction.ExecuteAsync(async ct =>
            {
                var claimed = await idempotencyRepository.TryClaimAsync(
                    organizationId,
                    userId,
                    operation.OperationId,
                    farmId,
                    entityKind,
                    operation.EntityId,
                    operationType,
                    requestHash,
                    nowUtc,
                    nowUtc.Add(IdempotencyRetention),
                    ct);

                if (!claimed)
                    return await ReplayOrConflictAsync(organizationId, userId, operation, requestHash, ct);

                var result = entityKind switch
                {
                    "field" => await ApplyFieldAsync(organizationId, userId, farmId, operation, operationType, ct),
                    "season" => await ApplySeasonAsync(organizationId, userId, farmId, operation, operationType, ct),
                    _ => throw new InvalidOperationException("Validated offline entity kind was not recognized.")
                };

                var stored = result with { Replayed = false };
                await idempotencyRepository.CompleteAsync(
                    organizationId,
                    userId,
                    operation.OperationId,
                    requestHash,
                    JsonSerializer.Serialize(stored),
                    DateTime.UtcNow,
                    ct);
                return stored;
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Result(
                operation,
                "RetryableError",
                errorCode: "TransientFailure",
                message: "The operation was not committed and can be retried safely.");
        }
    }

    private async Task<OfflinePushOperationResultDto> ReplayOrConflictAsync(
        Guid organizationId,
        Guid userId,
        OfflinePushOperationDto operation,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var stored = await idempotencyRepository.GetAsync(
            organizationId,
            userId,
            operation.OperationId,
            cancellationToken);
        if (stored is null)
            throw new InvalidOperationException("Idempotency claim disappeared before replay resolution.");
        if (!string.Equals(stored.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Result(
                operation,
                "Conflict",
                errorCode: "OperationIdReuse",
                message: "The same operationId was already used with different content.");
        }
        if (!string.Equals(stored.Status, "completed", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(stored.ResultJson))
            throw new InvalidOperationException("Idempotency operation is not in a replayable state.");

        var replay = JsonSerializer.Deserialize<OfflinePushOperationResultDto>(stored.ResultJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored idempotency result is invalid.");
        return replay with { Replayed = true };
    }

    private async Task<OfflinePushOperationResultDto> ApplyFieldAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        OfflinePushOperationDto operation,
        string operationType,
        CancellationToken cancellationToken)
    {
        if (operationType == "create")
        {
            if (!string.IsNullOrWhiteSpace(operation.BaseServerVersion))
                return Result(operation, "ValidationError", errorCode: "UnexpectedBaseVersion", message: "Create operations must not include baseServerVersion.");
            var payload = DeserializePayload<OfflineFieldMutationPayload>(operation);
            if (payload is null)
                return Result(operation, "ValidationError", errorCode: "InvalidPayload", message: "Field payload is required and invalid.");
            if (payload.FarmId != farmId)
                return Result(operation, "Forbidden", errorCode: "CrossFarmMutation", message: "Offline field mutations cannot target another farm.");

            var created = await fieldService.CreateWithIdAsync(
                organizationId,
                userId,
                operation.EntityId,
                new CreateFieldCommand(payload.FarmId, payload.Name, payload.AreaHectares),
                cancellationToken);
            return FromOperationResult(operation, created);
        }

        var locked = await concurrencyRepository.LockFieldAsync(
            organizationId,
            farmId,
            operation.EntityId,
            cancellationToken);
        if (locked is null)
            return Result(operation, "NotFound", errorCode: "FieldNotFound", message: "Field was not found in the selected farm.");
        if (!BaseVersionMatches(operation.BaseServerVersion, locked.UpdatedAtUtc))
        {
            var current = await fieldService.GetAsync(organizationId, userId, operation.EntityId, cancellationToken);
            return Result(
                operation,
                "Conflict",
                Version(locked.UpdatedAtUtc),
                current,
                "VersionConflict",
                "Field changed on the server after the offline copy was created.");
        }

        if (operationType == "delete")
        {
            if (operation.Payload is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
                return Result(operation, "ValidationError", errorCode: "UnexpectedPayload", message: "Delete operations must not include a payload.");
            var deleted = await fieldService.DeactivateAsync(organizationId, userId, operation.EntityId, cancellationToken);
            if (!deleted.Succeeded) return FromOperationResult(operation, deleted);
            var current = await fieldService.GetAsync(organizationId, userId, operation.EntityId, cancellationToken);
            return Result(operation, "Applied", current is null ? null : Version(current.UpdatedAtUtc), current);
        }

        var updatePayload = DeserializePayload<OfflineFieldMutationPayload>(operation);
        if (updatePayload is null)
            return Result(operation, "ValidationError", errorCode: "InvalidPayload", message: "Field payload is required and invalid.");
        if (updatePayload.FarmId != farmId)
            return Result(operation, "Forbidden", errorCode: "CrossFarmMutation", message: "Offline field mutations cannot move a field to another farm.");
        var updated = await fieldService.UpdateAsync(
            organizationId,
            userId,
            operation.EntityId,
            new UpdateFieldCommand(updatePayload.FarmId, updatePayload.Name, updatePayload.AreaHectares),
            cancellationToken);
        return FromOperationResult(operation, updated);
    }

    private async Task<OfflinePushOperationResultDto> ApplySeasonAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        OfflinePushOperationDto operation,
        string operationType,
        CancellationToken cancellationToken)
    {
        if (operationType == "create")
        {
            if (!string.IsNullOrWhiteSpace(operation.BaseServerVersion))
                return Result(operation, "ValidationError", errorCode: "UnexpectedBaseVersion", message: "Create operations must not include baseServerVersion.");
            var payload = DeserializePayload<OfflineSeasonCreatePayload>(operation);
            if (payload is null)
                return Result(operation, "ValidationError", errorCode: "InvalidPayload", message: "Season payload is required and invalid.");
            if (!await FieldBelongsToFarmAsync(organizationId, payload.FieldId, farmId, cancellationToken))
                return Result(operation, "Forbidden", errorCode: "CrossFarmMutation", message: "Offline season mutations cannot target a field from another farm.");

            var created = await seasonService.CreateWithIdAsync(
                organizationId,
                userId,
                operation.EntityId,
                new CreateSeasonCommand(
                    payload.FieldId,
                    payload.CropId,
                    payload.Name,
                    payload.StartDate,
                    payload.EndDate,
                    payload.ExpectedYieldPerHectare),
                cancellationToken);
            return FromOperationResult(operation, created);
        }

        var locked = await concurrencyRepository.LockSeasonAsync(
            organizationId,
            farmId,
            operation.EntityId,
            cancellationToken);
        if (locked is null)
            return Result(operation, "NotFound", errorCode: "SeasonNotFound", message: "Season was not found in the selected farm.");
        if (!BaseVersionMatches(operation.BaseServerVersion, locked.UpdatedAtUtc))
        {
            var current = await seasonService.GetAsync(organizationId, userId, operation.EntityId, cancellationToken);
            return Result(
                operation,
                "Conflict",
                Version(locked.UpdatedAtUtc),
                current,
                "VersionConflict",
                "Season changed on the server after the offline copy was created.");
        }

        if (operationType == "delete")
        {
            if (operation.Payload is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
                return Result(operation, "ValidationError", errorCode: "UnexpectedPayload", message: "Delete operations must not include a payload.");
            var deleted = await seasonService.DeactivateAsync(organizationId, userId, operation.EntityId, cancellationToken);
            if (!deleted.Succeeded) return FromOperationResult(operation, deleted);
            var current = await seasonService.GetAsync(organizationId, userId, operation.EntityId, cancellationToken);
            return Result(operation, "Applied", current is null ? null : Version(current.UpdatedAtUtc), current);
        }

        var updatePayload = DeserializePayload<OfflineSeasonUpdatePayload>(operation);
        if (updatePayload is null || !Enum.TryParse<SeasonStatus>(updatePayload.Status, true, out var status))
            return Result(operation, "ValidationError", errorCode: "InvalidPayload", message: "Season update payload or status is invalid.");
        if (!await FieldBelongsToFarmAsync(organizationId, updatePayload.FieldId, farmId, cancellationToken))
            return Result(operation, "Forbidden", errorCode: "CrossFarmMutation", message: "Offline season mutations cannot move a season to another farm.");

        var updated = await seasonService.UpdateAsync(
            organizationId,
            userId,
            operation.EntityId,
            new UpdateSeasonCommand(
                updatePayload.FieldId,
                updatePayload.CropId,
                updatePayload.Name,
                updatePayload.StartDate,
                updatePayload.EndDate,
                updatePayload.ExpectedYieldPerHectare,
                updatePayload.ActualYieldPerHectare,
                status),
            cancellationToken);
        return FromOperationResult(operation, updated);
    }

    private async Task<bool> FieldBelongsToFarmAsync(
        Guid organizationId,
        Guid fieldId,
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var field = await productionRepository.GetFieldAsync(organizationId, fieldId, false, cancellationToken);
        return field is not null && field.FarmId == farmId && field.IsActive;
    }

    private static T? DeserializePayload<T>(OfflinePushOperationDto operation) where T : class
    {
        if (operation.Payload is null || operation.Payload.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(operation.Payload.Value.GetRawText(), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool BaseVersionMatches(string? baseServerVersion, DateTime serverUpdatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(baseServerVersion)) return false;
        if (!DateTimeOffset.TryParse(
                baseServerVersion,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)) return false;
        return parsed.UtcDateTime.Ticks == NormalizeUtc(serverUpdatedAtUtc).Ticks;
    }

    private static string Version(DateTime updatedAtUtc) =>
        NormalizeUtc(updatedAtUtc).ToString("O", CultureInfo.InvariantCulture);

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static OfflinePushOperationResultDto FromOperationResult<T>(
        OfflinePushOperationDto operation,
        OperationResult<T> result)
    {
        if (result.Succeeded)
        {
            var version = result.Value switch
            {
                FieldDto field => Version(field.UpdatedAtUtc),
                SeasonDto season => Version(season.UpdatedAtUtc),
                _ => null
            };
            return Result(operation, "Applied", version, result.Value);
        }

        return result.ErrorKind switch
        {
            OperationErrorKind.Validation => Result(operation, "ValidationError", errorCode: "DomainValidation", message: result.Error),
            OperationErrorKind.NotFound => Result(operation, "NotFound", errorCode: "NotFound", message: result.Error),
            OperationErrorKind.Conflict => Result(operation, "Conflict", errorCode: "DomainConflict", message: result.Error),
            OperationErrorKind.Forbidden => Result(operation, "Forbidden", errorCode: "Forbidden", message: result.Error),
            _ => Result(operation, "RetryableError", errorCode: "UnexpectedResult", message: "Operation did not return a supported result.")
        };
    }

    private static OfflinePushOperationResultDto Result(
        OfflinePushOperationDto operation,
        string status,
        string? serverVersion = null,
        object? serverEntity = null,
        string? errorCode = null,
        string? message = null,
        bool replayed = false) => new(
            operation.OperationId,
            operation.EntityKind,
            operation.EntityId,
            status,
            serverVersion,
            serverEntity,
            errorCode,
            message,
            replayed);
}
