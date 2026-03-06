namespace Wolfgang.Etl.FixedWidth.Enums
{
    /// <summary>
    /// Specifies how a field value is aligned within its fixed-width column.
    /// </summary>
    public enum FieldAlignment
    {
        /// <summary>
        /// The value is left-aligned and padded on the right.
        /// This is the default alignment for string fields.
        /// </summary>
        Left,

        /// <summary>
        /// The value is right-aligned and padded on the left.
        /// This is the default alignment for numeric fields.
        /// </summary>
        Right
    }
}
