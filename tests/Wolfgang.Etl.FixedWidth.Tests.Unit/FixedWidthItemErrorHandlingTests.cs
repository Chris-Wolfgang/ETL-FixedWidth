using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the extractor's adoption of the Abstractions #84 per-item error mechanism: a malformed
/// line routed through <c>MalformedLineHandling</c> now flows through the base
/// <c>OnItemError</c>/<c>HandleItemError</c> policy, so a genuine parse failure is counted in the
/// base <see cref="Wolfgang.Etl.Abstractions.ExtractorBase{TSource,TProgress}.CurrentErrorItemCount"/>
/// and surfaced as <see cref="EtlPipelineProgress.RecordsErrored"/> — distinct from the broader
/// <c>CurrentRejectedItemCount</c>, which also counts validator rejects. <c>ReturnDefault</c>
/// recovers before the give-up decision and is therefore never an error.
/// </summary>
public class FixedWidthItemErrorHandlingTests
{
    // FirstName(10) + LastName(10) + Age(3). "abc" is an unparseable Age -> malformed line.
    private static string Line(string first, string last, string age) =>
        first.PadRight(10) + last.PadRight(10) + age.PadLeft(3, '0');

    private const string BadAge = "abc";

    private static readonly string GoodBadGoodBad = string.Join
    (
        "\n",
        Line("Carol", "Clark", "35"),
        Line("Eve", "Evans", BadAge),
        Line("Dan", "Davis", "40"),
        Line("Foo", "Bar", BadAge)
    );


    [Fact]
    public async Task Malformed_Skip_counts_as_a_base_error_item()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(GoodBadGoodBad))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        var yielded = await Drain(extractor);

        Assert.Equal(2, yielded.Count);                     // Carol, Dan
        Assert.Equal(2, extractor.CurrentErrorItemCount);   // Eve, Foo — genuine parse failures
        Assert.Equal(2, extractor.CurrentRejectedItemCount);// same two (no validator here)
    }


    [Fact]
    public async Task Validator_reject_is_not_a_base_error_item()
    {
        // The one line parses fine but the validator rejects it. That is a business decision,
        // not a parse error — CurrentRejectedItemCount counts it, CurrentErrorItemCount does not.
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(Line("Bob", "Brown", "30")))
        {
            RecordValidator = _ => ValidationResult.Skip("no bobs"),
        };

        var yielded = await Drain(extractor);

        Assert.Empty(yielded);
        Assert.Equal(0, extractor.CurrentErrorItemCount);    // not an error
        Assert.Equal(1, extractor.CurrentRejectedItemCount); // but still a reject
    }


    [Fact]
    public async Task Malformed_ReturnDefault_recovers_and_is_not_an_error()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(Line("Eve", "Evans", BadAge)))
        {
            MalformedLineHandling = MalformedLineHandling.ReturnDefault,
        };

        var yielded = await Drain(extractor);

        Assert.Single(yielded);                             // a substitute default record
        Assert.Equal(0, extractor.CurrentErrorItemCount);   // recovery never reaches the error policy
    }


    [Fact]
    public async Task Malformed_ThrowException_aborts_the_run()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(GoodBadGoodBad));
        // default MalformedLineHandling == ThrowException -> OnItemError returns Abort -> rethrow

        await Assert.ThrowsAnyAsync<MalformedLineException>(() => Drain(extractor));
    }


    [Fact]
    public async Task Pipeline_RecordsErrored_surfaces_the_malformed_skips()
    {
        var reports = new List<EtlPipelineProgress>();
        var progress = new SyncProgress(reports.Add);
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(GoodBadGoodBad))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };
        var loader = new CountingLoader();

        await EtlPipeline
            .Create()
            .From(extractor)
            .To(loader)
            .RunAsync(progress);

        var final = reports[^1];
        Assert.Equal(2, final.RecordsExtracted);
        Assert.Equal(2, final.RecordsLoaded);
        Assert.Equal(2, final.RecordsErrored);
        Assert.Equal(2, loader.Loaded.Count);
    }


    // ---- helpers / doubles ----

    private static async Task<List<PersonRecord>> Drain(FixedWidthExtractor<PersonRecord> extractor)
    {
        var result = new List<PersonRecord>();
        await foreach (var item in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
        {
            result.Add(item);
        }

        return result;
    }


    private sealed class SyncProgress : System.IProgress<EtlPipelineProgress>
    {
        private readonly System.Action<EtlPipelineProgress> _report;

        public SyncProgress(System.Action<EtlPipelineProgress> report) => _report = report;

        public void Report(EtlPipelineProgress value) => _report(value);
    }


    private sealed class CountingLoader : LoaderBase<PersonRecord, CountingLoader.Report>
    {
        public List<PersonRecord> Loaded { get; } = new();

        protected override async Task LoadWorkerAsync(IAsyncEnumerable<PersonRecord> items, CancellationToken token)
        {
            await foreach (var item in items.WithCancellation(token).ConfigureAwait(false))
            {
                Loaded.Add(item);
                IncrementCurrentItemCount();
            }
        }

        protected override Report CreateProgressReport() => new(CurrentItemCount);

        public sealed record Report(int Count);
    }
}
