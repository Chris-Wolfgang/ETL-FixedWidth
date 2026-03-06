using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth
{
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
        /// Initializes a new instance of <see cref="FixedWidthReport"/>.
        /// </summary>
        /// <param name="currentCount">The number of data records processed so far.</param>
        /// <param name="currentSkippedItemCount">The number of records skipped so far.</param>
        /// <param name="currentLineNumber">
        /// The 1-based physical line number currently being processed in the file.
        /// </param>
        public FixedWidthReport
        (
            int currentCount,
            int currentSkippedItemCount,
            long currentLineNumber
        )
            : base(currentCount)
        {
            CurrentSkippedItemCount = currentSkippedItemCount;
            CurrentLineNumber = currentLineNumber;
        }



        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>
        /// The number of records skipped so far. For the extractor, includes records
        /// skipped by the skip budget and records silently discarded due to
        /// <see cref="MalformedLineHandling.Skip"/>. For the loader, includes records
        /// skipped by the skip budget.
        /// </summary>
        public int CurrentSkippedItemCount { get; }



        /// <summary>
        /// The 1-based physical line number of the line currently being processed.
        /// Matches the line number shown in a text editor — no adjustment is needed
        /// for header or separator lines. If an exception is thrown during processing,
        /// this value points to the line that caused it.
        /// </summary>
        public long CurrentLineNumber { get; }
    }
}
