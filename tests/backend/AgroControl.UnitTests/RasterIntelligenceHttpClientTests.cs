using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroControl.Application.Intelligence;
using AgroControl.Infrastructure.Intelligence;

namespace AgroControl.UnitTests;

public sealed class RasterIntelligenceHttpClientTests
{
    [Fact]
    public async Task Raster_client_sends_geometry_as_json_and_maps_statistics()
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
                    status = "ok",
                    metadata = new { crs = "EPSG:4326", width = 10, height = 10, nodata = -9999m, resolution_x = 10m, resolution_y = 10m },
                    results = new[]
                    {
                        new
                        {
                            key = "field",
                            statistics = new
                            {
                                minimum = 0.2m, maximum = 0.8m, mean = 0.6m, median = 0.61m,
                                standard_deviation = 0.1m, valid_coverage_percent = 95m, sample_count = 80L
                            }
                        }
                    }
                })
            };
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://intelligence.test") };
        var client = new IntelligenceHttpClient(httpClient);
        const string polygon = "{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]}";

        var result = await client.ProcessRasterAsync(new RasterProcessingData(
            "/data/ndvi.tif", "EPSG:4326", 1, [new RasterTargetData("field", polygon)]));

        Assert.True(result.Succeeded);
        Assert.Equal(0.6m, result.Value!.Results[0].Mean);
        using var json = JsonDocument.Parse(requestBody!);
        Assert.Equal(JsonValueKind.Object, json.RootElement.GetProperty("targets")[0].GetProperty("geometry").ValueKind);
        Assert.Equal("v1", json.RootElement.GetProperty("contract_version").GetString());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }
}
