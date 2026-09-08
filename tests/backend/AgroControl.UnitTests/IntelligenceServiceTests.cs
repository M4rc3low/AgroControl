using AgroControl.Application.Intelligence;

namespace AgroControl.UnitTests;

public sealed class IntelligenceServiceTests
{
    [Fact]
    public async Task Returns_not_found_without_calling_intelligence_for_another_tenant_or_missing_season()
    {
        var client = new FakeClient();
        var service = new IntelligenceService(new FakeDataSource(null), client);

        var result = await service.PredictSeasonYieldAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(PredictionResultKind.NotFound, result.Kind);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task Converts_insufficient_data_status_into_explicit_result_kind()
    {
        var data = CreateData();
        var prediction = CreatePrediction("insufficient_data", null);
        var service = new IntelligenceService(
            new FakeDataSource(data),
            new FakeClient(IntelligenceCallResult.Success(prediction)));

        var result = await service.PredictSeasonYieldAsync(Guid.NewGuid(), data.SeasonId);

        Assert.Equal(PredictionResultKind.InsufficientData, result.Kind);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Preserves_timeout_as_gateway_specific_failure()
    {
        var data = CreateData();
        var service = new IntelligenceService(
            new FakeDataSource(data),
            new FakeClient(IntelligenceCallResult.Timeout("timed out")));

        var result = await service.PredictSeasonYieldAsync(Guid.NewGuid(), data.SeasonId);

        Assert.Equal(PredictionResultKind.Timeout, result.Kind);
        Assert.Equal("timed out", result.Error);
    }

    private static SeasonPredictionData CreateData() => new(
        Guid.NewGuid(),
        "Soja",
        "Cultivar A",
        120m,
        62m,
        []);

    private static YieldPredictionDto CreatePrediction(string status, decimal? predicted) => new(
        "v1",
        status,
        predicted,
        predicted,
        "expected-yield-baseline",
        "yield-v1.0",
        0,
        null,
        DateTime.UtcNow,
        "warning");

    private sealed class FakeDataSource(SeasonPredictionData? data) : IIntelligenceDataSource
    {
        public Task<SeasonPredictionData?> GetSeasonPredictionDataAsync(
            Guid organizationId,
            Guid seasonId,
            CancellationToken cancellationToken = default) => Task.FromResult(data);
    }

    private sealed class FakeClient : IIntelligenceClient
    {
        private readonly IntelligenceCallResult _result;

        public FakeClient(IntelligenceCallResult? result = null)
        {
            _result = result ?? IntelligenceCallResult.Unavailable("not configured");
        }

        public int CallCount { get; private set; }

        public Task<IntelligenceCallResult> PredictYieldAsync(
            SeasonPredictionData data,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }

        public Task<RasterIntelligenceCallResult> ProcessRasterAsync(
            RasterProcessingData data,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(RasterIntelligenceCallResult.Unavailable("not configured"));
    }
}
