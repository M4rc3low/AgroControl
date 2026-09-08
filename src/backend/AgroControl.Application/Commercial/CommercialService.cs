using AgroControl.Application.Common;
using AgroControl.Application.Exporting;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Commercial;

namespace AgroControl.Application.Commercial;

public sealed class CommercialService(ICommercialRepository repository, IProductionRepository productionRepository,
    IExportRepository exportRepository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<CommercialCustomerDto>> ListCustomersAsync(Guid organizationId, int page, int pageSize,
        string? search, CustomerStatus? status, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListCustomersAsync(organizationId, (page - 1) * pageSize, pageSize, search, status, cancellationToken);
        return new PagedResult<CommercialCustomerDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<CommercialCustomerDto?> GetCustomerAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetCustomerAsync(organizationId, customerId, false, cancellationToken);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<OperationResult<CommercialCustomerDto>> CreateCustomerAsync(Guid organizationId,
        CreateCommercialCustomerCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = CommercialCustomer.Create(organizationId, command.Name, command.TradeName, command.TaxId,
                command.Email, command.Phone, command.CountryCode, command.City, command.State, command.Status,
                command.Notes, DateTime.UtcNow);
            repository.AddCustomer(customer);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialCustomerDto>.Success(ToDto(customer));
        }
        catch (ArgumentException ex) { return OperationResult<CommercialCustomerDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<CommercialCustomerDto>> UpdateCustomerAsync(Guid organizationId, Guid customerId,
        UpdateCommercialCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetCustomerAsync(organizationId, customerId, true, cancellationToken);
        if (customer is null) return OperationResult<CommercialCustomerDto>.NotFound("Commercial customer not found.");
        try
        {
            customer.Update(command.Name, command.TradeName, command.TaxId, command.Email, command.Phone,
                command.CountryCode, command.City, command.State, command.Status, command.Notes, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialCustomerDto>.Success(ToDto(customer));
        }
        catch (ArgumentException ex) { return OperationResult<CommercialCustomerDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<IReadOnlyList<CommercialContactDto>>> ListContactsAsync(Guid organizationId,
        Guid customerId, CancellationToken cancellationToken = default)
    {
        if (await repository.GetCustomerAsync(organizationId, customerId, false, cancellationToken) is null)
            return OperationResult<IReadOnlyList<CommercialContactDto>>.NotFound("Commercial customer not found.");
        var contacts = await repository.ListContactsAsync(organizationId, customerId, cancellationToken);
        return OperationResult<IReadOnlyList<CommercialContactDto>>.Success(contacts.Select(ToDto).ToList());
    }

    public async Task<OperationResult<CommercialContactDto>> CreateContactAsync(Guid organizationId, Guid customerId,
        CreateCommercialContactCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetCustomerAsync(organizationId, customerId, false, cancellationToken);
        if (customer is null) return OperationResult<CommercialContactDto>.NotFound("Commercial customer not found.");
        if (customer.IsInactive) return OperationResult<CommercialContactDto>.Conflict("Contacts cannot be added to an inactive customer.");
        try
        {
            var contact = CommercialContact.Create(organizationId, customerId, command.Name, command.Role,
                command.Email, command.Phone, command.IsPrimary, DateTime.UtcNow);
            repository.AddContact(contact);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialContactDto>.Success(ToDto(contact));
        }
        catch (ArgumentException ex) { return OperationResult<CommercialContactDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<CommercialContactDto>> UpdateContactAsync(Guid organizationId, Guid contactId,
        UpdateCommercialContactCommand command, CancellationToken cancellationToken = default)
    {
        var contact = await repository.GetContactAsync(organizationId, contactId, true, cancellationToken);
        if (contact is null) return OperationResult<CommercialContactDto>.NotFound("Commercial contact not found.");
        try
        {
            contact.Update(command.Name, command.Role, command.Email, command.Phone, command.IsPrimary, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialContactDto>.Success(ToDto(contact));
        }
        catch (InvalidOperationException ex) { return OperationResult<CommercialContactDto>.Conflict(ex.Message); }
        catch (ArgumentException ex) { return OperationResult<CommercialContactDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<CommercialContactDto>> DeactivateContactAsync(Guid organizationId, Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contact = await repository.GetContactAsync(organizationId, contactId, true, cancellationToken);
        if (contact is null) return OperationResult<CommercialContactDto>.NotFound("Commercial contact not found.");
        contact.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<CommercialContactDto>.Success(ToDto(contact));
    }

    public async Task<PagedResult<CommercialOpportunityDto>> ListOpportunitiesAsync(Guid organizationId, int page, int pageSize,
        string? search, Guid? customerId, OpportunityStage? stage, string? ownerName, string? currency,
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListOpportunitiesAsync(organizationId, (page - 1) * pageSize, pageSize,
            search, customerId, stage, Normalize(ownerName), NormalizeCurrency(currency), from, to, cancellationToken);
        return new PagedResult<CommercialOpportunityDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<CommercialOpportunityDto?> GetOpportunityAsync(Guid organizationId, Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        var opportunity = await repository.GetOpportunityAsync(organizationId, opportunityId, false, cancellationToken);
        return opportunity is null ? null : ToDto(opportunity);
    }

    public async Task<OperationResult<CommercialOpportunityDto>> CreateOpportunityAsync(Guid organizationId,
        CreateCommercialOpportunityCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetCustomerAsync(organizationId, command.CustomerId, false, cancellationToken);
        if (customer is null) return OperationResult<CommercialOpportunityDto>.Validation("Customer does not belong to this organization.");
        if (customer.IsInactive) return OperationResult<CommercialOpportunityDto>.Conflict("Opportunities cannot be created for an inactive customer.");
        var refs = await ValidateReferencesAsync(organizationId, command.FarmId, command.CropId, command.SeasonId,
            command.ExportOrderId, cancellationToken);
        if (refs.Error is not null) return OperationResult<CommercialOpportunityDto>.Validation(refs.Error);
        try
        {
            var now = DateTime.UtcNow;
            var opportunity = CommercialOpportunity.Create(organizationId, command.CustomerId, command.Title,
                refs.FarmId, refs.CropId, refs.SeasonId, command.ExportOrderId, command.ExpectedValue,
                command.Currency, command.ProbabilityPercent, command.ExpectedCloseDate, command.OwnerName,
                command.NextStep, command.Notes, now);
            repository.AddOpportunity(opportunity);
            repository.AddStageEvent(OpportunityStageEvent.Create(organizationId, opportunity.Id, null,
                OpportunityStage.Lead, DateOnly.FromDateTime(now), "Oportunidade criada", now));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialOpportunityDto>.Success(ToDto(opportunity));
        }
        catch (ArgumentException ex) { return OperationResult<CommercialOpportunityDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<CommercialOpportunityDto>> UpdateOpportunityAsync(Guid organizationId, Guid opportunityId,
        UpdateCommercialOpportunityCommand command, CancellationToken cancellationToken = default)
    {
        var opportunity = await repository.GetOpportunityAsync(organizationId, opportunityId, true, cancellationToken);
        if (opportunity is null) return OperationResult<CommercialOpportunityDto>.NotFound("Commercial opportunity not found.");
        var refs = await ValidateReferencesAsync(organizationId, command.FarmId, command.CropId, command.SeasonId,
            command.ExportOrderId, cancellationToken);
        if (refs.Error is not null) return OperationResult<CommercialOpportunityDto>.Validation(refs.Error);
        try
        {
            opportunity.Update(command.Title, refs.FarmId, refs.CropId, refs.SeasonId, command.ExportOrderId,
                command.ExpectedValue, command.Currency, command.ProbabilityPercent, command.ExpectedCloseDate,
                command.OwnerName, command.NextStep, command.Notes, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialOpportunityDto>.Success(ToDto(opportunity));
        }
        catch (InvalidOperationException ex) { return OperationResult<CommercialOpportunityDto>.Conflict(ex.Message); }
        catch (ArgumentException ex) { return OperationResult<CommercialOpportunityDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<CommercialOpportunityDto>> TransitionStageAsync(Guid organizationId, Guid opportunityId,
        TransitionOpportunityStageCommand command, CancellationToken cancellationToken = default)
    {
        var opportunity = await repository.GetOpportunityAsync(organizationId, opportunityId, true, cancellationToken);
        if (opportunity is null) return OperationResult<CommercialOpportunityDto>.NotFound("Commercial opportunity not found.");
        var from = opportunity.Stage;
        try
        {
            opportunity.TransitionTo(command.Stage, command.OccurredOn, DateTime.UtcNow);
            if (opportunity.Stage != from)
                repository.AddStageEvent(OpportunityStageEvent.Create(organizationId, opportunity.Id, from,
                    opportunity.Stage, command.OccurredOn, command.Notes, DateTime.UtcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommercialOpportunityDto>.Success(ToDto(opportunity));
        }
        catch (InvalidOperationException ex) { return OperationResult<CommercialOpportunityDto>.Conflict(ex.Message); }
        catch (ArgumentException ex) { return OperationResult<CommercialOpportunityDto>.Validation(ex.Message); }
    }

    public async Task<OperationResult<IReadOnlyList<OpportunityStageEventDto>>> GetTimelineAsync(Guid organizationId,
        Guid opportunityId, CancellationToken cancellationToken = default)
    {
        if (await repository.GetOpportunityAsync(organizationId, opportunityId, false, cancellationToken) is null)
            return OperationResult<IReadOnlyList<OpportunityStageEventDto>>.NotFound("Commercial opportunity not found.");
        var events = await repository.ListTimelineAsync(organizationId, opportunityId, cancellationToken);
        return OperationResult<IReadOnlyList<OpportunityStageEventDto>>.Success(events.Select(ToDto).ToList());
    }

    public async Task<CommercialSummaryDto> GetSummaryAsync(Guid organizationId, DateOnly? from, DateOnly? to,
        string? ownerName, string? currency, CancellationToken cancellationToken = default)
    {
        var opportunities = await repository.ListSummaryOpportunitiesAsync(organizationId, from, to,
            Normalize(ownerName), NormalizeCurrency(currency), cancellationToken);
        var open = opportunities.Where(x => !x.IsTerminal).ToList();
        var won = opportunities.Where(x => x.Stage == OpportunityStage.Won).ToList();
        var lost = opportunities.Where(x => x.Stage == OpportunityStage.Lost).ToList();
        var closedCount = won.Count + lost.Count;
        var conversion = closedCount == 0 ? 0m : decimal.Round(won.Count * 100m / closedCount, 2, MidpointRounding.AwayFromZero);
        var currencies = opportunities.Select(x => x.Currency).Distinct().OrderBy(x => x).Select(code =>
        {
            var openCurrency = open.Where(x => x.Currency == code).ToList();
            var wonCurrency = won.Where(x => x.Currency == code).ToList();
            return new CommercialCurrencySummaryDto(code, openCurrency.Count, openCurrency.Sum(x => x.ExpectedValue),
                openCurrency.Sum(x => x.WeightedValue), wonCurrency.Count, wonCurrency.Sum(x => x.ExpectedValue));
        }).ToList();
        var stages = opportunities.GroupBy(x => x.Stage).OrderBy(x => x.Key)
            .Select(group => new CommercialStageSummaryDto(group.Key, group.Count())).ToList();
        return new CommercialSummaryDto(from, to, opportunities.Count, open.Count, won.Count, lost.Count,
            conversion, currencies, stages);
    }

    private async Task<ReferenceResult> ValidateReferencesAsync(Guid organizationId, Guid? farmId, Guid? cropId,
        Guid? seasonId, Guid? exportOrderId, CancellationToken cancellationToken)
    {
        Guid? normalizedFarm = farmId;
        Guid? normalizedCrop = cropId;
        Guid? normalizedSeason = seasonId;

        if (exportOrderId is not null)
        {
            var exportOrder = await exportRepository.GetOrderAsync(organizationId, exportOrderId.Value, false, cancellationToken);
            if (exportOrder is null) return ReferenceResult.Fail("Export order does not belong to this organization.");

            if (exportOrder.FarmId is not null)
            {
                if (normalizedFarm is not null && normalizedFarm.Value != exportOrder.FarmId.Value)
                    return ReferenceResult.Fail("Export order does not belong to the informed farm.");
                normalizedFarm = exportOrder.FarmId;
            }

            if (exportOrder.CropId is not null)
            {
                if (normalizedCrop is not null && normalizedCrop.Value != exportOrder.CropId.Value)
                    return ReferenceResult.Fail("Export order does not belong to the informed crop.");
                normalizedCrop = exportOrder.CropId;
            }

            if (exportOrder.SeasonId is not null)
            {
                if (normalizedSeason is not null && normalizedSeason.Value != exportOrder.SeasonId.Value)
                    return ReferenceResult.Fail("Export order does not belong to the informed season.");
                normalizedSeason = exportOrder.SeasonId;
            }
        }

        if (normalizedSeason is not null)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, normalizedSeason.Value, false, cancellationToken);
            if (season is null) return ReferenceResult.Fail("Season does not belong to this organization.");
            if (normalizedCrop is not null && normalizedCrop.Value != season.CropId)
                return ReferenceResult.Fail("Season does not belong to the informed crop.");
            normalizedCrop = season.CropId;

            var field = await productionRepository.GetFieldAsync(organizationId, season.FieldId, false, cancellationToken);
            if (field is null) return ReferenceResult.Fail("Season field does not belong to this organization.");
            if (normalizedFarm is not null && normalizedFarm.Value != field.FarmId)
                return ReferenceResult.Fail("Season does not belong to the informed farm.");
            normalizedFarm = field.FarmId;
        }

        if (normalizedFarm is not null && await productionRepository.GetFarmAsync(organizationId, normalizedFarm.Value, false, cancellationToken) is null)
            return ReferenceResult.Fail("Farm does not belong to this organization.");
        if (normalizedCrop is not null && await productionRepository.GetCropAsync(organizationId, normalizedCrop.Value, false, cancellationToken) is null)
            return ReferenceResult.Fail("Crop does not belong to this organization.");

        return new ReferenceResult(normalizedFarm, normalizedCrop, normalizedSeason, null);
    }

    private static CommercialCustomerDto ToDto(CommercialCustomer x) => new(x.Id, x.Name, x.TradeName, x.TaxId,
        x.Email, x.Phone, x.CountryCode, x.City, x.State, x.Status, x.Notes, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static CommercialContactDto ToDto(CommercialContact x) => new(x.Id, x.CustomerId, x.Name, x.Role,
        x.Email, x.Phone, x.IsPrimary, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
    private static CommercialOpportunityDto ToDto(CommercialOpportunity x) => new(x.Id, x.CustomerId, x.Title,
        x.FarmId, x.CropId, x.SeasonId, x.ExportOrderId, x.ExpectedValue, x.Currency, x.ProbabilityPercent,
        x.WeightedValue, x.Stage, x.ExpectedCloseDate, x.ClosedOn, x.OwnerName, x.NextStep, x.Notes,
        x.CreatedAtUtc, x.UpdatedAtUtc);
    private static OpportunityStageEventDto ToDto(OpportunityStageEvent x) => new(x.Id, x.OpportunityId,
        x.FromStage, x.ToStage, x.OccurredOn, x.Notes, x.CreatedAtUtc);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    private sealed record ReferenceResult(Guid? FarmId, Guid? CropId, Guid? SeasonId, string? Error)
    {
        public static ReferenceResult Fail(string error) => new(null, null, null, error);
    }
}
