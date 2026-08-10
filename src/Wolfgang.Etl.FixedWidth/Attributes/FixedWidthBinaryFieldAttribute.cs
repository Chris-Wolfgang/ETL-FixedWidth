using System;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Attributes;

/// <summary>
/// Marks a property as a field in a fixed-length <b>binary</b> record (#21) — the mainframe
/// counterpart to <see cref="FixedWidthFieldAttribute"/>. Widths are in <b>bytes</b> (not
/// characters), the byte offset is summed from the <see cref="ByteLength"/> of preceding columns in
/// <see cref="Index"/> order, and <see cref="Type"/> selects how the bytes are decoded (text,
/// <c>COMP-3</c> packed decimal, or <c>COMP</c> binary integer). Read a record with
/// <see cref="FixedWidthBinaryExtractor{TRecord}"/>.
/// </summary>
/// <example>
/// <code>
/// public class AccountRecord
/// {
///     [FixedWidthBinaryField(0, 8, BinaryFieldType.Text)]
///     public string AccountId { get; set; } = string.Empty;
///
///     [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
///     public int TransactionCount { get; set; }
///
///     [FixedWidthBinaryField(2, 5, BinaryFieldType.PackedDecimal, Scale = 2)]
///     public decimal Balance { get; set; }   // PIC S9(7)V99 COMP-3
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FixedWidthBinaryFieldAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="FixedWidthBinaryFieldAttribute"/>.
    /// </summary>
    /// <param name="index">The zero-based column index; must be unique across the record.</param>
    /// <param name="byteLength">The field width in bytes (greater than zero).</param>
    /// <param name="type">How the bytes are decoded.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative or <paramref name="byteLength"/> is not positive.
    /// </exception>
    public FixedWidthBinaryFieldAttribute(int index, int byteLength, BinaryFieldType type)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index cannot be negative.");
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "Byte length must be greater than zero.");
        }

        Index = index;
        ByteLength = byteLength;
        Type = type;
    }

    /// <summary>The zero-based column index within the record.</summary>
    public int Index { get; }

    /// <summary>The field width in bytes.</summary>
    public int ByteLength { get; }

    /// <summary>How the bytes are decoded.</summary>
    public BinaryFieldType Type { get; }

    /// <summary>
    /// The number of implied decimal places for a <see cref="BinaryFieldType.PackedDecimal"/> field
    /// (the <c>V</c> position in a COBOL picture). Ignored for other types. Default 0.
    /// </summary>
    public int Scale { get; set; }

    /// <summary>
    /// Whether a <see cref="BinaryFieldType.Binary"/> field is two's-complement signed. Ignored for
    /// other types. Default <see langword="true"/>.
    /// </summary>
    public bool Signed { get; set; } = true;
}
