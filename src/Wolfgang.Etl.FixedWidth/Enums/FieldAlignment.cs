namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// Specifies how a field value is aligned within its fixed-width column.
/// </summary>
public enum FieldAlignment
{
    /// <summary>
    /// The value is left-aligned and padded on the right.
    /// This is the default alignment for string fields.
    /// </summary>
    Left = 0,

    /// <summary>
    /// The value is right-aligned and padded on the left. Not applied automatically:
    /// <see cref="Left"/> is the default for every field type, so set it explicitly for numbers.
    /// </summary>
    Right = 1,
}
