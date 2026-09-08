using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.UnitTests;

public sealed class ManagementZoneDomainTests
{
    [Fact]
    public void Create_normalizes_metadata_and_preserves_numeric_value()
    {
        var zone = ManagementZone.Create(Guid.NewGuid(), Guid.NewGuid(), ManagementZoneType.Soil, "  Zona A  ", "  descrição  ", " Alta ", 6.2m, " pH ", DateTime.UtcNow);
        Assert.Equal("Zona A", zone.Name);
        Assert.Equal("Alta", zone.Classification);
        Assert.Equal(6.2m, zone.Value);
        Assert.Equal("pH", zone.Unit);
    }

    [Fact]
    public void Create_rejects_missing_field_and_name()
    {
        Assert.Throws<ArgumentException>(() => ManagementZone.Create(Guid.NewGuid(), Guid.Empty, ManagementZoneType.Custom, "Zona", null, null, null, null, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => ManagementZone.Create(Guid.NewGuid(), Guid.NewGuid(), ManagementZoneType.Custom, " ", null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Deactivate_keeps_record_and_marks_it_inactive()
    {
        var zone = ManagementZone.Create(Guid.NewGuid(), Guid.NewGuid(), ManagementZoneType.Yield, "Produtividade", null, null, 55m, "sc/ha", DateTime.UtcNow);
        zone.Deactivate(DateTime.UtcNow.AddMinutes(1));
        Assert.False(zone.IsActive);
    }
}
