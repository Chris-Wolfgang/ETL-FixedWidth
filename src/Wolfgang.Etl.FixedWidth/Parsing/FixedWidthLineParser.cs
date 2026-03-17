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
        var segments = new List<string>(descriptors.Count);
        var currentPosition = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];

            // Insert placeholder for any skipped columns before this field.
            if (descriptor.Start > currentPosition)
            {
                var skipWidth = descriptor.Start - currentPosition;
                if (skipWidth > 0)
                {
                    segments.Add(new string(' ', skipWidth));
                }
            }

            // Format the actual field segment.
            segments.Add(FormatSegment(record, descriptor, converter));
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        // Pad any remaining columns at the end of the line.
        if (fieldMap.ExpectedLineWidth > currentPosition)
        {
            var trailingWidth = fieldMap.ExpectedLineWidth - currentPosition;
            if (trailingWidth > 0)
            {
                segments.Add(new string(' ', trailingWidth));
            }
        }

        return segments.ToArray();
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
        var segments = new List<string>(descriptors.Count);
        var currentPosition = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];

            // Insert placeholder for any skipped columns before this header field.
            if (descriptor.Start > currentPosition)
            {
                var skipWidth = descriptor.Start - currentPosition;
                if (skipWidth > 0)
                {
                    segments.Add(new string(' ', skipWidth));
                }
            }

            // Format the header segment for the actual field.
            segments.Add(FormatHeaderSegment(descriptor, headerConverter));
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        // Pad any remaining columns at the end of the header line.
        if (fieldMap.ExpectedLineWidth > currentPosition)
        {
            var trailingWidth = fieldMap.ExpectedLineWidth - currentPosition;
            if (trailingWidth > 0)
            {
                segments.Add(new string(' ', trailingWidth));
            }
        }

        return segments.ToArray();
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
        var segments = new List<string>(descriptors.Count);
        var currentPosition = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];

            // Insert separator chars for any skipped columns before this field.
            if (descriptor.Start > currentPosition)
            {
                var skipWidth = descriptor.Start - currentPosition;
                if (skipWidth > 0)
                {
                    segments.Add(new string(separatorChar, skipWidth));
                }
            }

            segments.Add(new string(separatorChar, descriptor.Attribute.Length));
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        // Pad any remaining columns at the end.
        if (fieldMap.ExpectedLineWidth > currentPosition)
        {
            var trailingWidth = fieldMap.ExpectedLineWidth - currentPosition;
            if (trailingWidth > 0)
            {
                segments.Add(new string(separatorChar, trailingWidth));
            }
        }

        return segments.ToArray();
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
        var delimiterCount = Math.Max(0, fieldMap.TotalColumnCount - 1);
        var fullExpectedWidth = fieldMap.ExpectedLineWidth + delimiterWidth * delimiterCount;

        if (line.Length < fullExpectedWidth)
        {
            throw new LineTooShortException
            (
                $"Line {lineNumber} is too short. Expected {fullExpectedWidth} characters" +
                (
                    delimiterWidth > 0
                    ? $" ({fieldMap.ExpectedLineWidth} field/skip width {delimiterWidth * delimiterCount} delimiter width)"
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
