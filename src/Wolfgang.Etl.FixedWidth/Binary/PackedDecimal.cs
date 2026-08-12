using System;
using System.Globalization;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Decodes COBOL <c>COMP-3</c> (packed-decimal / BCD) fields (#21). Each byte holds two decimal
/// digits (high nibble first); the final byte holds one digit in its high nibble and a sign nibble
/// in its low nibble. An <c>N</c>-byte field therefore encodes <c>2N-1</c> digits plus the sign.
/// The <c>scale</c> is the number of implied decimal places (the <c>V</c> position in a COBOL
/// picture such as <c>PIC S9(5)V99 COMP-3</c>).
/// </summary>
internal static class PackedDecimal
{
    /// <summary>
    /// Decodes a big-endian packed-decimal value.
    /// </summary>
    /// <param name="bytes">The raw COMP-3 bytes (most-significant byte first).</param>
    /// <param name="scale">The number of implied decimal places (0 for an integer).</param>
    /// <returns>The decoded value, with sign and implied decimal point applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is negative.</exception>
    /// <exception cref="FormatException">A digit nibble is not in the range 0–9.</exception>
    internal static decimal Decode(ReadOnlySpan<byte> bytes, int scale = 0)
    {
        if (bytes.Length == 0)
        {
            throw new ArgumentException("A packed-decimal field needs at least one byte.", nameof(bytes));
        }

        if (scale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale cannot be negative.");
        }

        decimal value = 0m;
        var last = bytes.Length - 1;

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];

            var high = (b >> 4) & 0x0F;
            if (high > 9)
            {
                throw new FormatException($"Invalid packed-decimal digit nibble 0x{high:X} at byte {i}.");
            }

            value = (value * 10) + high;

            if (i < last)
            {
                var low = b & 0x0F;
                if (low > 9)
                {
                    throw new FormatException($"Invalid packed-decimal digit nibble 0x{low:X} at byte {i}.");
                }

                value = (value * 10) + low;
            }
        }

        // Low nibble of the final byte is the sign. Convention: 0xD and 0xB are negative; every other
        // nibble (0xC, 0xF unsigned, 0xA, 0xE) is positive.
        var sign = bytes[last] & 0x0F;
        if (sign == 0x0D || sign == 0x0B)
        {
            value = -value;
        }

        if (scale > 0)
        {
            value /= Pow10(scale);
        }

        return value;
    }

    /// <summary>
    /// Encodes <paramref name="value"/> as COMP-3 packed decimal into <paramref name="destination"/>,
    /// whose length is the field's byte width. The value is truncated (COBOL default, not rounded) to
    /// <paramref name="scale"/> implied decimal places.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="scale">The number of implied decimal places.</param>
    /// <param name="destination">The field bytes to write (its length is the field width).</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is negative.</exception>
    /// <exception cref="OverflowException">The value needs more digits than the field can hold.</exception>
    internal static void Encode(decimal value, int scale, Span<byte> destination)
    {
        if (destination.Length == 0)
        {
            throw new ArgumentException("A packed-decimal field needs at least one byte.", nameof(destination));
        }

        if (scale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale cannot be negative.");
        }

        var negative = value < 0;
        var unscaled = Math.Truncate(Math.Abs(value) * Pow10(scale));

        var maxDigits = (destination.Length * 2) - 1;
        var digits = unscaled.ToString("F0", CultureInfo.InvariantCulture);
        if (digits.Length > maxDigits)
        {
            throw new OverflowException($"Value '{value}' needs {digits.Length} digits but the {destination.Length}-byte packed-decimal field holds only {maxDigits}.");
        }

        var padded = digits.PadLeft(maxDigits, '0');
        destination.Clear();

        for (var d = 0; d < maxDigits; d++)
        {
            var digit = padded[d] - '0';
            var byteIndex = d / 2;
            destination[byteIndex] |= (byte)((d % 2 == 0) ? digit << 4 : digit);
        }

        // Sign nibble in the low nibble of the last byte: 0xC positive, 0xD negative.
        destination[destination.Length - 1] |= (byte)(negative ? 0x0D : 0x0C);
    }

    private static decimal Pow10(int scale)
    {
        decimal result = 1m;
        for (var i = 0; i < scale; i++)
        {
            result *= 10m;
        }

        return result;
    }
}
