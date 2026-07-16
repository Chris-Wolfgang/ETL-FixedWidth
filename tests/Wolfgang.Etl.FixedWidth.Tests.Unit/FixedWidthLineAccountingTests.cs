using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Verifies the extractor's line accounting: every physical line lands in exactly
/// one counter, and the counters partition <c>CurrentLineNumber</c> — see #18 /
/// the skipped / rejected / filtered split.
/// </summary>
public class FixedWidthLineAccountingTests
{
    // One line per category:
    //   1 comment  -> LineFilter.Skip        -> filtered
    //   2 Alice    -> SkipItemCount budget   -> skipped
    //   3 (blank)  -> BlankLineHandling.Skip -> filtered
    //   4 Bob      -> RecordValidator.Skip   -> rejected
    //   5 Eve/abc  -> malformed (bad Age)    -> rejected
    //   6 Carol    -> emitted                -> item
    //   7 Dan      -> emitted                -> item
    private const string Input =
        "# comment line\n" +
        "Alice     Anderson  025\n" +
        "\n" +
        "Bob       Brown     030\n" +
        "Eve       Evans     abc\n" +
        "Carol     Clark     035\n" +
        "Dan       Davis     040";



    private static FixedWidthExtractor<PersonRecord> CreateExtractor() =>
        new(new StringReader(Input))
        {
            SkipItemCount = 1,
            BlankLineHandling = BlankLineHandling.Skip,
            MalformedLineHandling = MalformedLineHandling.Skip,
            LineFilter = line => line.StartsWith("#", StringComparison.Ordinal)
                ? LineAction.Skip
                : LineAction.Process,
            RecordValidator = record => string.Equals(record.FirstName, "Bob", StringComparison.Ordinal)
                ? ValidationResult.Skip("no bobs")
                : ValidationResult.Accept(),
        };



    [Fact]
    public async Task Counters_partition_every_line_by_category()
    {
        var extractor = CreateExtractor();

        var results = new List<PersonRecord>();
        await foreach (var record in extractor.ExtractAsync(CancellationToken.None))
        {
            results.Add(record);
        }

        Assert.Equal(2, extractor.CurrentItemCount);         // Carol, Dan
        Assert.Equal(1, extractor.CurrentSkippedItemCount);  // Alice (pagination)
        Assert.Equal(2, extractor.CurrentRejectedItemCount); // Bob (validator) + Eve (malformed)
        Assert.Equal(2, extractor.CurrentFilteredLineCount); // comment + blank
    }



    [Fact]
    public async Task Line_accounting_closes_exactly()
    {
        var extractor = CreateExtractor();

        await foreach (var _ in extractor.ExtractAsync(CancellationToken.None))
        {
        }

        var accounted = extractor.CurrentItemCount
            + extractor.CurrentSkippedItemCount
            + extractor.CurrentRejectedItemCount
            + extractor.CurrentFilteredLineCount;

        Assert.Equal(7L, extractor.CurrentLineNumber);
        Assert.Equal(extractor.CurrentLineNumber, accounted);
    }
}
