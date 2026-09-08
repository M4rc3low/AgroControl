using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Sustainability;

namespace AgroControl.Application.Sustainability;

public sealed class SustainabilityService(
    ISustainabilityRepository repository,
    IProductionRepository productionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<EmissionFactorDto>> ListFactorsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        EmissionSourceCategory? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListFactorsAsync(
            organizationId, (page - 1) * pageSize, pageSize, search, category, includeInactive, cancellationToken);
        return new PagedResult<EmissionFactorDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<EmissionFactorDto?> GetFactorAsync(Guid organizationId, Guid factorId, CancellationToken cancellationToken = default)
    {
        var factor = await repository.GetFactorAsync(organizationId, factorId, false, cancellationToken);
        return factor is null ? null : ToDto(factor);
    }

    public async Task<OperationResult<EmissionFactorDto>> CreateFactorAsync(
        Guid organizationId,
        CreateEmissionFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.Name) &&
            await repository.FactorNameExistsAsync(organizationId, command.Name.Trim(), null, cancellationToken))
            return OperationResult<EmissionFactorDto>.Conflict("An emission factor with this name already exists.");

        try
        {
            var factor = EmissionFactor.Create(
                organizationId, command.Name, command.Category, command.Unit, command.KgCo2ePerUnit,
                command.MethodologyReference, command.Notes, command.ValidFrom, command.ValidTo, DateTime.UtcNow);
            repository.AddFactor(factor);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<EmissionFactorDto>.Success(ToDto(factor));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<EmissionFactorDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<EmissionFactorDto>> UpdateFactorAsync(
        Guid organizationId,
        Guid factorId,
        UpdateEmissionFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        var factor = await repository.GetFactorAsync(organizationId, factorId, true, cancellationToken);
        if (factor is null) return OperationResult<EmissionFactorDto>.NotFound("Emission factor not found.");
        if (!string.IsNullOrWhiteSpace(command.Name) &&
            await repository.FactorNameExistsAsync(organizationId, command.Name.Trim(), factorId, cancellationToken))
            return OperationResult<EmissionFactorDto>.Conflict("An emission factor with this name already exists.");

        try
        {
            factor.Update(
                command.Name, command.Category, command.Unit, command.KgCo2ePerUnit,
                command.MethodologyReference, command.Notes, command.ValidFrom, command.ValidTo, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<EmissionFactorDto>.Success(ToDto(factor));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<EmissionFactorDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateFactorAsync(
        Guid organizationId,
        Guid factorId,
        CancellationToken cancellationToken = default)
    {
        var factor = await repository.GetFactorAsync(organizationId, factorId, true, cancellationToken);
        if (factor is null) return OperationResult<bool>.NotFound("Emission factor not found.");
        factor.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<EmissionActivityDto>> ListActivitiesAsync(
        Guid organizationId,
        int page,
        int pageSize,
        DateOnly? from,
        DateOnly? to,
        EmissionSourceCategory? category,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? sourceModule,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListActivitiesAsync(
            organizationId, (page - 1) * pageSize, pageSize, from, to, category,
            farmId, fieldId, seasonId, sourceModule, cancellationToken);
        return new PagedResult<EmissionActivityDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<EmissionActivityDto>> CreateActivityAsync(
        Guid organizationId,
        CreateEmissionActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        var factor = await repository.GetFactorAsync(organizationId, command.EmissionFactorId, false, cancellationToken);
        if (factor is null) return OperationResult<EmissionActivityDto>.NotFound("Emission factor not found.");
        if (!factor.IsActive) return OperationResult<EmissionActivityDto>.Conflict("Inactive emission factors cannot be used for new activities.");
        if (!factor.IsValidOn(command.ActivityDate))
            return OperationResult<EmissionActivityDto>.Validation("Emission factor is not valid on the activity date.");

        var refs = await ValidateAndNormalizeReferencesAsync(
            organizationId, command.FarmId, command.FieldId, command.SeasonId, cancellationToken);
        if (refs.Error is not null) return OperationResult<EmissionActivityDto>.Validation(refs.Error);

        if (!string.IsNullOrWhiteSpace(command.SourceModule) && !string.IsNullOrWhiteSpace(command.SourceReferenceId) &&
            await repository.ExternalReferenceExistsAsync(
                organizationId, command.SourceModule.Trim(), command.SourceReferenceId.Trim(), cancellationToken))
            return OperationResult<EmissionActivityDto>.Conflict("An emission activity already exists for this external reference.");

        try
        {
            var activity = EmissionActivity.Create(
                organizationId, factor, command.Quantity, command.ActivityDate, command.Origin, command.DataQuality,
                command.Description, refs.FarmId, refs.FieldId, refs.SeasonId,
                command.SourceModule, command.SourceReferenceId, command.Notes, DateTime.UtcNow);
            repository.AddActivity(activity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<EmissionActivityDto>.Success(ToDto(activity));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<EmissionActivityDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<SustainabilitySummaryDto>> GetSummaryAsync(
        Guid organizationId,
        DateOnly? from,
        DateOnly? to,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        CancellationToken cancellationToken = default)
    {
        if (from is not null && to is not null && from.Value > to.Value)
            return OperationResult<SustainabilitySummaryDto>.Validation("From date cannot be after to date.");

        var aggregate = await repository.GetAggregateAsync(
            organizationId, from, to, farmId, fieldId, seasonId, cancellationToken);

        decimal? previousKg = null;
        decimal? changePercent = null;
        if (from is not null && to is not null)
        {
            var days = to.Value.DayNumber - from.Value.DayNumber + 1;
            var previousTo = from.Value.AddDays(-1);
            var previousFrom = previousTo.AddDays(-(days - 1));
            var previous = await repository.GetAggregateAsync(
                organizationId, previousFrom, previousTo, farmId, fieldId, seasonId, cancellationToken);
            previousKg = previous.TotalKgCo2e;
            if (previous.TotalKgCo2e != 0m)
                changePercent = decimal.Round((aggregate.TotalKgCo2e - previous.TotalKgCo2e) / Math.Abs(previous.TotalKgCo2e) * 100m, 2, MidpointRounding.AwayFromZero);
        }

        return OperationResult<SustainabilitySummaryDto>.Success(new SustainabilitySummaryDto(
            from, to, farmId, fieldId, seasonId, aggregate.ActivityCount, aggregate.EstimatedActivityCount,
            aggregate.TotalKgCo2e, ToTonnes(aggregate.TotalKgCo2e), ToBreakdown(aggregate), previousKg, changePercent));
    }

    public async Task<OperationResult<SeasonSustainabilitySummaryDto>> GetSeasonSummaryAsync(
        Guid organizationId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        var season = await productionRepository.GetSeasonAsync(organizationId, seasonId, false, cancellationToken);
        if (season is null) return OperationResult<SeasonSustainabilitySummaryDto>.NotFound("Season not found.");
        var field = await productionRepository.GetFieldAsync(organizationId, season.FieldId, false, cancellationToken);
        if (field is null) return OperationResult<SeasonSustainabilitySummaryDto>.NotFound("Season field not found.");

        var aggregate = await repository.GetAggregateAsync(organizationId, null, null, null, null, seasonId, cancellationToken);
        var totalT = ToTonnes(aggregate.TotalKgCo2e);
        var perHectare = field.AreaHectares > 0m
            ? decimal.Round(totalT / field.AreaHectares, 6, MidpointRounding.AwayFromZero)
            : 0m;
        decimal? productionUnits = season.ActualYieldPerHectare is > 0m
            ? season.ActualYieldPerHectare.Value * field.AreaHectares
            : null;
        decimal? perUnit = productionUnits is > 0m
            ? decimal.Round(aggregate.TotalKgCo2e / productionUnits.Value, 6, MidpointRounding.AwayFromZero)
            : null;

        return OperationResult<SeasonSustainabilitySummaryDto>.Success(new SeasonSustainabilitySummaryDto(
            season.Id, field.Id, field.FarmId, field.AreaHectares, season.ActualYieldPerHectare,
            productionUnits, aggregate.ActivityCount, aggregate.TotalKgCo2e, totalT, perHectare,
            perUnit, ToBreakdown(aggregate)));
    }

    private async Task<NormalizedReferences> ValidateAndNormalizeReferencesAsync(
        Guid organizationId,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        CancellationToken cancellationToken)
    {
        Guid? normalizedFarmId = farmId;
        Guid? normalizedFieldId = fieldId;

        if (seasonId is not null)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, seasonId.Value, false, cancellationToken);
            if (season is null) return NormalizedReferences.Fail("Season does not belong to this organization.");
            if (fieldId is not null && fieldId.Value != season.FieldId)
                return NormalizedReferences.Fail("Season does not belong to the informed field.");
            normalizedFieldId = season.FieldId;
        }

        if (normalizedFieldId is not null)
        {
            var field = await productionRepository.GetFieldAsync(organizationId, normalizedFieldId.Value, false, cancellationToken);
            if (field is null) return NormalizedReferences.Fail("Field does not belong to this organization.");
            if (farmId is not null && farmId.Value != field.FarmId)
                return NormalizedReferences.Fail("Field does not belong to the informed farm.");
            normalizedFarmId = field.FarmId;
        }

        if (normalizedFarmId is not null)
        {
            var farm = await productionRepository.GetFarmAsync(organizationId, normalizedFarmId.Value, false, cancellationToken);
            if (farm is null) return NormalizedReferences.Fail("Farm does not belong to this organization.");
        }

        return new NormalizedReferences(normalizedFarmId, normalizedFieldId, seasonId, null);
    }

    private static IReadOnlyList<SustainabilityBreakdownDto> ToBreakdown(SustainabilityAggregateProjection aggregate) =>
        aggregate.Breakdown
            .OrderByDescending(item => Math.Abs(item.TotalKgCo2e))
            .Select(item => new SustainabilityBreakdownDto(
                item.Category,
                item.ActivityCount,
                item.TotalKgCo2e,
                ToTonnes(item.TotalKgCo2e),
                aggregate.TotalKgCo2e == 0m ? 0m : decimal.Round(item.TotalKgCo2e / aggregate.TotalKgCo2e * 100m, 2, MidpointRounding.AwayFromZero)))
            .ToList();

    private static EmissionFactorDto ToDto(EmissionFactor factor) => new(
        factor.Id, factor.Name, factor.Category, factor.Unit, factor.KgCo2ePerUnit,
        factor.MethodologyReference, factor.Notes, factor.ValidFrom, factor.ValidTo,
        factor.IsActive, factor.CreatedAtUtc, factor.UpdatedAtUtc);

    private static EmissionActivityDto ToDto(EmissionActivity activity) => new(
        activity.Id, activity.EmissionFactorId, activity.FactorNameSnapshot, activity.CategorySnapshot,
        activity.UnitSnapshot, activity.FactorKgCo2ePerUnitSnapshot, activity.Quantity,
        activity.EmissionsKgCo2e, activity.EmissionsTCo2e, activity.ActivityDate, activity.Origin,
        activity.DataQuality, activity.Description, activity.FarmId, activity.FieldId, activity.SeasonId,
        activity.SourceModule, activity.SourceReferenceId, activity.Notes, activity.CreatedAtUtc);

    private static decimal ToTonnes(decimal kgCo2e) => decimal.Round(kgCo2e / 1000m, 6, MidpointRounding.AwayFromZero);
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private sealed record NormalizedReferences(Guid? FarmId, Guid? FieldId, Guid? SeasonId, string? Error)
    {
        public static NormalizedReferences Fail(string error) => new(null, null, null, error);
    }
}
