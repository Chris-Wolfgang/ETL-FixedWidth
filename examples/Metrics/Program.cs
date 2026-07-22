// ---------------------------------------------------------------------------
// Metrics Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the built-in System.Diagnostics.Metrics
// instrumentation (issue #30). The extractor and loader emit counters and a
// duration histogram from the meter "Wolfgang.Etl.FixedWidth" — with no
// configuration on the library side.
//
// In production you would subscribe with OpenTelemetry:
//
//     builder.Services.AddOpenTelemetry()
//         .WithMetrics(m => m.AddMeter("Wolfgang.Etl.FixedWidth"));
//
// so the metrics flow to Prometheus, Grafana, Application Insights, etc. Here we
// use a raw MeterListener so the example has no external dependencies.
//
// Key concepts covered:
//   - Subscribing to the "Wolfgang.Etl.FixedWidth" meter with a MeterListener
//   - The emitted instruments (items.extracted / items.loaded / items.skipped /
//     lines.read / operation.duration) and their etl.operation / etl.record_type
//     tags
//   - Metrics are a no-op when no listener is registered — zero overhead by default
// ---------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// ---------------------------------------------------------------------------
// Step 1: Subscribe to the library's meter.
//
// The MeterListener enables every instrument published by the
// "Wolfgang.Etl.FixedWidth" meter and accumulates each measurement, keyed by
// instrument name plus its etl.operation tag so extract and load stay distinct.
// ---------------------------------------------------------------------------

var totals = new SortedDictionary<string, double>(StringComparer.Ordinal);

using var listener = new MeterListener();
listener.InstrumentPublished = (instrument, l) =>
{
    if (instrument.Meter.Name == "Wolfgang.Etl.FixedWidth")
    {
        l.EnableMeasurementEvents(instrument);
    }
};
listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Accumulate(instrument.Name, value, tags));
listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Accumulate(instrument.Name, value, tags));
listener.Start();

// ---------------------------------------------------------------------------
// Step 2: Run an extract -> load round trip.
//
// SkipItemCount = 1 skips the first record so the items.skipped counter is
// non-zero and you can see it in the output.
// ---------------------------------------------------------------------------

var input =
    "Alice     Smith     030\n" +
    "Bob       Jones     025\n" +
    "Carol     White     035\n";

var extractor = new FixedWidthExtractor<Person>(new StringReader(input)) { SkipItemCount = 1 };

var people = new List<Person>();
await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
{
    people.Add(person);
}

var writer = new StringWriter();
var loader = new FixedWidthLoader<Person>(writer);
await loader.LoadAsync(ToAsyncEnumerable(people), CancellationToken.None);

// ---------------------------------------------------------------------------
// Step 3: Report the collected metrics.
// ---------------------------------------------------------------------------

Console.WriteLine("Metrics from meter 'Wolfgang.Etl.FixedWidth':");
Console.WriteLine(new string('-', 52));
foreach (var pair in totals)
{
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  {0,-48} {1:0.##}", pair.Key, pair.Value));
}

// ---------------------------------------------------------------------------
// Accumulate a measurement into the totals map, tagging the key with the
// operation (extract/load) so the two ends of the pipeline are distinct.
// ---------------------------------------------------------------------------

void Accumulate(string instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
{
    var operation = string.Empty;
    foreach (var tag in tags)
    {
        if (string.Equals(tag.Key, "etl.operation", StringComparison.Ordinal))
        {
            operation = tag.Value?.ToString() ?? string.Empty;
        }
    }

    var key = $"{instrument} [{operation}]";
    totals[key] = totals.TryGetValue(key, out var current) ? current + value : value;
}

// ---------------------------------------------------------------------------
// Adapts a synchronous list to the IAsyncEnumerable the loader consumes.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // synchronous sequence — no await needed
static async IAsyncEnumerable<Person> ToAsyncEnumerable(IEnumerable<Person> items)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
#pragma warning restore CS1998

// ---------------------------------------------------------------------------
// Person — FirstName(10) + LastName(10) + Age(3), a 23-character line.
// ---------------------------------------------------------------------------

/// <summary>A person record used to exercise the metrics instruments.</summary>
public class Person
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
