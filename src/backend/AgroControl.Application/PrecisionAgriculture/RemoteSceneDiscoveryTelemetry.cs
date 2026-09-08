using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgroControl.Application.PrecisionAgriculture;

public static class RemoteSceneDiscoveryTelemetry
{
    public const string MeterName = "AgroControl.RemoteSceneDiscovery";
    private static readonly Meter Meter = new(MeterName, "0.19.0");

    public static readonly Counter<long> Searches = Meter.CreateCounter<long>(
        "agrocontrol.stac.searches", unit: "search", description: "STAC discovery searches.");

    public static readonly Histogram<double> SearchDurationMs = Meter.CreateHistogram<double>(
        "agrocontrol.stac.search.duration", unit: "ms", description: "STAC discovery search latency.");

    public static readonly Histogram<long> SearchResultCount = Meter.CreateHistogram<long>(
        "agrocontrol.stac.search.results", unit: "item", description: "Normalized STAC items returned per search.");

    public static readonly Counter<long> Imports = Meter.CreateCounter<long>(
        "agrocontrol.stac.imports", unit: "import", description: "STAC item import attempts.");

    public static void RecordSearch(string provider, RemoteSceneDiscoveryResultKind kind, TimeSpan elapsed, int resultCount)
    {
        var tags = new TagList
        {
            { "provider", NormalizeProvider(provider) },
            { "outcome", kind.ToString() }
        };
        Searches.Add(1, tags);
        SearchDurationMs.Record(elapsed.TotalMilliseconds, tags);
        if (kind == RemoteSceneDiscoveryResultKind.Success)
            SearchResultCount.Record(resultCount, new TagList { { "provider", NormalizeProvider(provider) } });
    }

    public static void RecordImport(string provider, RemoteSceneDiscoveryResultKind kind)
    {
        Imports.Add(1, new TagList
        {
            { "provider", NormalizeProvider(provider) },
            { "outcome", kind.ToString() }
        });
    }

    private static string NormalizeProvider(string provider) =>
        string.IsNullOrWhiteSpace(provider) ? "unknown" : provider.Trim().ToLowerInvariant();
}
