using System;
using System.Collections.Generic;
using System.Reflection;
using Wolfgang.Etl.FixedWidth.Attributes;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// Represents the resolved metadata for a single fixed-width field, including its
/// calculated start position and absolute column index within the record.
/// </summary>
internal sealed class FieldDescriptor
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    internal FieldDescriptor
    (
        PropertyInfo property,
        FixedWidthFieldAttribute attribute,
        int start,
        int absoluteColumnIndex
    )
    {
        Property = property;
        Attribute = attribute;
        Start = start;
        AbsoluteColumnIndex = absoluteColumnIndex;
        Context = new FieldContext
        (
            property.Name,
            property.PropertyType,
            attribute.Length,
            attribute.Pad,
            attribute.Alignment,
            attribute.Format,
            attribute.Header ?? property.Name
        );
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    internal PropertyInfo Property { get; }



    internal FixedWidthFieldAttribute Attribute { get; }



    internal int Start { get; }



    /// <summary>
    /// The zero-based position of this field among all columns in the record,
    /// including skipped columns. Used to correctly offset start positions when
    /// a field delimiter is present.
    /// </summary>
    internal int AbsoluteColumnIndex { get; }



    /// <summary>
    /// Pre-built immutable context for this field, constructed once from the attribute
    /// metadata and reused for every row during formatting.
    /// </summary>
    internal FieldContext Context { get; }
}



/// <summary>
/// The resolved result from <see cref="FieldMap"/>, carrying the ordered field descriptors,
/// the pre-computed expected line width (sum of all field and skip lengths), and the total
/// column count including skipped columns.
/// </summary>
internal sealed class FieldMapResult
{
    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    internal FieldMapResult
    (
        IReadOnlyList<FieldDescriptor> descriptors,
        int expectedLineWidth,
        int totalColumnCount
    )
    {
        Descriptors = descriptors;
        ExpectedLineWidth = expectedLineWidth;
        TotalColumnCount = totalColumnCount;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The ordered, start-position-resolved field descriptors for the type.
    /// </summary>
    internal IReadOnlyList<FieldDescriptor> Descriptors { get; }



    /// <summary>
    /// The sum of all field and skip lengths — the minimum line width required to
    /// read the record, excluding any delimiter contribution.
    /// </summary>
    internal int ExpectedLineWidth { get; }



    /// <summary>
    /// The total number of columns in the record including skipped columns.
    /// Used to calculate the delimiter contribution to the expected line width:
    /// <c>delimiter.Length * (TotalColumnCount - 1)</c>.
    /// </summary>
    internal int TotalColumnCount { get; }
}



/// <summary>
/// Resolves, validates, and caches <see cref="FixedWidthFieldAttribute"/> and
/// <see cref="FixedWidthSkipAttribute"/> metadata for a given type.
/// </summary>
internal static class FieldMap
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    private static readonly Dictionary<Type, FieldMapResult> Cache = new();

    private static readonly object Lock = new();



    // ------------------------------------------------------------------
    // Internal methods
    // ------------------------------------------------------------------

    internal static FieldMapResult GetResult<T>()
        => GetResult(typeof(T));


    private static FieldMapResult GetResult(Type type)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var resolved = Resolve(type);
            Cache[type] = resolved;
            return resolved;
        }
    }



    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    // Represents a single entry in the globally-sorted column list — either
    // a mapped field or a skipped column.
    private readonly struct ColumnEntry
    {
        public ColumnEntry
        (
            PropertyInfo? property,
            FixedWidthFieldAttribute? field,
            int index
        )
        {
            Property = property;
            Field = field;
            Skip = null;
            Index = index;
        }



        public ColumnEntry
        (
            FixedWidthSkipAttribute skip,
            int index
        )
        {
            Property = null;
            Field = null;
            Skip = skip;
            Index = index;
        }



        public PropertyInfo? Property { get; }
        public FixedWidthFieldAttribute? Field { get; }
        public FixedWidthSkipAttribute? Skip { get; }
        public int Index { get; }
        public bool IsSkip => Skip != null;
        public int Length => IsSkip
            ? Skip.Length
            : Field.Length;
    }



    private static FieldMapResult Resolve(Type type)
    {
        // ------------------------------------------------------------------
        // Collect all FixedWidthField and FixedWidthSkip entries from the type.
        // ------------------------------------------------------------------
        var entries = new List<ColumnEntry>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldAttr = prop.GetCustomAttribute<FixedWidthFieldAttribute>();
            if (fieldAttr != null)
            {
                if (!prop.CanWrite)
                {
                    throw new InvalidOperationException($"Property '{prop.Name}' on '{type.FullName}' is decorated with [FixedWidthField] but has no public setter.");
                }

                entries.Add(new ColumnEntry(prop, fieldAttr, fieldAttr.Index));
            }

            foreach (var skipAttr in prop.GetCustomAttributes<FixedWidthSkipAttribute>())
            {
                entries.Add(new ColumnEntry(skipAttr, skipAttr.Index));
            }
        }

        if (entries.Count == 0 || entries.TrueForAll(e => e.IsSkip))
        {
            return new FieldMapResult
            (
                Array.AsReadOnly(Array.Empty<FieldDescriptor>()),
                expectedLineWidth: 0,
                totalColumnCount: 0
            );
        }

        // ------------------------------------------------------------------
        // Validate: no duplicate indexes across fields and skips.
        // ------------------------------------------------------------------
        var seen = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (!seen.Add(entry.Index))
            {
                throw new InvalidOperationException( $"Type '{type.FullName}' has duplicate column Index value {entry.Index}. " + $"Each [FixedWidthField] and [FixedWidthSkip] must have a unique Index.");
            }
        }



        // Sort globally by index.
        entries.Sort((a, b) => a.Index.CompareTo(b.Index));

        // ------------------------------------------------------------------
        // Walk sorted entries, accumulate positions, build descriptors.
        // ------------------------------------------------------------------
        var descriptors = new List<FieldDescriptor>();
        var position = 0;
        var absoluteColIndex = 0;

        foreach (var entry in entries)
        {
            if (!entry.IsSkip)
            {
                descriptors.Add(new FieldDescriptor(entry.Property, entry.Field, position, absoluteColIndex));
            }

            position += entry.Length;
            absoluteColIndex++;
        }

        return new FieldMapResult
        (
            descriptors.AsReadOnly(),
            expectedLineWidth: position,
            totalColumnCount: absoluteColIndex
        );
    }
}
