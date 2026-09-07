using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Machinery;

namespace AgroControl.Application.Machinery;

public sealed class MachineryService(
    IMachineryRepository repository,
    IProductionRepository productionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<MachineDto>> ListMachinesAsync(
        Guid organizationId,
        int page,
        int pageSize,
        MachineStatus? status,
        Guid? farmId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListMachinesAsync(
            organizationId, (page - 1) * pageSize, pageSize, status, farmId, search, cancellationToken);
        return new PagedResult<MachineDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<MachineDto?> GetMachineAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, id, false, cancellationToken);
        return machine is null ? null : ToDto(machine);
    }

    public async Task<OperationResult<MachineDto>> CreateMachineAsync(
        Guid organizationId,
        CreateMachineCommand command,
        CancellationToken cancellationToken = default)
    {
        var farmValidation = await ValidateFarmAsync(organizationId, command.FarmId, cancellationToken);
        if (farmValidation is not null) return OperationResult<MachineDto>.Validation(farmValidation);
        if (string.IsNullOrWhiteSpace(command.InternalCode)) return OperationResult<MachineDto>.Validation("Internal code is required.");
        if (await repository.InternalCodeExistsAsync(organizationId, command.InternalCode.Trim(), null, cancellationToken))
            return OperationResult<MachineDto>.Conflict("A machine with this internal code already exists.");

        try
        {
            var now = DateTime.UtcNow;
            var machine = Machine.Create(
                organizationId, command.FarmId, command.Name, command.InternalCode, command.Kind,
                command.Manufacturer, command.Model, command.Year, command.InitialHourMeter, now);
            repository.AddMachine(machine);
            repository.AddHourMeterReading(HourMeterReading.Create(
                organizationId, machine.Id, command.InitialHourMeter, now, "Initial hour meter", now));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<MachineDto>.Success(ToDto(machine));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<MachineDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<MachineDto>> UpdateMachineAsync(
        Guid organizationId,
        Guid id,
        UpdateMachineCommand command,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, id, true, cancellationToken);
        if (machine is null) return OperationResult<MachineDto>.NotFound("Machine not found.");

        var farmValidation = await ValidateFarmAsync(organizationId, command.FarmId, cancellationToken);
        if (farmValidation is not null) return OperationResult<MachineDto>.Validation(farmValidation);
        if (string.IsNullOrWhiteSpace(command.InternalCode)) return OperationResult<MachineDto>.Validation("Internal code is required.");
        if (await repository.InternalCodeExistsAsync(organizationId, command.InternalCode.Trim(), id, cancellationToken))
            return OperationResult<MachineDto>.Conflict("A machine with this internal code already exists.");

        try
        {
            machine.Update(
                command.FarmId, command.Name, command.InternalCode, command.Kind,
                command.Manufacturer, command.Model, command.Year, command.Status, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<MachineDto>.Success(ToDto(machine));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<MachineDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateMachineAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, id, true, cancellationToken);
        if (machine is null) return OperationResult<bool>.NotFound("Machine not found.");
        machine.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<HourMeterReadingDto>> RecordHourMeterAsync(
        Guid organizationId,
        Guid machineId,
        RecordHourMeterCommand command,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, machineId, true, cancellationToken);
        if (machine is null) return OperationResult<HourMeterReadingDto>.NotFound("Machine not found.");
        if (machine.Status == MachineStatus.Inactive)
            return OperationResult<HourMeterReadingDto>.Conflict("Inactive machines cannot receive new hour meter readings.");
        if (command.Hours < machine.CurrentHourMeter)
            return OperationResult<HourMeterReadingDto>.Conflict("Hour meter cannot regress.");

        try
        {
            var now = DateTime.UtcNow;
            machine.RecordHourMeter(command.Hours, now);
            var reading = HourMeterReading.Create(
                organizationId, machineId, command.Hours, command.OccurredAtUtc ?? now, command.Notes, now);
            repository.AddHourMeterReading(reading);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<HourMeterReadingDto>.Success(ToDto(reading));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<HourMeterReadingDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<PagedResult<HourMeterReadingDto>>> ListHourMeterAsync(
        Guid organizationId,
        Guid machineId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetMachineAsync(organizationId, machineId, false, cancellationToken) is null)
            return OperationResult<PagedResult<HourMeterReadingDto>>.NotFound("Machine not found.");

        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListHourMeterReadingsAsync(
            organizationId, machineId, (page - 1) * pageSize, pageSize, cancellationToken);
        return OperationResult<PagedResult<HourMeterReadingDto>>.Success(
            new PagedResult<HourMeterReadingDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    public async Task<OperationResult<FuelingDto>> AddFuelingAsync(
        Guid organizationId,
        Guid machineId,
        CreateFuelingCommand command,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, machineId, true, cancellationToken);
        if (machine is null) return OperationResult<FuelingDto>.NotFound("Machine not found.");
        if (machine.Status == MachineStatus.Inactive)
            return OperationResult<FuelingDto>.Conflict("Inactive machines cannot receive fueling records.");
        if (command.HourMeter < machine.CurrentHourMeter)
            return OperationResult<FuelingDto>.Conflict("Fueling hour meter cannot be lower than the current machine hour meter.");

        try
        {
            var now = DateTime.UtcNow;
            var occurredAt = command.OccurredAtUtc ?? now;
            if (command.HourMeter > machine.CurrentHourMeter)
            {
                machine.RecordHourMeter(command.HourMeter, now);
                repository.AddHourMeterReading(HourMeterReading.Create(
                    organizationId, machineId, command.HourMeter, occurredAt, "Automatic reading from fueling", now));
            }

            var fueling = Fueling.Create(
                organizationId, machineId, command.Liters, command.TotalCost, command.HourMeter, occurredAt, command.Notes, now);
            repository.AddFueling(fueling);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<FuelingDto>.Success(ToDto(fueling));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FuelingDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<PagedResult<FuelingDto>>> ListFuelingsAsync(
        Guid organizationId,
        Guid machineId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetMachineAsync(organizationId, machineId, false, cancellationToken) is null)
            return OperationResult<PagedResult<FuelingDto>>.NotFound("Machine not found.");

        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListFuelingsAsync(
            organizationId, machineId, (page - 1) * pageSize, pageSize, cancellationToken);
        return OperationResult<PagedResult<FuelingDto>>.Success(
            new PagedResult<FuelingDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    public async Task<OperationResult<MaintenanceDto>> AddMaintenanceAsync(
        Guid organizationId,
        Guid machineId,
        CreateMaintenanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, machineId, true, cancellationToken);
        if (machine is null) return OperationResult<MaintenanceDto>.NotFound("Machine not found.");
        if (machine.Status == MachineStatus.Inactive)
            return OperationResult<MaintenanceDto>.Conflict("Inactive machines cannot receive maintenance records.");
        if (command.HourMeter is not null && command.HourMeter.Value < machine.CurrentHourMeter)
            return OperationResult<MaintenanceDto>.Conflict("Maintenance hour meter cannot be lower than the current machine hour meter.");

        try
        {
            var now = DateTime.UtcNow;
            if (command.HourMeter is not null && command.HourMeter.Value > machine.CurrentHourMeter)
            {
                machine.RecordHourMeter(command.HourMeter.Value, now);
                repository.AddHourMeterReading(HourMeterReading.Create(
                    organizationId, machineId, command.HourMeter.Value, now, "Automatic reading from maintenance", now));
            }

            var maintenance = MaintenanceRecord.Create(
                organizationId, machineId, command.Kind, command.Description, command.PerformedOn, command.HourMeter,
                command.PartsCost, command.LaborCost, command.OtherCost, command.NextMaintenanceDate,
                command.NextMaintenanceHourMeter, command.Notes, now);
            repository.AddMaintenance(maintenance);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<MaintenanceDto>.Success(ToDto(maintenance));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<MaintenanceDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<PagedResult<MaintenanceDto>>> ListMaintenanceAsync(
        Guid organizationId,
        Guid machineId,
        int page,
        int pageSize,
        MaintenanceKind? kind,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetMachineAsync(organizationId, machineId, false, cancellationToken) is null)
            return OperationResult<PagedResult<MaintenanceDto>>.NotFound("Machine not found.");

        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListMaintenanceAsync(
            organizationId, machineId, (page - 1) * pageSize, pageSize, kind, cancellationToken);
        return OperationResult<PagedResult<MaintenanceDto>>.Success(
            new PagedResult<MaintenanceDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    public async Task<OperationResult<MachineryCostSummaryDto>> GetCostSummaryAsync(
        Guid organizationId,
        Guid machineId,
        CancellationToken cancellationToken = default)
    {
        var machine = await repository.GetMachineAsync(organizationId, machineId, false, cancellationToken);
        if (machine is null) return OperationResult<MachineryCostSummaryDto>.NotFound("Machine not found.");

        var totals = await repository.GetCostTotalsAsync(organizationId, machineId, cancellationToken);
        var totalCost = totals.FuelCost + totals.MaintenanceCost;
        var trackedHours = totals.MinimumTrackedHour is not null && totals.MaximumTrackedHour is not null
            ? Math.Max(0m, totals.MaximumTrackedHour.Value - totals.MinimumTrackedHour.Value)
            : 0m;
        decimal? costPerTrackedHour = trackedHours > 0m ? totalCost / trackedHours : null;

        return OperationResult<MachineryCostSummaryDto>.Success(new MachineryCostSummaryDto(
            machine.Id, machine.CurrentHourMeter, totals.FuelLiters, totals.FuelCost, totals.MaintenanceCost,
            totalCost, trackedHours, costPerTrackedHour));
    }

    private async Task<string?> ValidateFarmAsync(Guid organizationId, Guid? farmId, CancellationToken cancellationToken)
    {
        if (farmId is null) return null;
        var farm = await productionRepository.GetFarmAsync(organizationId, farmId.Value, false, cancellationToken);
        return farm is null || !farm.IsActive ? "Farm does not belong to this organization or is inactive." : null;
    }

    private static MachineDto ToDto(Machine item) => new(
        item.Id, item.FarmId, item.Name, item.InternalCode, item.Kind, item.Manufacturer, item.Model, item.Year,
        item.Status, item.CurrentHourMeter, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static HourMeterReadingDto ToDto(HourMeterReading item) => new(
        item.Id, item.MachineId, item.Hours, item.OccurredAtUtc, item.Notes, item.CreatedAtUtc);

    private static FuelingDto ToDto(Fueling item) => new(
        item.Id, item.MachineId, item.Liters, item.TotalCost, item.UnitCost, item.HourMeter,
        item.OccurredAtUtc, item.Notes, item.CreatedAtUtc);

    private static MaintenanceDto ToDto(MaintenanceRecord item) => new(
        item.Id, item.MachineId, item.Kind, item.Description, item.PerformedOn, item.HourMeter,
        item.PartsCost, item.LaborCost, item.OtherCost, item.TotalCost, item.NextMaintenanceDate,
        item.NextMaintenanceHourMeter, item.Notes, item.CreatedAtUtc);

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}
