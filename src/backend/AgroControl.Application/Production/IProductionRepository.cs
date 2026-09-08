using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.Application.Production;

public interface IProductionRepository
{
    Task<(IReadOnlyList<Farm> Items, int TotalCount)> ListFarmsAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool includeInactive,
        IReadOnlyCollection<Guid>? allowedFarmIds = null,
        Guid? regionId = null,
        string? stateCode = null,
        CancellationToken cancellationToken = default);

    Task<Farm?> GetFarmAsync(Guid organizationId, Guid farmId, bool tracking, CancellationToken cancellationToken = default);
    void AddFarm(Farm farm);

    Task<(IReadOnlyList<Field> Items, int TotalCount)> ListFieldsAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? farmId,
        string? search,
        bool includeInactive,
        IReadOnlyCollection<Guid>? allowedFarmIds = null,
        CancellationToken cancellationToken = default);

    Task<Field?> GetFieldAsync(Guid organizationId, Guid fieldId, bool tracking, CancellationToken cancellationToken = default);
    Task<decimal> GetAllocatedFieldAreaAsync(Guid organizationId, Guid farmId, Guid? excludingFieldId = null, CancellationToken cancellationToken = default);
    void AddField(Field field);

    Task<(IReadOnlyList<Crop> Items, int TotalCount)> ListCropsAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<Crop?> GetCropAsync(Guid organizationId, Guid cropId, bool tracking, CancellationToken cancellationToken = default);
    void AddCrop(Crop crop);

    Task<(IReadOnlyList<Season> Items, int TotalCount)> ListSeasonsAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? fieldId,
        SeasonStatus? status,
        string? search,
        bool includeInactive,
        IReadOnlyCollection<Guid>? allowedFarmIds = null,
        CancellationToken cancellationToken = default);

    Task<Season?> GetSeasonAsync(Guid organizationId, Guid seasonId, bool tracking, CancellationToken cancellationToken = default);
    void AddSeason(Season season);
}
