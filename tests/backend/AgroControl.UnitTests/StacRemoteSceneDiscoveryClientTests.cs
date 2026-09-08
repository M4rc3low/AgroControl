using System.Net;
using System.Text;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Infrastructure.RemoteSensing;
using Microsoft.Extensions.Configuration;

namespace AgroControl.UnitTests;

public sealed class StacRemoteSceneDiscoveryClientTests
{
    [Fact]
    public async Task Search_normalizes_item_strips_asset_query_and_ignores_cross_origin_next_link()
    {
        const string payload = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "id": "S2-TEST-001",
              "collection": "sentinel-2-l2a",
              "bbox": [-50.0, -10.0, -49.0, -9.0],
              "geometry": { "type": "Polygon", "coordinates": [[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]] },
              "properties": {
                "datetime": "2026-09-08T12:00:00Z",
                "eo:cloud_cover": 12.5,
                "gsd": 10,
                "platform": "sentinel-2a",
                "constellation": "sentinel-2"
              },
              "assets": {
                "red": {
                  "href": "https://assets.example.test/red.tif?X-Amz-Signature=secret-value",
                  "type": "image/tiff; application=geotiff",
                  "roles": ["data"]
                },
                "thumbnail": {
                  "href": "https://assets.example.test/thumb.jpg",
                  "type": "image/jpeg",
                  "roles": ["thumbnail"]
                }
              }
            }
          ],
          "links": [
            { "rel": "next", "href": "https://evil.example.test/v1/search?token=stolen" }
          ]
        }
        """;

        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, payload));
        var client = CreateClient(handler);
        var result = await client.SearchAsync(new RemoteSceneDiscoverySearchRequest(
            "earth-search",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 8, 23, 59, 59, DateTimeKind.Utc),
            "{\"type\":\"Polygon\",\"coordinates\":[[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]]}",
            20,
            "sentinel-2-l2a",
            20m));

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("S2-TEST-001", item.ExternalId);
        Assert.Equal(12.5m, item.CloudCoveragePercent);
        Assert.Equal(10m, item.SpatialResolutionMeters);
        Assert.Null(result.Value.ContinuationToken);

        var red = Assert.Single(item.Assets.Where(asset => asset.Key == "red"));
        Assert.True(red.IsRasterCandidate);
        Assert.Equal("https://assets.example.test/red.tif", red.Href);
        Assert.DoesNotContain("secret-value", red.Href, StringComparison.Ordinal);

        var thumbnail = Assert.Single(item.Assets.Where(asset => asset.Key == "thumbnail"));
        Assert.False(thumbnail.IsRasterCandidate);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://catalog.example.test/v1/search", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task Search_accepts_only_same_origin_same_path_continuation_query()
    {
        const string firstPayload = """
        { "type":"FeatureCollection", "features":[], "links":[{"rel":"next","href":"https://catalog.example.test/v1/search?token=page-2"}] }
        """;
        const string secondPayload = """
        { "type":"FeatureCollection", "features":[], "links":[] }
        """;

        var handler = new RecordingHandler(request =>
            request.RequestUri!.Query.Contains("page-2", StringComparison.Ordinal)
                ? JsonResponse(HttpStatusCode.OK, secondPayload)
                : JsonResponse(HttpStatusCode.OK, firstPayload));
        var client = CreateClient(handler);
        var request = new RemoteSceneDiscoverySearchRequest(
            "earth-search", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            "{\"type\":\"Polygon\",\"coordinates\":[[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]]}",
            10, "sentinel-2-l2a");

        var first = await client.SearchAsync(request);
        Assert.True(first.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(first.Value!.ContinuationToken));

        var second = await client.SearchAsync(request with { ContinuationToken = first.Value.ContinuationToken });
        Assert.True(second.Succeeded);
        Assert.Null(second.Value!.ContinuationToken);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("catalog.example.test", handler.Requests[1].RequestUri!.Host);
        Assert.Equal("/v1/search", handler.Requests[1].RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task GetItem_maps_provider_404_to_not_found()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        var result = await client.GetItemAsync("earth-search", "sentinel-2-l2a", "missing-item");

        Assert.False(result.Succeeded);
        Assert.Equal(RemoteSceneDiscoveryCallErrorKind.NotFound, result.ErrorKind);
        Assert.Single(handler.Requests);
        Assert.Equal("/v1/collections/sentinel-2-l2a/items/missing-item", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Disabled_or_unknown_provider_is_rejected_before_network_call()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = CreateClient(handler);

        var result = await client.SearchAsync(new RemoteSceneDiscoverySearchRequest(
            "unknown", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            "{\"type\":\"Polygon\",\"coordinates\":[[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]]}", 10));

        Assert.False(result.Succeeded);
        Assert.Equal(RemoteSceneDiscoveryCallErrorKind.Validation, result.ErrorKind);
        Assert.Empty(handler.Requests);
    }

    private static StacRemoteSceneDiscoveryClient CreateClient(RecordingHandler handler)
    {
        var values = new Dictionary<string, string?>
        {
            ["RemoteSensing:Stac:Providers:earth-search:DisplayName"] = "Earth Search",
            ["RemoteSensing:Stac:Providers:earth-search:BaseUrl"] = "https://catalog.example.test/v1",
            ["RemoteSensing:Stac:Providers:earth-search:Enabled"] = "true",
            ["RemoteSensing:Stac:Providers:earth-search:Collections:0"] = "sentinel-2-l2a"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new StacRemoteSceneDiscoveryClient(new SingleClientFactory(new HttpClient(handler)), configuration);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string payload) => new(status)
    {
        Content = new StringContent(payload, Encoding.UTF8, "application/json")
    };

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
