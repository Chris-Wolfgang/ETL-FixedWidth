namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// Controls what a <see cref="FixedWidthMultiExtractor"/> does with a data line that no
/// registered <c>When</c> predicate matches and for which no <c>Otherwise</c> fallback
/// record type has been registered (#19).
/// </summary>
public enum UnmatchedLineHandling
{
    /// <summary>
    /// Throw a <see cref="System.IO.InvalidDataException"/> for the first unmatched line.
    /// This is the default — an unroutable line usually signals a layout the caller did
    /// not anticipate, and failing fast surfaces it.
    /// </summary>
    ThrowException = 0,

    /// <summary>
    /// Silently skip the unmatched line and continue. The line does not produce a record
    /// and is counted toward the filtered-line total rather than the extracted or skipped
    /// budgets.
    /// </summary>
    Skip = 1,
}
