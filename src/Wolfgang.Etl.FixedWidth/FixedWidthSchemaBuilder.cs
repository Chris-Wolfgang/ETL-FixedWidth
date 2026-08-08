using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Builds a <see cref="FixedWidthSchema"/> for <typeparamref name="T"/> from a fluent, type-safe code
/// API instead of <c>[FixedWidthField]</c> / <c>[FixedWidthSkip]</c> attributes (#23). Use it when the
/// record type is not yours to decorate, when you prefer configuration-as-code, or when the layout is
/// built dynamically. The resulting schema is equivalent to one resolved from attributes — assign it to
/// <see cref="FixedWidthExtractor{TRecord}.Schema"/> / <see cref="FixedWidthLoader{TRecord}.Schema"/> to
/// use it, or inspect it with <see cref="FixedWidthSchema.Fields"/> / <see cref="FixedWidthSchema.ToDiagram"/>.
/// </summary>
/// <typeparam name="T">The record type the schema describes.</typeparam>
/// <example>
/// <code>
/// var schema = new FixedWidthSchemaBuilder&lt;CustomerRecord&gt;()
///     .Field(r =&gt; r.CustomerId, index: 0, length: 8)
///     .Field(r =&gt; r.Name, index: 1, length: 30)
///     .Skip(index: 2, length: 5)
///     .Field(r =&gt; r.Balance, index: 3, length: 9, alignment: FieldAlignment.Right, format: "0000000.00")
///     .Build();
///
/// var extractor = new FixedWidthExtractor&lt;CustomerRecord&gt;(reader) { Schema = schema };
/// </code>
/// </example>
public sealed class FixedWidthSchemaBuilder<T>
    where T : notnull
{
    private readonly List<Entry> _entries = new();


    /// <summary>
    /// Adds a mapped field. The <paramref name="index"/> is a zero-based column ordinal (matching
    /// <c>[FixedWidthField(index, length)]</c>); start positions are computed by summing the lengths of
    /// all lower-indexed columns, so indexes only need to be unique, not contiguous.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="selector">A simple property access expression, e.g. <c>r =&gt; r.Name</c>.</param>
    /// <param name="index">The zero-based column index. Must be unique across fields and skips.</param>
    /// <param name="length">The field width in characters. Must be greater than zero.</param>
    /// <param name="alignment">Padding alignment for writing. Defaults to <see cref="FieldAlignment.Left"/>.</param>
    /// <param name="pad">Pad character for writing. Defaults to <c>' '</c>.</param>
    /// <param name="format">Optional format string for parsing/formatting the value.</param>
    /// <param name="header">Optional header label; defaults to the property name when writing headers.</param>
    /// <param name="numberStyles">Optional <see cref="NumberStyles"/> for numeric parsing; defaults to the type's natural style.</param>
    /// <param name="trimValue">Whether to trim whitespace from the extracted value. Defaults to <see langword="true"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="selector"/> is not a simple property access.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or <paramref name="length"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">The selected property has no public setter.</exception>
    public FixedWidthSchemaBuilder<T> Field<TProperty>
    (
        Expression<Func<T, TProperty>> selector,
        int index,
        int length,
        FieldAlignment alignment = FieldAlignment.Left,
        char pad = ' ',
        string? format = null,
        string? header = null,
        NumberStyles? numberStyles = null,
        bool trimValue = true
    )
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var property = ResolveProperty(selector);
        if (property.SetMethod is null || !property.SetMethod.IsPublic)
        {
            throw new InvalidOperationException
            (
                $"Property '{property.Name}' on '{typeof(T).FullName}' has no public setter; " +
                "a fixed-width field must be writable for extraction."
            );
        }

        var attribute = new FixedWidthFieldAttribute(index, length)
        {
            Alignment = alignment,
            Pad = pad,
            Format = format,
            Header = header,
            TrimValue = trimValue,
            NumberStyles = numberStyles ?? FixedWidthFieldAttribute.UnspecifiedNumberStyles,
        };

        _entries.Add(new Entry(property, attribute, skip: null));
        return this;
    }


    /// <summary>
    /// Adds a skipped column — a range in the file mapped to no property. Its width still contributes to
    /// the positions of later fields.
    /// </summary>
    /// <param name="index">The zero-based column index. Must be unique across fields and skips.</param>
    /// <param name="length">The skipped width in characters. Must be greater than zero.</param>
    /// <param name="message">Optional note describing the skipped column (surfaced by schema introspection).</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or <paramref name="length"/> is not positive.</exception>
    public FixedWidthSchemaBuilder<T> Skip(int index, int length, string? message = null)
    {
        var attribute = new FixedWidthSkipAttribute(index, length) { Message = message };
        _entries.Add(new Entry(property: null, field: null, attribute));
        return this;
    }


    /// <summary>
    /// Validates the accumulated fields and skips and produces the immutable <see cref="FixedWidthSchema"/>.
    /// </summary>
    /// <returns>The resolved schema.</returns>
    /// <exception cref="InvalidOperationException">
    /// No fields were defined, or two columns share the same index.
    /// </exception>
    public FixedWidthSchema Build()
    {
        if (_entries.TrueForAll(e => e.IsSkip))
        {
            throw new InvalidOperationException("The schema must define at least one field.");
        }

        var duplicate = _entries
            .GroupBy(e => e.Index)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new InvalidOperationException
            (
                $"Duplicate column index {duplicate.Key}. Each field and skip must have a unique index."
            );
        }

        var ordered = _entries.OrderBy(e => e.Index).ToList();
        var descriptors = new List<FieldDescriptor>();
        var fields = new List<FixedWidthFieldInfo>(ordered.Count);
        var position = 0;
        var columnIndex = 0;

        foreach (var entry in ordered)
        {
            if (!entry.IsSkip)
            {
                descriptors.Add(new FieldDescriptor(entry.Property!, entry.Field!, position, columnIndex));
            }

            fields.Add(FixedWidthFieldInfo.From(new FieldMap.ColumnLayout(columnIndex, position, entry.Property, entry.Field, entry.Skip)));
            position += entry.Length;
            columnIndex++;
        }

        var mapResult = new FieldMapResult
        (
            descriptors.AsReadOnly(),
            expectedLineWidth: position,
            totalColumnCount: columnIndex,
            factory: FieldMapResult.CompileFactory(typeof(T))
        );

        return new FixedWidthSchema(typeof(T), fields.AsReadOnly(), position, mapResult);
    }


    private static PropertyInfo ResolveProperty<TProperty>(Expression<Func<T, TProperty>> selector)
    {
        var body = selector.Body;

        // Unwrap the Convert node the compiler inserts when a value-type property is used through the
        // Func<T, TProperty> boxing (e.g. r => r.Age where TProperty is object).
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Member: PropertyInfo property })
        {
            return property;
        }

        throw new ArgumentException
        (
            "The selector must be a simple property access, for example 'r => r.Name'.",
            nameof(selector)
        );
    }


    private sealed class Entry
    {
        internal Entry(PropertyInfo? property, FixedWidthFieldAttribute? field, FixedWidthSkipAttribute? skip)
        {
            Property = property;
            Field = field;
            Skip = skip;
        }

        internal PropertyInfo? Property { get; }

        internal FixedWidthFieldAttribute? Field { get; }

        internal FixedWidthSkipAttribute? Skip { get; }

        internal bool IsSkip => Skip != null;

        internal int Index => Skip?.Index ?? Field!.Index;

        internal int Length => Skip?.Length ?? Field!.Length;
    }
}
