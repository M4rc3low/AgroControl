using AgroControl.Application.Machinery;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;

namespace AgroControl.Application.Telemetry;

public sealed class TelemetryAccessService(
    ITelemetryClient client,
    IOperationalScopeContext operationalScope,
    IProductionRepository productionRepository,
    IMachineryRepository machineryRepository)
{
    public async Task<IReadOnlyList<TelemetryDeviceDto>> ListDevicesAsync(
        Guid organizationId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var requested = Math.Clamp(limit, 1, 500);
        var devices = await client.ListDevicesAsync(organizationId, 1000, cancellationToken);
        var visible = new List<TelemetryDeviceDto>(Math.Min(requested, devices.Count));

        foreach (var device in devices)
        {
            if (!await CanAccessAsync(organizationId, device, cancellationToken)) continue;
            visible.Add(device);
            if (visible.Count == requested) break;
        }

        return visible;
    }

    public async Task<TelemetryDeviceDto> CreateDeviceAsync(
        Guid organizationId,
        CreateTelemetryDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(organizationId, command, cancellationToken);
        return await client.CreateDeviceAsync(organizationId, command, cancellationToken);
    }

    public Task<TelemetryDeviceDto> GetDeviceAsync(Guid organizationId, Guid deviceId, CancellationToken cancellationToken = default) =>
        GetAuthorizedDeviceAsync(organizationId, deviceId, cancellationToken);

    public async Task<TelemetryDeviceDto> UpdateStatusAsync(
        Guid organizationId,
        Guid deviceId,
        string status,
        CancellationToken cancellationToken = default)
    {
        await GetAuthorizedDeviceAsync(organizationId, deviceId, cancellationToken);
        return await client.UpdateStatusAsync(organizationId, deviceId, status, cancellationToken);
    }

    public async Task<TelemetryIngestResponse> IngestAsync(
        Guid organizationId,
        Guid deviceId,
        CreateTelemetryEventCommand command,
        CancellationToken cancellationToken = default)
    {
        await GetAuthorizedDeviceAsync(organizationId, deviceId, cancellationToken);
        return await client.IngestAsync(organizationId, deviceId, command, cancellationToken);
    }

    public async Task<TelemetryEventDto> GetLatestAsync(
        Guid organizationId,
        Guid deviceId,
        string? metric,
        CancellationToken cancellationToken = default)
    {
        await GetAuthorizedDeviceAsync(organizationId, deviceId, cancellationToken);
        return await client.GetLatestAsync(organizationId, deviceId, metric, cancellationToken);
    }

    public async Task<IReadOnlyList<TelemetryEventDto>> GetHistoryAsync(
        Guid organizationId,
        Guid deviceId,
        string? metric,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await GetAuthorizedDeviceAsync(organizationId, deviceId, cancellationToken);
        return await client.GetHistoryAsync(organizationId, deviceId, metric, from, to, Math.Clamp(limit, 1, 500), cancellationToken);
    }

    private async Task<TelemetryDeviceDto> GetAuthorizedDeviceAsync(
        Guid organizationId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var device = await client.GetDeviceAsync(organizationId, deviceId, cancellationToken);
        if (!await CanAccessAsync(organizationId, device, cancellationToken))
            throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Telemetry device was not found.");
        return device;
    }

    private async Task<bool> CanAccessAsync(
        Guid organizationId,
        TelemetryDeviceDto device,
        CancellationToken cancellationToken)
    {
        if (!operationalScope.IsInitialized || !operationalScope.IsRestricted) return true;

        if (device.FarmId is { } farmId)
            return operationalScope.FarmIds.Contains(farmId);

        if (device.FieldId is { } fieldId)
            return await productionRepository.GetFieldAsync(organizationId, fieldId, false, cancellationToken) is not null;

        if (device.MachineId is { } machineId)
            return await machineryRepository.GetMachineAsync(organizationId, machineId, false, cancellationToken) is not null;

        // Organization-level devices (for example a shared weather gateway) remain organization resources.
        return true;
    }

    private async Task ValidateReferencesAsync(
        Guid organizationId,
        CreateTelemetryDeviceCommand command,
        CancellationToken cancellationToken)
    {
        Guid? resolvedFarmId = null;

        if (command.FarmId is { } farmId)
        {
            var farm = await productionRepository.GetFarmAsync(organizationId, farmId, false, cancellationToken);
            if (farm is null)
                throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Farm was not found in the current operational scope.");
            resolvedFarmId = farm.Id;
        }

        if (command.FieldId is { } fieldId)
        {
            var field = await productionRepository.GetFieldAsync(organizationId, fieldId, false, cancellationToken);
            if (field is null)
                throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Field was not found in the current operational scope.");
            if (resolvedFarmId is not null && resolvedFarmId.Value != field.FarmId)
                throw new TelemetryClientException(TelemetryClientErrorKind.Validation, "Field does not belong to the informed farm.");
            resolvedFarmId ??= field.FarmId;
        }

        if (command.MachineId is { } machineId)
        {
            var machine = await machineryRepository.GetMachineAsync(organizationId, machineId, false, cancellationToken);
            if (machine is null)
                throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Machine was not found in the current operational scope.");
            if (machine.FarmId is { } machineFarmId && resolvedFarmId is not null && machineFarmId != resolvedFarmId.Value)
                throw new TelemetryClientException(TelemetryClientErrorKind.Validation, "Machine does not belong to the informed farm or field context.");
        }
    }
}
