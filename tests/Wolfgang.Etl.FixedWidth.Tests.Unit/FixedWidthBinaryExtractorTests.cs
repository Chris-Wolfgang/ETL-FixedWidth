using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null);

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
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(Array.Empty<byte>()), options: null);

        Assert.Empty(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }



    [Fact]
    public async Task Extract_with_a_logger_writes_the_start_information_log()
    {
        var data = Concat(Record("ACCT0001", 42, Balance1234_56));
        var logger = new SpyLogger<FixedWidthBinaryExtractor<Account>>();
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null, logger: logger);

        await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Contains
        (
            logger.Entries,
            e => e.Level == LogLevel.Information
                && e.Message.Contains("Binary extraction started", StringComparison.Ordinal)
                && e.Message.Contains("Account", StringComparison.Ordinal)
        );
    }


    [Fact]
    public async Task Skip_and_Maximum_item_counts_are_honored()
    {
        var data = Concat(
            Record("A", 1, Balance1234_56),
            Record("B", 2, Balance1234_56),
            Record("C", 3, Balance1234_56),
            Record("D", 4, Balance1234_56));
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null)
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
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null);

        await Assert.ThrowsAsync<EndOfStreamException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task ExtractAsync_when_token_already_cancelled_throws_OperationCanceledException()
    {
        var data = Record("ACCT0001", 42, Balance1234_56);
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await extractor.ExtractAsync(cts.Token).ToListAsync());
    }


    [Fact]
    public async Task ExtractAsync_cancelled_after_the_first_record_stops_iterating()
    {
        var data = Concat(Record("A", 1, Balance1234_56), Record("B", 2, Balance1234_56));
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), options: null);
        using var cts = new CancellationTokenSource();
        var seen = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in extractor.ExtractAsync(cts.Token))
            {
                seen++;
                cts.Cancel();   // the next iteration's in-loop guard must observe this
            }
        });

        Assert.Equal(1, seen);
    }


    [Fact]
    public async Task Extract_uses_the_supplied_encoding_for_text_fields()
    {
        // 0xE9 is 'é' in Latin-1 but decodes to '?' under the default ASCII encoding.
        var record = new byte[17];
        record[0] = 0xE9;
        for (var i = 1; i < 8; i++)
        {
            record[i] = (byte)' ';
        }

        Balance1234_56.CopyTo(record, 12);   // a valid packed value so decoding the record succeeds
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(record), new FixedWidthBinaryExtractorOptions { Encoding = latin1 });

        var account = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal("é", account.AccountId);   // ASCII would have produced "?"
    }


    [ExcludeFromCodeCoverage]
    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }


    [Fact]
    public void Constructor_validates_stream_and_layout()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryExtractor<Account>(null!, options: null));
        Assert.Throws<ArgumentException>(() => new FixedWidthBinaryExtractor<Account>(new WriteOnlyStream(), options: null));
    }


    [ExcludeFromCodeCoverage]
    private sealed class NoBinaryFields
    {
        public string Name { get; set; } = string.Empty;
    }


    [Fact]
    public void A_record_type_without_binary_fields_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new FixedWidthBinaryExtractor<NoBinaryFields>(new MemoryStream(), options: null));
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
        using var extractor = new FixedWidthBinaryExtractor<NullableAccount>(new MemoryStream(data), options: null);

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
        => Assert.Throws<InvalidOperationException>(() => new FixedWidthBinaryExtractor<DuplicateIndex>(new MemoryStream(), options: null));


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
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(Record("A", 1, Balance1234_56)), options: null);
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
        using var extractor = new FixedWidthBinaryExtractor<Account>(new MemoryStream(data), timer, options: null);

        await foreach (var _ in extractor.ExtractAsync(sink, CancellationToken.None))
        {
            timer.Fire();
        }

        Assert.NotEmpty(sink.Reports);
        Assert.Equal(2, (int)sink.Reports.Max(r => r.CurrentItemCount));
        Assert.Equal(2, (int)sink.Reports.Max(r => r.CurrentLineNumber));   // record counter advances per record
    }


    [Fact]
    public void Attribute_validates_arguments_and_defaults_signed_to_true()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryFieldAttribute(-1, 4, BinaryFieldType.Binary));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryFieldAttribute(0, 0, BinaryFieldType.Binary));

        var attribute = new FixedWidthBinaryFieldAttribute(2, 4, BinaryFieldType.Binary);

        Assert.True(attribute.Signed);   // signed defaults to true
        Assert.Equal(2, attribute.Index);
        Assert.Equal(4, attribute.ByteLength);
    }


    [ExcludeFromCodeCoverage]
    private sealed class UnsignedBig
    {
        [FixedWidthBinaryField(0, 8, BinaryFieldType.Binary, Signed = false)]
        public ulong Value { get; set; }
    }


    [ExcludeFromCodeCoverage]
    private sealed class SignedBig
    {
        [FixedWidthBinaryField(0, 8, BinaryFieldType.Binary, Signed = false)]
        public long Value { get; set; }
    }


