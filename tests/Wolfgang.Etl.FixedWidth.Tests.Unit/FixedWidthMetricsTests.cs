using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Diagnostics;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Verifies the OpenTelemetry-compatible metrics emitted by the extractor and loader (#30). Each test
/// captures measurements from a <see cref="MeterListener"/> on the <c>Wolfgang.Etl.FixedWidth</c> meter
/// and filters by the <c>etl.record_type</c> tag of a test-private record type, so measurements from
/// other (parallel) test classes on the same process-global meter cannot pollute the counts.
/// </summary>
public sealed class FixedWidthMetricsTests
{
    private const string MeterName = "Wolfgang.Etl.FixedWidth";
    private const string RecordTypeName = nameof(MetricsSample);


    [ExcludeFromCodeCoverage]
    private sealed record MetricsSample
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }
    }


    [Fact]
    public async Task Extraction_emits_extracted_lines_and_duration_tagged_as_extract()
    {
        using var capture = new MetricCapture();

        var extractor = new FixedWidthExtractor<MetricsSample>
        (
            new StringReader(Content(("Alice", "Smith", 30), ("Bob", "Jones", 25), ("Carol", "White", 35)))
        );

        await DrainAsync(extractor);

        Assert.Equal(3, capture.Sum("wolfgang.etl.fixedwidth.items.extracted"));
        Assert.Equal(3, capture.Sum("wolfgang.etl.fixedwidth.lines.read"));

        var durations = capture.Measurements("wolfgang.etl.fixedwidth.operation.duration");
        Assert.Single(durations);
        Assert.True(durations[0].Value >= 0);

        // Every measurement carries the operation + record-type tags.
        Assert.All(capture.All, m => Assert.Equal("extract", m.Tags["etl.operation"]));
        Assert.All(capture.All, m => Assert.Equal(RecordTypeName, m.Tags["etl.record_type"]));
    }


    [Fact]
    public async Task Skipped_items_emit_items_skipped()
    {
        using var capture = new MetricCapture();

        var extractor = new FixedWidthExtractor<MetricsSample>
        (
            new StringReader(Content(("Alice", "Smith", 30), ("Bob", "Jones", 25), ("Carol", "White", 35)))
        )
        {
            SkipItemCount = 2,
        };

        await DrainAsync(extractor);

        Assert.Equal(2, capture.Sum("wolfgang.etl.fixedwidth.items.skipped"));
        Assert.Equal(1, capture.Sum("wolfgang.etl.fixedwidth.items.extracted"));
    }


    [Fact]
    public async Task Loading_emits_loaded_and_duration_tagged_as_load()
    {
        using var capture = new MetricCapture();

        var writer = new StringWriter();
        var loader = new FixedWidthLoader<MetricsSample>(writer);

        await loader.LoadAsync(SampleAsync(), CancellationToken.None);

        Assert.Equal(2, capture.Sum("wolfgang.etl.fixedwidth.items.loaded"));

        var durations = capture.Measurements("wolfgang.etl.fixedwidth.operation.duration");
        Assert.Single(durations);
        Assert.True(durations[0].Value >= 0);

        Assert.All(capture.All, m => Assert.Equal("load", m.Tags["etl.operation"]));
        Assert.All(capture.All, m => Assert.Equal(RecordTypeName, m.Tags["etl.record_type"]));
    }


    [Fact]
    public void Duration_scope_dispose_is_idempotent()
    {
        using var capture = new MetricCapture();

        var tags = FixedWidthMetrics.CreateTags(FixedWidthMetrics.ExtractOperation, typeof(MetricsSample));
        var scope = FixedWidthMetrics.MeasureDuration(tags);

        scope.Dispose();
        scope.Dispose();   // second dispose is a no-op — the duration is recorded exactly once

        Assert.Single(capture.Measurements("wolfgang.etl.fixedwidth.operation.duration"));
    }


    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static async Task DrainAsync(FixedWidthExtractor<MetricsSample> extractor)
    {
        await foreach (var _ in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
        {
            // Enumerating to completion disposes the extractor's duration scope, recording the histogram.
        }
    }


#pragma warning disable CS1998 // synchronous sample sequence — no await needed
    private static async IAsyncEnumerable<MetricsSample> SampleAsync()
    {
        yield return new MetricsSample { FirstName = "Alice", LastName = "Smith", Age = 30 };
        yield return new MetricsSample { FirstName = "Bob", LastName = "Jones", Age = 25 };
    }
#pragma warning restore CS1998


    private static string Content(params (string First, string Last, int Age)[] people)
        => string.Concat(people.Select(p => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}\n", p.First, p.Last, p.Age)));


    private sealed class CapturedMeasurement
    {
        internal CapturedMeasurement(string instrument, double value, IReadOnlyDictionary<string, object?> tags)
        {
            Instrument = instrument;
            Value = value;
            Tags = tags;
        }

        internal string Instrument { get; }

        internal double Value { get; }

        internal IReadOnlyDictionary<string, object?> Tags { get; }
    }


    /// <summary>
    /// A <see cref="MeterListener"/> that records every measurement from the fixed-width meter tagged for
    /// <see cref="RecordTypeName"/>. Disposing it stops the listener.
    /// </summary>
    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly List<CapturedMeasurement> _captured = new();


        internal MetricCapture()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (string.Equals(instrument.Meter.Name, MeterName, StringComparison.Ordinal))
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                },
            };

            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument.Name, value, tags));
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument.Name, value, tags));
            _listener.Start();
        }


        internal IReadOnlyList<CapturedMeasurement> All
        {
            get
            {
                lock (_captured)
                {
                    return _captured.ToList();
                }
            }
        }


        internal IReadOnlyList<CapturedMeasurement> Measurements(string instrument)
            => All.Where(m => string.Equals(m.Instrument, instrument, StringComparison.Ordinal)).ToList();


        internal long Sum(string instrument) => (long)Measurements(instrument).Sum(m => m.Value);


        private void Record(string instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                map[tag.Key] = tag.Value;
            }

            // Ignore measurements from other test classes on the same process-global meter.
            if (map.TryGetValue("etl.record_type", out var recordType)
                && string.Equals(recordType as string, RecordTypeName, StringComparison.Ordinal))
            {
                lock (_captured)
                {
                    _captured.Add(new CapturedMeasurement(instrument, value, map));
                }
            }
        }


        public void Dispose() => _listener.Dispose();
    }
}
