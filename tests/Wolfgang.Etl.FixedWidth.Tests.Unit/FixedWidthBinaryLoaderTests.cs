using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthBinaryLoader{TRecord}"/> (#21) — writing fixed-length binary records
/// (text, COMP, COMP-3), verified primarily by round-tripping through the extractor.
/// </summary>
public sealed class FixedWidthBinaryLoaderTests
{
    [ExcludeFromCodeCoverage]
    public sealed class Account
    {
        [FixedWidthBinaryField(0, 8, BinaryFieldType.Text)]
        public string AccountId { get; set; } = string.Empty;

        [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
        public int TransactionCount { get; set; }

        [FixedWidthBinaryField(2, 5, BinaryFieldType.PackedDecimal, Scale = 2)]
        public decimal Balance { get; set; }
    }


#pragma warning disable CS1998 // synchronous sample sequence — no await needed
    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998


    [Fact]
    public async Task Loader_writes_records_the_extractor_reads_back_unchanged()
    {
        var accounts = new[]
        {
            new Account { AccountId = "ACCT0001", TransactionCount = 42, Balance = 1234.56m },
            new Account { AccountId = "ACCT0002", TransactionCount = 7, Balance = -0.05m },
        };

        using var ms = new MemoryStream();
        using (var loader = new FixedWidthBinaryLoader<Account>(ms))
        {
            Assert.Equal(17, loader.RecordByteLength);
            await loader.LoadAsync(ToAsync(accounts), CancellationToken.None);
        }

        Assert.Equal(34, ms.Length);   // 2 × 17-byte records, no delimiters

        ms.Position = 0;
        using var extractor = new FixedWidthBinaryExtractor<Account>(ms);
        var read = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(2, read.Count);
        Assert.Equal("ACCT0001", read[0].AccountId);
        Assert.Equal(42, read[0].TransactionCount);
        Assert.Equal(1234.56m, read[0].Balance);
        Assert.Equal("ACCT0002", read[1].AccountId);
        Assert.Equal(7, read[1].TransactionCount);
        Assert.Equal(-0.05m, read[1].Balance);
    }


    [Fact]
    public async Task Load_with_a_logger_writes_the_start_information_log()
    {
        var accounts = new[] { new Account { AccountId = "ACCT0001", TransactionCount = 42, Balance = 1234.56m } };
        var logger = new SpyLogger<FixedWidthBinaryLoader<Account>>();
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms, logger: logger);

        await loader.LoadAsync(ToAsync(accounts), CancellationToken.None);

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Information
                && e.Message.Contains("Binary load started", StringComparison.Ordinal)
                && e.Message.Contains("Account", StringComparison.Ordinal)
        );
    }



    [Fact]
    public async Task Text_value_longer_than_the_field_throws_FieldOverflowException()
    {
        var accounts = new[] { new Account { AccountId = "TOO-LONG-ACCOUNT-ID", TransactionCount = 1, Balance = 0m } };
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms);

        await Assert.ThrowsAsync<Exceptions.FieldOverflowException>(async () =>
            await loader.LoadAsync(ToAsync(accounts), CancellationToken.None));
    }


    [ExcludeFromCodeCoverage]
    private sealed class ReadOnlyStream : MemoryStream
    {
        public override bool CanWrite => false;
    }


    [Fact]
    public void Constructor_validates_the_stream()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryLoader<Account>(null!));
        Assert.Throws<ArgumentException>(() => new FixedWidthBinaryLoader<Account>(new ReadOnlyStream()));
    }


    [Fact]
    public async Task LoadAsync_rejects_a_null_source()
    {
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await loader.LoadAsync(null!, CancellationToken.None));
    }


    [Fact]
    public async Task LoadAsync_when_token_already_cancelled_writes_nothing()
    {
        var accounts = new[] { new Account { AccountId = "A", TransactionCount = 1, Balance = 1m } };
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await loader.LoadAsync(ToAsync(accounts), cts.Token));

        Assert.Equal(0, ms.Length);
    }


    [Fact]
    public async Task LoadAsync_cancelled_mid_stream_stops_writing_the_next_record()
    {
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await loader.LoadAsync(CancelAfterFirst(cts), cts.Token));

        Assert.Equal(17, ms.Length);   // only the first record was written before cancellation
    }


#pragma warning disable CS1998
    private static async IAsyncEnumerable<Account> CancelAfterFirst(CancellationTokenSource cts)
    {
        yield return new Account { AccountId = "A", TransactionCount = 1, Balance = 1m };
        cts.Cancel();
        yield return new Account { AccountId = "B", TransactionCount = 2, Balance = 2m };
    }
#pragma warning restore CS1998


    [Fact]
    public async Task A_multibyte_text_value_in_a_fixed_byte_field_throws()
    {
        // "€" is one char but three UTF-8 bytes; padded to 8 chars it encodes to more than 8 bytes.
        var accounts = new[] { new Account { AccountId = "€", TransactionCount = 1, Balance = 0m } };
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms, new FixedWidthBinaryLoaderOptions { Encoding = System.Text.Encoding.UTF8 });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await loader.LoadAsync(ToAsync(accounts), CancellationToken.None));
    }


    [Fact]
    public async Task LoadAsync_with_progress_and_no_injected_timer_uses_the_base_timer()
    {
        var accounts = new[] { new Account { AccountId = "A", TransactionCount = 1, Balance = 1m } };
        using var ms = new MemoryStream();
        using var loader = new FixedWidthBinaryLoader<Account>(ms);
        var sink = new CollectingProgress();

        await loader.LoadAsync(ToAsync(accounts), sink, CancellationToken.None);

        Assert.Equal(17, ms.Length);
    }


    [Fact]
    public async Task LoadAsync_reports_progress_via_the_injected_timer()
    {
        var accounts = new[]
        {
            new Account { AccountId = "A", TransactionCount = 1, Balance = 1m },
            new Account { AccountId = "B", TransactionCount = 2, Balance = 2m },
        };
        using var ms = new MemoryStream();
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var loader = new FixedWidthBinaryLoader<Account>(ms, timer);

        // The loader consumes the sequence internally; fire the timer from the source as each item flows.
        await loader.LoadAsync(Fired(accounts, timer), sink, CancellationToken.None);

        Assert.NotEmpty(sink.Reports);
        Assert.Equal(2, (int)sink.Reports.Max(r => r.CurrentItemCount));
        Assert.Equal(2, (int)sink.Reports.Max(r => r.CurrentLineNumber));   // record counter advances per record
    }


#pragma warning disable CS1998
    private static async IAsyncEnumerable<Account> Fired(IEnumerable<Account> items, ManualProgressTimer timer)
    {
        foreach (var item in items)
        {
            yield return item;
            timer.Fire();
        }
    }
#pragma warning restore CS1998


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
    [Fact]
    public void Internal_timer_ctor_accepts_a_logger_as_its_trailing_parameter()
    {
        // Rule 6: the logger is last on internal constructors too. This overload previously took
        // a timer but no logger, so a test could inject one or the other, never both.
        using var sut = new FixedWidthBinaryLoader<Account>
        (
            new MemoryStream(),
            new ManualProgressTimer(),
            logger: null
        );

        Assert.NotNull(sut);
    }


    private sealed class CollectingProgress : IProgress<FixedWidthReport>
    {
        public List<FixedWidthReport> Reports { get; } = new();

        public void Report(FixedWidthReport value) => Reports.Add(value);
    }
}
