using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Binary;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthBinaryExtractor{TRecord}"/> (#21) — reading fixed-length binary
/// (mainframe) records with text, COMP (binary integer), and COMP-3 (packed decimal) fields.
/// </summary>
public sealed class FixedWidthBinaryExtractorTests
{
    [ExcludeFromCodeCoverage]
    private sealed class Account
    {
        [FixedWidthBinaryField(0, 8, BinaryFieldType.Text)]
        public string AccountId { get; set; } = string.Empty;

        [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
        public int TransactionCount { get; set; }

        [FixedWidthBinaryField(2, 5, BinaryFieldType.PackedDecimal, Scale = 2)]
        public decimal Balance { get; set; }
    }


    private static byte[] Record(string accountId, int txnCount, byte[] packedBalance)
    {
        var bytes = new byte[17];
        Encoding.ASCII.GetBytes(accountId.PadRight(8)).CopyTo(bytes, 0);
        bytes[8] = (byte)(txnCount >> 24);
        bytes[9] = (byte)(txnCount >> 16);
        bytes[10] = (byte)(txnCount >> 8);
        bytes[11] = (byte)txnCount;
        packedBalance.CopyTo(bytes, 12);
        return bytes;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        using var ms = new MemoryStream();
        foreach (var p in parts)
        {
            ms.Write(p, 0, p.Length);
        }

        return ms.ToArray();
    }

    private static readonly byte[] Balance1234_56 = { 0x00, 0x01, 0x23, 0x45, 0x6C };   // +1234.56
    private static readonly byte[] BalanceNeg0_05 = { 0x00, 0x00, 0x00, 0x00, 0x5D };   // -0.05 (digit 5 in the final byte's high nibble, sign D)


    [Fact]
    public async Task Extract_decodes_text_binary_and_packed_decimal_fields()
    {
        var data = Concat(Record("ACCT0001", 42, Balance1234_56), Record("ACCT0002", 7, BalanceNeg0_05));
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data));

        Assert.Equal(17, extractor.RecordByteLength);

        var accounts = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(2, accounts.Count);
        Assert.Equal("ACCT0001", accounts[0].AccountId);
        Assert.Equal(42, accounts[0].TransactionCount);
        Assert.Equal(1234.56m, accounts[0].Balance);
        Assert.Equal("ACCT0002", accounts[1].AccountId);
        Assert.Equal(7, accounts[1].TransactionCount);
        Assert.Equal(-0.05m, accounts[1].Balance);
    }


    [Fact]
    public async Task Empty_stream_yields_no_records()
    {
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(Array.Empty<byte>()));

        Assert.Empty(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Skip_and_Maximum_item_counts_are_honored()
    {
        var data = Concat(
            Record("A", 1, Balance1234_56),
            Record("B", 2, Balance1234_56),
            Record("C", 3, Balance1234_56),
            Record("D", 4, Balance1234_56));
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data))
        {
            SkipItemCount = 1,
            MaximumItemCount = 2,
        };

        var accounts = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "B", "C" }, accounts.Select(a => a.AccountId));
    }


    [Fact]
    public async Task A_partial_trailing_record_throws_EndOfStreamException()
    {
        var data = Concat(Record("ACCT0001", 42, Balance1234_56), new byte[] { 0x01, 0x02, 0x03 });   // 3 stray bytes
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data));

        await Assert.ThrowsAsync<EndOfStreamException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task ExtractAsync_when_token_already_cancelled_reads_nothing()
    {
        var data = Record("ACCT0001", 42, Balance1234_56);
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await extractor.ExtractAsync(cts.Token).ToListAsync());
    }


    [ExcludeFromCodeCoverage]
    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }


    [Fact]
    public void Constructor_validates_stream_and_layout()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryExtractor<Account>(null!));
        Assert.Throws<ArgumentException>(() => new FixedWidthBinaryExtractor<Account>(new WriteOnlyStream()));
    }


    [ExcludeFromCodeCoverage]
    private sealed class NoBinaryFields
    {
        public string Name { get; set; } = string.Empty;
    }


    [Fact]
    public void A_record_type_without_binary_fields_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new FixedWidthBinaryExtractor<NoBinaryFields>(new MemoryStream()));
    }


    [ExcludeFromCodeCoverage]
    private sealed class NullableAccount
    {
        [FixedWidthBinaryField(0, 5, BinaryFieldType.PackedDecimal, Scale = 2)]
        public decimal? Balance { get; set; }

        [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
        public long? Count { get; set; }
    }


    [Fact]
    public async Task Nullable_numeric_fields_are_decoded()
    {
        var data = Concat(new byte[] { 0x00, 0x01, 0x23, 0x45, 0x6C, 0x00, 0x00, 0x00, 0x2A });   // 1234.56 + 42
        using var extractor = new FixedWidthBinaryExtractor<NullableAccount>(new MemoryStream(data));

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(1234.56m, record.Balance);
        Assert.Equal(42L, record.Count);
    }


    [ExcludeFromCodeCoverage]
    private sealed class DuplicateIndex
    {
        [FixedWidthBinaryField(0, 4, BinaryFieldType.Binary)]
        public int A { get; set; }

        [FixedWidthBinaryField(0, 4, BinaryFieldType.Binary)]
        public int B { get; set; }
    }


    [Fact]
    public void Duplicate_field_index_throws()
        => Assert.Throws<InvalidOperationException>(() => new FixedWidthBinaryExtractor<DuplicateIndex>(new MemoryStream()));


    [Fact]
    public void Descriptor_with_an_unknown_field_type_throws_on_decode_and_encode()
    {
        var prop = typeof(Account).GetProperty(nameof(Account.TransactionCount))!;
        var attribute = new FixedWidthBinaryFieldAttribute(0, 4, (BinaryFieldType)99);
        var descriptor = new BinaryFieldDescriptor(prop, attribute, 0, (_, _) => { }, _ => 0);

        Assert.Throws<InvalidOperationException>(() => descriptor.Decode(new byte[4], Encoding.ASCII));
        Assert.Throws<InvalidOperationException>(() => descriptor.Encode(new Account(), new byte[4], Encoding.ASCII));
    }


    [Fact]
    public async Task ExtractAsync_with_progress_and_no_injected_timer_uses_the_base_timer()
    {
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(Record("A", 1, Balance1234_56)));
        var sink = new CollectingProgress();

        var result = await extractor.ExtractAsync(sink, CancellationToken.None).ToListAsync();

        Assert.Single(result);
    }


    [Fact]
    public async Task ExtractAsync_reports_progress_via_the_injected_timer()
    {
        var data = Concat(Record("A", 1, Balance1234_56), Record("B", 2, Balance1234_56));
        var timer = new ManualProgressTimer();
        var sink = new CollectingProgress();
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), timer);

        await foreach (var _ in extractor.ExtractAsync(sink, CancellationToken.None))
        {
            timer.Fire();
        }

        Assert.NotEmpty(sink.Reports);
        Assert.Equal(2, (int)sink.Reports.Max(r => r.CurrentItemCount));
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
        public System.Collections.Generic.List<FixedWidthReport> Reports { get; } = new();

        public void Report(FixedWidthReport value) => Reports.Add(value);
    }
}
