using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

// ------------------------------------------------------------------
// Spy logger
// ------------------------------------------------------------------

[ExcludeFromCodeCoverage]
internal sealed class SpyLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = new();



    public IReadOnlyList<LogEntry> Entries => _entries;



    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;



    public bool IsEnabled(LogLevel logLevel) => true;



    public void Log<TState>
    (
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        _entries.Add
        (
            new LogEntry
            (
                logLevel,
                formatter(state, exception),
                exception
            )
        );
    }
}



[ExcludeFromCodeCoverage]
internal sealed record LogEntry
(
    LogLevel Level,
    string Message,
    Exception? Exception
);



// ------------------------------------------------------------------
// Extractor logging tests
// ------------------------------------------------------------------

public class FixedWidthExtractorLoggingTests
{
    private static readonly string PersonLine = "John      Smith     042";



    // ------------------------------------------------------------------
    // Information — extraction lifecycle
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_logs_Information_at_start_and_completion()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(PersonLine),
            logger
        );

        await extractor.ExtractAsync().ToListAsync();

        var infoEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Information)
            .ToList();

        Assert.Equal(2, infoEntries.Count);
        Assert.Contains("Extraction started", infoEntries[0].Message, StringComparison.Ordinal);
        Assert.Contains("PersonRecord", infoEntries[0].Message, StringComparison.Ordinal);
        Assert.Contains("Extraction completed", infoEntries[1].Message, StringComparison.Ordinal);
        Assert.Contains("1 items extracted", infoEntries[1].Message, StringComparison.Ordinal);
    }



    // ------------------------------------------------------------------
    // Debug — field map
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_logs_Debug_field_map_resolved()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(PersonLine),
            logger
        );

        await extractor.ExtractAsync().ToListAsync();

        var debugEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Debug)
            .ToList();

        Assert.Contains
        (
            debugEntries,
            e => e.Message.Contains("Field map resolved", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — structural lines
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_HasHeader_logs_Debug_structural_line_skipped()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = "FirstName LastName  Age\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            HasHeader = true,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("structural line", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — blank line handling
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_Skip_logs_Debug_blank_line_skipped()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            BlankLineHandling = BlankLineHandling.Skip,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("BlankLineHandling=Skip", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ReturnDefault_logs_Debug_yielded_as_default()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            BlankLineHandling = BlankLineHandling.ReturnDefault,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("BlankLineHandling=ReturnDefault", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Error — blank line throws
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ThrowException_logs_Error()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        );

        await Assert.ThrowsAsync<LineTooShortException>
        (
            async () => await extractor.ExtractAsync().ToListAsync()
        );

        var errorEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Error)
            .ToList();

        Assert.Single(errorEntries);
        Assert.Contains("Blank line", errorEntries[0].Message, StringComparison.Ordinal);
        Assert.IsType<LineTooShortException>(errorEntries[0].Exception);
    }



    // ------------------------------------------------------------------
    // Error — malformed line throws
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_MalformedLineHandling_is_ThrowException_logs_Error()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader("Short"),
            logger
        );

        await Assert.ThrowsAsync<LineTooShortException>
        (
            async () => await extractor.ExtractAsync().ToListAsync()
        );

        var errorEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Error)
            .ToList();

        Assert.Single(errorEntries);
        Assert.Contains("Malformed line", errorEntries[0].Message, StringComparison.Ordinal);
        Assert.IsType<LineTooShortException>(errorEntries[0].Exception);
    }



    // ------------------------------------------------------------------
    // Debug — malformed line skip / return default
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_MalformedLineHandling_is_Skip_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = "Short\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("MalformedLineHandling=Skip", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task ExtractAsync_when_MalformedLineHandling_is_ReturnDefault_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = "Short\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            MalformedLineHandling = MalformedLineHandling.ReturnDefault,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("MalformedLineHandling=ReturnDefault", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — line filter
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Skip_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = "# comment\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            LineFilter = line => line.StartsWith("#")
                ? LineAction.Skip
                : LineAction.Process,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("LineFilter returned Skip", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Stop_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\nEND\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            LineFilter = line => string.Equals(line, "END", StringComparison.Ordinal)
                ? LineAction.Stop
                : LineAction.Process,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("LineFilter returned Stop", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — skip budget and max item count
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_SkipItemCount_is_set_logs_Debug_data_line_skipped()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            SkipItemCount = 1,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("Skipping data line", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task ExtractAsync_when_MaximumItemCount_is_reached_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n" + PersonLine + "\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        )
        {
            MaximumItemCount = 1,
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("MaximumItemCount", StringComparison.Ordinal)
                && e.Message.Contains("stopping", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — record parsed
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_logs_Debug_for_each_parsed_record()
    {
        var logger = new SpyLogger<FixedWidthExtractor<PersonRecord, FixedWidthReport>>();
        var content = PersonLine + "\n" + PersonLine;
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(content),
            logger
        );

        await extractor.ExtractAsync().ToListAsync();

        var parsedEntries = logger.Entries
            .Where
            (
                e => e.Level == LogLevel.Debug
                    && e.Message.Contains("Parsed record", StringComparison.Ordinal)
            )
            .ToList();

        Assert.Equal(2, parsedEntries.Count);
    }



    // ------------------------------------------------------------------
    // No logger — does not throw
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_no_logger_provided_does_not_throw()
    {
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>
        (
            new StringReader(PersonLine)
        );

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
    }
}



// ------------------------------------------------------------------
// Loader logging tests
// ------------------------------------------------------------------

public class FixedWidthLoaderLoggingTests
{
    private static readonly PersonRecord[] OneRecord = new[]
    {
        new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
    };



    // ------------------------------------------------------------------
    // Information — loading lifecycle
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_logs_Information_at_start_and_completion()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        );

        await loader.LoadAsync(OneRecord.ToAsyncEnumerable());

        var infoEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Information)
            .ToList();

        Assert.Equal(2, infoEntries.Count);
        Assert.Contains("Loading started", infoEntries[0].Message, StringComparison.Ordinal);
        Assert.Contains("PersonRecord", infoEntries[0].Message, StringComparison.Ordinal);
        Assert.Contains("Loading completed", infoEntries[1].Message, StringComparison.Ordinal);
        Assert.Contains("1 items loaded", infoEntries[1].Message, StringComparison.Ordinal);
    }



    // ------------------------------------------------------------------
    // Debug — field map
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_logs_Debug_field_map_resolved()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        );

        await loader.LoadAsync(OneRecord.ToAsyncEnumerable());

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("Field map resolved", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — header and separator
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_WriteHeader_is_true_logs_Debug_header_written()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        )
        {
            WriteHeader = true,
        };

        await loader.LoadAsync(OneRecord.ToAsyncEnumerable());

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("Wrote header", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task LoadAsync_when_FieldSeparator_is_set_logs_Debug_separator_written()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        )
        {
            WriteHeader = true,
            FieldSeparator = '-',
        };

        await loader.LoadAsync(OneRecord.ToAsyncEnumerable());

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("Wrote separator", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — skip budget and max item count
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_SkipItemCount_is_set_logs_Debug_item_skipped()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var records = new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        };
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        )
        {
            SkipItemCount = 1,
        };

        await loader.LoadAsync(records.ToAsyncEnumerable());

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("Skipping item", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task LoadAsync_when_MaximumItemCount_is_reached_logs_Debug()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var records = new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        };
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        )
        {
            MaximumItemCount = 1,
        };

        await loader.LoadAsync(records.ToAsyncEnumerable());

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Debug
                && e.Message.Contains("MaximumItemCount", StringComparison.Ordinal)
                && e.Message.Contains("stopping", StringComparison.Ordinal)
        );
    }



    // ------------------------------------------------------------------
    // Debug — record written
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_logs_Debug_for_each_record_written()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var records = new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        };
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        );

        await loader.LoadAsync(records.ToAsyncEnumerable());

        var writtenEntries = logger.Entries
            .Where
            (
                e => e.Level == LogLevel.Debug
                    && e.Message.Contains("Wrote record", StringComparison.Ordinal)
            )
            .ToList();

        Assert.Equal(2, writtenEntries.Count);
    }



    // ------------------------------------------------------------------
    // Error — null record
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_null_record_encountered_logs_Error()
    {
        var logger = new SpyLogger<FixedWidthLoader<PersonRecord, FixedWidthReport>>();
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter(),
            logger
        );

        await Assert.ThrowsAsync<InvalidOperationException>
        (
            async () => await loader.LoadAsync
            (
                new PersonRecord[] { null! }.ToAsyncEnumerable()
            )
        );

        var errorEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Error)
            .ToList();

        Assert.Single(errorEntries);
        Assert.Contains("Null record", errorEntries[0].Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(errorEntries[0].Exception);
    }



    // ------------------------------------------------------------------
    // No logger — does not throw
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_no_logger_provided_does_not_throw()
    {
        var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>
        (
            new StringWriter()
        );

        await loader.LoadAsync(OneRecord.ToAsyncEnumerable());

        Assert.Equal(1, loader.CurrentItemCount);
    }
}
