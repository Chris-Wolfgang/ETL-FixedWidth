using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers byte-offset checkpoint / resume on <see cref="FixedWidthExtractor{TRecord}"/> (#31):
/// <c>TrackByteOffset</c>, <c>CurrentByteOffset</c>, and <c>StartByteOffset</c>.
/// </summary>
public sealed class FixedWidthExtractorCheckpointTests
{
    [ExcludeFromCodeCoverage]
    private sealed class Rec
    {
        [FixedWidthField(0, 3)] public string Code { get; set; } = string.Empty;
        [FixedWidthField(1, 5)] public int Value { get; set; }
    }


    // Three 8-char records; each line + '\n' is 9 ASCII bytes.
    private const string ThreeRecords = "ABC00001\nDEF00002\nGHI00003\n";


    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);


    [Fact]
    public async Task CurrentByteOffset_advances_one_line_at_a_time()
    {
        using var extractor = new FixedWidthExtractor<Rec>(new MemoryStream(Ascii(ThreeRecords))) { TrackByteOffset = true };

        var offsets = new List<long>();
        await foreach (var _ in extractor.ExtractAsync(CancellationToken.None))
        {
            offsets.Add(extractor.CurrentByteOffset);
        }

        Assert.Equal(new long[] { 9, 18, 27 }, offsets);
    }


    [Fact]
    public async Task Resume_from_a_checkpoint_yields_only_the_remaining_records()
    {
        var bytes = Ascii(ThreeRecords);

        // First run: process one record, then "crash" — capturing the checkpoint.
        long checkpoint;
        using (var first = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { TrackByteOffset = true })
        {
            checkpoint = 0;
            await foreach (var r in first.ExtractAsync(CancellationToken.None))
            {
                Assert.Equal("ABC", r.Code);
                checkpoint = first.CurrentByteOffset;
                break;
            }
        }

        Assert.Equal(9, checkpoint);

        // Resume: seek to the checkpoint and read the rest.
        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { StartByteOffset = checkpoint };
        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "DEF", "GHI" }, rest.Select(r => r.Code));
        Assert.Equal(new[] { 2, 3 }, rest.Select(r => r.Value));
        Assert.Equal(27, resumed.CurrentByteOffset);
    }


    [Fact]
    public async Task Resume_works_with_crlf_line_endings()
    {
        var bytes = Ascii("ABC00001\r\nDEF00002\r\nGHI00003\r\n");   // each line is 10 bytes

        long checkpoint;
        using (var first = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { TrackByteOffset = true })
        {
            checkpoint = 0;
            await foreach (var _ in first.ExtractAsync(CancellationToken.None))
            {
                checkpoint = first.CurrentByteOffset;
                break;
            }
        }

        Assert.Equal(10, checkpoint);

        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { StartByteOffset = checkpoint };
        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "DEF", "GHI" }, rest.Select(r => r.Code));
    }


    [Fact]
    public async Task Header_is_not_reskipped_on_resume()
    {
        var bytes = Ascii("HDR\nABC00001\nDEF00002\n");   // "HDR\n" = 4 bytes, then two 9-byte records

        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes))
        {
            HasHeader = true,          // still set, but must be ignored on resume
            StartByteOffset = 4 + 9,   // past the header and the first record
        };

        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "DEF" }, rest.Select(r => r.Code));   // DEF is NOT swallowed as a header
    }


    [Fact]
    public async Task Resume_counts_multibyte_utf8_bytes()
    {
        // The Code field holds 'é' (2 UTF-8 bytes) so a line is 9 chars but 10 bytes.
        var bytes = Encoding.UTF8.GetBytes("émA00001\némB00002\n");

        long checkpoint;
        using (var first = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { TrackByteOffset = true })
        {
            checkpoint = 0;
            await foreach (var _ in first.ExtractAsync(CancellationToken.None))
            {
                checkpoint = first.CurrentByteOffset;
                break;
            }
        }

        Assert.Equal(10, checkpoint);   // "émA00001\n" = 9 bytes content + 1 for the extra byte of 'é' ... = 10

        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { StartByteOffset = checkpoint };
        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Single(rest);
        Assert.Equal(2, rest[0].Value);
    }


    [Fact]
    public async Task Resume_accounts_for_a_utf8_byte_order_mark()
    {
        var bom = Encoding.UTF8.GetPreamble();   // EF BB BF, 3 bytes
        var content = Ascii(ThreeRecords);
        var bytes = new byte[bom.Length + content.Length];
        bom.CopyTo(bytes, 0);
        content.CopyTo(bytes, bom.Length);

        long checkpoint;
        using (var first = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { TrackByteOffset = true })
        {
            checkpoint = 0;
            await foreach (var _ in first.ExtractAsync(CancellationToken.None))
            {
                checkpoint = first.CurrentByteOffset;
                break;
            }
        }

        Assert.Equal(bom.Length + 9, checkpoint);   // BOM is counted so the offset aligns with real bytes

        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes)) { StartByteOffset = checkpoint };
        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "DEF", "GHI" }, rest.Select(r => r.Code));
    }


    [Fact]
    public async Task SkipItemCount_applies_from_the_resumed_position()
    {
        var bytes = Ascii(ThreeRecords);

        using var resumed = new FixedWidthExtractor<Rec>(new MemoryStream(bytes))
        {
            StartByteOffset = 9,   // resume at DEF
            SkipItemCount = 1,     // then skip DEF
        };

        var rest = await resumed.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "GHI" }, rest.Select(r => r.Code));
    }


    [Fact]
    public void CurrentByteOffset_throws_when_tracking_is_not_enabled()
    {
        using var extractor = new FixedWidthExtractor<Rec>(new MemoryStream(Ascii(ThreeRecords)));

        Assert.Throws<InvalidOperationException>(() => _ = extractor.CurrentByteOffset);
    }


    [Fact]
    public void StartByteOffset_rejects_a_negative_value()
    {
        using var extractor = new FixedWidthExtractor<Rec>(new MemoryStream(Ascii(ThreeRecords)));

        Assert.Throws<ArgumentOutOfRangeException>(() => extractor.StartByteOffset = -1);
    }


    [Fact]
    public async Task Tracking_without_a_stream_constructor_throws()
    {
        using var extractor = new FixedWidthExtractor<Rec>(new StringReader(ThreeRecords)) { TrackByteOffset = true };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [ExcludeFromCodeCoverage]
    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer) : base(buffer)
        {
        }

        public override bool CanSeek => false;
    }


    [Fact]
    public async Task StartByteOffset_on_a_non_seekable_stream_throws()
    {
        using var extractor = new FixedWidthExtractor<Rec>(new NonSeekableStream(Ascii(ThreeRecords))) { StartByteOffset = 9 };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Tracking_with_a_bomless_encoding_reports_offsets_from_zero()
    {
        // ASCII has no byte-order-mark preamble, exercising the preamble short-circuit.
        using var extractor = new FixedWidthExtractor<Rec>(new MemoryStream(Ascii(ThreeRecords)), new FixedWidthExtractorOptions { Encoding = Encoding.ASCII }) { TrackByteOffset = true };

        var offsets = new List<long>();
        await foreach (var _ in extractor.ExtractAsync(CancellationToken.None))
        {
            offsets.Add(extractor.CurrentByteOffset);
        }

        Assert.Equal(new long[] { 9, 18, 27 }, offsets);
    }


    [Fact]
    public async Task Byte_tracking_works_with_the_injected_timer_constructor()
    {
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var extractor = new FixedWidthExtractor<Rec>(new MemoryStream(Ascii(ThreeRecords)), timer) { TrackByteOffset = true };

        await foreach (var _ in extractor.ExtractAsync(sink, CancellationToken.None))
        {
            timer.Fire();
        }

        Assert.Equal(27, extractor.CurrentByteOffset);
        Assert.NotEmpty(sink.Reports);
    }


    [ExcludeFromCodeCoverage]
    private sealed class ManualProgressTimer : Abstractions.IProgressTimer
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
