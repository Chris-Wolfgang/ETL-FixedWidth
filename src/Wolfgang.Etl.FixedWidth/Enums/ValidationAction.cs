namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// The action a record validator asks the extractor to take for a parsed record.
/// Returned via <see cref="ValidationResult"/> from
/// <see cref="FixedWidthExtractor{TRecord}.RecordValidator"/>.
/// </summary>
public enum ValidationAction
{
    /// <summary>Accept the record and yield it.</summary>
    Accept = 0,

    /// <summary>
    /// Skip the record. It is not yielded; the extractor increments
    /// <c>CurrentSkippedItemCount</c> and continues with the next line.
    /// </summary>
    Skip = 1,

    /// <summary>
    /// Stop extraction immediately. The record is not yielded and no further
    /// lines are read.
    /// </summary>
    Stop = 2,
}
