using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Resolved metadata for one <see cref="FixedWidthBinaryFieldAttribute"/> field: its byte offset
/// within the record, decode/encode instructions, and compiled accessors on the record property.
/// </summary>
internal sealed class BinaryFieldDescriptor
{
    internal BinaryFieldDescriptor(PropertyInfo property, FixedWidthBinaryFieldAttribute attribute, int byteOffset, Action<object, object?> setter, Func<object, object?> getter)
    {
        Property = property;
        Attribute = attribute;
        ByteOffset = byteOffset;
        Setter = setter;
        Getter = getter;
        PropertyType = property.PropertyType;
        UnderlyingType = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
        TypeConverter = TypeDescriptor.GetConverter(PropertyType);
    }

    internal PropertyInfo Property { get; }

    internal FixedWidthBinaryFieldAttribute Attribute { get; }

    internal int ByteOffset { get; }

    internal Action<object, object?> Setter { get; }

    internal Func<object, object?> Getter { get; }

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

    // Encodes this field's property value into the record buffer, using the encoding for Text fields.
    internal void Encode(object record, byte[] buffer, Encoding encoding)
    {
        var value = Getter(record);
        var length = Attribute.ByteLength;
        var span = new Span<byte>(buffer, ByteOffset, length);

        switch (Attribute.Type)
        {
            case BinaryFieldType.Text:
                var text = value?.ToString() ?? string.Empty;
                if (text.Length > length)
                {
                    throw new FieldOverflowException($"Value '{text}' is {text.Length} characters, longer than the {length}-byte field '{Property.Name}'.", Property.Name, length, text.Length);
                }

                var encoded = encoding.GetBytes(text.PadRight(length));
                if (encoded.Length != length)
                {
                    throw new InvalidOperationException($"Text field '{Property.Name}' encoded to {encoded.Length} bytes for a {length}-byte field; binary records require a single-byte encoding for text fields.");
                }

                encoded.CopyTo(span);
                break;

            case BinaryFieldType.PackedDecimal:
                PackedDecimal.Encode(Convert.ToDecimal(value, CultureInfo.InvariantCulture), Attribute.Scale, span);
                break;

            case BinaryFieldType.Binary:
                BinaryInteger.Encode(Convert.ToInt64(value, CultureInfo.InvariantCulture), Attribute.Signed, span);
                break;

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
