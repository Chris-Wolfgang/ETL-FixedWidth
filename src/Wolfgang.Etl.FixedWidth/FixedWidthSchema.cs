using System;
using System.Collections.Generic;
using System.Linq;
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
}
