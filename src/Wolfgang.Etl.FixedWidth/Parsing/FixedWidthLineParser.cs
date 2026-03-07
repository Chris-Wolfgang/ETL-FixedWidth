using System;
using System.Collections.Generic;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// Provides methods for parsing a single fixed-width line into a POCO and for
/// formatting a POCO or header back into field segments.
/// </summary>
internal static class FixedWidthLineParser
{
    // ------------------------------------------------------------------
    // Formatting — returns one padded string per field.
    // The loader joins them with the configured delimiter (or empty string).
    // ------------------------------------------------------------------

    /// <summary>
    /// Formats each field of <paramref name="record"/> into a padded string segment,
    /// one per field in column order.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    internal static IReadOnlyList<string> FormatSegments<T>
    (
        T record,
        FieldMapResult fieldMap,
        Func<object, FieldContext, string> converter
    )
    {
        var descriptors = fieldMap.Descriptors;
        var segments = new string[descriptors.Count];

        for (var i = 0; i < descriptors.Count; i++)
        {
            segments[i] = FormatSegment(record, descriptors[i], converter);
        }

        return segments;
    }



    /// <summary>
    /// Formats the header label for each field into a padded string segment,
    /// one per field in column order. The label is either
    /// <see cref="FixedWidthFieldAttribute.Header"/> or the property name.
    /// The <paramref name="headerConverter"/> controls the text content and may
    /// truncate or validate the label, but headers are always left-aligned and
    /// space-padded regardless of the field's <see cref="FieldAlignment"/> setting.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    internal static IReadOnlyList<string> FormatHeaderSegments
    (
        FieldMapResult fieldMap,
        Func<string, FieldContext, string> headerConverter
    )
    {
        var descriptors = fieldMap.Descriptors;
        var segments = new string[descriptors.Count];

        for (var i = 0; i < descriptors.Count; i++)
        {
            segments[i] = FormatHeaderSegment(descriptors[i], headerConverter);
        }

        return segments;
    }



    /// <summary>
    /// Formats a separator segment for each field — a string of
    /// <paramref name="separatorChar"/> repeated to the field's width.
    /// </summary>
    internal static IReadOnlyList<string> FormatSeparatorSegments
    (
        FieldMapResult fieldMap,
        char separatorChar
    )
    {
        var descriptors = fieldMap.Descriptors;
        var segments = new string[descriptors.Count];

        for (var i = 0; i < descriptors.Count; i++)
        {
            segments[i] = new string
            (
                separatorChar,
                descriptors[i].Attribute.Length
            );
        }

        return segments;
    }



    // ------------------------------------------------------------------
    // Parsing
    // ------------------------------------------------------------------

    /// <summary>
    /// Parses a single fixed-width <paramref name="line"/> into a new instance of
    /// <typeparamref name="T"/>. Validates that the line is long enough to satisfy
    /// all field and skip definitions plus any delimiter contribution before parsing.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="lineNumber"></param>
    /// <param name="fieldMap"></param>
    /// <param name="fieldDelimiter"></param>
    /// <param name="valueParser"></param>
    /// <exception cref="LineTooShortException"></exception>
    internal static T ParseLine<T>
    (
        string line,
        long lineNumber,
        FieldMapResult fieldMap,
        string? fieldDelimiter = null,
        Func<string, FieldContext, object>? valueParser = null
    )
        where T : new()
    {
        valueParser ??= FixedWidthConverter.DefaultParser;
        var delimiterWidth = string.IsNullOrEmpty(fieldDelimiter)
            ? 0
            : fieldDelimiter!.Length;
        var fullExpectedWidth = fieldMap.ExpectedLineWidth + delimiterWidth * (fieldMap.TotalColumnCount - 1);

        if (line.Length < fullExpectedWidth)
        {
            throw new LineTooShortException
            (
                $"Line {lineNumber} is too short. Expected {fullExpectedWidth} characters" +
                (
                    delimiterWidth > 0
                    ? $" ({fieldMap.ExpectedLineWidth} field/skip width {delimiterWidth * (fieldMap.TotalColumnCount - 1)} delimiter width)"
                    : string.Empty
                )
                + $" but found {line.Length}.",
                lineNumber,
                line,
                fullExpectedWidth,
                line.Length
            );
        }

        var record = new T();

        foreach (var descriptor in fieldMap.Descriptors)
        {
            ParseField(record, descriptor, line, lineNumber, delimiterWidth, valueParser);
        }

        return record;
    }



    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Converts and assigns a single field value from <paramref name="line"/>
    /// into the matching property on <paramref name="record"/>.
    /// </summary>
    /// <exception cref="FieldConversionException"></exception>
    private static void ParseField<T>
    (
        T record,
        FieldDescriptor descriptor,
        string line,
        long lineNumber,
        int delimiterWidth,
        Func<string, FieldContext, object> valueParser
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;

        // Use AbsoluteColumnIndex so skipped columns are accounted for
        // when offsetting for delimiter characters.
        var start = descriptor.Start + (delimiterWidth * descriptor.AbsoluteColumnIndex);
        var raw = line.Substring(start, attr.Length);
        var value = attr.TrimValue
            ? raw.Trim()
            : raw;

        try
        {
            var converted = valueParser(value, descriptor.Context);
            prop.SetValue(record, converted);
        }
        catch (Exception ex) when (!(ex is MalformedLineException))
        {
            throw new FieldConversionException
            (
                $"Line {lineNumber}: could not convert value '{value}' to type " +
                $"'{prop.PropertyType.Name}' for property '{prop.Name}'.",
                lineNumber,
                line,
                prop.Name,
                prop.PropertyType,
                value,
                ex
            );
        }
    }



    /// <summary>
    /// Formats a single data field value into a padded string segment.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    private static string FormatSegment<T>
    (
        T record,
        FieldDescriptor descriptor,
        Func<object, FieldContext, string> converter
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;
        var text = converter(prop.GetValue(record)!, descriptor.Context);

        // Safety net — throw if the converter didn't honor the field width contract.
        if (text.Length > attr.Length)
        {
            throw new FieldOverflowException
            (
                $"The value converter returned a string of length {text.Length} " +
                $"for property '{prop.Name}' which exceeds the defined field " +
                $"width of {attr.Length}. Ensure your ValueConverter honors the " +
                $"FieldLength in FieldContext.",
                prop.Name,
                attr.Length,
                text.Length
            );
        }

        return attr.Alignment == FieldAlignment.Right
            ? text.PadLeft(attr.Length, attr.Pad)
            : text.PadRight(attr.Length, attr.Pad);
    }



    /// <summary>
    /// Formats a single header label into a left-aligned, space-padded string segment.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    private static string FormatHeaderSegment
    (
        FieldDescriptor descriptor,
        Func<string, FieldContext, string> headerConverter
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;
        var headerLabel = attr.Header ?? prop.Name;
        var text = headerConverter(headerLabel, descriptor.Context);

        if (text.Length > attr.Length)
        {
            throw new FieldOverflowException
            (
                $"The header converter returned a string of length {text.Length} for property " +
                $"'{prop.Name}' which exceeds the defined field width of {attr.Length}.",
                prop.Name,
                attr.Length,
                text.Length
            );
        }

        return text.PadRight(attr.Length, ' ');
    }
}
