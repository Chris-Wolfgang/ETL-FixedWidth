using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsCheck;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Fuzz;

/// <summary>
/// Property-based fuzz tests (#139) over the public extract/load surface using
/// CsCheck. Two invariants are exercised:
/// <list type="bullet">
///   <item>Robustness — the extractor drains arbitrary input without throwing an
///   undocumented exception when malformed/blank handling is set to skip.</item>
///   <item>Round-trip — a record with in-range field values survives
///   load → extract unchanged.</item>
/// </list>
/// The case count is <c>FUZZ_ITER</c> (default 1000 for the per-PR run); the
/// scheduled fuzz.yaml sets it far higher for a deep sweep. On failure CsCheck
/// shrinks to a minimal input and prints a replayable seed.
/// </summary>
public class FixedWidthFuzzTests
{
    [ExcludeFromCodeCoverage]
    public record FuzzRecord
    {
        [FixedWidthField(0, 10)]
        public string Name { get; set; } = string.Empty;

        [FixedWidthField(1, 5, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Count { get; set; }

        [FixedWidthField(2, 12)]
        public decimal Amount { get; set; }
    }



    private static long Iterations =>
        long.TryParse(Environment.GetEnvironmentVariable("FUZZ_ITER"), out var n) && n > 0
            ? n
            : 1000;



    // Values chosen so the fixed-width representation round-trips cleanly:
    //   Name  - letters/digits only, no leading/trailing space (padding is trimmed),
    //   Count - fits 5 digits,
    //   Amount- two decimal places, fits 12 chars.
    private static readonly Gen<FuzzRecord> SafeRecord =
        Gen.Select(
            Gen.String[Gen.Char.AlphaNumeric, 1, 10],
            Gen.Int[0, 99999],
            Gen.Int[-99999999, 99999999],
            (name, count, cents) => new FuzzRecord
            {
                Name = name,
                Count = count,
                Amount = cents / 100m,
            });



    private static List<T> Drain<T>(IAsyncEnumerable<T> source)
    {
        var results = new List<T>();
        var enumerator = source.GetAsyncEnumerator(CancellationToken.None);
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                results.Add(enumerator.Current);
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return results;
    }



    private static string Serialize(FuzzRecord record)
    {
        var writer = new StringWriter();
        using var loader = new FixedWidthLoader<FuzzRecord>(writer);
        loader.LoadAsync(One(record), CancellationToken.None).GetAwaiter().GetResult();
        return writer.ToString();
    }



    private static async IAsyncEnumerable<FuzzRecord> One(FuzzRecord record)
    {
        yield return record;
        await Task.CompletedTask;
    }



    [Fact]
    public void Extractor_drains_arbitrary_input_without_unexpected_throw()
    {
        Gen.String.Sample(
            line =>
            {
                using var extractor = new FixedWidthExtractor<FuzzRecord>(new StringReader(line))
                {
                    MalformedLineHandling = MalformedLineHandling.Skip,
                    BlankLineHandling = BlankLineHandling.Skip,
                };

                // Draining must complete; a crash here (IndexOutOfRange, NullRef,
                // ...) is a robustness bug for CsCheck to shrink and report.
                Drain(extractor.ExtractAsync(CancellationToken.None));
            },
            iter: Iterations);
    }



    [Fact]
    public void Record_round_trips_through_load_then_extract()
    {
        SafeRecord.Sample(
            expected =>
            {
                var text = Serialize(expected);

                using var extractor = new FixedWidthExtractor<FuzzRecord>(new StringReader(text));
                var actual = Assert.Single(Drain(extractor.ExtractAsync(CancellationToken.None)));

                Assert.Equal(expected, actual);
            },
            iter: Iterations);
    }
}
