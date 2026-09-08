using AgroControl.Domain.Modules.Commercial;

namespace AgroControl.Application.Commercial;

public interface ICommercialRepository
{
    Task<(IReadOnlyList<CommercialCustomer> Items, int TotalCount)> ListCustomersAsync(Guid organizationId, int skip, int take,
        string? search, CustomerStatus? status, CancellationToken cancellationToken = default);
    Task<CommercialCustomer?> GetCustomerAsync(Guid organizationId, Guid customerId, bool tracking, CancellationToken cancellationToken = default);
    void AddCustomer(CommercialCustomer customer);

    Task<IReadOnlyList<CommercialContact>> ListContactsAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken = default);
    Task<CommercialContact?> GetContactAsync(Guid organizationId, Guid contactId, bool tracking, CancellationToken cancellationToken = default);
    void AddContact(CommercialContact contact);

    Task<(IReadOnlyList<CommercialOpportunity> Items, int TotalCount)> ListOpportunitiesAsync(Guid organizationId, int skip, int take,
        string? search, Guid? customerId, OpportunityStage? stage, string? ownerName, string? currency,
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<CommercialOpportunity?> GetOpportunityAsync(Guid organizationId, Guid opportunityId, bool tracking, CancellationToken cancellationToken = default);
    void AddOpportunity(CommercialOpportunity opportunity);

    Task<IReadOnlyList<OpportunityStageEvent>> ListTimelineAsync(Guid organizationId, Guid opportunityId, CancellationToken cancellationToken = default);
    void AddStageEvent(OpportunityStageEvent stageEvent);

    Task<IReadOnlyList<CommercialOpportunity>> ListSummaryOpportunitiesAsync(Guid organizationId, DateOnly? from, DateOnly? to,
        string? ownerName, string? currency, CancellationToken cancellationToken = default);
}
