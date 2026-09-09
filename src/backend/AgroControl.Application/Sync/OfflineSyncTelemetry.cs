using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgroControl.Application.Sync;

public static class OfflineSyncTelemetry
{
    public const string MeterName = "AgroControl.OfflineSync";
    private static readonly Meter Meter = new(MeterName, "0.21.0");

    public static readonly Counter<long> Batches = Meter.CreateCounter<long>(
        "agrocontrol.sync.batches", unit: "batch", description: "Offline synchronization batches.");

    public static readonly Counter<long> Operations = Meter.CreateCounter<long>(
        "agrocontrol.sync.operations", unit: "operation", description: "Offline mutation results.");

    public static readonly Counter<long> Retries = Meter.CreateCounter<long>(
        "agrocontrol.sync.retries", unit: "operation", description: "Offline operations marked safe to retry.");

    public static readonly Counter<long> InvalidCursors = Meter.CreateCounter<long>(
        "agrocontrol.sync.invalid_cursors", unit: "cursor", description: "Invalid or expired offline pull cursors.");

    public static readonly Counter<long> AccessRevocations = Meter.CreateCounter<long>(
        "agrocontrol.sync.access_revocations", unit: "event", description: "Farm access revocations detected during synchronization.");

    public static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>(
        "agrocontrol.sync.duration", unit: "ms", description: "Offline synchronization batch latency.");

    public static readonly Histogram<long> BatchSize = Meter.CreateHistogram<long>(
        "agrocontrol.sync.batch_size", unit: "operation", description: "Offline synchronization batch size.");

    public static void RecordBatch(string direction, string outcome, long startedTimestamp, int batchSize)
    {
        var tags = new TagList
        {
            { "direction", NormalizeDirection(direction) },
            { "outcome", NormalizeOutcome(outcome) }
        };
        Batches.Add(1, tags);
        DurationMs.Record(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, tags);
        BatchSize.Record(batchSize, new TagList { { "direction", NormalizeDirection(direction) } });
    }

    public static void RecordOperation(string entityKind, string outcome, bool replayed)
    {
        Operations.Add(1, new TagList
        {
            { "entity", NormalizeEntityKind(entityKind) },
            { "outcome", NormalizeOutcome(outcome) },
            { "replayed", replayed }
        });
    }

    public static void RecordRetry() => Retries.Add(1);

    public static void RecordInvalidCursor() => InvalidCursors.Add(1);

    public static void RecordAccessRevocation(string direction) =>
        AccessRevocations.Add(1, new TagList { { "direction", NormalizeDirection(direction) } });

    private static string NormalizeDirection(string direction) =>
        direction.Equals("pull", StringComparison.OrdinalIgnoreCase) ? "pull" : "push";

    private static string NormalizeEntityKind(string entityKind) => entityKind.Trim().ToLowerInvariant() switch
    {
        "field" => "field",
        "season" => "season",
        _ => "other"
    };

    private static string NormalizeOutcome(string outcome) => outcome.Trim().ToLowerInvariant() switch
    {
        "applied" => "applied",
        "conflict" => "conflict",
        "validationerror" or "validation" => "validation",
        "forbidden" => "forbidden",
        "notfound" => "not_found",
        "retryableerror" or "retryable" => "retryable",
        "invalid_cursor" => "invalid_cursor",
        "completed" => "completed",
        _ => "other"
    };
}
