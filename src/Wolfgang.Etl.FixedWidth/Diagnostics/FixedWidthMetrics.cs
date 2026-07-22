using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Wolfgang.Etl.FixedWidth.Diagnostics;

/// <summary>
/// The <see cref="System.Diagnostics.Metrics.Meter"/> and instruments emitted by
/// <see cref="FixedWidthExtractor{TRecord}"/> and <see cref="FixedWidthLoader{TRecord}"/> (#30).
/// </summary>
/// <remarks>
/// <para>
/// Subscribe by the meter name <c>Wolfgang.Etl.FixedWidth</c> (see <see cref="MeterName"/>) — for
/// example with OpenTelemetry's <c>AddMeter("Wolfgang.Etl.FixedWidth")</c> or a raw
/// <see cref="MeterListener"/>. When no listener is registered every instrument is a no-op, so there is
/// no measurable overhead in the default case.
/// </para>
/// <para>
/// Every measurement is tagged with <c>etl.operation</c> (<c>extract</c> or <c>load</c>) and
/// <c>etl.record_type</c> (<c>typeof(TRecord).Name</c>).
/// </para>
/// </remarks>
internal static class FixedWidthMetrics
{
    /// <summary>The meter name callers subscribe to.</summary>
    internal const string MeterName = "Wolfgang.Etl.FixedWidth";

    internal const string OperationTagName = "etl.operation";

    internal const string RecordTypeTagName = "etl.record_type";

    internal const string ExtractOperation = "extract";

    internal const string LoadOperation = "load";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Total items successfully extracted.</summary>
    internal static readonly Counter<long> ItemsExtracted = Meter.CreateCounter<long>
    (
        "wolfgang.etl.fixedwidth.items.extracted",
        unit: "{item}",
        description: "Total items successfully extracted."
    );

    /// <summary>Total items successfully loaded.</summary>
    internal static readonly Counter<long> ItemsLoaded = Meter.CreateCounter<long>
    (
        "wolfgang.etl.fixedwidth.items.loaded",
        unit: "{item}",
        description: "Total items successfully loaded."
    );

    /// <summary>Total items skipped via the skip budget.</summary>
    internal static readonly Counter<long> ItemsSkipped = Meter.CreateCounter<long>
    (
        "wolfgang.etl.fixedwidth.items.skipped",
        unit: "{item}",
        description: "Total items skipped via the skip budget."
    );

    /// <summary>Total physical lines read, including blank and skipped lines.</summary>
    internal static readonly Counter<long> LinesRead = Meter.CreateCounter<long>
    (
        "wolfgang.etl.fixedwidth.lines.read",
        unit: "{line}",
        description: "Total physical lines read, including blank and skipped lines."
    );

    /// <summary>Duration of an extract or load operation, in milliseconds.</summary>
    internal static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>
    (
        "wolfgang.etl.fixedwidth.operation.duration",
        unit: "ms",
        description: "Duration of an extract or load operation."
    );


    /// <summary>
    /// Builds the tag set shared by every measurement of a single operation.
    /// </summary>
    internal static TagList CreateTags(string operation, Type recordType)
        => new()
        {
            { OperationTagName, operation },
            { RecordTypeTagName, recordType.Name },
        };


    // The per-line / per-record helpers below are called inside the extract and load hot loops. Each
    // guards its instrument with Instrument.Enabled so that, when no MeterListener is subscribed (the
    // default), the JIT skips the Add call and its argument marshalling entirely — the measurement
    // machinery is only touched once a consumer opts in by subscribing. Instrument.Enabled is a cheap
    // volatile field read; an unguarded Counter.Add would otherwise run a non-inlined call per item.

    /// <summary>Records one successfully extracted item, if a listener is subscribed.</summary>
    internal static void RecordExtracted(in TagList tags)
    {
        if (ItemsExtracted.Enabled)
        {
            ItemsExtracted.Add(1, tags);
        }
    }


    /// <summary>Records one successfully loaded item, if a listener is subscribed.</summary>
    internal static void RecordLoaded(in TagList tags)
    {
        if (ItemsLoaded.Enabled)
        {
            ItemsLoaded.Add(1, tags);
        }
    }


    /// <summary>Records one skipped item, if a listener is subscribed.</summary>
    internal static void RecordSkipped(in TagList tags)
    {
        if (ItemsSkipped.Enabled)
        {
            ItemsSkipped.Add(1, tags);
        }
    }


    /// <summary>Records one physical line read, if a listener is subscribed.</summary>
    internal static void RecordLineRead(in TagList tags)
    {
        if (LinesRead.Enabled)
        {
            LinesRead.Add(1, tags);
        }
    }


    /// <summary>
    /// Begins timing an operation. Dispose the returned scope — the <c>using</c> the extractor and loader
    /// wrap it in records <see cref="OperationDuration"/> when the operation completes, ends early, or
    /// throws.
    /// </summary>
    internal static DurationScope MeasureDuration(TagList tags) => new(tags);


    /// <summary>
    /// Records <see cref="OperationDuration"/> for the tags it was created with when disposed. Allocated
    /// once per operation (not per record), so its cost is negligible.
    /// </summary>
    internal sealed class DurationScope : IDisposable
    {
        private readonly TagList _tags;
        private readonly long _startTimestamp;
        private bool _disposed;


        internal DurationScope(TagList tags)
        {
            _tags = tags;
            _startTimestamp = Stopwatch.GetTimestamp();
        }


        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!OperationDuration.Enabled)
            {
                return;
            }

            var elapsedMs = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
            OperationDuration.Record(elapsedMs, _tags);
        }
    }
}
