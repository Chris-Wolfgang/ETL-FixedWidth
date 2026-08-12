using System;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Decodes COBOL <c>COMP</c> / <c>COMP-4</c> binary integer fields (#21): a big-endian
/// (most-significant byte first) integer of 1–8 bytes, signed as two's complement by default.
/// </summary>
internal static class BinaryInteger
{
    /// <summary>
    /// Decodes a big-endian binary integer.
    /// </summary>
    /// <param name="bytes">The raw bytes, most-significant first (1–8 bytes).</param>
    /// <param name="signed">
    /// <see langword="true"/> (default) to interpret the value as two's-complement signed;
    /// <see langword="false"/> for an unsigned magnitude.
    /// </param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes"/> is empty or longer than 8 bytes.</exception>
    internal static long Decode(ReadOnlySpan<byte> bytes, bool signed = true)
    {
        var magnitude = DecodeUnsigned(bytes);
        var bits = bytes.Length * 8;

        if (signed && bits < 64)
        {
            // Sign-extend when the top bit of the most-significant byte is set.
            var signBit = 1UL << (bits - 1);
            if ((magnitude & signBit) != 0)
            {
                magnitude |= ~((1UL << bits) - 1);
            }
        }

        return unchecked((long)magnitude);
    }


    /// <summary>
    /// Decodes a big-endian binary integer as an <b>unsigned</b> magnitude. Unlike
    /// <see cref="Decode"/> (which returns <see cref="long"/>), this preserves the full 0…2⁶⁴−1
    /// range, so an 8-byte <c>COMP</c> field with the high bit set is not misread as a negative
    /// value. The caller converts the result to the target property type — a value above
    /// <see cref="long.MaxValue"/> landing in a signed property then overflows (throws) rather than
    /// wrapping to a negative number.
    /// </summary>
    /// <param name="bytes">The raw bytes, most-significant first (1–8 bytes).</param>
    /// <returns>The decoded unsigned value.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes"/> is empty or longer than 8 bytes.</exception>
    internal static ulong DecodeUnsigned(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > 8)
        {
            throw new ArgumentException("A binary integer field must be 1–8 bytes.", nameof(bytes));
        }

        ulong magnitude = 0;
        foreach (var b in bytes)
        {
            magnitude = (magnitude << 8) | b;
        }

        return magnitude;
    }


    /// <summary>
    /// Encodes <paramref name="value"/> as a big-endian binary integer into
    /// <paramref name="destination"/>, whose length (1–8 bytes) is the field width.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="signed">Whether the field is two's-complement signed.</param>
    /// <param name="destination">The field bytes to write.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is empty or longer than 8 bytes.</exception>
    /// <exception cref="OverflowException">The value does not fit in the field width.</exception>
    internal static void Encode(long value, bool signed, Span<byte> destination)
    {
        var byteLength = destination.Length;
        if (byteLength is < 1 or > 8)
        {
            throw new ArgumentException("A binary integer field must be 1–8 bytes.", nameof(destination));
        }

        if (!FitsInField(value, byteLength, signed))
        {
            throw new OverflowException($"Value {value} does not fit in a {byteLength}-byte {(signed ? "signed" : "unsigned")} binary field.");
        }

        var bits = unchecked((ulong)value);
        for (var i = byteLength - 1; i >= 0; i--)
        {
            destination[i] = (byte)(bits & 0xFF);
            bits >>= 8;
        }
    }

    /// <summary>
    /// Encodes an <b>unsigned</b> <paramref name="value"/> as a big-endian binary integer into
    /// <paramref name="destination"/> (1–8 bytes). The counterpart to <see cref="DecodeUnsigned"/>,
    /// this writes the full 0…2⁶⁴−1 range so an 8-byte value above <see cref="long.MaxValue"/>
    /// round-trips correctly.
    /// </summary>
    /// <param name="value">The unsigned value to encode.</param>
    /// <param name="destination">The field bytes to write.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is empty or longer than 8 bytes.</exception>
    /// <exception cref="OverflowException">The value does not fit in the field width.</exception>
    internal static void EncodeUnsigned(ulong value, Span<byte> destination)
    {
        var byteLength = destination.Length;
        if (byteLength is < 1 or > 8)
        {
            throw new ArgumentException("A binary integer field must be 1–8 bytes.", nameof(destination));
        }

        if (byteLength < 8 && value > (1UL << (byteLength * 8)) - 1)
        {
            throw new OverflowException($"Value {value} does not fit in a {byteLength}-byte unsigned binary field.");
        }

        var bits = value;
        for (var i = byteLength - 1; i >= 0; i--)
        {
            destination[i] = (byte)(bits & 0xFF);
            bits >>= 8;
        }
    }


    private static bool FitsInField(long value, int byteLength, bool signed)
    {
        if (byteLength >= 8)
        {
            return signed || value >= 0;   // any long fits 8 signed bytes; unsigned needs non-negative
        }

        var bits = byteLength * 8;
        if (signed)
        {
            var max = (1L << (bits - 1)) - 1;
            var min = -(1L << (bits - 1));
            return value >= min && value <= max;
        }

        return value >= 0 && value <= (1L << bits) - 1;
    }
}
