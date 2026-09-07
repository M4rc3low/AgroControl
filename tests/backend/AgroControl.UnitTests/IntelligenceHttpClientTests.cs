using System.Net;
using System.Net.Http.Json;
using AgroControl.Application.Intelligence;
using AgroControl.Infrastructure.Intelligence;

namespace AgroControl.UnitTests;

public sealed class IntelligenceHttpClientTests
{
    [Fact]
    public async Task Client_uses_snake_case_contract_and_maps_success_response()
    {
        string? requestBody = null;
        var handler = new StubHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    contract_version = "v1",
                    status = "limited",
                    predicted_yield_per_hectare = 61.5m,
                    baseline_yield_per_hectare = 61.5m,
                    model_kind = "expected-yield-baseline",
                    model_version = "yield-v1.0",
                    sample_count = 0,
                    metrics = (object?)null,
                    generated_at_utc = DateTime.UtcNow,
                    warning = "limited data"
                })
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://intelligence.test")
        };
        var client = new IntelligenceHttpClient(httpClient);
        var data = new SeasonPredictionData(
            Guid.NewGuid(),
            "Soja",
            "A",
            120m,
            61.5m,
            []);

        var result = await client.PredictYieldAsync(data);

        Assert.True(result.Succeeded);
        Assert.Equal(61.5m, result.Value!.PredictedYieldPerHectare);
        Assert.Contains("\"crop_name\":", requestBody);
        Assert.Contains("\"historical_samples\":", requestBody);
        Assert.DoesNotContain("cropName", requestBody);
    }

    [Fact]
    public async Task Client_maps_validation_response_without_throwing()
    {
        var handler = new StubHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent("invalid payload")
            }));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://intelligence.test")
        };
        var client = new IntelligenceHttpClient(httpClient);
        var data = new SeasonPredictionData(Guid.NewGuid(), "Soja", null, 1m, null, []);

        var result = await client.PredictYieldAsync(data);

        Assert.False(result.Succeeded);
        Assert.Equal(IntelligenceCallErrorKind.Validation, result.ErrorKind);
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }
}
