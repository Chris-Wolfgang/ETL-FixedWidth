using System;
using System.Collections.Generic;
using System.Reflection;
using Wolfgang.Etl.FixedWidth.Attributes;

namespace Wolfgang.Etl.FixedWidth.Parsing;

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
#pragma warning disable S3898 // value type only used for internal sorting — equality is never needed
    private readonly struct ColumnEntry
#pragma warning restore S3898
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
            ? Skip!.Length
            : Field!.Length;
    }



    private static FieldMapResult Resolve(Type type)
    {
        var entries = CollectEntries(type);

        if (entries.Count == 0 || entries.TrueForAll(e => e.IsSkip))
        {
            return new FieldMapResult
            (
                Array.AsReadOnly(Array.Empty<FieldDescriptor>()),
                expectedLineWidth: 0,
                totalColumnCount: 0
            );
        }

        ValidateNoDuplicateIndexes(type, entries);

        entries.Sort((a, b) => a.Index.CompareTo(b.Index));

        return BuildResult(entries);
    }



    /// <summary>
    /// Collects all <see cref="FixedWidthFieldAttribute"/> and
    /// <see cref="FixedWidthSkipAttribute"/> entries from the type's public properties.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a property decorated with <see cref="FixedWidthFieldAttribute"/>
    /// has no public setter.
    /// </exception>
    private static List<ColumnEntry> CollectEntries(Type type)
    {
        var entries = new List<ColumnEntry>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldAttr = prop.GetCustomAttribute<FixedWidthFieldAttribute>();
            if (fieldAttr != null)
            {
                if (!prop.CanWrite)
                {
                    throw new InvalidOperationException(
                        $"Property '{prop.Name}' on '{type.FullName}' is decorated with " +
                        "[FixedWidthField] but has no public setter.");
                }

                entries.Add(new ColumnEntry(prop, fieldAttr, fieldAttr.Index));
            }

            foreach (var skipAttr in prop.GetCustomAttributes<FixedWidthSkipAttribute>())
            {
                entries.Add(new ColumnEntry(skipAttr, skipAttr.Index));
            }
        }

        return entries;
    }



    /// <summary>
    /// Throws if any two entries share the same column index.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two or more entries share the same column index value.
    /// </exception>
    private static void ValidateNoDuplicateIndexes(Type type, List<ColumnEntry> entries)
    {
        var seen = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (!seen.Add(entry.Index))
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' has duplicate column Index value {entry.Index}. " +
                    "Each [FixedWidthField] and [FixedWidthSkip] must have a unique Index.");
            }
        }
    }



    /// <summary>
    /// Walks the sorted entries, accumulates start positions, and builds the
    /// <see cref="FieldMapResult"/>.
    /// </summary>
    private static FieldMapResult BuildResult(List<ColumnEntry> entries)
    {
        var descriptors = new List<FieldDescriptor>();
        var position = 0;
        var absoluteColIndex = 0;

        foreach (var entry in entries)
        {
            if (!entry.IsSkip)
            {
                descriptors.Add(new FieldDescriptor(entry.Property!, entry.Field!, position, absoluteColIndex));
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
