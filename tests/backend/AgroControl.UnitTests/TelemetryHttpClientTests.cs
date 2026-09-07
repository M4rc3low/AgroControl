using System.Net;
using System.Text;
using AgroControl.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;

namespace AgroControl.UnitTests;

public sealed class TelemetryHttpClientTests
{
    [Fact]
    public async Task Client_scopes_request_to_organization_and_sends_internal_key()
    {
        var organizationId = Guid.NewGuid();
        HttpRequestMessage? captured = null;
        var handler = new DelegateHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://telemetry.test") };
        var configuration = new ConfigurationManager();
        configuration["Telemetry:InternalApiKey"] = "unit-test-telemetry-internal-key-123456789";
        var client = new TelemetryHttpClient(http, configuration);

        await client.ListDevicesAsync(organizationId, 25);

        Assert.NotNull(captured);
        Assert.Contains(organizationId.ToString(), captured!.RequestUri!.AbsolutePath);
        Assert.Equal("unit-test-telemetry-internal-key-123456789", captured.Headers.GetValues("X-AgroControl-Internal-Key").Single());
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }
}
