using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.UnitTests;

public sealed class RemoteSensingDomainTests
{
    [Fact]
    public void Scene_create_normalizes_metadata_and_utc()
    {
        var scene = RemoteSensingScene.Create(Guid.NewGuid(), Guid.NewGuid(), null, " Sentinel-2 ", " S2-001 ",
            RemoteSensingPlatform.Satellite, new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Unspecified),
            12.5m, 10m, " s3://bucket/scene.tif ", " observação ", DateTime.UtcNow);

        Assert.Equal("Sentinel-2", scene.Provider);
        Assert.Equal("S2-001", scene.ExternalId);
        Assert.Equal(DateTimeKind.Utc, scene.AcquiredAtUtc.Kind);
        Assert.Equal(12.5m, scene.CloudCoveragePercent);
        Assert.Equal(10m, scene.SpatialResolutionMeters);
    }

    [Fact]
    public void Scene_rejects_invalid_cloud_and_resolution()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RemoteSensingScene.Create(Guid.NewGuid(), Guid.NewGuid(), null,
            "Provider", "ext", RemoteSensingPlatform.Satellite, DateTime.UtcNow, 101m, 10m, null, null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => RemoteSensingScene.Create(Guid.NewGuid(), Guid.NewGuid(), null,
            "Provider", "ext", RemoteSensingPlatform.Satellite, DateTime.UtcNow, 10m, 0m, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Standardized_index_rejects_values_outside_normalized_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VegetationIndexObservation.Create(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, null, VegetationIndexType.NDVI, null, -0.2m, 1.2m, 0.6m, 0.5m, 0.1m, 95m,
            1000, "Sentinel-2", DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Custom_index_requires_name_and_preserves_statistics()
    {
        Assert.Throws<ArgumentException>(() => VegetationIndexObservation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, null, VegetationIndexType.Custom, null, 0m, 10m, 5m, 5m, 1m, 90m, 500, "Drone", DateTime.UtcNow, DateTime.UtcNow));

        var observation = VegetationIndexObservation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null,
            VegetationIndexType.Custom, "  SAVI custom  ", 0m, 2m, 1.1m, 1m, 0.2m, 88m, 500, "Drone", DateTime.UtcNow, DateTime.UtcNow);
        Assert.Equal("SAVI custom", observation.CustomIndexName);
        Assert.Equal(1.1m, observation.Mean);
        Assert.Equal(88m, observation.ValidCoveragePercent);
    }
}
