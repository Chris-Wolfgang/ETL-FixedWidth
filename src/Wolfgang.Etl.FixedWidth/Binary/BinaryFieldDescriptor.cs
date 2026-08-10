using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Resolved metadata for one <see cref="FixedWidthBinaryFieldAttribute"/> field: its byte offset
/// within the record, decode instructions, and a compiled setter onto the record property.
/// </summary>
internal sealed class BinaryFieldDescriptor
{
    internal BinaryFieldDescriptor(PropertyInfo property, FixedWidthBinaryFieldAttribute attribute, int byteOffset, Action<object, object?> setter)
    {
        Property = property;
        Attribute = attribute;
        ByteOffset = byteOffset;
        Setter = setter;
        PropertyType = property.PropertyType;
        UnderlyingType = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
        TypeConverter = TypeDescriptor.GetConverter(PropertyType);
    }

    internal PropertyInfo Property { get; }

    internal FixedWidthBinaryFieldAttribute Attribute { get; }

    internal int ByteOffset { get; }

    internal Action<object, object?> Setter { get; }

    internal Type PropertyType { get; }

    internal Type UnderlyingType { get; }

    internal TypeConverter TypeConverter { get; }


    // Decodes this field's value from the record buffer, using the encoding for Text fields.
    internal object? Decode(byte[] record, Encoding encoding)
    {
        var length = Attribute.ByteLength;

        switch (Attribute.Type)
        {
            case BinaryFieldType.Text:
                var text = encoding.GetString(record, ByteOffset, length).TrimEnd();
                return FixedWidthConverter.ParseValue(text.AsMemory(), PropertyType, format: null, TypeConverter, numberStyles: null);

            case BinaryFieldType.PackedDecimal:
                return Coerce(PackedDecimal.Decode(new ReadOnlySpan<byte>(record, ByteOffset, length), Attribute.Scale));

            case BinaryFieldType.Binary:
                return Coerce(BinaryInteger.Decode(new ReadOnlySpan<byte>(record, ByteOffset, length), Attribute.Signed));

            default:
                throw new InvalidOperationException($"Unknown {nameof(BinaryFieldType)} '{Attribute.Type}'.");
        }
    }

    // Convert the decoded numeric to the property's underlying type BEFORE boxing, so the compiled
    // setter's unbox matches for both T and T? (a boxed long won't unbox to int?).
    private object Coerce(object value)
        => value.GetType() == UnderlyingType
            ? value
            : Convert.ChangeType(value, UnderlyingType, CultureInfo.InvariantCulture);
}
