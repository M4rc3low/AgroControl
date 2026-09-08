using AgroControl.Application.Irrigation;
using AgroControl.Application.Telemetry;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Irrigation;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class IrrigationPersistenceTests
{
    [Fact]
    public async Task Irrigation_is_tenant_isolated_and_calculates_volume_and_status()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Irrigation A", $"irrigation-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Irrigation B", $"irrigation-b-{Guid.NewGuid():N}", now);
        var farmA = Farm.Create(organizationA.Id, "Fazenda A", 100m, "Rio Verde", "GO", now);
        var farmB = Farm.Create(organizationB.Id, "Fazenda B", 100m, "Sorriso", "MT", now);
        var fieldA = Field.Create(organizationA.Id, farmA.Id, "Talhão A", 20m, now);
        var fieldB = Field.Create(organizationB.Id, farmB.Id, "Talhão B", 20m, now);
        var deviceId = Guid.NewGuid();

        dbContext.Organizations.AddRange(organizationA, organizationB);
        dbContext.Farms.AddRange(farmA, farmB);
        dbContext.Fields.AddRange(fieldA, fieldB);
        await dbContext.SaveChangesAsync();

        var repository = new IrrigationRepository(dbContext);
        var productionRepository = new ProductionRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var telemetry = new FakeTelemetryClient(organizationA.Id, deviceId, fieldA.Id, 42d);
        var service = new IrrigationService(repository, productionRepository, telemetry, unitOfWork);

        var created = await service.CreateZoneAsync(organizationA.Id, new CreateIrrigationZoneCommand(
            fieldA.Id, "Zona 1", 5m, IrrigationMethod.Drip, 35m, 50m, 75m, deviceId));
        Assert.True(created.Succeeded);

        repository.AddZone(IrrigationZone.Create(
            organizationB.Id, fieldB.Id, "Zona B", 5m, IrrigationMethod.Sprinkler, 35m, 50m, 75m, null, now));
        await dbContext.SaveChangesAsync();

        var zonesA = await service.ListZonesAsync(organizationA.Id, 1, 20, null, false);
        Assert.Single(zonesA.Items);
        Assert.Equal(organizationA.Id, (await repository.GetZoneAsync(organizationA.Id, created.Value!.Id, false))!.OrganizationId);
        Assert.Null(await repository.GetZoneAsync(organizationB.Id, created.Value.Id, false));

        var application = await service.CreateApplicationAsync(organizationA.Id, new CreateIrrigationApplicationCommand(
            created.Value.Id, 12m, IrrigationApplicationSource.Manual, now, now.AddHours(2), null));
        Assert.True(application.Succeeded);
        Assert.Equal(600m, application.Value!.EstimatedVolumeCubicMeters);

        var summary = await service.GetSummaryAsync(organizationA.Id, fieldA.Id, created.Value.Id, null, null);
        Assert.Equal(1, summary.ApplicationCount);
        Assert.Equal(600m, summary.EstimatedVolumeCubicMeters);

        var status = await service.GetZoneStatusAsync(organizationA.Id, created.Value.Id);
        Assert.True(status.Succeeded);
        Assert.Equal(WaterCondition.Dry, status.Value!.Condition);
        Assert.Equal(IrrigationRecommendation.Irrigate, status.Value.Recommendation);
    }

    private sealed class FakeTelemetryClient(Guid organizationId, Guid deviceId, Guid fieldId, double soilMoisture) : ITelemetryClient
    {
        public Task<IReadOnlyList<TelemetryDeviceDto>> ListDevicesAsync(Guid organizationId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TelemetryDeviceDto> CreateDeviceAsync(Guid organizationId, CreateTelemetryDeviceCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TelemetryDeviceDto> GetDeviceAsync(Guid requestedOrganizationId, Guid requestedDeviceId, CancellationToken cancellationToken = default)
        {
            if (requestedOrganizationId != organizationId || requestedDeviceId != deviceId)
                throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Device not found.");
            return Task.FromResult(new TelemetryDeviceDto(
                deviceId, organizationId, "soil-01", "SoilSensor", null, null, fieldId, "Active", null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        }
        public Task<TelemetryDeviceDto> UpdateStatusAsync(Guid organizationId, Guid deviceId, string status, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TelemetryIngestResponse> IngestAsync(Guid organizationId, Guid deviceId, CreateTelemetryEventCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TelemetryEventDto> GetLatestAsync(Guid requestedOrganizationId, Guid requestedDeviceId, string? metric, CancellationToken cancellationToken = default)
        {
            if (requestedOrganizationId != organizationId || requestedDeviceId != deviceId || metric != "soil_moisture_percent")
                throw new TelemetryClientException(TelemetryClientErrorKind.NotFound, "Reading not found.");
            return Task.FromResult(new TelemetryEventDto(
                Guid.NewGuid(), organizationId, deviceId, "evt-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                "soil_moisture_percent", soilMoisture, null, "%", null, null, "good", "test", null));
        }
        public Task<IReadOnlyList<TelemetryEventDto>> GetHistoryAsync(Guid organizationId, Guid deviceId, string? metric, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
