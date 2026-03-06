namespace Wolfgang.Etl.FixedWidth.Enums
{
    /// <summary>
    /// Specifies how the extractor behaves when it encounters a malformed line
    /// (e.g. a line that is shorter than expected) or a field that cannot be parsed.
    /// </summary>
    public enum MalformedLineHandling
    {
        /// <summary>
        /// Throw a <see cref="Exceptions.MalformedLineException"/> when a malformed line
        /// is encountered. This is the default behavior.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Skip the malformed line entirely and continue processing the next line.
        /// The skipped line count is tracked via <c>CurrentSkippedItemCount</c>.
        /// </summary>
        Skip,

        /// <summary>
        /// Return a default (empty) instance of the target type for the malformed line.
        /// All fields will contain their type's default value — the partial state of any
        /// fields that were successfully read before the failure is discarded.
        /// </summary>
        ReturnDefault
    }
}
