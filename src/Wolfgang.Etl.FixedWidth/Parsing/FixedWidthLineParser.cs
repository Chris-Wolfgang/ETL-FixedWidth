using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
    // Direct-write formatting — writes segments directly to a TextWriter
    // to avoid intermediate List<string> and Join allocations.
    // ------------------------------------------------------------------

    /// <summary>
    /// Writes a data record directly to <paramref name="writer"/>, one field at a time.
    /// Skipped-column gaps are filled with spaces, and <paramref name="fieldDelimiter"/>
    /// is inserted between adjacent logical columns (including skip columns) to match
    /// the parser's <see cref="FieldDescriptor.AbsoluteColumnIndex"/> semantics.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    internal static void WriteRecord<T>
    (
        TextWriter writer,
        T record,
        FieldMapResult fieldMap,
        Func<object, FieldContext, string> converter,
        string? fieldDelimiter
    )
    {
        var descriptors = fieldMap.Descriptors;
        var hasDelimiter = !string.IsNullOrEmpty(fieldDelimiter);
        var currentPosition = 0;
        var nextColumnIndex = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            WriteGapDelimiters(writer, descriptor, fieldDelimiter, hasDelimiter, ref nextColumnIndex);

            if (descriptor.Start > currentPosition)
            {
                WritePadding(writer, ' ', descriptor.Start - currentPosition);
            }

            WriteFieldSegment(writer, record, descriptor, converter);
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        WriteTrailingDelimiters(writer, fieldMap, fieldDelimiter, hasDelimiter, nextColumnIndex);
        WriteTrailingPadding(writer, fieldMap, currentPosition, ' ');
    }



    /// <summary>
    /// Writes a header line directly to <paramref name="writer"/>.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    internal static void WriteHeader
    (
        TextWriter writer,
        FieldMapResult fieldMap,
        Func<string, FieldContext, string> headerConverter,
        string? fieldDelimiter
    )
    {
        var descriptors = fieldMap.Descriptors;
        var hasDelimiter = !string.IsNullOrEmpty(fieldDelimiter);
        var currentPosition = 0;
        var nextColumnIndex = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            WriteGapDelimiters(writer, descriptor, fieldDelimiter, hasDelimiter, ref nextColumnIndex);

            if (descriptor.Start > currentPosition)
            {
                WritePadding(writer, ' ', descriptor.Start - currentPosition);
            }

            WriteHeaderSegmentTo(writer, descriptor, headerConverter);
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        WriteTrailingDelimiters(writer, fieldMap, fieldDelimiter, hasDelimiter, nextColumnIndex);
        WriteTrailingPadding(writer, fieldMap, currentPosition, ' ');
    }



    /// <summary>
    /// Writes a separator line directly to <paramref name="writer"/>.
    /// </summary>
    internal static void WriteSeparator
    (
        TextWriter writer,
        FieldMapResult fieldMap,
        char separatorChar,
        string? fieldDelimiter
    )
    {
        var descriptors = fieldMap.Descriptors;
        var hasDelimiter = !string.IsNullOrEmpty(fieldDelimiter);
        var currentPosition = 0;
        var nextColumnIndex = 0;

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            WriteGapDelimiters(writer, descriptor, fieldDelimiter, hasDelimiter, ref nextColumnIndex);

            if (descriptor.Start > currentPosition)
            {
                WritePadding(writer, separatorChar, descriptor.Start - currentPosition);
            }

            WritePadding(writer, separatorChar, descriptor.Attribute.Length);
            currentPosition = descriptor.Start + descriptor.Attribute.Length;
        }

        WriteTrailingDelimiters(writer, fieldMap, fieldDelimiter, hasDelimiter, nextColumnIndex);
        WriteTrailingPadding(writer, fieldMap, currentPosition, separatorChar);
    }



    /// <summary>
    /// Writes delimiters for all logical columns (including skip columns) between
    /// the last written column and the current descriptor. Aligns with the parser's
    /// <see cref="FieldDescriptor.AbsoluteColumnIndex"/> semantics.
    /// </summary>
    private static void WriteGapDelimiters
    (
        TextWriter writer,
        FieldDescriptor descriptor,
        string? fieldDelimiter,
        bool hasDelimiter,
        ref int nextColumnIndex
    )
    {
        if (!hasDelimiter)
        {
            return;
        }

        // Emit a delimiter for each logical column between the previous and current
        var targetColumnIndex = descriptor.AbsoluteColumnIndex;
        while (nextColumnIndex < targetColumnIndex)
        {
            if (nextColumnIndex > 0)
            {
                writer.Write(fieldDelimiter);
            }
            nextColumnIndex++;
        }

        // Emit the delimiter before this descriptor's column (unless it's the first)
        if (targetColumnIndex > 0)
        {
            writer.Write(fieldDelimiter);
        }

        nextColumnIndex = targetColumnIndex + 1;
    }



    /// <summary>
    /// Writes delimiters for any trailing skip columns after the last field descriptor.
    /// </summary>
    private static void WriteTrailingDelimiters
    (
        TextWriter writer,
        FieldMapResult fieldMap,
        string? fieldDelimiter,
        bool hasDelimiter,
        int nextColumnIndex
    )
    {
        if (!hasDelimiter || fieldMap.TotalColumnCount <= 0)
        {
            return;
        }

        while (nextColumnIndex < fieldMap.TotalColumnCount)
        {
            if (nextColumnIndex > 0)
            {
                writer.Write(fieldDelimiter);
            }
            nextColumnIndex++;
        }
    }



    /// <summary>
    /// Writes trailing padding to fill any remaining width after the last field.
    /// </summary>
    private static void WriteTrailingPadding
    (
        TextWriter writer,
        FieldMapResult fieldMap,
        int currentPosition,
        char padChar
    )
    {
        if (fieldMap.ExpectedLineWidth > currentPosition)
        {
            var trailingWidth = fieldMap.ExpectedLineWidth - currentPosition;
            if (trailingWidth > 0)
            {
                WritePadding(writer, padChar, trailingWidth);
            }
        }
    }



    /// <summary>
    /// Writes <paramref name="count"/> copies of <paramref name="padChar"/> to
    /// <paramref name="writer"/> without allocating an intermediate <see cref="string"/>.
    /// On net8+ uses a stack-allocated span; on older targets falls back to a
    /// pooled <see cref="char"/> buffer.
    /// </summary>
    private static void WritePadding(TextWriter writer, char padChar, int count)
    {
        if (count <= 0)
        {
            return;
        }

#if NET8_0_OR_GREATER
        // Stack-allocate a small buffer; field widths are typically tiny.
        const int stackBufferSize = 128;
        Span<char> buffer = stackalloc char[stackBufferSize];
        var prefix = count < stackBufferSize ? count : stackBufferSize;
        buffer.Slice(0, prefix).Fill(padChar);
        while (count > 0)
        {
            var chunk = count < stackBufferSize ? count : stackBufferSize;
            writer.Write(buffer.Slice(0, chunk));
            count -= chunk;
        }
#else
        const int maxBufferSize = 128;
        var bufferSize = count < maxBufferSize ? count : maxBufferSize;
        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        try
        {
            for (var i = 0; i < bufferSize; i++)
            {
                buffer[i] = padChar;
            }

            while (count > 0)
            {
                var chunk = count < bufferSize ? count : bufferSize;
                writer.Write(buffer, 0, chunk);
                count -= chunk;
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
#endif
    }



    /// <summary>
    /// Writes a single data field directly to <paramref name="writer"/> as
    /// "value + padding" (or "padding + value" for right-aligned fields), avoiding
    /// the <see cref="string.PadLeft(int,char)"/> / <see cref="string.PadRight(int,char)"/>
    /// allocation that <see cref="FormatSegment{T}"/> produces.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the property has no public getter.
    /// </exception>
    private static void WriteFieldSegment<T>
    (
        TextWriter writer,
        T record,
        FieldDescriptor descriptor,
        Func<object, FieldContext, string> converter
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;
        var getter = descriptor.Getter
            ?? throw new InvalidOperationException(
                $"Property '{prop.Name}' has no public getter. " +
                "The loader requires readable properties to format field values.");
        var text = converter(getter(record!)!, descriptor.Context);

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

        var padCount = attr.Length - text.Length;
#if NET8_0_OR_GREATER
        // Stack-allocate the full padded field so it can be written in a single
        // call when small enough — typical fixed-width fields are ≤128 chars.
        const int stackFieldLimit = 256;
        if (attr.Length <= stackFieldLimit)
        {
            Span<char> field = stackalloc char[attr.Length];
            if (attr.Alignment == FieldAlignment.Right)
            {
                field.Slice(0, padCount).Fill(attr.Pad);
                text.AsSpan().CopyTo(field.Slice(padCount));
            }
            else
            {
                text.AsSpan().CopyTo(field);
                field.Slice(text.Length).Fill(attr.Pad);
            }
            writer.Write(field);
            return;
        }
#endif

        if (attr.Alignment == FieldAlignment.Right)
        {
            WritePadding(writer, attr.Pad, padCount);
            writer.Write(text);
        }
        else
        {
            writer.Write(text);
            WritePadding(writer, attr.Pad, padCount);
        }
    }



    /// <summary>
    /// Writes a single header label directly to <paramref name="writer"/> as
    /// "label + space-padding", avoiding the <see cref="string.PadRight(int,char)"/>
    /// allocation that <see cref="FormatHeaderSegment"/> produces.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    private static void WriteHeaderSegmentTo
    (
        TextWriter writer,
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

        writer.Write(text);
        WritePadding(writer, ' ', attr.Length - text.Length);
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
        FixedWidthValueParser? valueParser = null
    )
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

        var record = (T)fieldMap.Factory();

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
        FixedWidthValueParser valueParser
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;

        // Use AbsoluteColumnIndex so skipped columns are accounted for
        // when offsetting for delimiter characters.
        var start = descriptor.Start + (delimiterWidth * descriptor.AbsoluteColumnIndex);
        var raw = line.AsMemory().Slice(start, attr.Length);
        var value = attr.TrimValue
            ? raw.TrimMemory()
            : raw;

        try
        {
            // Fast path: when using the default parser, pass the cached TypeConverter
            // to avoid TypeDescriptor.GetConverter on every field of every record.
            var converted = ReferenceEquals(valueParser, FixedWidthConverter.DefaultParser)
                ? FixedWidthConverter.ParseValue(value, descriptor.Context.PropertyType, descriptor.Context.Format, descriptor.TypeConverter)
                : valueParser(value, descriptor.Context);
            descriptor.Setter(record!, converted);
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
                value.ToString(),
                ex
            );
        }
    }



    /// <summary>
    /// Formats a single data field value into a padded string segment.
    /// </summary>
    /// <exception cref="FieldOverflowException"></exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the property has no public getter.
    /// </exception>
    private static string FormatSegment<T>
    (
        T record,
        FieldDescriptor descriptor,
        Func<object, FieldContext, string> converter
    )
    {
        var attr = descriptor.Attribute;
        var prop = descriptor.Property;
        var getter = descriptor.Getter
            ?? throw new InvalidOperationException(
                $"Property '{prop.Name}' has no public getter. " +
                "The loader requires readable properties to format field values.");
        var text = converter(getter(record!)!, descriptor.Context);

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
