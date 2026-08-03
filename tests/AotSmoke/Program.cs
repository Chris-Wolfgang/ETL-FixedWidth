using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.AotSmoke;

// Native-AOT / trim smoke test (#153). Exercises every reflection- and
// Expression.Compile-backed path in the library against concrete record types,
// then ASSERTS the results. Under a trimmer that removed the record property
// metadata FixedWidth reflects over, extraction/loading would silently produce
// default records — the assertions turn that into a non-zero exit instead of a
// green "it ran" result. Returns 0 only if every check passes.
internal static class Program
{
    private const string Source =
        "Alice     Anderson  025\n" +
        "Bob       Baker     042\n";

    private static async Task<int> Main()
    {
        try
        {
            // 0. Statically root PersonRecord's parameterless ctor + property getters/setters. A real
            // AOT app constructs and reads its record types in code; doing so here keeps the trimmer
            // from removing the members the library then reaches via reflection / compiled accessors.
            var seed = new PersonRecord { FirstName = "Seed", LastName = "Row", Age = 1 };
            Check(seed.FirstName == "Seed" && seed.LastName == "Row" && seed.Age == 1, "static record round-trip failed");

            // 1. Schema introspection — FieldMap reflection + FieldMapResult compiled activator.
            var schema = FixedWidthSchema.For<PersonRecord>();
            Check(schema.FieldCount == 3, $"schema.FieldCount expected 3, got {schema.FieldCount}");
            Check(schema.ToDiagram().Contains("FirstName", StringComparison.Ordinal), "ToDiagram missing FirstName");

            // 2. Extract — compiled property setters (FieldDescriptor.CompileSetter).
            var people = new List<PersonRecord>();
            using (var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(Source)))
            {
                await foreach (var p in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    people.Add(p);
                }
            }

            Check(people.Count == 2, $"extracted {people.Count} records, expected 2");
            Check(people[0].FirstName == "Alice" && people[0].LastName == "Anderson" && people[0].Age == 25,
                $"record 0 mismatched: '{people[0].FirstName}'/'{people[0].LastName}'/{people[0].Age}");

            // 3. Transform via ByMatchingProperties — Expression.Compile projection.
            var transformer = FixedWidthTransformer<PersonRecord, PersonRecord>.ByMatchingProperties();
            var transformed = new List<PersonRecord>();
            await foreach (var p in transformer.TransformAsync(ToAsync(people), CancellationToken.None).ConfigureAwait(false))
            {
                transformed.Add(p);
            }

            Check(transformed.Count == 2 && transformed[1].Age == 42, "ByMatchingProperties transform lost data");

            // 4. Load — compiled property getters (FieldDescriptor.CompileGetter).
            var loaded = new StringWriter();
            using (var loader = new FixedWidthLoader<PersonRecord>(loaded))
            {
                await loader.LoadAsync(ToAsync(people), CancellationToken.None).ConfigureAwait(false);
            }

            Check(loaded.ToString().Contains("Alice", StringComparison.Ordinal), "loader output missing 'Alice'");

            // 5. Full pipeline round trip — extractor factory + loader terminator over EtlPipeline.
            var piped = new StringWriter();
            await EtlPipeline
                .Create()
                .FixedWidthExtractor<PersonRecord>(new StringReader(Source))
                .FixedWidthLoader<PersonRecord>(piped)
                .RunAsync()
                .ConfigureAwait(false);

            Check(piped.ToString().Contains("Baker", StringComparison.Ordinal), "pipeline output missing 'Baker'");

            Console.WriteLine("AOT smoke: OK — all public paths ran and produced correct results under AOT+trim.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AOT smoke: FAILED — {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

#pragma warning disable CS1998 // synchronous sample sequence — no await needed
    private static async IAsyncEnumerable<PersonRecord> ToAsync(IEnumerable<PersonRecord> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998
}

internal sealed class PersonRecord
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;

    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;

    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
