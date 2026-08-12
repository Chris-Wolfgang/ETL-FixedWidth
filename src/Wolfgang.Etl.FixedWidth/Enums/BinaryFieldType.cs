namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// How the bytes of a <see cref="Attributes.FixedWidthBinaryFieldAttribute"/> field are decoded (#21).
/// </summary>
public enum BinaryFieldType
{
    /// <summary>
    /// Character data — the bytes are decoded to text with the extractor's encoding (ASCII, a code
    /// page such as EBCDIC, UTF-8, …) and then converted to the property type, exactly like a text
    /// fixed-width field.
    /// </summary>
    Text = 0,

    /// <summary>
    /// COBOL <c>COMP-3</c> packed decimal (BCD): two digits per byte with a sign nibble in the low
    /// nibble of the last byte. Use <see cref="Attributes.FixedWidthBinaryFieldAttribute.Scale"/> for
    /// the implied decimal places.
    /// </summary>
    PackedDecimal = 1,

    /// <summary>
    /// COBOL <c>COMP</c> / <c>COMP-4</c> binary integer: a big-endian value, two's-complement signed
    /// by default (see <see cref="Attributes.FixedWidthBinaryFieldAttribute.Signed"/>).
    /// </summary>
    Binary = 2,
}
