using AgroControl.Application.Machinery;
using AgroControl.Domain.Modules.Machinery;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class MachineryRepository(AgroControlDbContext dbContext) : IMachineryRepository
{
    public async Task<(IReadOnlyList<Machine> Items, int TotalCount)> ListMachinesAsync(
        Guid organizationId, int skip, int take, MachineStatus? status, Guid? farmId, string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Machines.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (farmId is not null) query = query.Where(x => x.FarmId == farmId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.InternalCode.ToLower().Contains(term) ||
                (x.Manufacturer != null && x.Manufacturer.ToLower().Contains(term)) ||
                (x.Model != null && x.Model.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.InternalCode)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Machine?> GetMachineAsync(Guid organizationId, Guid machineId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Machine> query = dbContext.Machines.Where(x => x.OrganizationId == organizationId && x.Id == machineId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> InternalCodeExistsAsync(Guid organizationId, string internalCode, Guid? excludingId, CancellationToken cancellationToken = default)
    {
        var normalized = internalCode.ToLower();
        return dbContext.Machines.AsNoTracking().AnyAsync(
            x => x.OrganizationId == organizationId && x.InternalCode.ToLower() == normalized &&
                 (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public void AddMachine(Machine machine) => dbContext.Machines.Add(machine);

    public void AddHourMeterReading(HourMeterReading reading) => dbContext.HourMeterReadings.Add(reading);

    public async Task<(IReadOnlyList<HourMeterReading> Items, int TotalCount)> ListHourMeterReadingsAsync(
        Guid organizationId, Guid machineId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.HourMeterReadings.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void AddFueling(Fueling fueling) => dbContext.Fuelings.Add(fueling);

    public async Task<(IReadOnlyList<Fueling> Items, int TotalCount)> ListFuelingsAsync(
        Guid organizationId, Guid machineId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Fuelings.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void AddMaintenance(MaintenanceRecord maintenance) => dbContext.MaintenanceRecords.Add(maintenance);

    public async Task<(IReadOnlyList<MaintenanceRecord> Items, int TotalCount)> ListMaintenanceAsync(
        Guid organizationId, Guid machineId, int skip, int take, MaintenanceKind? kind,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MaintenanceRecords.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);
        if (kind is not null) query = query.Where(x => x.Kind == kind);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.PerformedOn).ThenByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<MachineryCostTotals> GetCostTotalsAsync(
        Guid organizationId,
        Guid machineId,
        CancellationToken cancellationToken = default)
    {
        var fuelQuery = dbContext.Fuelings.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);
        var maintenanceQuery = dbContext.MaintenanceRecords.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);
        var readingQuery = dbContext.HourMeterReadings.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MachineId == machineId);

        var fuelLiters = await fuelQuery.SumAsync(x => (decimal?)x.Liters, cancellationToken) ?? 0m;
        var fuelCost = await fuelQuery.SumAsync(x => (decimal?)x.TotalCost, cancellationToken) ?? 0m;
        var maintenanceCost = await maintenanceQuery.SumAsync(
            x => (decimal?)(x.PartsCost + x.LaborCost + x.OtherCost), cancellationToken) ?? 0m;
        var minimumHour = await readingQuery.Select(x => (decimal?)x.Hours).MinAsync(cancellationToken);
        var maximumHour = await readingQuery.Select(x => (decimal?)x.Hours).MaxAsync(cancellationToken);

        return new MachineryCostTotals(fuelLiters, fuelCost, maintenanceCost, minimumHour, maximumHour);
    }
}
