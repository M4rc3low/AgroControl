using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.Application.Production;

public sealed class SeasonService(IProductionRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<SeasonDto>> ListAsync(Guid organizationId, int page, int pageSize, Guid? fieldId, SeasonStatus? status, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await repository.ListSeasonsAsync(organizationId, (page - 1) * pageSize, pageSize, fieldId, status, search, includeInactive, cancellationToken);
        return new PagedResult<SeasonDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<SeasonDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var season = await repository.GetSeasonAsync(organizationId, id, false, cancellationToken);
        return season is null ? null : ToDto(season);
    }

    public async Task<OperationResult<SeasonDto>> CreateAsync(Guid organizationId, CreateSeasonCommand command, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command.Name, command.StartDate, command.EndDate, command.ExpectedYieldPerHectare, null);
        if (validation is not null)
            return OperationResult<SeasonDto>.Validation(validation);

        var field = await repository.GetFieldAsync(organizationId, command.FieldId, false, cancellationToken);
        if (field is null || !field.IsActive)
            return OperationResult<SeasonDto>.Validation("Field does not exist or is inactive for this organization.");

        var crop = await repository.GetCropAsync(organizationId, command.CropId, false, cancellationToken);
        if (crop is null || !crop.IsActive)
            return OperationResult<SeasonDto>.Validation("Crop does not exist or is inactive for this organization.");

        var season = Season.Create(organizationId, command.FieldId, command.CropId, command.Name, command.StartDate, command.EndDate, command.ExpectedYieldPerHectare, DateTime.UtcNow);
        repository.AddSeason(season);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<SeasonDto>.Success(ToDto(season));
    }

    public async Task<OperationResult<SeasonDto>> UpdateAsync(Guid organizationId, Guid id, UpdateSeasonCommand command, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command.Name, command.StartDate, command.EndDate, command.ExpectedYieldPerHectare, command.ActualYieldPerHectare);
        if (validation is not null)
            return OperationResult<SeasonDto>.Validation(validation);

        var season = await repository.GetSeasonAsync(organizationId, id, true, cancellationToken);
        if (season is null)
            return OperationResult<SeasonDto>.NotFound("Season not found.");

        var field = await repository.GetFieldAsync(organizationId, command.FieldId, false, cancellationToken);
        if (field is null || !field.IsActive)
            return OperationResult<SeasonDto>.Validation("Field does not exist or is inactive for this organization.");

        var crop = await repository.GetCropAsync(organizationId, command.CropId, false, cancellationToken);
        if (crop is null || !crop.IsActive)
            return OperationResult<SeasonDto>.Validation("Crop does not exist or is inactive for this organization.");

        season.Update(command.FieldId, command.CropId, command.Name, command.StartDate, command.EndDate, command.ExpectedYieldPerHectare, command.ActualYieldPerHectare, command.Status, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<SeasonDto>.Success(ToDto(season));
    }

    public async Task<OperationResult<bool>> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var season = await repository.GetSeasonAsync(organizationId, id, true, cancellationToken);
        if (season is null)
            return OperationResult<bool>.NotFound("Season not found.");
        season.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static string? Validate(string name, DateOnly startDate, DateOnly? endDate, decimal? expectedYield, decimal? actualYield)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (endDate is not null && endDate < startDate) return "EndDate cannot be before StartDate.";
        if (expectedYield is < 0 || actualYield is < 0) return "Yield values cannot be negative.";
        return null;
    }

    private static SeasonDto ToDto(Season season) => new(season.Id, season.FieldId, season.CropId, season.Name, season.StartDate, season.EndDate, season.ExpectedYieldPerHectare, season.ActualYieldPerHectare, season.Status.ToString(), season.IsActive, season.CreatedAtUtc, season.UpdatedAtUtc);
}
