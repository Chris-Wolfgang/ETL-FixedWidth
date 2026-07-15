using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the <see cref="FixedWidthExtractor{TRecord}.RecordValidator"/> callback (#18).
/// </summary>
public class FixedWidthRecordValidatorTests
{
    private static readonly IReadOnlyList<PersonRecord> Source = new[]
    {
        new PersonRecord { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new PersonRecord { FirstName = "Bob", LastName = "Brown", Age = 30 },
        new PersonRecord { FirstName = "Carol", LastName = "Clark", Age = 35 },
    };



    // Serialize via the loader so the fixed-width layout is guaranteed correct.
    private static async Task<string> SerializeAsync(IEnumerable<PersonRecord> people)
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer);
        await loader.LoadAsync(people.ToAsyncEnumerable(), CancellationToken.None);
        return writer.ToString();
    }



    private static async Task<List<PersonRecord>> ExtractAsync(FixedWidthExtractor<PersonRecord> extractor)
    {
        var results = new List<PersonRecord>();
        await foreach (var record in extractor.ExtractAsync(CancellationToken.None))
        {
            results.Add(record);
        }

        return results;
    }



    [Fact]
    public async Task RecordValidator_Skip_drops_the_record_and_increments_skipped_count()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(await SerializeAsync(Source)))
        {
            RecordValidator = record => string.Equals(record.FirstName, "Bob", StringComparison.Ordinal)
                ? ValidationResult.Skip("no bobs")
                : ValidationResult.Accept(),
        };

        var results = await ExtractAsync(extractor);

        Assert.Equal(new[] { "Alice", "Carol" }, results.Select(r => r.FirstName));
        Assert.Equal(1, extractor.CurrentSkippedItemCount);
        Assert.Equal(2, extractor.CurrentItemCount);
    }



    [Fact]
    public async Task RecordValidator_Stop_ends_extraction_before_the_matching_record()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(await SerializeAsync(Source)))
        {
            RecordValidator = record => string.Equals(record.FirstName, "Carol", StringComparison.Ordinal)
                ? ValidationResult.Stop("hit Carol")
                : ValidationResult.Accept(),
        };

        var results = await ExtractAsync(extractor);

        Assert.Equal(new[] { "Alice", "Bob" }, results.Select(r => r.FirstName));
        Assert.Equal(2, extractor.CurrentItemCount);
    }



    [Fact]
    public async Task RecordValidator_null_yields_every_record()
    {
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(await SerializeAsync(Source)));

        var results = await ExtractAsync(extractor);

        Assert.Equal(3, results.Count);
        Assert.Equal(0, extractor.CurrentSkippedItemCount);
    }



    [Fact]
    public async Task RecordValidator_Skip_logs_the_reason_at_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord>>();
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(await SerializeAsync(Source)), logger)
        {
            RecordValidator = record => string.Equals(record.FirstName, "Bob", StringComparison.Ordinal)
                ? ValidationResult.Skip("no bobs")
                : ValidationResult.Accept(),
        };

        await ExtractAsync(extractor);

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug && e.Message.IndexOf("no bobs", StringComparison.Ordinal) >= 0
        );
    }



    [Fact]
    public async Task RecordValidator_Stop_logs_the_reason_at_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord>>();
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(await SerializeAsync(Source)), logger)
        {
            RecordValidator = record => string.Equals(record.FirstName, "Carol", StringComparison.Ordinal)
                ? ValidationResult.Stop("hit Carol")
                : ValidationResult.Accept(),
        };

        await ExtractAsync(extractor);

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug && e.Message.IndexOf("hit Carol", StringComparison.Ordinal) >= 0
        );
    }
}
