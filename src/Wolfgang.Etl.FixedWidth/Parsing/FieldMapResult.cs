using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Wolfgang.Etl.FixedWidth.Parsing;

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
        int totalColumnCount,
        Func<object> factory
    )
    {
        Descriptors = descriptors;
        ExpectedLineWidth = expectedLineWidth;
        TotalColumnCount = totalColumnCount;
        Factory = factory;
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



    /// <summary>
    /// Compiled factory delegate that creates a new instance of the record type
    /// via <see cref="Expression.New(Type)"/>, avoiding the overhead of
    /// <see cref="Activator.CreateInstance{T}"/> on every record.
    /// </summary>
    internal Func<object> Factory { get; }



    // ------------------------------------------------------------------
    // Factory compilation
    // ------------------------------------------------------------------

    /// <summary>
    /// Compiles a factory delegate: () => (object)new T().
    /// Returns a throwing delegate if the type has no public parameterless constructor
    /// (e.g. when used by the loader which never needs to instantiate records).
    /// </summary>
    internal static Func<object> CompileFactory(Type type)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null)
        {
            return () => throw new InvalidOperationException
            (
                $"Type '{type.FullName}' has no public parameterless constructor. " +
                "Records must have a public parameterless constructor for extraction."
            );
        }

        var body = Expression.Convert
        (
            Expression.New(ctor),
            typeof(object)
        );
        return Expression.Lambda<Func<object>>(body).Compile();
    }
}
