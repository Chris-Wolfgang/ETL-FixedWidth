using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Resolved metadata for one <see cref="FixedWidthBinaryFieldAttribute"/> field: its byte offset
/// within the record, decode/encode instructions, and compiled accessors on the record property.
/// </summary>
internal sealed class BinaryFieldDescriptor
{
    internal BinaryFieldDescriptor
    (
        PropertyInfo property,
        FixedWidthBinaryFieldAttribute attribute,
        int byteOffset,
        Action<object, object?> setter,
        Func<object, object?> getter
    )
    {
        Property = property;
        Attribute = attribute;
        ByteOffset = byteOffset;
        Setter = setter;
        Getter = getter;
        PropertyType = property.PropertyType;
        UnderlyingType = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
        TypeConverter = TrimSafeReflection.GetConverter(PropertyType);
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
    // Return stays `object?` — FixedWidthConverter.ParseValue is itself nullable, and callers
    // (FixedWidthBinaryExtractor) treat null as "no value" rather than as an error, so the
    // ReSharper hint to make this non-nullable would push a null-forgiving cast to every
    // call-site instead.
    // ReSharper disable once ReturnTypeCanBeNotNullable
    internal object? Decode(byte[] record, Encoding encoding)
    {
        var length = Attribute.ByteLength;

        switch (Attribute.Type)
        {
            case BinaryFieldType.Text:
                var text = encoding.GetString(record, ByteOffset, length).TrimEnd();
                return FixedWidthConverter.ParseValue(text.AsMemory(), PropertyType, Attribute.Format, TypeConverter, numberStyles: null);

            case BinaryFieldType.PackedDecimal:
                return Coerce(PackedDecimal.Decode(new ReadOnlySpan<byte>(record, ByteOffset, length), Attribute.Scale));

            case BinaryFieldType.Binary:
                // Decode signed fields as long and unsigned fields as ulong, so an 8-byte unsigned
                // value above long.MaxValue keeps its magnitude. Coerce then converts to the property
                // type — a value that doesn't fit a signed target overflows (throws) via Convert.ChangeType.
                return Attribute.Signed
                    ? Coerce(BinaryInteger.Decode(new ReadOnlySpan<byte>(record, ByteOffset, length)))
                    : Coerce(BinaryInteger.DecodeUnsigned(new ReadOnlySpan<byte>(record, ByteOffset, length)));

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
                var format = FixedWidthConverter.ResolveFormat(value, Attribute.Format);
                var text = value is IFormattable formattable && !string.IsNullOrEmpty(format)
                    ? formattable.ToString(format, CultureInfo.InvariantCulture)
                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (text.Length > length)
                {
                    throw new FieldOverflowException
                    (
                        $"Value '{text}' is {text.Length} characters, longer than the {length}-byte field '{Property.Name}'.",
                        Property.Name,
                        length,
                        text.Length
                    );
                }

                var encoded = encoding.GetBytes(text.PadRight(length));
                if (encoded.Length != length)
                {
                    throw new InvalidOperationException
                    (
                        $"Text field '{Property.Name}' encoded to {encoded.Length} bytes for a {length}-byte field; " +
                        "binary records require a single-byte encoding for text fields."
                    );
                }

                encoded.CopyTo(span);
                break;

            case BinaryFieldType.PackedDecimal:
                PackedDecimal.Encode(Convert.ToDecimal(value, CultureInfo.InvariantCulture), Attribute.Scale, span);
                break;

            case BinaryFieldType.Binary:
                if (Attribute.Signed)
                {
                    BinaryInteger.Encode(Convert.ToInt64(value, CultureInfo.InvariantCulture), signed: true, span);
                }
                else
                {
                    BinaryInteger.EncodeUnsigned(Convert.ToUInt64(value, CultureInfo.InvariantCulture), span);
                }

                break;

            default:
                throw new InvalidOperationException($"Unknown {nameof(BinaryFieldType)} '{Attribute.Type}'.");
        }
    }

    // True for the failures the codecs (PackedDecimal, BinaryInteger, FixedWidthConverter, Convert.ToXxx)
    // raise for bad data or an unrepresentable value. Layout and encoding errors (InvalidOperationException,
    // FieldOverflowException) are not wrapped.
    internal static bool IsCodecFailure(Exception ex)
        => ex is FormatException || ex is OverflowException || ex is ArgumentException;



    // Wraps a decode failure in a FieldConversionException that names the record, the field's byte
    // offset and the property, with the record and field bytes in hex.
    internal FieldConversionException CreateDecodeError(long recordNumber, byte[] record, Exception innerException)
    {
        var fieldBytes = BitConverter.ToString(record, ByteOffset, Attribute.ByteLength);

        return new FieldConversionException
        (
            $"Record {recordNumber}: could not decode the {Attribute.Type} field at byte offset {ByteOffset} " +
            $"(bytes {fieldBytes}) to type '{PropertyType.Name}' for property '{Property.Name}': {innerException.Message}",
            recordNumber,
            BitConverter.ToString(record),
            Property.Name,
            PropertyType,
            fieldBytes,
            ByteOffset,
            innerException
        );
    }



    // Wraps an encode failure in a FieldConversionException that names the record, the field's byte
    // offset and the property, with the property value as the raw value. No record bytes exist yet,
    // so the record content is empty.
    internal FieldConversionException CreateEncodeError(long recordNumber, object record, Exception innerException)
    {
        var rawValue = Convert.ToString(Getter(record), CultureInfo.InvariantCulture) ?? string.Empty;

        return new FieldConversionException
        (
            $"Record {recordNumber}: could not encode value '{rawValue}' of property '{Property.Name}' as the " +
            $"{Attribute.Type} field at byte offset {ByteOffset}: {innerException.Message}",
            recordNumber,
            string.Empty,
            Property.Name,
            PropertyType,
            rawValue,
            ByteOffset,
            innerException
        );
    }



    // Convert the decoded numeric to the property's underlying type BEFORE boxing, so the compiled
    // setter's unbox matches for both T and T? (a boxed long won't unbox to int?).
    private object Coerce(object value)
    {
        if (value.GetType() == UnderlyingType)
        {
            return value;
        }

        // A packed-decimal field with Scale > 0 decodes to a decimal with a fractional part. Assigning
        // that to an integral property would silently round via Convert.ChangeType, quietly corrupting
        // the value — fail fast instead, mirroring how the text parser rejects "1234.56" as an int.
        if (value is decimal dec && dec != Math.Truncate(dec) && IsIntegralType(UnderlyingType))
        {
            throw new OverflowException
            (
                $"Value {dec.ToString(CultureInfo.InvariantCulture)} for property '{Property.Name}' has a " +
                $"fractional part and cannot be assigned to the integral type '{UnderlyingType.Name}'. Map the " +
                $"field to a decimal or floating-point property, or declare Scale = 0."
            );
        }

        return Convert.ChangeType(value, UnderlyingType, CultureInfo.InvariantCulture);
    }



    private static bool IsIntegralType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }
}
