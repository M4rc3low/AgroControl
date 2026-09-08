using AgroControl.Domain.Modules.Sustainability;

namespace AgroControl.UnitTests;

public sealed class SustainabilityDomainTests
{
    [Fact]
    public void Emission_factor_rejects_non_positive_factor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmissionFactor.Create(
            Guid.NewGuid(), "Diesel", EmissionSourceCategory.Fuel, "L", 0m,
            "Internal test reference", null, new DateOnly(2026, 1, 1), null, DateTime.UtcNow));
    }

    [Fact]
    public void Activity_uses_factor_snapshot_and_calculates_co2e()
    {
        var organizationId = Guid.NewGuid();
        var factor = EmissionFactor.Create(
            organizationId, "Diesel", EmissionSourceCategory.Fuel, "L", 2.5m,
            "Internal test reference", null, new DateOnly(2026, 1, 1), null, DateTime.UtcNow);

        var activity = EmissionActivity.Create(
            organizationId, factor, 100m, new DateOnly(2026, 9, 1), EmissionActivityOrigin.Manual,
            SustainabilityDataQuality.Recorded, "Abastecimento", null, null, null, null, null, null, DateTime.UtcNow);

        Assert.Equal(250m, activity.EmissionsKgCo2e);
        Assert.Equal(0.25m, activity.EmissionsTCo2e);
        Assert.Equal(2.5m, activity.FactorKgCo2ePerUnitSnapshot);
    }

    [Fact]
    public void Correction_allows_negative_quantity_but_not_zero()
    {
        var organizationId = Guid.NewGuid();
        var factor = EmissionFactor.Create(
            organizationId, "Diesel", EmissionSourceCategory.Fuel, "L", 2m,
            "Internal test reference", null, new DateOnly(2026, 1, 1), null, DateTime.UtcNow);

        var correction = EmissionActivity.Create(
            organizationId, factor, -10m, new DateOnly(2026, 9, 1), EmissionActivityOrigin.Correction,
            SustainabilityDataQuality.Recorded, "Correção", null, null, null, null, null, null, DateTime.UtcNow);

        Assert.Equal(-20m, correction.EmissionsKgCo2e);
        Assert.Throws<ArgumentOutOfRangeException>(() => EmissionActivity.Create(
            organizationId, factor, 0m, new DateOnly(2026, 9, 1), EmissionActivityOrigin.Correction,
            SustainabilityDataQuality.Recorded, "Correção", null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void External_activity_requires_complete_source_reference()
    {
        var organizationId = Guid.NewGuid();
        var factor = EmissionFactor.Create(
            organizationId, "Energia", EmissionSourceCategory.Energy, "kWh", 0.1m,
            "Internal test reference", null, new DateOnly(2026, 1, 1), null, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => EmissionActivity.Create(
            organizationId, factor, 10m, new DateOnly(2026, 9, 1), EmissionActivityOrigin.External,
            SustainabilityDataQuality.Measured, "Energia", null, null, null, "Telemetry", null, null, DateTime.UtcNow));
    }
}
