using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Xunit;
using Wolfgang.Etl.TestKit.Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthTransformer{TSource, TDestination}"/> (#14).
/// </summary>
public class FixedWidthTransformerTests
{
    private sealed record Src
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }



    private sealed record Dst
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }

        public string Extra { get; set; } = "unset";
    }



    private sealed class NoParameterlessCtor
    {
        public NoParameterlessCtor(string name) => Name = name;

        public string Name { get; }
    }



    private static readonly IReadOnlyList<Src> Sources = new[]
    {
        new Src { Name = "alice", Value = 1 },
        new Src { Name = "bob", Value = 2 },
        new Src { Name = "carol", Value = 3 },
    };



    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }



    [Fact]
    public async Task TransformAsync_projects_each_record()
    {
        using var transformer = new FixedWidthTransformer<Src, Dst>
        (
            s => new Dst { Name = s.Name.ToUpperInvariant(), Value = s.Value * 10, Extra = "mapped" }
        );

        var results = await transformer.TransformAsync(ToAsync(Sources), CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "ALICE", "BOB", "CAROL" }, results.Select(r => r.Name));
        Assert.Equal(new[] { 10, 20, 30 }, results.Select(r => r.Value));
        Assert.All(results, r => Assert.Equal("mapped", r.Extra));
        Assert.Equal(3, transformer.CurrentItemCount);
    }



    [Fact]
    public async Task ByMatchingProperties_copies_same_named_assignable_properties()
    {
        using var transformer = FixedWidthTransformer<Src, Dst>.ByMatchingProperties();

        var result = Assert.Single(await transformer.TransformAsync(ToAsync(Sources.Take(1)), CancellationToken.None).ToListAsync());

        Assert.Equal("alice", result.Name);   // copied
        Assert.Equal(1, result.Value);         // copied
        Assert.Equal("unset", result.Extra);   // no source property -> left at default
    }



    [Fact]
    public async Task SkipItemCount_and_MaximumItemCount_are_honored()
    {
        using var transformer = new FixedWidthTransformer<Src, Dst>
        (
            s => new Dst { Name = s.Name },
            new FixedWidthTransformerOptions
            {
                SkipItemCount = 1,
                MaximumItemCount = 1,
            }
        );

        var results = await transformer.TransformAsync(ToAsync(Sources), CancellationToken.None).ToListAsync();

        // First skipped by the budget, then exactly one emitted.
        Assert.Equal(new[] { "bob" }, results.Select(r => r.Name));
        Assert.Equal(1, transformer.CurrentItemCount);
        Assert.Equal(1, transformer.CurrentSkippedItemCount);
    }



    [Fact]
    public void Constructor_null_transform_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthTransformer<Src, Dst>(null!));
    }



    [Fact]
    public async Task Transforming_a_null_source_record_throws()
    {
        using var transformer = new FixedWidthTransformer<Src, Dst>(s => new Dst { Name = s.Name });

        async IAsyncEnumerable<Src> WithNull()
        {
            await Task.CompletedTask;
            yield return new Src { Name = "alice" };
            yield return null!;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await transformer.TransformAsync(WithNull(), CancellationToken.None).ToListAsync());
    }



    [Fact]
    public void ByMatchingProperties_without_parameterless_ctor_throws()
    {
        // The mapper is compiled eagerly by the factory, so it throws here.
        Assert.Throws<InvalidOperationException>(FixedWidthTransformer<Src, NoParameterlessCtor>.ByMatchingProperties);
    }



    [Fact]
    public async Task TransformAsync_reports_progress_via_the_injected_timer()
    {
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var transformer = new FixedWidthTransformer<Src, Dst>(
            s => new Dst { Name = s.Name, Value = s.Value },
            timer);

        await foreach (var _ in transformer.TransformAsync(ToAsync(Sources), sink, CancellationToken.None))
        {
            // Fire the injected timer mid-enumeration so the wired progress callback runs.
            timer.Fire();
        }

        // A report per Fire() proves the injected timer's Elapsed is wired to the progress callback;
        // the fire after the last item reports the full transformed count.
        Assert.NotEmpty(sink.Reports);
        Assert.Equal(Sources.Count, (int)sink.Reports.Max(r => r.CurrentItemCount));
    }



    [Fact]
    public async Task TransformAsync_when_token_already_cancelled_reads_nothing()
    {
        using var transformer = new FixedWidthTransformer<Src, Dst>(s => new Dst { Name = s.Name });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var source = new TrackedSource();

        // The pre-cancellation guard throws before the source is touched.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await transformer.TransformAsync(source.ReadAsync(), cts.Token).ToListAsync());

        Assert.False(source.Enumerated);
    }



    private sealed class CollectingProgress : IProgress<FixedWidthReport>
    {
        public List<FixedWidthReport> Reports { get; } = new();

        public void Report(FixedWidthReport value) => Reports.Add(value);
    }



    [Fact]
    public void NoParameterlessCtor_keeps_the_name_it_was_built_with()
    {
        // ByMatchingProperties rejects this type by reflection; this pins its shape.
        Assert.Equal("a", new NoParameterlessCtor("a").Name);
    }



    [Fact]
    public async Task TransformAsync_when_token_not_cancelled_reads_the_tracked_source()
    {
        // Counterpart to the pre-cancelled test: the same tracked source IS read when
        // nothing is cancelled, so that test's "never enumerated" is down to the guard.
        using var transformer = new FixedWidthTransformer<Src, Dst>(s => new Dst { Name = s.Name });
        var source = new TrackedSource();

        var results = await transformer.TransformAsync(source.ReadAsync(), CancellationToken.None).ToListAsync();

        Assert.True(source.Enumerated);
        Assert.Equal("never", Assert.Single(results).Name);
    }



    // A one-record source that records whether anything pulled from it.
    private sealed class TrackedSource
    {
        public bool Enumerated { get; private set; }



        public async IAsyncEnumerable<Src> ReadAsync()
        {
            await Task.CompletedTask;
            Enumerated = true;
            yield return new Src { Name = "never" };
        }
    }
}
