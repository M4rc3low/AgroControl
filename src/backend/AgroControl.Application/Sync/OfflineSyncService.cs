using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;

namespace AgroControl.Application.Sync;

public sealed class OfflineSyncService(
    IProductionRepository productionRepository,
    IFarmAccessScope farmAccessScope)
{
    private static readonly string[] InitialEntityKinds = ["farm", "field", "crop", "season"];

    public async Task<OperationResult<OfflineSyncStatusDto>> GetStatusAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        if (!await farmAccessScope.CanAccessFarmAsync(organizationId, userId, farmId, cancellationToken))
        {
            return OperationResult<OfflineSyncStatusDto>.Forbidden(
                "Farm access is not available for offline synchronization.");
        }

        var farm = await productionRepository.GetFarmAsync(
            organizationId,
            farmId,
            tracking: false,
            cancellationToken);

        if (farm is null || !farm.IsActive)
        {
            return OperationResult<OfflineSyncStatusDto>.NotFound("Farm not found or inactive.");
        }

        return OperationResult<OfflineSyncStatusDto>.Success(new OfflineSyncStatusDto(
            farm.Id,
            farm.Name,
            ProtocolVersion: 1,
            LocalSchemaVersion: 1,
            EntityKinds: InitialEntityKinds,
            ServerTimeUtc: DateTime.UtcNow,
            FarmUpdatedAtUtc: farm.UpdatedAtUtc));
    }
}
