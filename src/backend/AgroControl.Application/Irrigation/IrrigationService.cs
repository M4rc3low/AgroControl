using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.Telemetry;
using AgroControl.Domain.Modules.Irrigation;

namespace AgroControl.Application.Irrigation;

public sealed class IrrigationService(
    IIrrigationRepository repository,
    IProductionRepository productionRepository,
    ITelemetryClient telemetryClient,
    IUnitOfWork unitOfWork)
{
    private const string SoilMoistureMetric = "soil_moisture_percent";

    public async Task<PagedResult<IrrigationZoneDto>> ListZonesAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Guid? fieldId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListZonesAsync(
            organizationId, (page - 1) * pageSize, pageSize, fieldId, includeInactive, cancellationToken);
        return new PagedResult<IrrigationZoneDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<IrrigationZoneDto?> GetZoneAsync(Guid organizationId, Guid zoneId, CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneAsync(organizationId, zoneId, false, cancellationToken);
        return zone is null ? null : ToDto(zone);
    }

    public async Task<OperationResult<IrrigationZoneDto>> CreateZoneAsync(
        Guid organizationId,
        CreateIrrigationZoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateFieldAndDeviceAsync(
            organizationId, command.FieldId, command.AreaHectares, command.TelemetryDeviceId, cancellationToken);
        if (validation is not null) return OperationResult<IrrigationZoneDto>.Validation(validation);

        try
        {
            var now = DateTime.UtcNow;
            var zone = IrrigationZone.Create(
                organizationId, command.FieldId, command.Name, command.AreaHectares, command.Method,
                command.MinimumMoisturePercent, command.TargetMoisturePercent, command.MaximumMoisturePercent,
                command.TelemetryDeviceId, now);
            repository.AddZone(zone);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<IrrigationZoneDto>.Success(ToDto(zone));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<IrrigationZoneDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<IrrigationZoneDto>> UpdateZoneAsync(
        Guid organizationId,
        Guid zoneId,
        UpdateIrrigationZoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneAsync(organizationId, zoneId, true, cancellationToken);
        if (zone is null) return OperationResult<IrrigationZoneDto>.NotFound("Irrigation zone not found.");

        var validation = await ValidateFieldAndDeviceAsync(
            organizationId, command.FieldId, command.AreaHectares, command.TelemetryDeviceId, cancellationToken);
        if (validation is not null) return OperationResult<IrrigationZoneDto>.Validation(validation);

        try
        {
            zone.Update(
                command.FieldId, command.Name, command.AreaHectares, command.Method,
                command.MinimumMoisturePercent, command.TargetMoisturePercent, command.MaximumMoisturePercent,
                command.TelemetryDeviceId, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<IrrigationZoneDto>.Success(ToDto(zone));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<IrrigationZoneDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateZoneAsync(
        Guid organizationId,
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneAsync(organizationId, zoneId, true, cancellationToken);
        if (zone is null) return OperationResult<bool>.NotFound("Irrigation zone not found.");
        zone.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<IrrigationZoneStatusDto>> GetZoneStatusAsync(
        Guid organizationId,
        Guid zoneId,
        CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneAsync(organizationId, zoneId, false, cancellationToken);
        if (zone is null) return OperationResult<IrrigationZoneStatusDto>.NotFound("Irrigation zone not found.");

        if (zone.TelemetryDeviceId is null)
        {
            return OperationResult<IrrigationZoneStatusDto>.Success(new IrrigationZoneStatusDto(
                zone.Id, zone.FieldId, null, true, false, null, null, null,
                IrrigationRecommendation.Monitor,
                "No telemetry device is linked to this irrigation zone."));
        }

        var device = await telemetryClient.GetDeviceAsync(organizationId, zone.TelemetryDeviceId.Value, cancellationToken);
        if (device.OrganizationId != organizationId || (device.FieldId is not null && device.FieldId != zone.FieldId))
            return OperationResult<IrrigationZoneStatusDto>.Conflict("Telemetry device is not authorized for this irrigation zone.");

        TelemetryEventDto latest;
        try
        {
            latest = await telemetryClient.GetLatestAsync(
                organizationId, zone.TelemetryDeviceId.Value, SoilMoistureMetric, cancellationToken);
        }
        catch (TelemetryClientException ex) when (ex.Kind == TelemetryClientErrorKind.NotFound)
        {
            return OperationResult<IrrigationZoneStatusDto>.Success(new IrrigationZoneStatusDto(
                zone.Id, zone.FieldId, zone.TelemetryDeviceId, true, false, null, null, null,
                IrrigationRecommendation.Monitor,
                "No soil moisture reading is available for this zone yet."));
        }

        if (latest.NumericValue is null || double.IsNaN(latest.NumericValue.Value) || double.IsInfinity(latest.NumericValue.Value) || latest.NumericValue < 0d || latest.NumericValue > 100d)
        {
            return OperationResult<IrrigationZoneStatusDto>.Success(new IrrigationZoneStatusDto(
                zone.Id, zone.FieldId, zone.TelemetryDeviceId, true, false, latest.NumericValue, latest.CapturedAtUtc, null,
                IrrigationRecommendation.Monitor,
                "The latest soil moisture reading is not a valid numeric percentage."));
        }

        var condition = IrrigationDecisionPolicy.Classify(
            latest.NumericValue.Value,
            zone.MinimumMoisturePercent,
            zone.TargetMoisturePercent,
            zone.MaximumMoisturePercent);
        var recommendation = IrrigationDecisionPolicy.Recommend(condition);
        return OperationResult<IrrigationZoneStatusDto>.Success(new IrrigationZoneStatusDto(
            zone.Id, zone.FieldId, zone.TelemetryDeviceId, true, true, latest.NumericValue, latest.CapturedAtUtc,
            condition, recommendation, BuildDecisionMessage(condition)));
    }

    public async Task<PagedResult<IrrigationApplicationDto>> ListApplicationsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListApplicationsAsync(
            organizationId, (page - 1) * pageSize, pageSize, fieldId, zoneId, fromUtc, toUtc, cancellationToken);
        return new PagedResult<IrrigationApplicationDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<IrrigationApplicationDto>> CreateApplicationAsync(
        Guid organizationId,
        CreateIrrigationApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        var zone = await repository.GetZoneAsync(organizationId, command.ZoneId, false, cancellationToken);
        if (zone is null) return OperationResult<IrrigationApplicationDto>.NotFound("Irrigation zone not found.");
        if (!zone.IsActive) return OperationResult<IrrigationApplicationDto>.Conflict("Inactive irrigation zones cannot receive applications.");

        try
        {
            var application = IrrigationApplication.Create(
                organizationId, zone.Id, zone.FieldId, zone.AreaHectares, command.DepthMillimeters,
                command.Source, EnsureUtc(command.StartedAtUtc), command.EndedAtUtc is null ? null : EnsureUtc(command.EndedAtUtc.Value),
                command.Notes, DateTime.UtcNow);
            repository.AddApplication(application);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<IrrigationApplicationDto>.Success(ToDto(application));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<IrrigationApplicationDto>.Validation(ex.Message);
        }
    }

    public async Task<IrrigationSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc is not null) fromUtc = EnsureUtc(fromUtc.Value);
        if (toUtc is not null) toUtc = EnsureUtc(toUtc.Value);
        var projection = await repository.GetSummaryAsync(organizationId, fieldId, zoneId, fromUtc, toUtc, cancellationToken);
        return new IrrigationSummaryDto(
            fromUtc, toUtc, fieldId, zoneId, projection.ApplicationCount,
            projection.TotalDepthMillimeters, projection.EstimatedVolumeCubicMeters);
    }

    private async Task<string?> ValidateFieldAndDeviceAsync(
        Guid organizationId,
        Guid fieldId,
        decimal zoneAreaHectares,
        Guid? telemetryDeviceId,
        CancellationToken cancellationToken)
    {
        var field = await productionRepository.GetFieldAsync(organizationId, fieldId, false, cancellationToken);
        if (field is null || !field.IsActive) return "Field does not belong to this organization or is inactive.";
        if (zoneAreaHectares > field.AreaHectares) return "Irrigation zone area cannot exceed the field area.";

        if (telemetryDeviceId is null) return null;
        try
        {
            var device = await telemetryClient.GetDeviceAsync(organizationId, telemetryDeviceId.Value, cancellationToken);
            if (device.OrganizationId != organizationId) return "Telemetry device does not belong to this organization.";
            if (device.FieldId is not null && device.FieldId.Value != fieldId)
                return "Telemetry device is linked to a different field.";
            return null;
        }
        catch (TelemetryClientException ex) when (ex.Kind == TelemetryClientErrorKind.NotFound)
        {
            return "Telemetry device was not found for this organization.";
        }
    }

    private static string BuildDecisionMessage(WaterCondition condition) => condition switch
    {
        WaterCondition.Critical => "Soil moisture is below the configured minimum. Irrigation should be evaluated promptly.",
        WaterCondition.Dry => "Soil moisture is below the target range. Irrigation is recommended for evaluation.",
        WaterCondition.Target => "Soil moisture is within the configured target range. Continue monitoring.",
        WaterCondition.Wet => "Soil moisture is above the configured maximum. Avoid additional irrigation and keep monitoring.",
        _ => "Continue monitoring soil moisture."
    };

    private static IrrigationZoneDto ToDto(IrrigationZone zone) => new(
        zone.Id, zone.FieldId, zone.Name, zone.AreaHectares, zone.Method,
        zone.MinimumMoisturePercent, zone.TargetMoisturePercent, zone.MaximumMoisturePercent,
        zone.TelemetryDeviceId, zone.IsActive, zone.CreatedAtUtc, zone.UpdatedAtUtc);

    private static IrrigationApplicationDto ToDto(IrrigationApplication application) => new(
        application.Id, application.ZoneId, application.FieldId, application.AreaHectaresSnapshot,
        application.DepthMillimeters, application.EstimatedVolumeCubicMeters, application.Source,
        application.StartedAtUtc, application.EndedAtUtc, application.Notes, application.CreatedAtUtc);

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
