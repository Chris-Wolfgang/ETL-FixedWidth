using System;

namespace Wolfgang.Etl.FixedWidth.Exceptions;

/// <summary>
/// The exception thrown when a value converter or header converter returns a string
/// that is wider than the field width defined by
/// <see cref="Attributes.FixedWidthFieldAttribute.Length"/>. This indicates a
/// programming error — the converter does not respect the
/// <see cref="FieldContext.FieldLength"/> contract.
/// </summary>
/// <remarks>
/// This exception is not thrown for data values in the file that are too wide for
/// a target type — it is only thrown when a <see cref="FixedWidthLoader{TRecord,TProgress}.ValueConverter"/>
/// or <see cref="FixedWidthLoader{TRecord,TProgress}.HeaderConverter"/> returns a
/// string longer than <see cref="FieldLength"/>. To silently truncate instead of
/// throwing, use <see cref="FixedWidthConverter.Truncate"/> or
/// <see cref="FixedWidthConverter.TruncateHeader"/>.
/// </remarks>
public sealed class FieldOverflowException : InvalidOperationException
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of <see cref="FieldOverflowException"/> with full
    /// context about the overflowing field.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="propertyName">The name of the property whose converted value overflowed.</param>
    /// <param name="fieldLength">The maximum number of characters allowed for the field.</param>
    /// <param name="actualLength">The actual length of the string returned by the converter.</param>
    public FieldOverflowException
    (
        string message,
        string propertyName,
        int fieldLength,
        int actualLength
    )
        : base(message)
    {
        PropertyName = propertyName;
        FieldLength = fieldLength;
        ActualLength = actualLength;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The name of the property whose converted value exceeded the defined field width.
    /// </summary>
    public string PropertyName { get; }



    /// <summary>
    /// The maximum number of characters allowed for the field, as defined by
    /// <see cref="Attributes.FixedWidthFieldAttribute.Length"/>.
    /// </summary>
    public int FieldLength { get; }



    /// <summary>
    /// The actual number of characters in the string returned by the converter.
    /// </summary>
    public int ActualLength { get; }
}
