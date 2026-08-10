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
}
