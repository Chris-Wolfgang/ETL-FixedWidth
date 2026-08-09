using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthTransformer{TSource, TDestination}"/> (#14).
/// </summary>
public class FixedWidthTransformerTests
{
    [ExcludeFromCodeCoverage]
    private sealed record Src
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private sealed record Dst
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }

        public string Extra { get; set; } = "unset";
    }



    [ExcludeFromCodeCoverage]
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
        using var transformer = new FixedWidthTransformer<Src, Dst>(s => new Dst { Name = s.Name })
        {
            SkipItemCount = 1,
            MaximumItemCount = 1,
        };

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
            yield return new Src { Name = "alice" };
            yield return null!;
            await Task.CompletedTask;
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

        var enumerated = false;
        async IAsyncEnumerable<Src> Poisoned()
        {
            enumerated = true;
            yield return new Src { Name = "never" };
            await Task.CompletedTask;
        }

        // The pre-cancellation guard throws before the source is touched.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await transformer.TransformAsync(Poisoned(), cts.Token).ToListAsync());

        Assert.False(enumerated);
    }


    [ExcludeFromCodeCoverage]
    private sealed class ManualProgressTimer : IProgressTimer
    {
        private Action? _elapsed;

        public bool WasStarted { get; private set; }

        public event Action? Elapsed
        {
            add => _elapsed += value;
            remove => _elapsed -= value;
        }

        public void Start(int intervalMilliseconds) => WasStarted = true;

        public void StopTimer()
        {
        }

        public void Fire() => _elapsed?.Invoke();

        public void Dispose()
        {
        }
    }


    [ExcludeFromCodeCoverage]
    private sealed class CollectingProgress : IProgress<FixedWidthReport>
    {
        public List<FixedWidthReport> Reports { get; } = new();

        public void Report(FixedWidthReport value) => Reports.Add(value);
    }
}
