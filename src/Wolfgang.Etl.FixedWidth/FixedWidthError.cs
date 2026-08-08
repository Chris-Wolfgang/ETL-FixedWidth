using System;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A record that failed to parse during extraction — a "dead letter". When
/// <see cref="FixedWidthExtractor{TRecord}.OnError"/> is set, each failed line is
/// reported as one of these instead of the exception aborting the run (provided
/// <see cref="Enums.MalformedLineHandling.Skip"/> is configured to continue). Capture
/// them for logging, a dead-letter queue, or post-run inspection (#29).
/// </summary>
public sealed class FixedWidthError
{
    /// <summary>
    /// Initializes a new <see cref="FixedWidthError"/>.
    /// </summary>
    /// <param name="itemNumber">The 1-based ordinal of the failed record within the run.</param>
    /// <param name="rawContent">The original line that failed, or <see langword="null"/> if unavailable.</param>
    /// <param name="exception">The exception the record raised.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public FixedWidthError(long itemNumber, string? rawContent, Exception exception)
    {
        ItemNumber = itemNumber;
        RawContent = rawContent;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    /// The 1-based ordinal of the failed record within the current run.
    /// </summary>
    public long ItemNumber { get; }

    /// <summary>
    /// The original source line that failed to parse, or <see langword="null"/> if the
    /// stage did not capture it.
    /// </summary>
    public string? RawContent { get; }

    /// <summary>
    /// The exception the record raised — typically a
    /// <see cref="Exceptions.LineTooShortException"/>,
    /// <see cref="Exceptions.MalformedLineException"/>, or
    /// <see cref="Exceptions.FieldConversionException"/>.
    /// </summary>
    public Exception Exception { get; }
}
