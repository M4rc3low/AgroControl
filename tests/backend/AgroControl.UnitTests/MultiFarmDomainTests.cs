using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Operations;

namespace AgroControl.UnitTests;

public sealed class MultiFarmDomainTests
{
    [Fact]
    public void Brazilian_farm_normalizes_location_and_timezone()
    {
        var farm = Farm.Create(
            Guid.NewGuid(),
            "Fazenda Primavera",
            1200m,
            "Sorriso",
            "mt",
            DateTime.UtcNow,
            countryCode: "br",
            municipalityCode: "5107925",
            postalCode: "78890-000",
            latitude: -12.542m,
            longitude: -55.721m,
            timeZoneId: "America/Cuiaba");

        Assert.Equal("BR", farm.CountryCode);
        Assert.Equal("MT", farm.StateCode);
        Assert.Equal("MT", farm.State);
        Assert.Equal("America/Cuiaba", farm.TimeZoneId);
        Assert.Equal("5107925", farm.MunicipalityCode);
    }

    [Fact]
    public void Brazilian_farm_rejects_invalid_state_code()
    {
        Assert.Throws<ArgumentException>(() => Farm.Create(
            Guid.NewGuid(),
            "Fazenda",
            10m,
            "Cidade",
            "XX",
            DateTime.UtcNow));
    }

    [Fact]
    public void Farm_rejects_invalid_coordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Farm.Create(
            Guid.NewGuid(),
            "Fazenda",
            10m,
            "Cidade",
            "SP",
            DateTime.UtcNow,
            latitude: 91m));
    }

    [Fact]
    public void Operational_region_normalizes_code()
    {
        var region = OperationalRegion.Create(
            Guid.NewGuid(),
            "Centro-Oeste",
            "co_01",
            "Operação regional",
            DateTime.UtcNow);

        Assert.Equal("CO_01", region.Code);
        Assert.True(region.IsActive);
    }

    [Fact]
    public void All_farms_assignment_has_explicit_scope_key()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignment = FarmAccessAssignment.CreateAllFarms(organizationId, userId, userId, DateTime.UtcNow);

        Assert.Equal(FarmAccessScopeType.AllFarms, assignment.ScopeType);
        Assert.Null(assignment.TargetId);
        Assert.Equal("all", assignment.ScopeKey);
    }

    [Fact]
    public void Region_assignment_requires_target_id()
    {
        Assert.Throws<ArgumentException>(() => FarmAccessAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            FarmAccessScopeType.Region,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow));
    }
}
