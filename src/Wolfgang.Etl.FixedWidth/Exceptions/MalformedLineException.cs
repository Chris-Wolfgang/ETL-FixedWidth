using System;

namespace Wolfgang.Etl.FixedWidth.Exceptions;

/// <summary>
/// Base class for exceptions thrown when a line in a fixed-width file cannot be parsed.
/// </summary>
/// <remarks>
/// <para>
/// Two derived types cover the specific failure modes:
/// <list type="bullet">
///   <item><see cref="LineTooShortException"/> — the line is shorter than the total
///   width required to read all fields.</item>
///   <item><see cref="FieldConversionException"/> — the line was long enough but a
///   field's raw string value could not be converted to the target property type.</item>
/// </list>
/// </para>
/// <para>
/// Catch <see cref="MalformedLineException"/> to handle both cases uniformly, or catch
/// the derived types individually for finer-grained error handling.
/// </para>
/// </remarks>
public abstract class MalformedLineException : Exception
{
    // ------------------------------------------------------------------
    // Constructors
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of <see cref="MalformedLineException"/> with a
    /// descriptive message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number of the offending line.</param>
    /// <param name="lineContent">The raw content of the offending line.</param>
    protected MalformedLineException
    (
        string message,
        long lineNumber,
        string lineContent
    )
        : base(message)
    {
        LineNumber = lineNumber;
        LineContent = lineContent;
    }



    /// <summary>
    /// Initializes a new instance of <see cref="MalformedLineException"/> with a
    /// descriptive message and an inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="lineNumber">The 1-based line number of the offending line.</param>
    /// <param name="lineContent">The raw content of the offending line.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected MalformedLineException
    (
        string message,
        long lineNumber,
        string lineContent,
        Exception innerException
    )
        : base
        (
            message,
            innerException
        )
    {
        LineNumber = lineNumber;
        LineContent = lineContent;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The 1-based line number of the offending line in the file.
    /// </summary>
    public long LineNumber { get; }



    /// <summary>
    /// The raw string content of the offending line.
    /// </summary>
    public string LineContent { get; }
}
