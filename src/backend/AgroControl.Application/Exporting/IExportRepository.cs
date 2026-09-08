using AgroControl.Domain.Modules.Exporting;

namespace AgroControl.Application.Exporting;

public interface IExportRepository
{
    Task<(IReadOnlyList<ExportOrder> Items, int TotalCount)> ListOrdersAsync(
        Guid organizationId, int skip, int take, string? search, ExportOrderStatus? status,
        string? countryCode, Guid? seasonId, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken = default);
    Task<ExportOrder?> GetOrderAsync(Guid organizationId, Guid orderId, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> OrderNumberExistsAsync(Guid organizationId, string orderNumber, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddOrder(ExportOrder order);

    Task<IReadOnlyList<ExportDocument>> ListDocumentsAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default);
    Task<ExportDocument?> GetDocumentAsync(Guid organizationId, Guid orderId, Guid documentId, bool tracking, CancellationToken cancellationToken = default);
    void AddDocument(ExportDocument document);

    Task<IReadOnlyList<ExportCost>> ListCostsAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default);
    void AddCost(ExportCost cost);

    Task<IReadOnlyList<ExportOrderStatusEvent>> ListTimelineAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default);
    void AddStatusEvent(ExportOrderStatusEvent statusEvent);

    Task<IReadOnlyList<ExportOrder>> ListSummaryOrdersAsync(Guid organizationId, DateOnly? from, DateOnly? to,
        ExportOrderStatus? status, string? countryCode, string? currency, CancellationToken cancellationToken = default);
    Task<decimal> SumCostsBrlAsync(Guid organizationId, IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
}
