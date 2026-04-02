using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.TestKit.Xunit;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthLoggingCompletionTests
{
    private static readonly string PersonLine = "John      Smith     042";



    [Fact]
    public async Task ExtractAsync_when_MaximumItemCount_stops_early_still_logs_completed()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord>>();
        var content = PersonLine + "\n" + PersonLine + "\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord>
        (
            new StringReader(content),
            new ManualProgressTimer(),
            logger
        )
        {
            MaximumItemCount = 1,
        };

        await extractor.ExtractAsync().ToListAsync();

        var completed = logger.Entries
            .Where
            (
                e => e.Level == LogLevel.Information
                    && e.Message.Contains("Extraction completed", StringComparison.Ordinal)
            )
            .ToList();

        Assert.Single(completed);
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_stops_early_still_logs_completed()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord>>();
        var content = PersonLine + "\nEND\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord>
        (
            new StringReader(content),
            new ManualProgressTimer(),
            logger
        )
        {
            LineFilter = line => string.Equals(line, "END", StringComparison.Ordinal)
                ? LineAction.Stop
                : LineAction.Process,
        };

        await extractor.ExtractAsync().ToListAsync();

        var completed = logger.Entries
            .Where
            (
                e => e.Level == LogLevel.Information
                    && e.Message.Contains("Extraction completed", StringComparison.Ordinal)
            )
            .ToList();

        Assert.Single(completed);
    }



    [Fact]
    public async Task LoadAsync_when_MaximumItemCount_stops_early_still_logs_completed()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord>>();
        var records = new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        };
        var loader = new FixedWidthLoader<PersonRecord>
        (
            new StringWriter(),
            new ManualProgressTimer(),
            logger
        )
        {
            MaximumItemCount = 1,
        };

        await loader.LoadAsync(records.ToAsyncEnumerable());

        var completed = logger.Entries
            .Where
            (
                e => e.Level == LogLevel.Information
                    && e.Message.Contains("Loading completed", StringComparison.Ordinal)
            )
            .ToList();

        Assert.Single(completed);
    }
}
