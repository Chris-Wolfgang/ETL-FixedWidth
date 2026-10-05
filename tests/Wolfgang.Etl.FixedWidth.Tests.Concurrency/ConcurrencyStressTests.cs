using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Concurrency;

/// <summary>
/// Race-condition stress tests (#147). Production usage hits real schedule
/// interleavings that a single-threaded per-PR test never explores: concurrent
/// first-use of the process-global caches (<c>FieldMap</c>'s
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
/// and <c>FixedWidthTransformer</c>'s static property-mapper), racing disposal,
/// and cancellation arriving on a different worker mid-enumeration.
///
/// Every test asserts <em>correctness under contention</em> (not timing), so it
/// is deterministic: a torn cache or shared-state bleed makes a result wrong, not
/// merely slow. <see cref="Iterations"/> scales via the <c>STRESS_ITERATIONS</c>
/// environment variable — modest on a PR, generous on the weekly sweep.
/// </summary>
[Trait("Category", "Concurrency")]
public sealed class ConcurrencyStressTests
{
    private static int Iterations =>
        int.TryParse(Environment.GetEnvironmentVariable("STRESS_ITERATIONS"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0
            ? n
            : 64;

    private static readonly int Fanout = Math.Max(4, Environment.ProcessorCount * 2);


    [Fact]
    public async Task Concurrent_extraction_shares_the_field_map_cache_safely()
    {
        // Many workers extract the same record type at once. They race to populate
        // FieldMap's process-global cache on first use; every worker must still get
        // a correct, complete result.
        var content = Content(("Alice", "Anderson", 25), ("Bob", "Baker", 42), ("Carol", "Clark", 33));

        await RunFanoutAsync(async () =>
        {
            using var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(content));
            var rows = new List<PersonRecord>();
            await foreach (var r in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
            {
                rows.Add(r);
            }

            Assert.Equal(3, rows.Count);
            Assert.Equal("Alice", rows[0].FirstName);
            Assert.Equal(42, rows[1].Age);
            Assert.Equal("Clark", rows[2].LastName);
        });
    }


    [Fact]
    public async Task ByMatchingProperties_is_safe_under_concurrent_first_use()
    {
        // FixedWidthTransformer<,>._propertyMapper is a static ??= — concurrent first
        // callers may build it twice, but both mappers are equivalent, so every
        // transform must produce identical, correct output.
        await RunFanoutAsync(async () =>
        {
            var transformer = FixedWidthTransformer<PersonRecord, PersonRecord>.ByMatchingProperties();
            var src = new[]
            {
                new PersonRecord { FirstName = "Dana", LastName = "Doe", Age = 51 },
                new PersonRecord { FirstName = "Evan", LastName = "East", Age = 27 },
            };

            var outp = new List<PersonRecord>();
            await foreach (var r in transformer.TransformAsync(ToAsync(src), CancellationToken.None).ConfigureAwait(false))
            {
                outp.Add(r);
            }

            Assert.Equal(2, outp.Count);
            Assert.Equal("Dana", outp[0].FirstName);
            Assert.Equal(27, outp[1].Age);
        });
    }


    [Fact]
    public async Task Parallel_round_trips_do_not_bleed_state_between_operations()
    {
        // Each worker round-trips a distinct payload through an independent
        // extractor+loader. Shared static state (caches, mappers) must not let one
        // operation's data leak into another's output.
        await RunFanoutAsync(async index =>
        {
            var name = $"N{index:D5}";
            var content = Content((name, "Row", index % 100));

            var people = new List<PersonRecord>();
            using (var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(content)))
            {
                await foreach (var r in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    people.Add(r);
                }
            }

            var writer = new StringWriter();
            using (var loader = new FixedWidthLoader<PersonRecord>(writer))
            {
                await loader.LoadAsync(ToAsync(people), CancellationToken.None).ConfigureAwait(false);
            }

            Assert.Single(people);
            Assert.Equal(name, people[0].FirstName.TrimEnd());
            Assert.Equal(index % 100, people[0].Age);
            Assert.Contains(name, writer.ToString(), StringComparison.Ordinal);
        });
    }


    [Fact]
    public async Task Dispose_during_enumeration_never_corrupts_yielded_records()
    {
        // Dispose the extractor from another task while a worker enumerates. Any
        // records already yielded must be correct; the enumeration must end cleanly
        // (completion or a disposal-related exception), never a torn record.
        var content = Content(Enumerable.Range(0, 500).Select(i => ($"P{i:D6}", "X", i % 90)).ToArray());

        await RunFanoutAsync(async () =>
        {
            var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(content));
            var seen = 0;

            // Record the outcome instead of catching each acceptable exception type in
            // its own catch block: which outcome occurs depends on who wins the race,
            // so per-outcome catch blocks would leave lines unexecuted on most runs.
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var r in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Assert.False(string.IsNullOrEmpty(r.FirstName));   // never a torn/default record
                    if (++seen == 10)
                    {
#pragma warning disable CS4014 // fire-and-forget dispose to race the enumerator
                        Task.Run(() => extractor.Dispose());
#pragma warning restore CS4014
                    }
                }
            }).ConfigureAwait(false);

            extractor.Dispose();

            // Acceptable: completion, disposal winning the race (ObjectDisposedException),
            // or the reader closing underneath the enumerator (InvalidOperationException).
            Assert.True
            (
                exception is null or ObjectDisposedException or InvalidOperationException,
                $"Unexpected outcome when disposing during enumeration: {exception}"
            );
        });
    }


    [Fact]
    public async Task Cancellation_from_another_thread_stops_enumeration_cleanly()
    {
        var content = Content(Enumerable.Range(0, 1000).Select(i => ($"C{i:D6}", "Y", i % 90)).ToArray());

        await RunFanoutAsync(async () =>
        {
            using var cts = new CancellationTokenSource();
            using var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(content));
            var seen = 0;

            // The extractor checks the token before every record, so cancelling after
            // the fifth record always ends the enumeration with OperationCanceledException.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var r in extractor.ExtractAsync(cts.Token).ConfigureAwait(false))
                {
                    Assert.False(string.IsNullOrEmpty(r.FirstName));
                    if (++seen == 5)
                    {
                        cts.Cancel();
                    }
                }
            }).ConfigureAwait(false);
        });
    }


    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static Task RunFanoutAsync(Func<Task> body) => RunFanoutAsync(_ => body());


    private static async Task RunFanoutAsync(Func<int, Task> body)
    {
        var total = Iterations * Fanout;

        var tasks = Enumerable
            .Range(0, total)
            .Select(i => Task.Run(() => body(i)))
            .ToArray();

        // Wait for every iteration without throwing on the first failure, then check
        // each one. Assert.All reports how many of the iterations failed and each one's
        // exception, and there is no failure-only branch that a passing run never executes.
        // SuppressThrowing also swallows cancellation, and a canceled task has a null
        // Exception, so each iteration must additionally have run to completion.
        await Task
            .WhenAll(tasks)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        Assert.All
        (
            tasks,
            task =>
            {
                Assert.Null(task.Exception);
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }
        );
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


    private static string Content(params (string First, string Last, int Age)[] rows)
        => string.Concat(rows.Select(r => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}\n", r.First, r.Last, r.Age)));


    private sealed class PersonRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }
    }
}
