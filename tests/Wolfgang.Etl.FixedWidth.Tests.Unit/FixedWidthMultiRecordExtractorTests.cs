using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthMultiRecordExtractor"/> (#19) — routing a file of interleaved record
/// types to different POCOs via discriminator predicates.
/// </summary>
public sealed class FixedWidthMultiRecordExtractorTests
{
    [ExcludeFromCodeCoverage]
    private sealed class HeaderRecord
    {
        [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
        [FixedWidthField(1, 8)] public string BatchDate { get; set; } = string.Empty;
    }


    [ExcludeFromCodeCoverage]
    private sealed class DetailRecord
    {
        [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
        [FixedWidthField(1, 8)] public int Id { get; set; }
        [FixedWidthField(2, 10)] public string Name { get; set; } = string.Empty;
    }


    [ExcludeFromCodeCoverage]
    private sealed class TrailerRecord
    {
        [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
        [FixedWidthField(1, 8)] public int Count { get; set; }
    }


    // "John Smith" and "Jane Doe  " are each exactly 10 chars wide.
    private const string SampleFile =
        "H20260320\n" +
        "D00000001John Smith\n" +
        "D00000002Jane Doe  \n" +
        "T00000002\n";


    private static FixedWidthMultiRecordExtractor NewExtractor(string content)
        => new FixedWidthMultiRecordExtractor(new StringReader(content))
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .When(l => l[0] == 'T', typeof(TrailerRecord));


    [Fact]
    public async Task Routes_each_line_to_its_registered_record_type()
    {
        using var extractor = NewExtractor(SampleFile);

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Collection
        (
            records,
            r => Assert.Equal("20260320", Assert.IsType<HeaderRecord>(r).BatchDate),
            r => { var d = Assert.IsType<DetailRecord>(r); Assert.Equal(1, d.Id); Assert.Equal("John Smith", d.Name); },
            r => { var d = Assert.IsType<DetailRecord>(r); Assert.Equal(2, d.Id); Assert.Equal("Jane Doe", d.Name); },
            r => Assert.Equal(2, Assert.IsType<TrailerRecord>(r).Count)
        );

        Assert.Equal(4, extractor.CurrentLineNumber);
    }


    [Fact]
    public async Task First_matching_rule_wins()
    {
        // Both rules match a 'D' line; the first registered rule (HeaderRecord) must win.
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\n"))
            .When(l => l[0] == 'D', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord));

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.IsType<HeaderRecord>(record);
    }


    [Fact]
    public async Task Otherwise_routes_unmatched_lines_to_the_fallback_type()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\nX........ ........\n"))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .Otherwise(typeof(HeaderRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.IsType<DetailRecord>(records[0]);
        Assert.IsType<HeaderRecord>(records[1]);   // the 'X' line fell through to the fallback
    }


    [Fact]
    public async Task Unmatched_line_throws_InvalidDataException_by_default()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("Z-unknown\n"))
            .When(l => l[0] == 'D', typeof(DetailRecord));

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Unmatched_line_skipped_when_configured()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\nZ-unknown\n"))
        {
            UnmatchedLineHandling = UnmatchedLineHandling.Skip,
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Single(records);
        Assert.Equal(1, extractor.CurrentFilteredLineCount);   // the 'Z' line was filtered
    }


    [Fact]
    public async Task Blank_lines_are_skipped_by_default_without_calling_predicates()
    {
        // The predicates index line[0]; if a blank line reached them it would throw.
        using var extractor = NewExtractor("H20260320\n\nT00000002\n");

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.Equal(1, extractor.CurrentFilteredLineCount);
    }


    [Fact]
    public async Task Blank_line_is_unmatched_when_SkipBlankLines_is_false()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\n\n"))
        {
            SkipBlankLines = false,
            UnmatchedLineHandling = UnmatchedLineHandling.Skip,
        };
        // Guard the predicate against the empty line that now reaches it.
        extractor.When(l => l.Length > 0 && l[0] == 'D', typeof(DetailRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Single(records);
        Assert.Equal(1, extractor.CurrentFilteredLineCount);
    }


    [Fact]
    public async Task Header_lines_are_skipped()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("BANNER LINE\nD00000001John Smith\n"))
        {
            HasHeader = true,
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.IsType<DetailRecord>(record);
        Assert.Equal(1, extractor.CurrentFilteredLineCount);
        Assert.True(extractor.HasHeader);
    }


