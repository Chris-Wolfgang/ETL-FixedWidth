namespace Wolfgang.Etl.FixedWidth.Enums;

/// <summary>
/// Specifies how the extractor behaves when it encounters a blank line in the file.
/// </summary>
public enum BlankLineHandling
{
    /// <summary>
    /// Throw a <see cref="Exceptions.LineTooShortException"/> when a blank line
    /// is encountered. This is the default behavior.
    /// </summary>
    ThrowException = 0,

    /// <summary>
    /// Blank lines are invisible to all counting logic — they do not count toward
    /// <c>SkipItemCount</c>, <c>MaximumItemCount</c>, or <c>CurrentSkippedItemCount</c>.
    /// Processing continues with the next line.
    /// </summary>
    Skip = 1,

    /// <summary>
    /// Return a default (empty) instance of the target type for the blank line.
    /// All fields will contain their type's default value.
    /// </summary>
    ReturnDefault = 2,
}
