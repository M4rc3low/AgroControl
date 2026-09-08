using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgroControl.Application.PrecisionAgriculture;
using Microsoft.Extensions.Configuration;

namespace AgroControl.Infrastructure.RemoteSensing;

public sealed class StacRemoteSceneDiscoveryClient : IRemoteSceneDiscoveryClient
{
    private const int MaxResponseBytes = 5 * 1024 * 1024;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReadOnlyDictionary<string, ProviderConfiguration> _providers;

    public StacRemoteSceneDiscoveryClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _providers = LoadProviders(configuration);
    }

    public IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders() =>
        _providers.Values
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new RemoteSceneDiscoveryProviderDto(item.Key, item.DisplayName, item.Enabled, item.Collections))
            .ToArray();

    public async Task<RemoteSceneDiscoveryCallResult> SearchAsync(
        RemoteSceneDiscoverySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
            return RemoteSceneDiscoveryCallResult.Validation(validation);

        if (!_providers.TryGetValue(request.Provider.Trim(), out var provider) || !provider.Enabled)
            return RemoteSceneDiscoveryCallResult.Validation("STAC provider is not configured or enabled.");

        if (!TryValidateProviderUri(provider.BaseUrl, out var baseUri))
            return RemoteSceneDiscoveryCallResult.Unavailable("STAC provider configuration is invalid.");

        var collection = string.IsNullOrWhiteSpace(request.Collection) ? null : request.Collection.Trim();
        if (collection is not null && provider.Collections.Count > 0 &&
            !provider.Collections.Contains(collection, StringComparer.OrdinalIgnoreCase))
            return RemoteSceneDiscoveryCallResult.Validation("Requested STAC collection is not allowed for this provider.");

        var searchUri = new Uri(EnsureTrailingSlash(baseUri), "search");
        using var message = BuildRequest(searchUri, request, collection);

        try
        {
            var client = _httpClientFactory.CreateClient("stac-discovery");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity)
                return RemoteSceneDiscoveryCallResult.Validation("STAC provider rejected the search request.");

            if (!response.IsSuccessStatusCode)
                return RemoteSceneDiscoveryCallResult.Unavailable($"STAC provider returned HTTP {(int)response.StatusCode}.");

            await using var payload = await ReadLimitedPayloadAsync(response, cancellationToken);
            if (payload is null)
                return RemoteSceneDiscoveryCallResult.InvalidPayload("STAC response exceeded the allowed payload size.");

            using var document = await JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken);
            var page = ParsePage(document.RootElement, provider, searchUri, request.MaxCloudCoveragePercent);
            return page is null
                ? RemoteSceneDiscoveryCallResult.InvalidPayload("STAC response did not contain a valid ItemCollection.")
                : RemoteSceneDiscoveryCallResult.Success(page);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RemoteSceneDiscoveryCallResult.Timeout("STAC provider timed out.");
        }
        catch (HttpRequestException)
        {
            return RemoteSceneDiscoveryCallResult.Unavailable("STAC provider is unavailable.");
        }
        catch (JsonException)
        {
            return RemoteSceneDiscoveryCallResult.InvalidPayload("STAC provider returned invalid JSON.");
        }
    }

    private static HttpRequestMessage BuildRequest(
        Uri searchUri,
        RemoteSceneDiscoverySearchRequest request,
        string? collection)
    {
        if (!string.IsNullOrWhiteSpace(request.ContinuationToken) &&
            TryDecodeContinuationToken(request.ContinuationToken!, out var query))
        {
            var builder = new UriBuilder(searchUri) { Query = query.TrimStart('?') };
            return new HttpRequestMessage(HttpMethod.Get, builder.Uri);
        }

        var geometry = JsonNode.Parse(request.GeometryGeoJson) ?? throw new JsonException("Geometry is required.");
        var body = new JsonObject
        {
            ["intersects"] = geometry,
            ["datetime"] = $"{NormalizeUtc(request.FromUtc):O}/{NormalizeUtc(request.ToUtc):O}",
            ["limit"] = request.PageSize
        };

        if (collection is not null)
            body["collections"] = new JsonArray(collection);

        return new HttpRequestMessage(HttpMethod.Post, searchUri)
        {
            Content = JsonContent.Create(body)
        };
    }

    private static RemoteSceneDiscoveryPageDto? ParsePage(
        JsonElement root,
        ProviderConfiguration provider,
        Uri searchUri,
        decimal? maxCloudCoveragePercent)
    {
        if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
            return null;

        var items = new List<RemoteSceneDiscoveryItemDto>();
        foreach (var feature in features.EnumerateArray())
        {
            var item = ParseItem(feature, provider);
            if (item is null)
                continue;
            if (maxCloudCoveragePercent is not null && item.CloudCoveragePercent is not null &&
                item.CloudCoveragePercent > maxCloudCoveragePercent)
                continue;
            items.Add(item);
        }

        return new RemoteSceneDiscoveryPageDto(items, ParseContinuationToken(root, searchUri));
    }

    private static RemoteSceneDiscoveryItemDto? ParseItem(JsonElement feature, ProviderConfiguration provider)
    {
        if (!TryGetString(feature, "id", out var externalId))
            return null;

        var collection = TryGetString(feature, "collection", out var collectionValue) ? collectionValue : "unknown";
        if (!feature.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
            return null;

        if (!TryGetDateTime(properties, "datetime", out var acquiredAt) &&
            !TryGetDateTime(properties, "start_datetime", out acquiredAt))
            return null;

        var cloudCoverage = TryGetDecimal(properties, "eo:cloud_cover");
        var gsd = TryGetDecimal(properties, "gsd");
        var platform = TryGetString(properties, "platform", out var platformValue) ? platformValue : null;
        var constellation = TryGetString(properties, "constellation", out var constellationValue) ? constellationValue : null;

        string? geometryGeoJson = null;
        if (feature.TryGetProperty("geometry", out var geometry) && geometry.ValueKind is JsonValueKind.Object)
            geometryGeoJson = geometry.GetRawText();

        IReadOnlyList<double>? bbox = null;
        if (feature.TryGetProperty("bbox", out var bboxElement) && bboxElement.ValueKind == JsonValueKind.Array)
        {
            var values = bboxElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out _))
                .Select(item => item.GetDouble())
                .ToArray();
            if (values.Length is 4 or 6)
                bbox = values;
        }

        var assets = new List<RemoteSceneDiscoveryAssetDto>();
        if (feature.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var assetProperty in assetsElement.EnumerateObject())
            {
                if (assetProperty.Value.ValueKind != JsonValueKind.Object ||
                    !TryGetString(assetProperty.Value, "href", out var href) ||
                    !TryNormalizeAssetReference(href, out var safeHref))
                    continue;

                var mediaType = TryGetString(assetProperty.Value, "type", out var typeValue) ? typeValue : null;
                var roles = new List<string>();
                if (assetProperty.Value.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                {
                    roles.AddRange(rolesElement.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))!
                        .Select(item => item!));
                }

                assets.Add(new RemoteSceneDiscoveryAssetDto(
                    assetProperty.Name,
                    safeHref,
                    mediaType,
                    roles,
                    IsRasterCandidate(safeHref, mediaType, roles)));
            }
        }

        return new RemoteSceneDiscoveryItemDto(
            provider.Key,
            collection,
            externalId,
            NormalizeUtc(acquiredAt),
            geometryGeoJson,
            bbox,
            cloudCoverage,
            gsd,
            platform,
            constellation,
            assets);
    }

    private static bool IsRasterCandidate(string href, string? mediaType, IReadOnlyList<string> roles)
    {
        if (roles.Count > 0 && !roles.Contains("data", StringComparer.OrdinalIgnoreCase))
            return false;

        var mediaLooksRaster = !string.IsNullOrWhiteSpace(mediaType) &&
            (mediaType.StartsWith("image/tiff", StringComparison.OrdinalIgnoreCase) ||
             mediaType.StartsWith("image/vnd.stac.geotiff", StringComparison.OrdinalIgnoreCase));

        var path = href.Split('?', '#')[0];
        var extensionLooksRaster = path.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase);
        return mediaLooksRaster || extensionLooksRaster;
    }

    private static bool TryNormalizeAssetReference(string value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo))
            return false;

        if (uri.Scheme is not ("https" or "http" or "s3" or "gs"))
            return false;

        if (uri.Scheme is "http" or "https")
        {
            var builder = new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty };
            normalized = builder.Uri.ToString();
        }
        else
        {
            normalized = uri.GetLeftPart(UriPartial.Path);
        }

        return normalized.Length <= 2048;
    }

    private static string? ParseContinuationToken(JsonElement root, Uri searchUri)
    {
        if (!root.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var link in links.EnumerateArray())
        {
            if (!TryGetString(link, "rel", out var rel) || !rel.Equals("next", StringComparison.OrdinalIgnoreCase) ||
                !TryGetString(link, "href", out var href))
                continue;

            if (!Uri.TryCreate(searchUri, href, out var nextUri) ||
                !SameOrigin(searchUri, nextUri) ||
                !nextUri.AbsolutePath.Equals(searchUri.AbsolutePath, StringComparison.Ordinal))
                continue;

            if (string.IsNullOrWhiteSpace(nextUri.Query) || nextUri.Query.Length > 2048)
                continue;

            return EncodeContinuationToken(nextUri.Query);
        }

        return null;
    }

    private static string? ValidateRequest(RemoteSceneDiscoverySearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider)) return "STAC provider is required.";
        if (string.IsNullOrWhiteSpace(request.GeometryGeoJson)) return "Search geometry is required.";
        if (request.PageSize is < 1 or > 100) return "Page size must be between 1 and 100.";
        if (NormalizeUtc(request.FromUtc) >= NormalizeUtc(request.ToUtc)) return "Search start must be before search end.";
        if (NormalizeUtc(request.ToUtc) - NormalizeUtc(request.FromUtc) > TimeSpan.FromDays(366 * 5))
            return "Search period cannot exceed five years.";
        if (request.MaxCloudCoveragePercent is < 0m or > 100m)
            return "Maximum cloud coverage must be between 0 and 100 percent.";
        if (!string.IsNullOrWhiteSpace(request.ContinuationToken) &&
            !TryDecodeContinuationToken(request.ContinuationToken!, out _))
            return "Continuation token is invalid.";
        try
        {
            using var geometry = JsonDocument.Parse(request.GeometryGeoJson);
            if (geometry.RootElement.ValueKind != JsonValueKind.Object)
                return "Search geometry must be a GeoJSON object.";
        }
        catch (JsonException)
        {
            return "Search geometry is invalid JSON.";
        }
        return null;
    }

    private static async Task<MemoryStream?> ReadLimitedPayloadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength is > MaxResponseBytes)
            return null;

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        var target = new MemoryStream();
        var buffer = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0) break;
            total += read;
            if (total > MaxResponseBytes)
            {
                await target.DisposeAsync();
                return null;
            }
            await target.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        target.Position = 0;
        return target;
    }

    private static IReadOnlyDictionary<string, ProviderConfiguration> LoadProviders(IConfiguration configuration)
    {
        var result = new Dictionary<string, ProviderConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in configuration.GetSection("RemoteSensing:Stac:Providers").GetChildren())
        {
            var key = section.Key.Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;
            var collections = section.GetSection("Collections").GetChildren()
                .Select(item => item.Value?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            result[key] = new ProviderConfiguration(
                key,
                section["DisplayName"]?.Trim() is { Length: > 0 } displayName ? displayName : key,
                section["BaseUrl"]?.Trim() ?? string.Empty,
                bool.TryParse(section["Enabled"], out var enabled) && enabled,
                collections);
        }
        return result;
    }

    private static bool TryValidateProviderUri(string value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || !string.IsNullOrEmpty(parsed.UserInfo))
            return false;
        if (parsed.Scheme == Uri.UriSchemeHttps)
        {
            uri = parsed;
            return true;
        }
        if (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback)
        {
            uri = parsed;
            return true;
        }
        return false;
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");

    private static bool SameOrigin(Uri left, Uri right) =>
        left.Scheme.Equals(right.Scheme, StringComparison.OrdinalIgnoreCase) &&
        left.Host.Equals(right.Host, StringComparison.OrdinalIgnoreCase) &&
        left.Port == right.Port;

    private static bool TryGetString(JsonElement element, string property, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(property, out var child) || child.ValueKind != JsonValueKind.String)
            return false;
        value = child.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
    }

    private static bool TryGetDateTime(JsonElement element, string property, out DateTime value)
    {
        value = default;
        return element.TryGetProperty(property, out var child) && child.ValueKind == JsonValueKind.String &&
               DateTime.TryParse(child.GetString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out value);
    }

    private static decimal? TryGetDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var child) || child.ValueKind != JsonValueKind.Number)
            return null;
        return child.TryGetDecimal(out var value) ? value : null;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string EncodeContinuationToken(string query)
    {
        var bytes = Encoding.UTF8.GetBytes(query);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeContinuationToken(string token, out string query)
    {
        query = string.Empty;
        if (token.Length is < 1 or > 4096) return false;
        try
        {
            var padded = token.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            query = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return query.StartsWith('?') && query.Length <= 2048;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record ProviderConfiguration(
        string Key,
        string DisplayName,
        string BaseUrl,
        bool Enabled,
        IReadOnlyList<string> Collections);
}
