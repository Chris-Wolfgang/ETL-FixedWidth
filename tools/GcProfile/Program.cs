using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.GcProfile;

// Sustained-load GC / allocation profiler (#152). Runs extract -> transform ->
// load in a tight loop for a configurable duration, then reports the two metrics
// that actually matter for a materializing streaming library under long-running
// server / ETL load:
//   * allocatedBytesPerRecord      — catches a hot-path allocation regression;
//   * gen2CollectionsPerMillion    — catches a retention leak (records or state
//                                     surviving into gen2 when they shouldn't).
// Emits JSON to stdout and (if GC_PROFILE_OUT is set) to that file. The scheduled
// workflow compares against docs/gc-baseline.json and fails on regression.
internal static class Program
{
    private const int BatchLines = 1000;

    private static async Task<int> Main(string[] args)
    {
        var seconds = ResolveSeconds(args);
        var batch = BuildBatch(BatchLines);

        // Warm up: JIT the paths and populate the process-global caches so the
        // measured window reflects steady state, not first-use cost.
        await ProcessOnceAsync(batch).ConfigureAwait(false);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var json = await MeasureAsync(batch, seconds).ConfigureAwait(false);

        Console.WriteLine(json);

        var outPath = Environment.GetEnvironmentVariable("GC_PROFILE_OUT");
        if (!string.IsNullOrEmpty(outPath))
        {
            await File.WriteAllTextAsync(outPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
        }

        return 0;
    }


    private static async Task<string> MeasureAsync(string batch, int seconds)
    {
        var allocStart = GC.GetTotalAllocatedBytes(precise: true);
        var g0 = GC.CollectionCount(0);
        var g1 = GC.CollectionCount(1);
        var g2 = GC.CollectionCount(2);

        long records = 0;
        var sw = Stopwatch.StartNew();
        var deadline = TimeSpan.FromSeconds(seconds);
        while (sw.Elapsed < deadline)
        {
            records += await ProcessOnceAsync(batch).ConfigureAwait(false);
        }

        sw.Stop();

        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocStart;
        var gen2 = GC.CollectionCount(2) - g2;
        var mem = GC.GetGCMemoryInfo();
        var perRecord = records == 0 ? 0d : (double)allocated / records;
        var gen2PerMillion = records == 0 ? 0d : gen2 / (records / 1_000_000d);

        return new StringBuilder()
            .Append('{').Append('\n')
            .AppendField("recordsProcessed", records).Append(",\n")
            .AppendField("elapsedSeconds", Math.Round(sw.Elapsed.TotalSeconds, 2)).Append(",\n")
            .AppendField("recordsPerSecond", Math.Round(records / sw.Elapsed.TotalSeconds, 0)).Append(",\n")
            .AppendField("allocatedBytesTotal", allocated).Append(",\n")
            .AppendField("allocatedBytesPerRecord", Math.Round(perRecord, 1)).Append(",\n")
            .AppendField("gen0Collections", GC.CollectionCount(0) - g0).Append(",\n")
            .AppendField("gen1Collections", GC.CollectionCount(1) - g1).Append(",\n")
            .AppendField("gen2Collections", gen2).Append(",\n")
            .AppendField("gen2CollectionsPerMillion", Math.Round(gen2PerMillion, 3)).Append(",\n")
            .AppendField("heapSizeBytes", mem.HeapSizeBytes).Append(",\n")
            .AppendField("fragmentedBytes", mem.FragmentedBytes).Append('\n')
            .Append('}')
            .ToString();
    }


    private static async Task<int> ProcessOnceAsync(string batch)
    {
        var people = new List<PersonRecord>(BatchLines);
        using (var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(batch)))
        {
            await foreach (var r in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
            {
                people.Add(r);
            }
        }

        var transformer = FixedWidthTransformer<PersonRecord, PersonRecord>.ByMatchingProperties();
        var transformed = new List<PersonRecord>(people.Count);
        await foreach (var r in transformer.TransformAsync(ToAsync(people), CancellationToken.None).ConfigureAwait(false))
        {
            transformed.Add(r);
        }

        using (var loader = new FixedWidthLoader<PersonRecord>(TextWriter.Null))
        {
            await loader.LoadAsync(ToAsync(transformed), CancellationToken.None).ConfigureAwait(false);
        }

        return transformed.Count;
    }


    private static int ResolveSeconds(string[] args)
    {
        if (args.Length > 0 && int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) && a > 0)
        {
            return a;
        }

        return int.TryParse(Environment.GetEnvironmentVariable("GC_PROFILE_SECONDS"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var e) && e > 0
            ? e
            : 10;
    }


    private static string BuildBatch(int lines)
    {
        var sb = new StringBuilder(lines * 24);
        for (var i = 0; i < lines; i++)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}\n", $"First{i % 997}", $"Last{i % 991}", i % 120);
        }

        return sb.ToString();
    }


#pragma warning disable CS1998
    private static async IAsyncEnumerable<PersonRecord> ToAsync(IEnumerable<PersonRecord> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998


    private sealed class PersonRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }
    }


    private static StringBuilder AppendField(this StringBuilder sb, string name, long value)
        => sb.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));


    private static StringBuilder AppendField(this StringBuilder sb, string name, double value)
        => sb.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
}