    [Fact]
    public async Task FieldDelimiter_is_honored()
    {
        // Detail with a "|" delimiter between the three columns.
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D|00000001|John Smith\n"))
        {
            FieldDelimiter = "|",
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        var record = Assert.IsType<DetailRecord>(Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync()));

        Assert.Equal(1, record.Id);
        Assert.Equal("John Smith", record.Name);
    }


    [Fact]
    public async Task Malformed_matched_line_is_skipped_and_reported()
    {
        var errors = new List<FixedWidthError>();
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D001\nD00000002Jane Doe  \n"))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
            OnError = errors.Add,
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Single(records);                       // the good detail line
        Assert.Equal(1, extractor.CurrentRejectedItemCount);
        var error = Assert.Single(errors);
        Assert.Equal(1, error.ItemNumber);
        Assert.Equal("D001", error.RawContent);
    }


    [Fact]
    public async Task Malformed_matched_line_throws_but_still_reports()
    {
        var errors = new List<FixedWidthError>();
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D001\n"))
        {
            OnError = errors.Add,   // MalformedLineHandling defaults to ThrowException
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        await Assert.ThrowsAnyAsync<Exceptions.MalformedLineException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Single(errors);   // reported before the re-throw
    }


    [Fact]
    public async Task ReturnDefault_malformed_handling_is_not_supported()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D001\n"))
        {
            MalformedLineHandling = MalformedLineHandling.ReturnDefault,
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task No_rules_registered_throws()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\n"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Skip_and_Maximum_item_counts_apply_across_record_types()
    {
        using var extractor = NewExtractor(SampleFile);
        extractor.SkipItemCount = 1;      // skip the header
        extractor.MaximumItemCount = 2;   // then take two

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.IsType<DetailRecord>(records[0]);
        Assert.IsType<DetailRecord>(records[1]);
        Assert.Equal(1, (int)extractor.CurrentSkippedItemCount);
    }


    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((TextReader)null!));
        Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((Stream)null!));

