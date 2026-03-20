using System;
using System.Linq.Expressions;
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
        Getter = property.GetMethod != null && property.GetMethod.IsPublic
            ? CompileGetter(property)
            : null;
        Setter = CompileSetter(property);
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



    /// <summary>
    /// Compiled delegate that reads the property value from an instance.
    /// Replaces <see cref="PropertyInfo.GetValue(object)"/> to avoid
    /// reflection overhead on every record.
    /// </summary>
    internal Func<object, object?>? Getter { get; }



    /// <summary>
    /// Compiled delegate that writes a value to the property on an instance.
    /// Replaces <see cref="PropertyInfo.SetValue(object, object)"/> to avoid
    /// reflection overhead on every record.
    /// </summary>
    internal Action<object, object?> Setter { get; }



    // ------------------------------------------------------------------
    // Delegate compilation
    // ------------------------------------------------------------------

    /// <summary>
    /// Compiles a getter delegate: (object instance) => (object?)instance.Property
    /// </summary>
    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        // Parameter: object instance
        var instance = Expression.Parameter(typeof(object), "instance");

        // (DeclaringType)instance
        var cast = Expression.Convert(instance, property.DeclaringType!);

        // ((DeclaringType)instance).Property
        var access = Expression.Property(cast, property);

        // Box value types to object
        var boxed = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<object, object?>>(boxed, instance).Compile();
    }



    /// <summary>
    /// Compiles a setter delegate: (object instance, object? value) => instance.Property = (T)value
    /// </summary>
    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        // Parameters: object instance, object? value
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        // (DeclaringType)instance
        var castInstance = Expression.Convert(instance, property.DeclaringType!);

        // (PropertyType)value — handles unboxing for value types
        var castValue = Expression.Convert(value, property.PropertyType);

        // ((DeclaringType)instance).Property = (PropertyType)value
        var assign = Expression.Assign
        (
            Expression.Property(castInstance, property),
            castValue
        );

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }
}