#pragma warning disable CS1998 // synchronous sample sequence
    private static async System.Collections.Generic.IAsyncEnumerable<T> ToAsync<T>(System.Collections.Generic.IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998


    [Fact]
    public async Task Eight_byte_unsigned_field_maps_its_full_range_to_a_ulong_property()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };   // ulong.MaxValue
        using var extractor = new FixedWidthBinaryExtractor<UnsignedBig>(new MemoryStream(data), options: null);

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(ulong.MaxValue, record.Value);
    }


    [Fact]
    public async Task Eight_byte_unsigned_value_above_Int64_max_throws_for_a_long_property()
    {
        var data = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };   // 2^63, one past long.MaxValue

        using var extractor = new FixedWidthBinaryExtractor<SignedBig>(new MemoryStream(data), options: null);

        await Assert.ThrowsAnyAsync<OverflowException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Unsigned_binary_value_round_trips_through_the_loader()
    {
        using var ms = new MemoryStream();
        using (var loader = new FixedWidthBinaryLoader<UnsignedBig>(ms, options: null))
        {
            await loader.LoadAsync(ToAsync(new[] { new UnsignedBig { Value = ulong.MaxValue } }), CancellationToken.None);
        }

        ms.Position = 0;
        using var extractor = new FixedWidthBinaryExtractor<UnsignedBig>(ms, options: null);
        var read = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(ulong.MaxValue, read.Value);
    }


    [ExcludeFromCodeCoverage]
    private sealed class FractionalToInt
    {
        [FixedWidthBinaryField(0, 4, BinaryFieldType.PackedDecimal, Scale = 2)]
        public int Amount { get; set; }
    }


    [ExcludeFromCodeCoverage]
    private sealed class WholeToInt
    {
        [FixedWidthBinaryField(0, 4, BinaryFieldType.PackedDecimal, Scale = 0)]
        public int Amount { get; set; }
    }


    [ExcludeFromCodeCoverage]
    private sealed class FractionalToDouble
    {
        [FixedWidthBinaryField(0, 4, BinaryFieldType.PackedDecimal, Scale = 2)]
        public double Amount { get; set; }
    }


    [Fact]
    public async Task Fractional_packed_decimal_into_an_integer_property_throws()
    {
        var data = new byte[] { 0x01, 0x23, 0x45, 0x6C };   // 0123456 @ Scale 2 = 1234.56 (fractional)
        using var extractor = new FixedWidthBinaryExtractor<FractionalToInt>(new MemoryStream(data), options: null);

        await Assert.ThrowsAnyAsync<OverflowException>(async () =>
            await extractor.ExtractAsync(CancellationToken.None).ToListAsync());
    }


    [Fact]
    public async Task Whole_packed_decimal_into_an_integer_property_succeeds()
    {
        var data = new byte[] { 0x00, 0x01, 0x23, 0x4C };   // 0001234 @ Scale 0 = 1234 (exact)
        using var extractor = new FixedWidthBinaryExtractor<WholeToInt>(new MemoryStream(data), options: null);

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(1234, record.Amount);
    }


    [Fact]
    public async Task Fractional_packed_decimal_into_a_floating_point_property_is_allowed()
    {
        var data = new byte[] { 0x01, 0x23, 0x45, 0x6C };   // 1234.56
        using var extractor = new FixedWidthBinaryExtractor<FractionalToDouble>(new MemoryStream(data), options: null);

        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(1234.56, record.Amount, 2);
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
    [Fact]
    public void Internal_timer_ctor_accepts_a_logger_as_its_trailing_parameter()
    {
        // Rule 6: the logger is last on internal constructors too. This overload previously took
        // a timer but no logger, so a test could inject one or the other, never both.
        using var sut = new FixedWidthBinaryExtractor<Account>
        (
            new MemoryStream(Concat(Record("A", 1, Balance1234_56))),
            new ManualProgressTimer(),
            logger: null
        , options: null);

        Assert.NotNull(sut);
    }


    private sealed class CollectingProgress : IProgress<FixedWidthReport>
    {
        public System.Collections.Generic.List<FixedWidthReport> Reports { get; } = new();

        public void Report(FixedWidthReport value) => Reports.Add(value);
    }
}
