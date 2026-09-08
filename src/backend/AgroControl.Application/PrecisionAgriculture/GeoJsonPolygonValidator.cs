namespace AgroControl.Application.PrecisionAgriculture;

public static class GeoJsonPolygonValidator
{
    private const double CoordinateTolerance = 1e-10;

    public static GeoJsonValidationResult ValidateAndNormalize(GeoJsonPolygonDto? polygon)
    {
        if (polygon is null)
            return GeoJsonValidationResult.Failure("A GeoJSON polygon is required.");
        if (!string.Equals(polygon.Type, "Polygon", StringComparison.OrdinalIgnoreCase))
            return GeoJsonValidationResult.Failure("GeoJSON type must be Polygon.");
        if (polygon.Coordinates is null || polygon.Coordinates.Length == 0)
            return GeoJsonValidationResult.Failure("Polygon must contain at least one linear ring.");

        var normalizedRings = new List<double[][]>(polygon.Coordinates.Length);
        for (var ringIndex = 0; ringIndex < polygon.Coordinates.Length; ringIndex++)
        {
            var ring = polygon.Coordinates[ringIndex];
            if (ring is null || ring.Length < 3)
                return GeoJsonValidationResult.Failure($"Ring {ringIndex + 1} must contain at least three positions.");

            var normalized = new List<double[]>(ring.Length + 1);
            foreach (var position in ring)
            {
                if (position is null || position.Length < 2)
                    return GeoJsonValidationResult.Failure($"Every position in ring {ringIndex + 1} must contain longitude and latitude.");

                var longitude = position[0];
                var latitude = position[1];
                if (!double.IsFinite(longitude) || !double.IsFinite(latitude))
                    return GeoJsonValidationResult.Failure("Coordinates must be finite numbers.");
                if (longitude is < -180 or > 180)
                    return GeoJsonValidationResult.Failure("Longitude must be between -180 and 180 degrees.");
                if (latitude is < -90 or > 90)
                    return GeoJsonValidationResult.Failure("Latitude must be between -90 and 90 degrees.");

                normalized.Add([longitude, latitude]);
            }

            var distinctLimit = normalized.Count > 1 && SamePosition(normalized[0], normalized[^1]) ? normalized.Count - 1 : normalized.Count;
            var distinctVertices = normalized.Take(distinctLimit).Distinct(new PositionComparer()).Count();
            if (distinctVertices < 3)
                return GeoJsonValidationResult.Failure($"Ring {ringIndex + 1} must contain at least three distinct vertices.");

            if (!SamePosition(normalized[0], normalized[^1]))
                normalized.Add([normalized[0][0], normalized[0][1]]);
            if (normalized.Count < 4)
                return GeoJsonValidationResult.Failure($"Ring {ringIndex + 1} is not a valid closed linear ring.");

            normalizedRings.Add(normalized.ToArray());
        }

        return GeoJsonValidationResult.Success(new GeoJsonPolygonDto("Polygon", normalizedRings.ToArray()));
    }

    private static bool SamePosition(double[] left, double[] right) =>
        Math.Abs(left[0] - right[0]) <= CoordinateTolerance && Math.Abs(left[1] - right[1]) <= CoordinateTolerance;

    private sealed class PositionComparer : IEqualityComparer<double[]>
    {
        public bool Equals(double[]? x, double[]? y) => x is not null && y is not null && SamePosition(x, y);
        public int GetHashCode(double[] obj) => HashCode.Combine(Math.Round(obj[0], 10), Math.Round(obj[1], 10));
    }
}
