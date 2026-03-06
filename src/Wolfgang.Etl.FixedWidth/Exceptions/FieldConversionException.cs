using System;

namespace Wolfgang.Etl.FixedWidth.Exceptions;

/// <summary>
/// The exception thrown when a field's raw string value cannot be converted to the
/// target property type during a fixed-width read (extract) operation. Derives from
/// <see cref="MalformedLineException"/>.
/// </summary>
public sealed class FieldConversionException : MalformedLineException
{
    // ------------------------------------------------------------------
    // Constructor
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
}
