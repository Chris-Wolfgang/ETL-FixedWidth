using System.Collections.Generic;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// The resolved result from <see cref="FieldMap"/>, carrying the ordered field descriptors,
/// the pre-computed expected line width (sum of all field and skip lengths), and the total
/// column count including skipped columns.
/// </summary>
internal sealed class FieldMapResult
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    internal FieldMapResult
    (
        IReadOnlyList<FieldDescriptor> descriptors,
        int expectedLineWidth,
        int totalColumnCount
    )
    {
        Descriptors = descriptors;
        ExpectedLineWidth = expectedLineWidth;
        TotalColumnCount = totalColumnCount;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The ordered, start-position-resolved field descriptors for the type.
    /// </summary>
    internal IReadOnlyList<FieldDescriptor> Descriptors { get; }



    /// <summary>
    /// The sum of all field and skip lengths — the minimum line width required to
    /// read the record, excluding any delimiter contribution.
    /// </summary>
    internal int ExpectedLineWidth { get; }



    /// <summary>
    /// The total number of columns in the record including skipped columns.
    /// Used to calculate the delimiter contribution to the expected line width:
    /// <c>delimiter.Length * (TotalColumnCount - 1)</c>.
    /// </summary>
    internal int TotalColumnCount { get; }
}
