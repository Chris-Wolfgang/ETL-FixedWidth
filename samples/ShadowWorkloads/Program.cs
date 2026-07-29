using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.ShadowWorkloads;

// Shadow-testing sample workloads (#140). Each scenario is a realistic end-to-end
// consumer flow written as readable usage (it doubles as documentation). Run with
// no args to execute each once and print a summary; run with `--measure` to emit
// per-scenario latency + allocation JSON for the nightly regression gate.
internal static class Program
{
    private const int Lines = 20_000;
    private const int Warmup = 3;
    private const int Measured = 15;

    private static readonly (string Name, Func<string, Task<int>> Run)[] Scenarios =
    {
        ("streaming_round_trip", StreamingRoundTripAsync),
        ("reformat_transform", ReformatTransformAsync),
        ("pipeline_composition", PipelineCompositionAsync),
    };


    private static async Task<int> Main(string[] args)
    {
        var data = BuildData(Lines);

        if (args.Contains("--measure"))
        {
            await MeasureAllAsync(data).ConfigureAwait(false);
        }
        else
        {
            foreach (var (name, run) in Scenarios)
            {
                var count = await run(data).ConfigureAwait(false);
                Console.WriteLine($"{name}: {count} records");
            }
        }

        return 0;
    }


    // --- Scenario 1: stream a large fixed-width file straight through -----------
    private static async Task<int> StreamingRoundTripAsync(string data)
    {
        var count = 0;
        using var extractor = new FixedWidthExtractor<Customer>(new StringReader(data));
        using var loader = new FixedWidthLoader<Customer>(TextWriter.Null);

        var buffer = new List<Customer>(Lines);
        await foreach (var customer in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
        {
            buffer.Add(customer);
            count++;
        }

        await loader.LoadAsync(ToAsync(buffer), CancellationToken.None).ConfigureAwait(false);
        return count;
    }


    // --- Scenario 2: reformat one layout to another via same-name mapping -------
    private static async Task<int> ReformatTransformAsync(string data)
    {
        var count = 0;
        using var extractor = new FixedWidthExtractor<Customer>(new StringReader(data));
        var transformer = FixedWidthTransformer<Customer, Customer>.ByMatchingProperties();
        using var loader = new FixedWidthLoader<Customer>(TextWriter.Null);

        var transformed = transformer.TransformAsync(extractor.ExtractAsync(CancellationToken.None), CancellationToken.None);
        var buffer = new List<Customer>(Lines);
        await foreach (var customer in transformed.ConfigureAwait(false))
        {
            buffer.Add(customer);
            count++;
        }

        await loader.LoadAsync(ToAsync(buffer), CancellationToken.None).ConfigureAwait(false);
        return count;
    }


    // --- Scenario 3: compose the whole flow as one EtlPipeline chain ------------
    private static async Task<int> PipelineCompositionAsync(string data)
    {
        await EtlPipeline
            .Create()
            .FixedWidthExtractor<Customer>(new StringReader(data))
            .FixedWidthLoader<Customer>(TextWriter.Null)
            .RunAsync()
            .ConfigureAwait(false);

        return Lines;
    }


    // --- Measurement ------------------------------------------------------------
    private static async Task MeasureAllAsync(string data)
    {
        var results = new List<string>();
        foreach (var (name, run) in Scenarios)
        {
            results.Add(await MeasureOneAsync(name, run, data).ConfigureAwait(false));
        }

        var json = "[\n" + string.Join(",\n", results) + "\n]";
        Console.WriteLine(json);

        var outPath = Environment.GetEnvironmentVariable("SHADOW_OUT");
        if (!string.IsNullOrEmpty(outPath))
        {
            await File.WriteAllTextAsync(outPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
        }
    }


    private static async Task<string> MeasureOneAsync(string name, Func<string, Task<int>> run, string data)
    {
        for (var i = 0; i < Warmup; i++)
        {
            await run(data).ConfigureAwait(false);
        }

        var samples = new double[Measured];
        var records = 0;
        var allocStart = GC.GetTotalAllocatedBytes(precise: true);
        for (var i = 0; i < Measured; i++)
        {
            var sw = Stopwatch.StartNew();
            records = await run(data).ConfigureAwait(false);
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds;
        }

        var allocatedPerIteration = (GC.GetTotalAllocatedBytes(precise: true) - allocStart) / Measured;
        Array.Sort(samples);
        var medianMs = samples[samples.Length / 2];

        return string.Format
        (
            CultureInfo.InvariantCulture,
            "  {{ \"scenario\": \"{0}\", \"records\": {1}, \"medianMs\": {2}, \"allocatedBytes\": {3} }}",
            name,
            records,
            Math.Round(medianMs, 3),
            allocatedPerIteration
        );
    }


    private static string BuildData(int lines)
    {
        var sb = new StringBuilder(lines * 60);
        for (var i = 0; i < lines; i++)
        {
            sb.AppendFormat
            (
                CultureInfo.InvariantCulture,
                "{0:00000000}{1,-15}{2,-15}{3,-15}{4,-2}{5,12:0.00}\n",
                i,
                $"First{i % 997}",
                $"Last{i % 991}",
                $"City{i % 503}",
                "PA",
                (i % 100000) / 100.0
            );
        }

        return sb.ToString();
    }


#pragma warning disable CS1998
    private static async IAsyncEnumerable<Customer> ToAsync(IEnumerable<Customer> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998


    private sealed class Customer
    {
        [FixedWidthField(0, 8, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Id { get; set; }

        [FixedWidthField(1, 15)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(2, 15)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(3, 15)]
        public string City { get; set; } = string.Empty;

        [FixedWidthField(4, 2)]
        public string State { get; set; } = string.Empty;

        [FixedWidthField(5, 12, Alignment = FieldAlignment.Right, Format = "0.00")]
        public double Balance { get; set; }
    }
}
