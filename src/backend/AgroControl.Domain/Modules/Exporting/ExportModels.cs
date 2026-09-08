namespace AgroControl.Domain.Modules.Exporting;

public enum ExportOrderStatus
{
    Draft = 1,
    Negotiation = 2,
    Contracted = 3,
    InTransit = 4,
    Delivered = 5,
    Cancelled = 6
}

public enum IncotermCode
{
    EXW = 1,
    FCA = 2,
    CPT = 3,
    CIP = 4,
    DAP = 5,
    DPU = 6,
    DDP = 7,
    FAS = 8,
    FOB = 9,
    CFR = 10,
    CIF = 11
}

public enum ExportDocumentType
{
    CommercialInvoice = 1,
    PackingList = 2,
    CertificateOfOrigin = 3,
    PhytosanitaryCertificate = 4,
    BillOfLading = 5,
    Custom = 6
}

public enum ExportDocumentStatus
{
    Pending = 1,
    Ready = 2,
    Issued = 3,
    NotApplicable = 4
}

public enum ExportCostType
{
    Freight = 1,
    Insurance = 2,
    Port = 3,
    Customs = 4,
    Inspection = 5,
    Other = 6
}

public sealed class ExportOrder
{
    private ExportOrder() { }

    private ExportOrder(
        Guid id,
        Guid organizationId,
        string orderNumber,
        string buyerName,
        string? buyerReference,
        string destinationCountryCode,
        Guid? farmId,
        Guid? fieldId,
        Guid? cropId,
        Guid? seasonId,
        string productDescription,
        decimal quantity,
        string unit,
        string currency,
        decimal unitPrice,
        decimal exchangeRateToBrl,
        IncotermCode incoterm,
        string? originLocation,
        string? destinationLocation,
        DateOnly? estimatedShipmentDate,
        DateOnly? estimatedDeliveryDate,
        string? shipmentReference,
        string? bookingReference,
        string? containerReference,
        string? notes,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Apply(orderNumber, buyerName, buyerReference, destinationCountryCode, farmId, fieldId, cropId, seasonId,
            productDescription, quantity, unit, currency, unitPrice, exchangeRateToBrl, incoterm,
            originLocation, destinationLocation, estimatedShipmentDate, estimatedDeliveryDate,
            shipmentReference, bookingReference, containerReference, notes, createdAtUtc);
        Status = ExportOrderStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string BuyerName { get; private set; } = string.Empty;
    public string? BuyerReference { get; private set; }
    public string DestinationCountryCode { get; private set; } = string.Empty;
    public Guid? FarmId { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? CropId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public string ProductDescription { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal ExchangeRateToBrl { get; private set; }
    public IncotermCode Incoterm { get; private set; }
    public ExportOrderStatus Status { get; private set; }
    public string? OriginLocation { get; private set; }
    public string? DestinationLocation { get; private set; }
    public DateOnly? ContractedOn { get; private set; }
    public DateOnly? EstimatedShipmentDate { get; private set; }
    public DateOnly? ActualShipmentDate { get; private set; }
    public DateOnly? EstimatedDeliveryDate { get; private set; }
    public DateOnly? ActualDeliveryDate { get; private set; }
    public string? ShipmentReference { get; private set; }
    public string? BookingReference { get; private set; }
    public string? ContainerReference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public decimal CommercialValue => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    public decimal EstimatedValueBrl => decimal.Round(CommercialValue * ExchangeRateToBrl, 2, MidpointRounding.AwayFromZero);
    public bool IsTerminal => Status is ExportOrderStatus.Delivered or ExportOrderStatus.Cancelled;

    public static ExportOrder Create(
        Guid organizationId,
        string orderNumber,
        string buyerName,
        string? buyerReference,
        string destinationCountryCode,
        Guid? farmId,
        Guid? fieldId,
        Guid? cropId,
        Guid? seasonId,
        string productDescription,
        decimal quantity,
        string unit,
        string currency,
        decimal unitPrice,
        decimal exchangeRateToBrl,
        IncotermCode incoterm,
        string? originLocation,
        string? destinationLocation,
        DateOnly? estimatedShipmentDate,
        DateOnly? estimatedDeliveryDate,
        string? shipmentReference,
        string? bookingReference,
        string? containerReference,
        string? notes,
        DateTime nowUtc) =>
        new(Guid.NewGuid(), organizationId, orderNumber, buyerName, buyerReference, destinationCountryCode,
            farmId, fieldId, cropId, seasonId, productDescription, quantity, unit, currency, unitPrice,
            exchangeRateToBrl, incoterm, originLocation, destinationLocation, estimatedShipmentDate,
            estimatedDeliveryDate, shipmentReference, bookingReference, containerReference, notes, nowUtc);

    public void Update(
        string orderNumber,
        string buyerName,
        string? buyerReference,
        string destinationCountryCode,
        Guid? farmId,
        Guid? fieldId,
        Guid? cropId,
        Guid? seasonId,
        string productDescription,
        decimal quantity,
        string unit,
        string currency,
        decimal unitPrice,
        decimal exchangeRateToBrl,
        IncotermCode incoterm,
        string? originLocation,
        string? destinationLocation,
        DateOnly? estimatedShipmentDate,
        DateOnly? estimatedDeliveryDate,
        string? shipmentReference,
        string? bookingReference,
        string? containerReference,
        string? notes,
        DateTime nowUtc)
    {
        if (IsTerminal) throw new InvalidOperationException("Delivered or cancelled export orders cannot be edited.");
        Apply(orderNumber, buyerName, buyerReference, destinationCountryCode, farmId, fieldId, cropId, seasonId,
            productDescription, quantity, unit, currency, unitPrice, exchangeRateToBrl, incoterm,
            originLocation, destinationLocation, estimatedShipmentDate, estimatedDeliveryDate,
            shipmentReference, bookingReference, containerReference, notes, nowUtc);
    }

    public void TransitionTo(ExportOrderStatus nextStatus, DateOnly occurredOn, DateTime nowUtc)
    {
        if (occurredOn == default) throw new ArgumentException("Transition date is required.", nameof(occurredOn));
        if (Status == nextStatus) return;
        if (!CanTransition(Status, nextStatus))
            throw new InvalidOperationException($"Invalid export order transition from {Status} to {nextStatus}.");

        Status = nextStatus;
        if (nextStatus == ExportOrderStatus.Contracted) ContractedOn ??= occurredOn;
        if (nextStatus == ExportOrderStatus.InTransit) ActualShipmentDate ??= occurredOn;
        if (nextStatus == ExportOrderStatus.Delivered) ActualDeliveryDate ??= occurredOn;
        UpdatedAtUtc = nowUtc;
    }

    public static bool CanTransition(ExportOrderStatus current, ExportOrderStatus next) => current switch
    {
        ExportOrderStatus.Draft => next is ExportOrderStatus.Negotiation or ExportOrderStatus.Contracted or ExportOrderStatus.Cancelled,
        ExportOrderStatus.Negotiation => next is ExportOrderStatus.Draft or ExportOrderStatus.Contracted or ExportOrderStatus.Cancelled,
        ExportOrderStatus.Contracted => next is ExportOrderStatus.InTransit or ExportOrderStatus.Cancelled,
        ExportOrderStatus.InTransit => next is ExportOrderStatus.Delivered or ExportOrderStatus.Cancelled,
        _ => false
    };

    private void Apply(
        string orderNumber,
        string buyerName,
        string? buyerReference,
        string destinationCountryCode,
        Guid? farmId,
        Guid? fieldId,
        Guid? cropId,
        Guid? seasonId,
        string productDescription,
        decimal quantity,
        string unit,
        string currency,
        decimal unitPrice,
        decimal exchangeRateToBrl,
        IncotermCode incoterm,
        string? originLocation,
        string? destinationLocation,
        DateOnly? estimatedShipmentDate,
        DateOnly? estimatedDeliveryDate,
        string? shipmentReference,
        string? bookingReference,
        string? containerReference,
        string? notes,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Export order number is required.", nameof(orderNumber));
        if (string.IsNullOrWhiteSpace(buyerName)) throw new ArgumentException("Buyer name is required.", nameof(buyerName));
        if (!IsAlphabeticCode(destinationCountryCode, 2)) throw new ArgumentException("Destination country must use ISO alpha-2 format.", nameof(destinationCountryCode));
        if (string.IsNullOrWhiteSpace(productDescription)) throw new ArgumentException("Product description is required.", nameof(productDescription));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (!IsAlphabeticCode(currency, 3)) throw new ArgumentException("Currency must use ISO 4217 three-letter format.", nameof(currency));
        if (unitPrice <= 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be greater than zero.");
        if (exchangeRateToBrl <= 0m) throw new ArgumentOutOfRangeException(nameof(exchangeRateToBrl), "Exchange rate to BRL must be greater than zero.");
        if (!Enum.IsDefined(incoterm)) throw new ArgumentOutOfRangeException(nameof(incoterm));
        if (estimatedShipmentDate is not null && estimatedDeliveryDate is not null && estimatedDeliveryDate < estimatedShipmentDate)
            throw new ArgumentException("Estimated delivery date cannot be before estimated shipment date.", nameof(estimatedDeliveryDate));

        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        BuyerName = buyerName.Trim();
        BuyerReference = Normalize(buyerReference);
        DestinationCountryCode = destinationCountryCode.Trim().ToUpperInvariant();
        FarmId = farmId;
        FieldId = fieldId;
        CropId = cropId;
        SeasonId = seasonId;
        ProductDescription = productDescription.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        UnitPrice = unitPrice;
        ExchangeRateToBrl = exchangeRateToBrl;
        Incoterm = incoterm;
        OriginLocation = Normalize(originLocation);
        DestinationLocation = Normalize(destinationLocation);
        EstimatedShipmentDate = estimatedShipmentDate;
        EstimatedDeliveryDate = estimatedDeliveryDate;
        ShipmentReference = Normalize(shipmentReference);
        BookingReference = Normalize(bookingReference);
        ContainerReference = Normalize(containerReference);
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
    }

    private static bool IsAlphabeticCode(string? value, int length) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length == length && value.Trim().All(char.IsLetter);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ExportDocument
{
    private ExportDocument() { }

    private ExportDocument(Guid id, Guid organizationId, Guid orderId, ExportDocumentType type, string? customLabel, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        OrderId = orderId;
        Type = type;
        CustomLabel = Normalize(customLabel);
        if (type == ExportDocumentType.Custom && CustomLabel is null)
            throw new ArgumentException("Custom documents require a label.", nameof(customLabel));
        Status = ExportDocumentStatus.Pending;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid OrderId { get; private set; }
    public ExportDocumentType Type { get; private set; }
    public string? CustomLabel { get; private set; }
    public ExportDocumentStatus Status { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public DateOnly? IssuedOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static ExportDocument Create(Guid organizationId, Guid orderId, ExportDocumentType type, string? customLabel, DateTime nowUtc)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Export order id is required.", nameof(orderId));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        return new ExportDocument(Guid.NewGuid(), organizationId, orderId, type, customLabel, nowUtc);
    }

    public void Update(ExportDocumentStatus status, string? referenceNumber, DateOnly? issuedOn, string? notes, DateTime nowUtc)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (status == ExportDocumentStatus.Issued && issuedOn is null)
            throw new ArgumentException("Issued documents require an issue date.", nameof(issuedOn));
        Status = status;
        ReferenceNumber = Normalize(referenceNumber);
        IssuedOn = issuedOn;
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ExportCost
{
    private ExportCost() { }

    private ExportCost(Guid id, Guid organizationId, Guid orderId, ExportCostType type, string description,
        decimal amount, string currency, decimal exchangeRateToBrl, DateOnly incurredOn, string? notes, DateTime createdAtUtc)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Export order id is required.", nameof(orderId));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Cost description is required.", nameof(description));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Cost amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || !currency.Trim().All(char.IsLetter))
            throw new ArgumentException("Currency must use ISO 4217 three-letter format.", nameof(currency));
        if (exchangeRateToBrl <= 0m) throw new ArgumentOutOfRangeException(nameof(exchangeRateToBrl), "Exchange rate to BRL must be greater than zero.");
        if (incurredOn == default) throw new ArgumentException("Cost date is required.", nameof(incurredOn));

        Id = id;
        OrganizationId = organizationId;
        OrderId = orderId;
        Type = type;
        Description = description.Trim();
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        ExchangeRateToBrl = exchangeRateToBrl;
        AmountBrl = decimal.Round(amount * exchangeRateToBrl, 2, MidpointRounding.AwayFromZero);
        IncurredOn = incurredOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid OrderId { get; private set; }
    public ExportCostType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal ExchangeRateToBrl { get; private set; }
    public decimal AmountBrl { get; private set; }
    public DateOnly IncurredOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ExportCost Create(Guid organizationId, Guid orderId, ExportCostType type, string description,
        decimal amount, string currency, decimal exchangeRateToBrl, DateOnly incurredOn, string? notes, DateTime nowUtc) =>
        new(Guid.NewGuid(), organizationId, orderId, type, description, amount, currency, exchangeRateToBrl, incurredOn, notes, nowUtc);
}

public sealed class ExportOrderStatusEvent
{
    private ExportOrderStatusEvent() { }

    private ExportOrderStatusEvent(Guid id, Guid organizationId, Guid orderId, ExportOrderStatus? fromStatus,
        ExportOrderStatus toStatus, DateOnly occurredOn, string? notes, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        OccurredOn = occurredOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid OrderId { get; private set; }
    public ExportOrderStatus? FromStatus { get; private set; }
    public ExportOrderStatus ToStatus { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ExportOrderStatusEvent Create(Guid organizationId, Guid orderId, ExportOrderStatus? fromStatus,
        ExportOrderStatus toStatus, DateOnly occurredOn, string? notes, DateTime nowUtc)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Export order id is required.", nameof(orderId));
        if (occurredOn == default) throw new ArgumentException("Transition date is required.", nameof(occurredOn));
        return new ExportOrderStatusEvent(Guid.NewGuid(), organizationId, orderId, fromStatus, toStatus, occurredOn, notes, nowUtc);
    }
}