        // The logger is optional — a null logger is tolerated (defaults to NullLogger), not rejected.
        using var withNullReaderLogger = new FixedWidthMultiRecordExtractor(new StringReader(""), logger: null);
        using var withNullStreamLogger = new FixedWidthMultiRecordExtractor(new MemoryStream(), logger: null);
    }


    [Fact]
    public void When_and_Otherwise_validate_arguments()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader(""));

        Assert.Throws<ArgumentNullException>(() => extractor.When(null!, typeof(DetailRecord)));
        Assert.Throws<ArgumentNullException>(() => extractor.When(_ => true, null!));
        Assert.Throws<ArgumentNullException>(() => extractor.Otherwise(null!));
    }


    [Fact]
    public void Registering_a_type_with_a_duplicate_index_throws()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader(""));

        Assert.Throws<InvalidOperationException>(() => extractor.When(_ => true, typeof(DuplicateIndex)));
    }


    [ExcludeFromCodeCoverage]
    private sealed class DuplicateIndex
    {
        [FixedWidthField(0, 4)] public string A { get; set; } = string.Empty;
        [FixedWidthField(0, 4)] public string B { get; set; } = string.Empty;
    }


    [Fact]
    public async Task Stream_constructor_reads_records_and_dispose_releases_internal_reader()
    {
        var bytes = Encoding.UTF8.GetBytes(SampleFile);
        using var stream = new MemoryStream(bytes);
        var extractor = new FixedWidthMultiRecordExtractor(stream)
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .When(l => l[0] == 'T', typeof(TrailerRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();
        extractor.Dispose();

        Assert.Equal(4, records.Count);
        Assert.True(stream.CanRead);   // caller retains the stream; only the internal reader is released
    }


    [Fact]
    public async Task Custom_ValueParser_is_applied()
    {
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader("D00000001John Smith\n"))
        {
            ValueParser = (text, ctx) => string.Equals(ctx.PropertyName, nameof(DetailRecord.Name), StringComparison.Ordinal)
                ? "OVERRIDDEN"
                : FixedWidthConverter.DefaultParser(text, ctx),
        };
        extractor.When(l => l[0] == 'D', typeof(DetailRecord));

        var record = Assert.IsType<DetailRecord>(Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync()));

        Assert.Equal("OVERRIDDEN", record.Name);
    }


    [Fact]
    public async Task ExtractAsync_reports_progress_via_the_injected_timer()
    {
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader(SampleFile), timer)
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .When(l => l[0] == 'T', typeof(TrailerRecord));

        await foreach (var _ in extractor.ExtractAsync(sink, CancellationToken.None))
        {
            timer.Fire();
        }

        Assert.NotEmpty(sink.Reports);
        Assert.Equal(4, (int)sink.Reports.Max(r => r.CurrentItemCount));
        Assert.Equal(4, (int)sink.Reports.Max(r => r.CurrentLineNumber));
    }


    [Fact]
    public async Task ExtractAsync_from_Stream_reports_progress_via_the_injected_timer()
    {
        // The Stream shape previously had no timer-injecting constructor, so only the TextReader
        // shape was testable with a deterministic timer.
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleFile));
        using var extractor = new FixedWidthMultiRecordExtractor(stream, timer)
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .When(l => l[0] == 'T', typeof(TrailerRecord));

        await foreach (var _ in extractor.ExtractAsync(sink, CancellationToken.None))
        {
            timer.Fire();
        }

        Assert.NotEmpty(sink.Reports);
        Assert.Equal(4, (int)sink.Reports.Max(r => r.CurrentItemCount));
    }


    [Fact]
    public void Internal_timer_ctor_accepts_a_logger_as_its_trailing_parameter()
    {
        // Rule 6: the logger is last on internal constructors too. This overload previously took
        // no logger at all.
        using var extractor = new FixedWidthMultiRecordExtractor
        (
            new StringReader(SampleFile),
            new ManualProgressTimer(),
            logger: null
        );

        Assert.NotNull(extractor);
    }


    [Fact]
    public async Task ExtractAsync_with_no_injected_timer_uses_the_base_timer()
    {
        using var extractor = NewExtractor(SampleFile);
        var sink = new CollectingProgress();

        var records = await extractor.ExtractAsync(sink, CancellationToken.None).ToListAsync();

        Assert.Equal(4, records.Count);
    }


    [Fact]
    public async Task ExtractAsync_when_token_already_cancelled_reads_nothing()
    {
        using var extractor = NewExtractor(SampleFile);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await extractor.ExtractAsync(cts.Token).ToListAsync());
    }


    [Fact]
    public async Task ExtractAsync_cancelled_after_the_first_record_stops()
    {
        using var extractor = NewExtractor(SampleFile);
        using var cts = new CancellationTokenSource();
        var seen = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in extractor.ExtractAsync(cts.Token))
            {
                seen++;
                cts.Cancel();
            }
        });

        Assert.Equal(1, seen);
    }


    [Fact]
    public async Task Logging_constructor_emits_start_and_completion_logs()
    {
        var logger = new CapturingLogger<FixedWidthMultiRecordExtractor>();
        using var extractor = new FixedWidthMultiRecordExtractor(new StringReader(SampleFile), logger: logger)
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .Otherwise(typeof(TrailerRecord));   // fallback name is included in the start log

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(4, records.Count);
        Assert.Contains(logger.Messages, m => m.Contains("started", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, m => m.Contains("completed", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public async Task Stream_logging_constructor_reads_records()
    {
        var logger = new CapturingLogger<FixedWidthMultiRecordExtractor>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleFile));
        using var extractor = new FixedWidthMultiRecordExtractor(stream, logger: logger)
            .When(l => l[0] == 'H', typeof(HeaderRecord))
            .When(l => l[0] == 'D', typeof(DetailRecord))
            .When(l => l[0] == 'T', typeof(TrailerRecord));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(4, records.Count);
    }


    [ExcludeFromCodeCoverage]
    private sealed class EncodedRecord
    {
        [FixedWidthField(0, 3)] public string Value { get; set; } = string.Empty;
    }


    [Fact]
    public async Task Encoding_property_decodes_the_stream_with_a_non_default_encoding()
    {
        // 0xE9 is 'é' in Latin-1 but an invalid lead byte under the default UTF-8.
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        var data = latin1.GetBytes("éXY\n");
        using var extractor = new FixedWidthMultiRecordExtractor(new MemoryStream(data), new FixedWidthMultiRecordExtractorOptions { Encoding = latin1 });
        extractor.Otherwise(typeof(EncodedRecord));

        var record = Assert.IsType<EncodedRecord>(Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync()));

        Assert.Equal("éXY", record.Value);   // UTF-8 (the default) would have produced a replacement char
    }


    [ExcludeFromCodeCoverage]
    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>
        (
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, exception));

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }


    [ExcludeFromCodeCoverage]
    private sealed class ManualProgressTimer : IProgressTimer
    {
        private Action? _elapsed;

        public event Action? Elapsed
        {
            add => _elapsed += value;
            remove => _elapsed -= value;
        }

        public void Start(int intervalMilliseconds)
        {
        }

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
