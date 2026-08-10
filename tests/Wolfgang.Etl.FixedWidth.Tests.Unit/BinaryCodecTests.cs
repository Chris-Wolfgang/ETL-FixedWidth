using System;
using System.Globalization;
using Wolfgang.Etl.FixedWidth.Binary;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the COBOL binary-field decoders (#21): <see cref="PackedDecimal"/> (COMP-3) and
/// <see cref="BinaryInteger"/> (COMP).
/// </summary>
public sealed class BinaryCodecTests
{
    private static byte[] Hex(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }


    // -------------------- COMP-3 packed decimal --------------------

    [Theory]
    [InlineData("123C", 0, "123")]     // positive sign nibble C
    [InlineData("123F", 0, "123")]     // unsigned nibble F -> positive
    [InlineData("123D", 0, "-123")]    // negative sign nibble D
    [InlineData("123B", 0, "-123")]    // negative sign nibble B
    [InlineData("0123456C", 2, "1234.56")]   // PIC S9(5)V99, 4 bytes, scale 2
    [InlineData("0C", 0, "0")]
    [InlineData("5C", 0, "5")]          // single byte: one digit + sign
    [InlineData("5D", 0, "-5")]
    [InlineData("00000C", 3, "0.000")]
    [InlineData("9999999C", 0, "9999999")]   // 4 bytes -> 7 digits
    public void PackedDecimal_decodes_value_sign_and_scale(string hex, int scale, string expected)
    {
        if (hex is null)
        {
            throw new ArgumentNullException(nameof(hex));
        }

        var actual = PackedDecimal.Decode(Hex(hex), scale);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), actual);
    }


    [Fact]
    public void PackedDecimal_decodes_a_large_value_beyond_long_range()
    {
        // 10 bytes -> 19 digits: 1234567890123456789 (fits in decimal, exceeds int)
        var actual = PackedDecimal.Decode(Hex("1234567890123456789C"), 0);

        Assert.Equal(1234567890123456789m, actual);
    }


    [Fact]
    public void PackedDecimal_rejects_empty_negative_scale_and_bad_nibbles()
    {
        Assert.Throws<ArgumentException>(() => PackedDecimal.Decode(ReadOnlySpan<byte>.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => PackedDecimal.Decode(Hex("123C"), -1));
        Assert.Throws<FormatException>(() => PackedDecimal.Decode(Hex("A23C")));   // high nibble A is not a digit
        Assert.Throws<FormatException>(() => PackedDecimal.Decode(Hex("1A3C")));   // low nibble A is not a digit
    }


    // -------------------- COMP big-endian integer --------------------

    [Theory]
    [InlineData("7B", true, 123L)]
    [InlineData("85", true, -123L)]                 // 1-byte two's complement
    [InlineData("007B", true, 123L)]
    [InlineData("FF85", true, -123L)]               // 2-byte two's complement
    [InlineData("0100", true, 256L)]                // big-endian order
    [InlineData("FF85", false, 65413L)]             // unsigned magnitude
    [InlineData("0000007B", true, 123L)]
    [InlineData("FFFFFF85", true, -123L)]           // 4-byte two's complement
    [InlineData("FFFFFFFFFFFFFF85", true, -123L)]   // 8-byte two's complement
    [InlineData("0000000000000100", true, 256L)]
    public void BinaryInteger_decodes_big_endian_two_complement(string hex, bool signed, long expected)
    {
        if (hex is null)
        {
            throw new ArgumentNullException(nameof(hex));
        }

        Assert.Equal(expected, BinaryInteger.Decode(Hex(hex), signed));
    }


    [Fact]
    public void BinaryInteger_rejects_empty_and_oversized()
    {
        Assert.Throws<ArgumentException>(() => BinaryInteger.Decode(ReadOnlySpan<byte>.Empty));
        Assert.Throws<ArgumentException>(() => BinaryInteger.Decode(new byte[9]));
    }


    // -------------------- encode round-trips --------------------

    [Theory]
    [InlineData("1234.56", 2, 4)]
    [InlineData("-0.05", 2, 5)]
    [InlineData("0", 0, 1)]
    [InlineData("9999999", 0, 4)]
    [InlineData("-123", 0, 2)]
    [InlineData("1234.567", 2, 4)]   // truncated to 1234.56 on encode
    public void PackedDecimal_encode_round_trips(string value, int scale, int byteLength)
    {
        var v = decimal.Parse(value, CultureInfo.InvariantCulture);
        var expected = Math.Truncate(Math.Abs(v) * Pow10(scale)) / Pow10(scale) * Math.Sign(v == 0 ? 1 : v);

        var buffer = new byte[byteLength];
        PackedDecimal.Encode(v, scale, buffer);

        Assert.Equal(expected, PackedDecimal.Decode(buffer, scale));
    }


    [Fact]
    public void PackedDecimal_encode_overflows_when_too_many_digits()
    {
        Assert.Throws<OverflowException>(() => PackedDecimal.Encode(12345m, 0, new byte[2]));   // 2 bytes -> 3 digits max
    }


    [Theory]
    [InlineData(123L, true, 2)]
    [InlineData(-123L, true, 2)]
    [InlineData(256L, true, 4)]
    [InlineData(65413L, false, 4)]
    [InlineData(-1L, true, 8)]
    public void BinaryInteger_encode_round_trips(long value, bool signed, int byteLength)
    {
        var buffer = new byte[byteLength];
        BinaryInteger.Encode(value, signed, buffer);

        Assert.Equal(value, BinaryInteger.Decode(buffer, signed));
    }


    [Fact]
    public void BinaryInteger_encode_overflows_when_out_of_range()
    {
        Assert.Throws<OverflowException>(() => BinaryInteger.Encode(300, signed: true, new byte[1]));   // max 127
        Assert.Throws<OverflowException>(() => BinaryInteger.Encode(-1, signed: false, new byte[2]));   // unsigned
    }


    [Fact]
    public void Encode_rejects_bad_destinations_and_scale()
    {
        Assert.Throws<ArgumentException>(() => PackedDecimal.Encode(1m, 0, Array.Empty<byte>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PackedDecimal.Encode(1m, -1, new byte[4]));
        Assert.Throws<ArgumentException>(() => BinaryInteger.Encode(1, signed: true, Array.Empty<byte>()));
        Assert.Throws<ArgumentException>(() => BinaryInteger.Encode(1, signed: true, new byte[9]));
    }

    private static decimal Pow10(int scale)
    {
        decimal r = 1m;
        for (var i = 0; i < scale; i++)
        {
            r *= 10m;
        }

        return r;
    }
}
