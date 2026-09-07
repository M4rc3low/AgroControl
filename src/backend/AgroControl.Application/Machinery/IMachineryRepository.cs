using AgroControl.Domain.Modules.Machinery;

namespace AgroControl.Application.Machinery;

public sealed record MachineryCostTotals(
    decimal FuelLiters,
    decimal FuelCost,
    decimal MaintenanceCost,
    decimal? MinimumTrackedHour,
    decimal? MaximumTrackedHour);

public interface IMachineryRepository
{
    Task<(IReadOnlyList<Machine> Items, int TotalCount)> ListMachinesAsync(
        Guid organizationId, int skip, int take, MachineStatus? status, Guid? farmId, string? search,
        CancellationToken cancellationToken = default);

    Task<Machine?> GetMachineAsync(Guid organizationId, Guid machineId, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> InternalCodeExistsAsync(Guid organizationId, string internalCode, Guid? excludingId, CancellationToken cancellationToken = default);
    void AddMachine(Machine machine);

    void AddHourMeterReading(HourMeterReading reading);
    Task<(IReadOnlyList<HourMeterReading> Items, int TotalCount)> ListHourMeterReadingsAsync(
        Guid organizationId, Guid machineId, int skip, int take, CancellationToken cancellationToken = default);

    void AddFueling(Fueling fueling);
    Task<(IReadOnlyList<Fueling> Items, int TotalCount)> ListFuelingsAsync(
        Guid organizationId, Guid machineId, int skip, int take, CancellationToken cancellationToken = default);

    void AddMaintenance(MaintenanceRecord maintenance);
    Task<(IReadOnlyList<MaintenanceRecord> Items, int TotalCount)> ListMaintenanceAsync(
        Guid organizationId, Guid machineId, int skip, int take, MaintenanceKind? kind,
        CancellationToken cancellationToken = default);

    Task<MachineryCostTotals> GetCostTotalsAsync(Guid organizationId, Guid machineId, CancellationToken cancellationToken = default);
}
