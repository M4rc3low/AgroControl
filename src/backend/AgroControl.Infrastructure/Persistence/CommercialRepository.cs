using AgroControl.Application.Commercial;
using AgroControl.Domain.Modules.Commercial;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class CommercialRepository(AgroControlDbContext dbContext) : ICommercialRepository
{
    public async Task<(IReadOnlyList<CommercialCustomer> Items, int TotalCount)> ListCustomersAsync(Guid organizationId,
        int skip, int take, string? search, CustomerStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CommercialCustomer>().AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) ||
                (x.TradeName != null && EF.Functions.ILike(x.TradeName, pattern)) ||
                (x.TaxId != null && EF.Functions.ILike(x.TaxId, pattern)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<CommercialCustomer?> GetCustomerAsync(Guid organizationId, Guid customerId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<CommercialCustomer> query = tracking ? dbContext.Set<CommercialCustomer>() : dbContext.Set<CommercialCustomer>().AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == customerId, cancellationToken);
    }

    public void AddCustomer(CommercialCustomer customer) => dbContext.Set<CommercialCustomer>().Add(customer);

    public async Task<IReadOnlyList<CommercialContact>> ListContactsAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<CommercialContact>().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CustomerId == customerId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<CommercialContact?> GetContactAsync(Guid organizationId, Guid contactId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<CommercialContact> query = tracking ? dbContext.Set<CommercialContact>() : dbContext.Set<CommercialContact>().AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == contactId, cancellationToken);
    }

    public void AddContact(CommercialContact contact) => dbContext.Set<CommercialContact>().Add(contact);

    public async Task<(IReadOnlyList<CommercialOpportunity> Items, int TotalCount)> ListOpportunitiesAsync(Guid organizationId,
        int skip, int take, string? search, Guid? customerId, OpportunityStage? stage, string? ownerName, string? currency,
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = FilterOpportunities(organizationId, from, to, ownerName, currency);
        if (customerId is not null) query = query.Where(x => x.CustomerId == customerId.Value);
        if (stage is not null) query = query.Where(x => x.Stage == stage.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern) ||
                (x.NextStep != null && EF.Functions.ILike(x.NextStep, pattern)) ||
                (x.OwnerName != null && EF.Functions.ILike(x.OwnerName, pattern)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<CommercialOpportunity?> GetOpportunityAsync(Guid organizationId, Guid opportunityId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<CommercialOpportunity> query = tracking ? dbContext.Set<CommercialOpportunity>() : dbContext.Set<CommercialOpportunity>().AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == opportunityId, cancellationToken);
    }

    public void AddOpportunity(CommercialOpportunity opportunity) => dbContext.Set<CommercialOpportunity>().Add(opportunity);

    public async Task<IReadOnlyList<OpportunityStageEvent>> ListTimelineAsync(Guid organizationId, Guid opportunityId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<OpportunityStageEvent>().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OpportunityId == opportunityId)
            .OrderBy(x => x.OccurredOn).ThenBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public void AddStageEvent(OpportunityStageEvent stageEvent) => dbContext.Set<OpportunityStageEvent>().Add(stageEvent);

    public async Task<IReadOnlyList<CommercialOpportunity>> ListSummaryOpportunitiesAsync(Guid organizationId, DateOnly? from,
        DateOnly? to, string? ownerName, string? currency, CancellationToken cancellationToken = default) =>
        await FilterOpportunities(organizationId, from, to, ownerName, currency)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    private IQueryable<CommercialOpportunity> FilterOpportunities(Guid organizationId, DateOnly? from, DateOnly? to,
        string? ownerName, string? currency)
    {
        var query = dbContext.Set<CommercialOpportunity>().AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (from is not null) query = query.Where(x => DateOnly.FromDateTime(x.CreatedAtUtc) >= from.Value);
        if (to is not null) query = query.Where(x => DateOnly.FromDateTime(x.CreatedAtUtc) <= to.Value);
        if (!string.IsNullOrWhiteSpace(ownerName)) query = query.Where(x => x.OwnerName == ownerName);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency);
        return query;
    }
}
