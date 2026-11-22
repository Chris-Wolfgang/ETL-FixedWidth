using System;

namespace Wolfgang.Etl.FixedWidth.Exceptions;

/// <summary>
/// The exception thrown when a field's raw string value cannot be converted to the
/// target property type during a fixed-width read (extract) operation, or when a
/// <see cref="FixedWidthBinaryExtractor{TRecord}"/> / <see cref="FixedWidthBinaryLoader{TRecord}"/>
/// field cannot be decoded or encoded. Derives from <see cref="MalformedLineException"/>.
/// </summary>
/// <remarks>
/// For the binary stages <see cref="MalformedLineException.LineNumber"/> is the 1-based record number and
/// <see cref="ByteOffset"/> is the 0-based offset of the field within the record.
/// </remarks>
#pragma warning disable RCS1194 // Exception requires contextual parameters — generic constructors are not meaningful
public sealed class FieldConversionException : MalformedLineException
{
#pragma warning restore RCS1194
    // ------------------------------------------------------------------
    // Constructors
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of <see cref="FieldConversionException"/> with full context.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number of the offending line.</param>
    /// <param name="lineContent">The raw content of the offending line.</param>
    /// <param name="fieldName">The name of the property whose value could not be converted.</param>
    /// <param name="expectedType">The CLR type the raw value was being converted to.</param>
    /// <param name="rawValue">The raw string value that failed conversion.</param>
    /// <param name="innerException">The underlying conversion exception.</param>
    public FieldConversionException
    (
        string message,
        long lineNumber,
        string lineContent,
        string fieldName,
        Type expectedType,
        string rawValue,
        Exception innerException
    )
        : base
        (
            message,
            lineNumber,
            lineContent,
            innerException
        )
    {
        FieldName = fieldName;
        ExpectedType = expectedType;
        RawValue = rawValue;
    }



    /// <summary>
    /// Initializes a new instance of <see cref="FieldConversionException"/> for a field of a
    /// fixed-length binary record, carrying the field's byte offset within the record.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="recordNumber">The 1-based number of the offending record.</param>
    /// <param name="recordContent">A printable form of the offending record (for example its bytes in hex).</param>
    /// <param name="fieldName">The name of the property whose value could not be converted.</param>
    /// <param name="expectedType">The CLR type of the property.</param>
    /// <param name="rawValue">A printable form of the value that failed conversion (for example the field's bytes in hex).</param>
    /// <param name="byteOffset">The 0-based offset of the field within the record.</param>
    /// <param name="innerException">The underlying conversion exception.</param>
    public FieldConversionException
    (
        string message,
        long recordNumber,
        string recordContent,
        string fieldName,
        Type expectedType,
        string rawValue,
        long byteOffset,
        Exception innerException
    )
        : this
        (
            message,
            recordNumber,
            recordContent,
            fieldName,
            expectedType,
            rawValue,
            innerException
        )
    {
        ByteOffset = byteOffset;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The name of the property whose value could not be converted.
    /// </summary>
    public string FieldName { get; }



    /// <summary>
    /// The CLR type the raw string value was being converted to.
    /// </summary>
    public Type ExpectedType { get; }



    /// <summary>
    /// The raw string value extracted from the line that failed conversion.
    /// </summary>
    public string RawValue { get; }



    /// <summary>
    /// The 0-based offset of the field within a fixed-length binary record, or
    /// <see langword="null"/> when the error came from a text (line-based) stage.
    /// </summary>
    public long? ByteOffset { get; }
}
