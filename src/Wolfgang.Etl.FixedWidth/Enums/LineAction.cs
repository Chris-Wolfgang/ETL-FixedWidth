namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// Indicates what the extractor should do with a line of input.
/// Returned by the <see cref="FixedWidthExtractor{TRecord}.LineFilter"/> delegate.
/// </summary>
public enum LineAction
{
    /// <summary>
    /// Parse the line normally and yield the resulting record.
    /// </summary>
    Process = 0,

    /// <summary>
    /// Skip this line without parsing it. The extractor continues reading
    /// subsequent lines. The line is invisible to all counting logic — it does
    /// not count toward <c>SkipItemCount</c>, <c>MaximumItemCount</c>, or
    /// <c>CurrentSkippedItemCount</c>.
    /// </summary>
    Skip = 1,

    /// <summary>
    /// Stop reading immediately. The current line is not parsed and no further
    /// lines are read. The async stream ends cleanly.
    /// </summary>
    Stop = 2,
}
