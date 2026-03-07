using System.Reflection;
using Wolfgang.Etl.FixedWidth.Attributes;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// Represents the resolved metadata for a single fixed-width field, including its
/// calculated start position and absolute column index within the record.
/// </summary>
internal sealed class FieldDescriptor
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    internal FieldDescriptor
    (
        PropertyInfo property,
        FixedWidthFieldAttribute attribute,
        int start,
        int absoluteColumnIndex
    )
    {
        Property = property;
        Attribute = attribute;
        Start = start;
        AbsoluteColumnIndex = absoluteColumnIndex;
        Context = new FieldContext
        (
            property.Name,
            property.PropertyType,
            attribute.Length,
            attribute.Pad,
            attribute.Alignment,
            attribute.Format,
            attribute.Header ?? property.Name
        );
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    internal PropertyInfo Property { get; }



    internal FixedWidthFieldAttribute Attribute { get; }



    internal int Start { get; }



    /// <summary>
    /// The zero-based position of this field among all columns in the record,
    /// including skipped columns. Used to correctly offset start positions when
    /// a field delimiter is present.
    /// </summary>
    internal int AbsoluteColumnIndex { get; }



    /// <summary>
    /// Pre-built immutable context for this field, constructed once from the attribute
    /// metadata and reused for every row during formatting.
    /// </summary>
    internal FieldContext Context { get; }
}
