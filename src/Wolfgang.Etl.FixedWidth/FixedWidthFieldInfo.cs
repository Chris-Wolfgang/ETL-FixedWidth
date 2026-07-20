using System;
using System.Globalization;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Read-only metadata for a single resolved column in a fixed-width record layout —
/// either a mapped field or a skipped column — exposed by
/// <see cref="FixedWidthSchema"/> (#22).
/// </summary>
/// <remarks>
/// Positions are zero-based and inclusive: a field at columns 0..7 has
/// <see cref="StartPosition"/> 0 and <see cref="EndPosition"/> 7. Skipped columns
/// (<see cref="IsSkip"/> is <see langword="true"/>) have no
/// <see cref="Name"/>/<see cref="PropertyType"/>/<see cref="Header"/> and expose
/// their <see cref="SkipMessage"/> instead.
/// </remarks>
public sealed class FixedWidthFieldInfo
{
    private FixedWidthFieldInfo
    (
        bool isSkip,
        string? name,
        int columnIndex,
        int startPosition,
        int length,
        Type? propertyType,
        FieldAlignment alignment,
        char pad,
        string? format,
        string? header,
        NumberStyles? numberStyles,
        string? skipMessage
    )
    {
        IsSkip = isSkip;
        Name = name;
        ColumnIndex = columnIndex;
        StartPosition = startPosition;
        Length = length;
        PropertyType = propertyType;
        Alignment = alignment;
        Pad = pad;
        Format = format;
        Header = header;
        NumberStyles = numberStyles;
        SkipMessage = skipMessage;
    }



    /// <summary>Whether this column is a skipped column rather than a mapped field.</summary>
    public bool IsSkip { get; }



    /// <summary>The mapped property name, or <see langword="null"/> for a skipped column.</summary>
    public string? Name { get; }



    /// <summary>The absolute zero-based column index within the record, including skipped columns.</summary>
    public int ColumnIndex { get; }



    /// <summary>The zero-based, inclusive start position of this column in the line.</summary>
    public int StartPosition { get; }



    /// <summary>The zero-based, inclusive end position of this column (<c>StartPosition + Length - 1</c>).</summary>
    public int EndPosition => StartPosition + Length - 1;



    /// <summary>The width of this column in characters.</summary>
    public int Length { get; }



    /// <summary>The mapped property's type, or <see langword="null"/> for a skipped column.</summary>
    public Type? PropertyType { get; }



    /// <summary>The field alignment. Defaults to <see cref="FieldAlignment.Left"/> for a skipped column.</summary>
    public FieldAlignment Alignment { get; }



    /// <summary>The pad character. Defaults to space for a skipped column.</summary>
    public char Pad { get; }



    /// <summary>The format string applied to this field, or <see langword="null"/> if none / a skipped column.</summary>
    public string? Format { get; }



    /// <summary>The header label written for this field, or <see langword="null"/> for a skipped column.</summary>
    public string? Header { get; }



    /// <summary>The explicit <see cref="System.Globalization.NumberStyles"/> for numeric parsing, or <see langword="null"/> to use the type's natural style.</summary>
    public NumberStyles? NumberStyles { get; }



    /// <summary>The optional message describing a skipped column, or <see langword="null"/> for a mapped field.</summary>
    public string? SkipMessage { get; }



    internal static FixedWidthFieldInfo From(FieldMap.ColumnLayout column)
    {
        if (column.IsSkip)
        {
            var skip = column.Skip!;
            return new FixedWidthFieldInfo
            (
                isSkip: true,
                name: null,
                columnIndex: column.ColumnIndex,
                startPosition: column.Start,
                length: column.Length,
                propertyType: null,
                alignment: FieldAlignment.Left,
                pad: ' ',
                format: null,
                header: null,
                numberStyles: null,
                skipMessage: skip.Message
            );
        }

        var property = column.Property!;
        var field = column.Field!;
        var numberStyles = field.NumberStyles == FixedWidthFieldAttribute.UnspecifiedNumberStyles
            ? (NumberStyles?)null
            : field.NumberStyles;

        return new FixedWidthFieldInfo
        (
            isSkip: false,
            name: property.Name,
            columnIndex: column.ColumnIndex,
            startPosition: column.Start,
            length: column.Length,
            propertyType: property.PropertyType,
            alignment: field.Alignment,
            pad: field.Pad,
            format: field.Format,
            header: field.Header ?? property.Name,
            numberStyles: numberStyles,
            skipMessage: null
        );
    }
}
