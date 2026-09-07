namespace AgroControl.Application.Intelligence;

public sealed record HistoricalYieldSample(
    decimal AreaHectares,
    decimal? ExpectedYieldPerHectare,
    decimal ActualYieldPerHectare);

public sealed record SeasonPredictionData(
    Guid SeasonId,
    string CropName,
    string? CropVariety,
    decimal AreaHectares,
    decimal? ExpectedYieldPerHectare,
    IReadOnlyList<HistoricalYieldSample> HistoricalSamples);

public sealed record YieldPredictionMetrics(decimal Mae, decimal Rmse);

public sealed record YieldPredictionDto(
    string ContractVersion,
    string Status,
    decimal? PredictedYieldPerHectare,
    decimal? BaselineYieldPerHectare,
    string ModelKind,
    string ModelVersion,
    int SampleCount,
    YieldPredictionMetrics? Metrics,
    DateTime GeneratedAtUtc,
    string? Warning);

public enum IntelligenceCallErrorKind
{
    None = 0,
    Validation = 1,
    Timeout = 2,
    Unavailable = 3
}

public sealed record IntelligenceCallResult(
    bool Succeeded,
    YieldPredictionDto? Value,
    IntelligenceCallErrorKind ErrorKind,
    string? Error)
{
    public static IntelligenceCallResult Success(YieldPredictionDto value) =>
        new(true, value, IntelligenceCallErrorKind.None, null);

    public static IntelligenceCallResult Validation(string error) =>
        new(false, null, IntelligenceCallErrorKind.Validation, error);

    public static IntelligenceCallResult Timeout(string error) =>
        new(false, null, IntelligenceCallErrorKind.Timeout, error);

    public static IntelligenceCallResult Unavailable(string error) =>
        new(false, null, IntelligenceCallErrorKind.Unavailable, error);
}

public enum PredictionResultKind
{
    Success = 0,
    NotFound = 1,
    InsufficientData = 2,
    Validation = 3,
    Timeout = 4,
    Unavailable = 5
}

public sealed record PredictionResult(
    PredictionResultKind Kind,
    YieldPredictionDto? Value,
    string? Error)
{
    public static PredictionResult Success(YieldPredictionDto value) =>
        new(PredictionResultKind.Success, value, null);

    public static PredictionResult NotFound(string error) =>
        new(PredictionResultKind.NotFound, null, error);

    public static PredictionResult InsufficientData(YieldPredictionDto value) =>
        new(PredictionResultKind.InsufficientData, value, value.Warning);

    public static PredictionResult Validation(string error) =>
        new(PredictionResultKind.Validation, null, error);

    public static PredictionResult Timeout(string error) =>
        new(PredictionResultKind.Timeout, null, error);

    public static PredictionResult Unavailable(string error) =>
        new(PredictionResultKind.Unavailable, null, error);
}
