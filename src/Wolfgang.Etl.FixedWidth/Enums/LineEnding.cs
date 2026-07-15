namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// Controls the line ending written after each record by
/// <see cref="FixedWidthLoader{TRecord}"/>.
/// </summary>
public enum LineEnding
{
    /// <summary>
    /// The platform default (<see cref="System.Environment.NewLine"/>) after every
    /// record, including the last. This is the default and preserves prior behavior.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Unix-style line feed (<c>\n</c>) after every record, including the last.
    /// </summary>
    Lf = 1,

    /// <summary>
    /// Windows-style carriage return + line feed (<c>\r\n</c>) after every record,
    /// including the last.
    /// </summary>
    CrLf = 2,

    /// <summary>
    /// No line ending after the final record. Intermediate records (and any header
    /// or separator line) remain separated by <see cref="System.Environment.NewLine"/>.
    /// </summary>
    None = 3,
}
