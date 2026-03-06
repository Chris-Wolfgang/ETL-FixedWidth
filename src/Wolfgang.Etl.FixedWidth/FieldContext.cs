using System;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth
{
    /// <summary>
    /// Provides contextual information about a fixed-width field to a
    /// <see cref="FixedWidthLoader{TRecord,TProgress}.ValueConverter"/> or
    /// <see cref="FixedWidthExtractor{TRecord,TProgress}.ValueParser"/> delegate.
    /// </summary>
    /// <remarks>
    /// An instance of this class is constructed by the framework for each field and passed
    /// to the delegate along with the raw value. It is immutable — the delegate may read
    /// from it but cannot modify it.
    /// </remarks>
    public sealed class FieldContext
    {
        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        /// <summary>
        /// Initializes a new immutable instance of <see cref="FieldContext"/>.
        /// </summary>
        internal FieldContext
        (
            string propertyName,
            Type propertyType,
            int fieldLength,
            char pad,
            FieldAlignment alignment,
            string format,
            string headerLabel
        )
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            FieldLength = fieldLength;
            Pad = pad;
            Alignment = alignment;
            Format = format;
            HeaderLabel = headerLabel;
        }



        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>
        /// The name of the property being converted or parsed.
        /// </summary>
        public string PropertyName { get; }



        /// <summary>
        /// The CLR type of the property being parsed. Available to
        /// <see cref="FixedWidthExtractor{TRecord,TProgress}.ValueParser"/> delegates
        /// so the parser knows what type to return.
        /// </summary>
        public Type PropertyType { get; }



        /// <summary>
        /// The maximum number of characters this field may occupy, as defined by
        /// <see cref="Attributes.FixedWidthFieldAttribute.Length"/>.
        /// </summary>
        public int FieldLength { get; }



        /// <summary>
        /// The character used to pad the converted value to <see cref="FieldLength"/>.
        /// </summary>
        public char Pad { get; }



        /// <summary>
        /// The alignment applied when padding the converted value.
        /// </summary>
        public FieldAlignment Alignment { get; }



        /// <summary>
        /// The header label for this field — either the value of
        /// <see cref="Attributes.FixedWidthFieldAttribute.Header"/> if set, or the property name.
        /// </summary>
        public string HeaderLabel { get; }



        /// <summary>
        /// The optional format string defined on the field attribute, or
        /// <see langword="null"/> if none was specified.
        /// </summary>
        public string Format { get; }
    }
}
