using System;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Attributes
{
    /// <summary>
    /// Marks a property as a fixed-width field and specifies its column index and width.
    /// Apply this attribute to any property on a POCO that represents a single record
    /// in a fixed-width file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The start position of each field is calculated automatically by summing the
    /// <see cref="Length"/> values of all preceding columns in index order, including
    /// any columns declared with <see cref="FixedWidthSkipAttribute"/>.
    /// </para>
    /// <para>
    /// <see cref="Index"/> is required and must be unique across all
    /// <see cref="FixedWidthFieldAttribute"/> and <see cref="FixedWidthSkipAttribute"/>
    /// instances on the type. Duplicate index values are detected at runtime by
    /// <see cref="Parsing.FieldMap"/>.
    /// </para>
    /// <para>
    /// When writing (loading), string values longer than <see cref="Length"/> throw a
    /// <see cref="Exceptions.FieldOverflowException"/>. Values shorter than
    /// <see cref="Length"/> are padded according to <see cref="Alignment"/> and
    /// <see cref="Pad"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerRecord
    /// {
    ///     [FixedWidthField(0, 10)]
    ///     public string FirstName { get; set; }
    ///
    ///     [FixedWidthField(1, 10)]
    ///     public string LastName { get; set; }
    ///
    ///     [FixedWidthField(2, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    ///     public int ZipCode { get; set; }
    /// }
    ///
    /// // With a skipped column between fields:
    /// public class EmployeeRecord
    /// {
    ///     [FixedWidthField(0, 10)]
    ///     public string FirstName { get; set; }
    ///
    ///     [FixedWidthSkip(1, 8, Message = "DOB")]
    ///     [FixedWidthField(2, 5)]
    ///     public string EmployeeNumber { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FixedWidthFieldAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FixedWidthFieldAttribute"/>.
        /// </summary>
        /// <param name="index">
        /// The zero-based column index. Must be unique across all
        /// <see cref="FixedWidthFieldAttribute"/> and <see cref="FixedWidthSkipAttribute"/>
        /// instances on the type.
        /// </param>
        /// <param name="length">
        /// The number of characters this field occupies in the fixed-width record.
        /// Must be greater than zero.
        /// </param>
        public FixedWidthFieldAttribute
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
        /// The zero-based column index used to order this field within the record.
        /// </summary>
        public int Index { get; }



        /// <summary>
        /// The number of characters this field occupies within the fixed-width record.
        /// </summary>
        public int Length { get; }



        /// <summary>
        /// The column header label written when
        /// <see cref="FixedWidthLoader{TRecord,TProgress}.WriteHeader"/> is <see langword="true"/>.
        /// If not set, the property name is used as the header label.
        /// </summary>
        public string Header { get; set; } = null;



        /// <summary>
        /// The alignment used when padding this field during a write (load) operation.
        /// Defaults to <see cref="FieldAlignment.Left"/> (left-aligned, padded on the right).
        /// </summary>
        public FieldAlignment Alignment { get; set; } = FieldAlignment.Left;



        /// <summary>
        /// The character used to pad this field to its full <see cref="Length"/> during
        /// a write (load) operation. Defaults to <c>' '</c> (space).
        /// </summary>
        public char Pad { get; set; } = ' ';



        /// <summary>
        /// Optional format string applied when converting a non-string property to its
        /// string representation during a write (load) operation, or when parsing a raw
        /// string value back to the property type during a read (extract) operation.
        /// For example, <c>"yyyyMMdd"</c> for a <see cref="DateTime"/> field or
        /// <c>"D5"</c> for a zero-padded integer.
        /// </summary>
        public string Format { get; set; } = null;



        /// <summary>
        /// When <see langword="true"/>, leading and trailing whitespace is trimmed from
        /// the extracted value before it is assigned to the property.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool TrimValue { get; set; } = true;
    }
}
