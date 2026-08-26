namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// The counts that make up a <see cref="FixedWidthReport"/>.
/// </summary>
/// <remarks>
/// Supplied as the single constructor parameter to <see cref="FixedWidthReport"/>. Every member is
/// optional and defaults to zero, so a caller states only the counts that are meaningful for the
/// operation being reported — a loader, for example, leaves the extractor-only rejected and
/// filtered counts alone rather than passing two explicit zeroes positionally.
/// <para>
/// This replaces the two positional constructors, which differed only by whether the two
/// extractor-only counts appeared in the middle of the list. Naming each count at the call site
/// removes the standing risk of transposing same-typed arguments.
/// </para>
/// </remarks>
public sealed record FixedWidthReportOptions
{
    /// <summary>Gets the number of data records processed so far. Defaults to <c>0</c>.</summary>
    public int CurrentCount { get; init; }

    /// <summary>
    /// Gets the number of records skipped by the skip budget so far. Defaults to <c>0</c>.
    /// </summary>
    public int CurrentSkippedItemCount { get; init; }

    /// <summary>
    /// Gets the number of parsed records rejected so far. Extractor only — leave unset for a
    /// loader. Defaults to <c>0</c>.
    /// </summary>
    public int CurrentRejectedItemCount { get; init; }

    /// <summary>
    /// Gets the number of physical lines read that produced no record. Extractor only — leave
    /// unset for a loader. Defaults to <c>0</c>.
    /// </summary>
    public int CurrentFilteredLineCount { get; init; }

    /// <summary>
    /// Gets the 1-based physical line number currently being processed. Defaults to <c>0</c>.
    /// </summary>
    public long CurrentLineNumber { get; init; }
}
