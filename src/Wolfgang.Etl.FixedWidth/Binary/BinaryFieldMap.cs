using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Wolfgang.Etl.FixedWidth.Attributes;

namespace Wolfgang.Etl.FixedWidth.Binary;

/// <summary>
/// Builds the <see cref="BinaryRecordMap"/> for a record type from its
/// <see cref="FixedWidthBinaryFieldAttribute"/>-decorated properties.
/// </summary>
internal static class BinaryFieldMap
{
    internal static BinaryRecordMap GetResult<T>() => GetResult(typeof(T));

    internal static BinaryRecordMap GetResult(Type type)
    {
        // Anonymous type (not a ValueTuple) — net462/netstandard2.0 have no System.ValueTuple.
        var entries = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new
            {
                Property = p,
                Attribute = (FixedWidthBinaryFieldAttribute?)Attribute.GetCustomAttribute(p, typeof(FixedWidthBinaryFieldAttribute)),
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute!.Index)
            .ToList();

        if (entries.Count == 0)
        {
            throw new InvalidOperationException($"Type '{type.FullName}' has no [FixedWidthBinaryField] properties.");
        }

        var seen = new HashSet<int>();
        var descriptors = new List<BinaryFieldDescriptor>(entries.Count);
        var offset = 0;

        foreach (var entry in entries)
        {
            var attribute = entry.Attribute!;
            if (!seen.Add(attribute.Index))
            {
                throw new InvalidOperationException($"Duplicate [FixedWidthBinaryField] index {attribute.Index} on type '{type.FullName}'.");
            }

            descriptors.Add(new BinaryFieldDescriptor(entry.Property, attribute, offset, CompileSetter(entry.Property)));
            offset += attribute.ByteLength;
        }

        return new BinaryRecordMap(descriptors.AsReadOnly(), offset, CompileFactory(type));
    }


#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Expression.Compile is RequiresDynamicCode but falls back to the interpreter under Native AOT, so the compiled setter still runs correctly (without JIT speed). See #153.")]
#endif
    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign
        (
            Expression.Property(Expression.Convert(instance, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType)
        );

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }


#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Expression.Compile is RequiresDynamicCode but falls back to the interpreter under Native AOT, so the compiled factory still runs correctly (without JIT speed). See #153.")]
#endif
    private static Func<object> CompileFactory(Type type)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Type '{type.FullName}' needs a public parameterless constructor for binary extraction.");

        return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(ctor), typeof(object))).Compile();
    }
}
