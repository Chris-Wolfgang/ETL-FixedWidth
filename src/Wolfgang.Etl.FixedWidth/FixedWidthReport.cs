using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A progress report for fixed-width extract and load operations. Extends
/// <see cref="Report"/> with additional diagnostic properties specific to
/// fixed-width file processing.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CurrentLineNumber"/> reflects the physical line currently being
/// processed — the same number shown in an editor such as Notepad++. It is updated
/// <em>before</em> a line is parsed or written so that if an exception is thrown,
/// the reported line number points directly to the offending line in the file.
/// </para>
/// <para>
/// For the extractor, <see cref="CurrentLineNumber"/> includes header lines,
/// the separator line, and all data lines. For the loader, it includes the header
/// line, the separator line, and all data lines written so far.
/// </para>
/// </remarks>
public record FixedWidthReport : Report
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of <see cref="FixedWidthReport"/> with the
    /// rejected and filtered counts defaulted to zero. Retained for backward
    /// compatibility; prefer the five-parameter overload.
    /// </summary>
    /// <param name="currentCount">The number of data records processed so far.</param>
    /// <param name="currentSkippedItemCount">The number of records skipped by the skip budget so far.</param>
    /// <param name="currentLineNumber">
    /// The 1-based physical line number currently being processed in the file.
    /// </param>
    public FixedWidthReport
    (
        int currentCount,
        int currentSkippedItemCount,
        long currentLineNumber
    )
        : this(currentCount, currentSkippedItemCount, 0, 0, currentLineNumber)
    {
    }



    /// <summary>
    /// Initializes a new instance of <see cref="FixedWidthReport"/>.
    /// </summary>
    /// <param name="currentCount">The number of data records processed so far.</param>
    /// <param name="currentSkippedItemCount">The number of records skipped by the skip budget so far.</param>
    /// <param name="currentRejectedItemCount">The number of records rejected so far (extractor only).</param>
    /// <param name="currentFilteredLineCount">The number of non-record lines read so far (extractor only).</param>
    /// <param name="currentLineNumber">
    /// The 1-based physical line number currently being processed in the file.
    /// </param>
    public FixedWidthReport
    (
        int currentCount,
        int currentSkippedItemCount,
        int currentRejectedItemCount,
        int currentFilteredLineCount,
        long currentLineNumber
    )
        : base(currentCount)
    {
        CurrentSkippedItemCount = currentSkippedItemCount;
        CurrentRejectedItemCount = currentRejectedItemCount;
        CurrentFilteredLineCount = currentFilteredLineCount;
        CurrentLineNumber = currentLineNumber;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The number of records skipped by the <c>SkipItemCount</c> budget (pagination)
    /// so far. This does <em>not</em> include records rejected for content or data
    /// quality — see <see cref="CurrentRejectedItemCount"/> — nor non-record lines —
    /// see <see cref="CurrentFilteredLineCount"/>. For the loader, includes records
    /// skipped by the skip budget.
    /// </summary>
    public int CurrentSkippedItemCount { get; }



    /// <summary>
    /// (Extractor only.) The number of parsed records rejected so far: records
    /// discarded via <see cref="MalformedLineHandling.Skip"/> and records rejected
    /// by <c>RecordValidator</c>. Always <c>0</c> for the loader.
    /// </summary>
    public int CurrentRejectedItemCount { get; }



    /// <summary>
    /// (Extractor only.) The number of physical lines read that did not produce a
    /// record and were neither skipped by the budget nor rejected: header lines, the
    /// separator line, blank lines dropped per <see cref="BlankLineHandling"/>, lines
    /// dropped by <c>LineFilter</c>, and the line that triggered early termination.
    /// With <see cref="CurrentLineNumber"/> this closes the line accounting:
    /// <c>CurrentLineNumber = CurrentItemCount + CurrentSkippedItemCount +
    /// CurrentRejectedItemCount + CurrentFilteredLineCount</c>. Always <c>0</c> for the loader.
    /// </summary>
    public int CurrentFilteredLineCount { get; }



    /// <summary>
    /// The 1-based physical line number of the line currently being processed.
    /// Matches the line number shown in a text editor — no adjustment is needed
    /// for header or separator lines. If an exception is thrown during processing,
    /// this value points to the line that caused it.
    /// </summary>
    public long CurrentLineNumber { get; }
}
