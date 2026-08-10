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
        if (bytes.Length is < 1 or > 8)
        {
            throw new ArgumentException("A binary integer field must be 1–8 bytes.", nameof(bytes));
        }

        ulong magnitude = 0;
        foreach (var b in bytes)
        {
            magnitude = (magnitude << 8) | b;
        }

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
}
