using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Exporting;

namespace AgroControl.Application.Exporting;

public sealed class ExportService(IExportRepository repository, IProductionRepository productionRepository, IUnitOfWork unitOfWork)
{
    private static readonly ExportDocumentType[] DefaultDocuments =
    [
        ExportDocumentType.CommercialInvoice,
        ExportDocumentType.PackingList,
        ExportDocumentType.CertificateOfOrigin,
        ExportDocumentType.PhytosanitaryCertificate,
        ExportDocumentType.BillOfLading
    ];

    public async Task<PagedResult<ExportOrderDto>> ListOrdersAsync(Guid organizationId, int page, int pageSize,
        string? search, ExportOrderStatus? status, string? countryCode, Guid? seasonId, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListOrdersAsync(organizationId, (page - 1) * pageSize, pageSize,
            search, status, NormalizeCountryFilter(countryCode), seasonId, from, to, cancellationToken);
        return new PagedResult<ExportOrderDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<ExportOrderDto?> GetOrderAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken);
        return order is null ? null : ToDto(order);
    }

    public async Task<OperationResult<ExportOrderDto>> CreateOrderAsync(Guid organizationId, CreateExportOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.OrderNumber) &&
            await repository.OrderNumberExistsAsync(organizationId, command.OrderNumber.Trim().ToUpperInvariant(), null, cancellationToken))
            return OperationResult<ExportOrderDto>.Conflict("An export order with this number already exists.");

        var refs = await ValidateAndNormalizeReferencesAsync(organizationId, command.FarmId, command.FieldId,
            command.CropId, command.SeasonId, cancellationToken);
        if (refs.Error is not null) return OperationResult<ExportOrderDto>.Validation(refs.Error);

        try
        {
            var now = DateTime.UtcNow;
            var order = ExportOrder.Create(organizationId, command.OrderNumber, command.BuyerName, command.BuyerReference,
                command.DestinationCountryCode, refs.FarmId, refs.FieldId, refs.CropId, refs.SeasonId,
                command.ProductDescription, command.Quantity, command.Unit, command.Currency, command.UnitPrice,
                command.ExchangeRateToBrl, command.Incoterm, command.OriginLocation, command.DestinationLocation,
                command.EstimatedShipmentDate, command.EstimatedDeliveryDate, command.ShipmentReference,
                command.BookingReference, command.ContainerReference, command.Notes, now);
            repository.AddOrder(order);
            foreach (var documentType in DefaultDocuments)
                repository.AddDocument(ExportDocument.Create(organizationId, order.Id, documentType, null, now));
            repository.AddStatusEvent(ExportOrderStatusEvent.Create(organizationId, order.Id, null,
                ExportOrderStatus.Draft, DateOnly.FromDateTime(now), "Pedido criado", now));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportOrderDto>.Success(ToDto(order));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportOrderDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<ExportOrderDto>> UpdateOrderAsync(Guid organizationId, Guid orderId,
        UpdateExportOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(organizationId, orderId, true, cancellationToken);
        if (order is null) return OperationResult<ExportOrderDto>.NotFound("Export order not found.");
        if (!string.IsNullOrWhiteSpace(command.OrderNumber) &&
            await repository.OrderNumberExistsAsync(organizationId, command.OrderNumber.Trim().ToUpperInvariant(), orderId, cancellationToken))
            return OperationResult<ExportOrderDto>.Conflict("An export order with this number already exists.");

        var refs = await ValidateAndNormalizeReferencesAsync(organizationId, command.FarmId, command.FieldId,
            command.CropId, command.SeasonId, cancellationToken);
        if (refs.Error is not null) return OperationResult<ExportOrderDto>.Validation(refs.Error);

        try
        {
            order.Update(command.OrderNumber, command.BuyerName, command.BuyerReference, command.DestinationCountryCode,
                refs.FarmId, refs.FieldId, refs.CropId, refs.SeasonId, command.ProductDescription, command.Quantity,
                command.Unit, command.Currency, command.UnitPrice, command.ExchangeRateToBrl, command.Incoterm,
                command.OriginLocation, command.DestinationLocation, command.EstimatedShipmentDate,
                command.EstimatedDeliveryDate, command.ShipmentReference, command.BookingReference,
                command.ContainerReference, command.Notes, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportOrderDto>.Success(ToDto(order));
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<ExportOrderDto>.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportOrderDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<ExportOrderDto>> TransitionStatusAsync(Guid organizationId, Guid orderId,
        TransitionExportOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(organizationId, orderId, true, cancellationToken);
        if (order is null) return OperationResult<ExportOrderDto>.NotFound("Export order not found.");
        var fromStatus = order.Status;
        try
        {
            order.TransitionTo(command.Status, command.OccurredOn, DateTime.UtcNow);
            if (order.Status != fromStatus)
                repository.AddStatusEvent(ExportOrderStatusEvent.Create(organizationId, order.Id, fromStatus,
                    order.Status, command.OccurredOn, command.Notes, DateTime.UtcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportOrderDto>.Success(ToDto(order));
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<ExportOrderDto>.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportOrderDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<IReadOnlyList<ExportDocumentDto>>> ListDocumentsAsync(Guid organizationId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken) is null)
            return OperationResult<IReadOnlyList<ExportDocumentDto>>.NotFound("Export order not found.");
        var documents = await repository.ListDocumentsAsync(organizationId, orderId, cancellationToken);
        return OperationResult<IReadOnlyList<ExportDocumentDto>>.Success(documents.Select(ToDto).ToList());
    }

    public async Task<OperationResult<ExportDocumentDto>> CreateDocumentAsync(Guid organizationId, Guid orderId,
        CreateExportDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken) is null)
            return OperationResult<ExportDocumentDto>.NotFound("Export order not found.");
        try
        {
            var document = ExportDocument.Create(organizationId, orderId, command.Type, command.CustomLabel, DateTime.UtcNow);
            repository.AddDocument(document);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportDocumentDto>.Success(ToDto(document));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportDocumentDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<ExportDocumentDto>> UpdateDocumentAsync(Guid organizationId, Guid orderId, Guid documentId,
        UpdateExportDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var document = await repository.GetDocumentAsync(organizationId, orderId, documentId, true, cancellationToken);
        if (document is null) return OperationResult<ExportDocumentDto>.NotFound("Export document not found.");
        try
        {
            document.Update(command.Status, command.ReferenceNumber, command.IssuedOn, command.Notes, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportDocumentDto>.Success(ToDto(document));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportDocumentDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<IReadOnlyList<ExportCostDto>>> ListCostsAsync(Guid organizationId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken) is null)
            return OperationResult<IReadOnlyList<ExportCostDto>>.NotFound("Export order not found.");
        var costs = await repository.ListCostsAsync(organizationId, orderId, cancellationToken);
        return OperationResult<IReadOnlyList<ExportCostDto>>.Success(costs.Select(ToDto).ToList());
    }

    public async Task<OperationResult<ExportCostDto>> AddCostAsync(Guid organizationId, Guid orderId,
        CreateExportCostCommand command, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken);
        if (order is null) return OperationResult<ExportCostDto>.NotFound("Export order not found.");
        if (order.Status == ExportOrderStatus.Cancelled)
            return OperationResult<ExportCostDto>.Conflict("Costs cannot be added to a cancelled export order.");
        try
        {
            var cost = ExportCost.Create(organizationId, orderId, command.Type, command.Description, command.Amount,
                command.Currency, command.ExchangeRateToBrl, command.IncurredOn, command.Notes, DateTime.UtcNow);
            repository.AddCost(cost);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<ExportCostDto>.Success(ToDto(cost));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ExportCostDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<IReadOnlyList<ExportStatusEventDto>>> GetTimelineAsync(Guid organizationId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetOrderAsync(organizationId, orderId, false, cancellationToken) is null)
            return OperationResult<IReadOnlyList<ExportStatusEventDto>>.NotFound("Export order not found.");
        var events = await repository.ListTimelineAsync(organizationId, orderId, cancellationToken);
        return OperationResult<IReadOnlyList<ExportStatusEventDto>>.Success(events.Select(ToDto).ToList());
    }

    public async Task<ExportSummaryDto> GetSummaryAsync(Guid organizationId, DateOnly? from, DateOnly? to,
        ExportOrderStatus? status, string? countryCode, string? currency, CancellationToken cancellationToken = default)
    {
        var orders = await repository.ListSummaryOrdersAsync(organizationId, from, to, status,
            NormalizeCountryFilter(countryCode), NormalizeCurrencyFilter(currency), cancellationToken);
        var relevant = orders.Where(order => order.Status != ExportOrderStatus.Cancelled).ToList();
        var ids = relevant.Select(order => order.Id).ToArray();
        var costsBrl = ids.Length == 0 ? 0m : await repository.SumCostsBrlAsync(organizationId, ids, cancellationToken);
        var commercialBrl = relevant.Sum(order => order.EstimatedValueBrl);

        IReadOnlyList<ExportSummaryBreakdownDto> Breakdown(IEnumerable<IGrouping<string, ExportOrder>> groups) =>
            groups.Select(group => new ExportSummaryBreakdownDto(group.Key, group.Count(), group.Sum(item => item.EstimatedValueBrl)))
                .OrderByDescending(item => item.EstimatedValueBrl).ToList();

        return new ExportSummaryDto(from, to, orders.Count,
            orders.Count(order => order.Status == ExportOrderStatus.Contracted),
            orders.Count(order => order.Status == ExportOrderStatus.InTransit),
            orders.Count(order => order.Status == ExportOrderStatus.Delivered),
            commercialBrl, costsBrl, commercialBrl - costsBrl,
            Breakdown(relevant.GroupBy(order => order.Status.ToString())),
            Breakdown(relevant.GroupBy(order => order.DestinationCountryCode)),
            Breakdown(relevant.GroupBy(order => order.Currency)));
    }

    private async Task<NormalizedReferences> ValidateAndNormalizeReferencesAsync(Guid organizationId,
        Guid? farmId, Guid? fieldId, Guid? cropId, Guid? seasonId, CancellationToken cancellationToken)
    {
        Guid? normalizedFarm = farmId;
        Guid? normalizedField = fieldId;
        Guid? normalizedCrop = cropId;

        if (seasonId is not null)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, seasonId.Value, false, cancellationToken);
            if (season is null) return NormalizedReferences.Fail("Season does not belong to this organization.");
            if (fieldId is not null && fieldId.Value != season.FieldId) return NormalizedReferences.Fail("Season does not belong to the informed field.");
            if (cropId is not null && cropId.Value != season.CropId) return NormalizedReferences.Fail("Season does not belong to the informed crop.");
            normalizedField = season.FieldId;
            normalizedCrop = season.CropId;
        }

        if (normalizedField is not null)
        {
            var field = await productionRepository.GetFieldAsync(organizationId, normalizedField.Value, false, cancellationToken);
            if (field is null) return NormalizedReferences.Fail("Field does not belong to this organization.");
            if (farmId is not null && farmId.Value != field.FarmId) return NormalizedReferences.Fail("Field does not belong to the informed farm.");
            normalizedFarm = field.FarmId;
        }

        if (normalizedFarm is not null && await productionRepository.GetFarmAsync(organizationId, normalizedFarm.Value, false, cancellationToken) is null)
            return NormalizedReferences.Fail("Farm does not belong to this organization.");

        if (normalizedCrop is not null && await productionRepository.GetCropAsync(organizationId, normalizedCrop.Value, false, cancellationToken) is null)
            return NormalizedReferences.Fail("Crop does not belong to this organization.");

        return new NormalizedReferences(normalizedFarm, normalizedField, normalizedCrop, seasonId, null);
    }

    private static ExportOrderDto ToDto(ExportOrder order) => new(order.Id, order.OrderNumber, order.BuyerName,
        order.BuyerReference, order.DestinationCountryCode, order.FarmId, order.FieldId, order.CropId, order.SeasonId,
        order.ProductDescription, order.Quantity, order.Unit, order.Currency, order.UnitPrice, order.CommercialValue,
        order.ExchangeRateToBrl, order.EstimatedValueBrl, order.Incoterm, order.Status, order.OriginLocation,
        order.DestinationLocation, order.ContractedOn, order.EstimatedShipmentDate, order.ActualShipmentDate,
        order.EstimatedDeliveryDate, order.ActualDeliveryDate, order.ShipmentReference, order.BookingReference,
        order.ContainerReference, order.Notes, order.CreatedAtUtc, order.UpdatedAtUtc);

    private static ExportDocumentDto ToDto(ExportDocument document) => new(document.Id, document.OrderId,
        document.Type, document.CustomLabel, document.Status, document.ReferenceNumber, document.IssuedOn,
        document.Notes, document.CreatedAtUtc, document.UpdatedAtUtc);

    private static ExportCostDto ToDto(ExportCost cost) => new(cost.Id, cost.OrderId, cost.Type, cost.Description,
        cost.Amount, cost.Currency, cost.ExchangeRateToBrl, cost.AmountBrl, cost.IncurredOn, cost.Notes, cost.CreatedAtUtc);

    private static ExportStatusEventDto ToDto(ExportOrderStatusEvent item) => new(item.Id, item.OrderId,
        item.FromStatus, item.ToStatus, item.OccurredOn, item.Notes, item.CreatedAtUtc);

    private static string? NormalizeCountryFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? NormalizeCurrencyFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private sealed record NormalizedReferences(Guid? FarmId, Guid? FieldId, Guid? CropId, Guid? SeasonId, string? Error)
    {
        public static NormalizedReferences Fail(string error) => new(null, null, null, null, error);
    }
}
