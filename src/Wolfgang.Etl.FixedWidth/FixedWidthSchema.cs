using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A read-only view over the resolved fixed-width field layout for a record type
/// (#22). Exposes the field/skip columns, positions, widths, types, and formatting
/// that <see cref="FixedWidthExtractor{TRecord}"/> / <see cref="FixedWidthLoader{TRecord}"/>
/// derive from <c>[FixedWidthField]</c> / <c>[FixedWidthSkip]</c> attributes — useful
/// for generating documentation, building validation tooling, or debugging a layout.
/// </summary>
/// <example>
/// <code>
/// var schema = FixedWidthSchema.For&lt;CustomerRecord&gt;();
/// foreach (var field in schema.Fields)
/// {
///     Console.WriteLine($"{field.StartPosition}-{field.EndPosition} {field.Name} ({field.Length})");
/// }
/// Console.WriteLine($"Line width: {schema.ExpectedLineWidth}");
/// </code>
/// </example>
public sealed class FixedWidthSchema
{
    private FixedWidthSchema
    (
        Type recordType,
        IReadOnlyList<FixedWidthFieldInfo> fields,
        int expectedLineWidth
    )
    {
        RecordType = recordType;
        Fields = fields;
        ExpectedLineWidth = expectedLineWidth;
    }



    /// <summary>
    /// Resolves the schema for record type <typeparamref name="T"/>. Applies the same
    /// validation as extraction/loading, so an invalid layout (duplicate column
    /// indexes, a mapped field with no public setter) throws here too.
    /// </summary>
    public static FixedWidthSchema For<T>()
        where T : notnull
        => For(typeof(T));



    /// <summary>
    /// Resolves the schema for <paramref name="recordType"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="recordType"/> is <see langword="null"/>.</exception>
    public static FixedWidthSchema For(Type recordType)
    {
        if (recordType == null)
        {
            throw new ArgumentNullException(nameof(recordType));
        }

        var columns = FieldMap.GetColumnLayout(recordType);
        var fields = new List<FixedWidthFieldInfo>(columns.Count);
        var width = 0;
        foreach (var column in columns)
        {
            fields.Add(FixedWidthFieldInfo.From(column));
            width += column.Length;
        }

        return new FixedWidthSchema(recordType, fields.AsReadOnly(), width);
    }



    /// <summary>The record type this schema describes.</summary>
    public Type RecordType { get; }



    /// <summary>
    /// All resolved columns in position order, including skipped columns
    /// (<see cref="FixedWidthFieldInfo.IsSkip"/>). Filter on <c>!IsSkip</c> for the
    /// mapped fields only.
    /// </summary>
    public IReadOnlyList<FixedWidthFieldInfo> Fields { get; }



    /// <summary>The total line width in characters, including skipped columns.</summary>
    public int ExpectedLineWidth { get; }



    /// <summary>The total number of columns, including skipped columns.</summary>
    public int TotalColumnCount => Fields.Count;



    /// <summary>The number of mapped fields, excluding skipped columns.</summary>
    public int FieldCount => Fields.Count(f => !f.IsSkip);



    /// <summary>The number of skipped columns.</summary>
    public int SkipCount => Fields.Count(f => f.IsSkip);



    /// <summary>
    /// Renders the resolved layout as a human-readable text table for debugging,
    /// logging, and documentation (#24). Columns line up on their widest cell; lines
    /// are separated by <c>\n</c> and carry no trailing whitespace.
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(FixedWidthSchema.For&lt;CustomerRecord&gt;().ToDiagram());
    /// </code>
    /// </example>
    public string ToDiagram()
    {
        string[] headers = { "Position", "Field", "Type", "Length", "Align", "Pad", "Format" };

        var rows = new List<string[]>(Fields.Count);
        foreach (var field in Fields)
        {
            rows.Add(new[]
            {
                $"{field.StartPosition}-{field.EndPosition}",
                field.IsSkip ? "[skip]" : field.Name!,
                field.IsSkip ? string.Empty : field.PropertyType!.Name,
                field.Length.ToString(CultureInfo.InvariantCulture),
                field.IsSkip ? string.Empty : field.Alignment.ToString(),
                field.IsSkip ? string.Empty : $"'{field.Pad}'",
                field.Format ?? string.Empty,
            });
        }

        var widths = new int[headers.Length];
        for (var col = 0; col < headers.Length; col++)
        {
            widths[col] = headers[col].Length;
            foreach (var row in rows)
            {
                widths[col] = Math.Max(widths[col], row[col].Length);
            }
        }

        var separators = widths.Select(w => new string('-', w)).ToArray();

        var builder = new StringBuilder();
        AppendRow(builder, headers, widths);
        AppendRow(builder, separators, widths);
        foreach (var row in rows)
        {
            AppendRow(builder, row, widths);
        }

        builder.Append('\n');
        var footer = string.Format
        (
            CultureInfo.InvariantCulture,
            "Total width: {0}  |  Columns: {1} ({2} field{3} + {4} skip{5})  |  Delimiter: none",
            ExpectedLineWidth,
            TotalColumnCount,
            FieldCount,
            Plural(FieldCount),
            SkipCount,
            Plural(SkipCount)
        );
        builder.Append(footer);

        return builder.ToString();
    }



    private static void AppendRow(StringBuilder builder, string[] cells, int[] widths)
    {
        var line = new StringBuilder();
        for (var col = 0; col < cells.Length; col++)
        {
            if (col > 0)
            {
                line.Append("  ");
            }

            line.Append(cells[col].PadRight(widths[col]));
        }

        // Drop the trailing padding on the last column so lines have no trailing space.
        var text = line.ToString().TrimEnd();
        builder.Append(text);
        builder.Append('\n');
    }



    private static string Plural(int count) => count == 1 ? string.Empty : "s";
}
