using System;

namespace Wolfgang.Etl.FixedWidth.Attributes;

/// <summary>
/// Declares a column in the fixed-width file that should be skipped during extraction
/// and not mapped to any property. Multiple instances may be stacked on the same
/// property to declare several skipped columns.
/// </summary>
/// <remarks>
/// <see cref="Index"/> participates in the same global ordering as
/// <see cref="FixedWidthFieldAttribute.Index"/> and must be unique across all
/// <see cref="FixedWidthFieldAttribute"/> and <see cref="FixedWidthSkipAttribute"/>
/// instances on the type.
/// </remarks>
/// <example>
/// <code>
/// // File columns: FirstName(10), DOB(8), HireDate(8), EmployeeNumber(5)
/// // POCO only cares about FirstName and EmployeeNumber.
/// public class EmployeeRecord
/// {
///     [FixedWidthField(0, 10)]
///     public string FirstName { get; set; }
///
///     [FixedWidthSkip(1, 8,  Message = "DOB")]
///     [FixedWidthSkip(2, 8,  Message = "HireDate")]
///     [FixedWidthField(3, 5)]
///     public string EmployeeNumber { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class FixedWidthSkipAttribute : Attribute
{
    /// <summary>
    /// Declares a skipped column at the specified index with the specified width.
    /// </summary>
    /// <param name="index">
    /// The zero-based column index. Must be unique across all
    /// <see cref="FixedWidthFieldAttribute"/> and <see cref="FixedWidthSkipAttribute"/>
    /// instances on the type.
    /// </param>
    /// <param name="length">
    /// The number of characters occupied by the skipped column in the file.
    /// Must be greater than zero.
    /// </param>
    public FixedWidthSkipAttribute
    (
        int index,
        int length
    )
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException
            (
                nameof(index),
                "Index must be zero or greater."
            );
        }
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException
            (
                nameof(length),
                $"Length must be greater than zero. Got {length}."
            );
        }

        Index = index;
        Length = length;
    }



    /// <summary>
    /// The zero-based column index.
    /// </summary>
    public int Index { get; }



    /// <summary>
    /// The number of characters occupied by this column in the file.
    /// </summary>
    public int Length { get; }



    /// <summary>
    /// An optional description of what this column contains. Used purely for
    /// documentation — it has no effect on parsing or writing.
    /// </summary>
    public string? Message { get; set; }
}
