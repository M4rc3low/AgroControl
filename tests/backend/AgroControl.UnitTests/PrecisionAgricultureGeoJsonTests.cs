using AgroControl.Application.PrecisionAgriculture;

namespace AgroControl.UnitTests;

public sealed class PrecisionAgricultureGeoJsonTests
{
    [Fact]
    public void Validator_closes_open_ring_and_preserves_polygon_type()
    {
        var input = new GeoJsonPolygonDto("Polygon", [[[-47.1000, -15.7000], [-47.0900, -15.7000], [-47.0900, -15.6900], [-47.1000, -15.6900]]]);
        var result = GeoJsonPolygonValidator.ValidateAndNormalize(input);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("Polygon", result.Value.Type);
        Assert.Equal(5, result.Value.Coordinates[0].Length);
        Assert.Equal(result.Value.Coordinates[0][0], result.Value.Coordinates[0][^1]);
    }

    [Fact]
    public void Validator_rejects_coordinate_outside_wgs84_range()
    {
        var input = new GeoJsonPolygonDto("Polygon", [[[181, -15], [180, -15], [180, -14], [181, -15]]]);
        var result = GeoJsonPolygonValidator.ValidateAndNormalize(input);
        Assert.False(result.Succeeded);
        Assert.Contains("Longitude", result.Error);
    }

    [Fact]
    public void Validator_rejects_ring_with_less_than_three_distinct_vertices()
    {
        var input = new GeoJsonPolygonDto("Polygon", [[[-47, -15], [-47, -15], [-46, -15], [-47, -15]]]);
        var result = GeoJsonPolygonValidator.ValidateAndNormalize(input);
        Assert.False(result.Succeeded);
        Assert.Contains("distinct", result.Error);
    }
}
