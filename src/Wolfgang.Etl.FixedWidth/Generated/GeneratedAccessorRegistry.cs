using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Wolfgang.Etl.FixedWidth.Generated;

/// <summary>
/// Runtime registry populated by the <c>Wolfgang.Etl.FixedWidth.Analyzers</c> source
/// generator. For every type with <see cref="Attributes.FixedWidthFieldAttribute"/>
/// properties, the generator emits a factory delegate plus per-property getter and
/// setter delegates and registers them here from a module initializer
/// (<c>[ModuleInitializer]</c>).
/// </summary>
/// <remarks>
/// <para>
/// The runtime field-mapping path (<see cref="Parsing.FieldMap"/>) consults this
/// registry first and only falls back to reflection-compiled
/// <see cref="System.Linq.Expressions.Expression"/> delegates when no generated entry
/// exists. The generated delegates use direct property access — no reflection, no
/// expression compilation — which removes the last <c>RequiresDynamicCode</c> code path
/// and makes extraction and loading Native AOT compatible (see #12, #13).
/// </para>
/// <para>
/// This type is <see langword="public"/> because the generated code is emitted into the
/// consumer's own assembly (which has no <c>InternalsVisibleTo</c> access to this
/// library) and must be able to call the <c>Register*</c> methods. It is hidden from
/// IntelliSense via <see cref="EditorBrowsableAttribute"/>; it is not intended for direct
/// use by application code.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedAccessorRegistry
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();

    private static readonly ConcurrentDictionary<AccessorKey, Func<object, object?>> Getters = new();

    private static readonly ConcurrentDictionary<AccessorKey, Action<object, object?>> Setters = new();



    /// <summary>
    /// Composite (declaring type, property name) key. A hand-rolled value type is used
    /// instead of a <see cref="ValueType"/> tuple because <c>System.ValueTuple</c> is not
    /// available in-box on net462/net47/net471.
    /// </summary>
    private readonly struct AccessorKey : IEquatable<AccessorKey>
    {
        private readonly Type _declaringType;
        private readonly string _propertyName;

        internal AccessorKey(Type declaringType, string propertyName)
        {
            _declaringType = declaringType;
            _propertyName = propertyName;
        }

        public bool Equals(AccessorKey other)
            => _declaringType == other._declaringType
                && string.Equals(_propertyName, other._propertyName, StringComparison.Ordinal);

        public override bool Equals(object? obj)
            => obj is AccessorKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (_declaringType.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(_propertyName);
            }
        }
    }



    // ------------------------------------------------------------------
    // Registration (called by generated code)
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers a factory delegate that creates a new instance of
    /// <paramref name="type"/>. Called by generated module initializers.
    /// </summary>
    /// <param name="type">The record type the factory creates.</param>
    /// <param name="factory">A delegate returning a new boxed instance of <paramref name="type"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> or <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    public static void RegisterFactory(Type type, Func<object> factory)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Factories[type] = factory;
    }



    /// <summary>
    /// Registers a getter delegate that reads property <paramref name="propertyName"/>
    /// from an instance of <paramref name="declaringType"/>. Called by generated
    /// module initializers.
    /// </summary>
    /// <param name="declaringType">The type that declares the property.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="getter">A delegate reading the (boxed) property value from a boxed instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is <see langword="null"/>.
    /// </exception>
    public static void RegisterGetter(Type declaringType, string propertyName, Func<object, object?> getter)
    {
        if (declaringType == null)
        {
            throw new ArgumentNullException(nameof(declaringType));
        }
        if (propertyName == null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }
        if (getter == null)
        {
            throw new ArgumentNullException(nameof(getter));
        }

        Getters[new AccessorKey(declaringType, propertyName)] = getter;
    }



    /// <summary>
    /// Registers a setter delegate that writes property <paramref name="propertyName"/>
    /// on an instance of <paramref name="declaringType"/>. Called by generated
    /// module initializers.
    /// </summary>
    /// <param name="declaringType">The type that declares the property.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="setter">A delegate writing a (boxed) value to the property on a boxed instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is <see langword="null"/>.
    /// </exception>
    public static void RegisterSetter(Type declaringType, string propertyName, Action<object, object?> setter)
    {
        if (declaringType == null)
        {
            throw new ArgumentNullException(nameof(declaringType));
        }
        if (propertyName == null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }
        if (setter == null)
        {
            throw new ArgumentNullException(nameof(setter));
        }

        Setters[new AccessorKey(declaringType, propertyName)] = setter;
    }



    // ------------------------------------------------------------------
    // Lookup (called by the runtime field-mapping path)
    // ------------------------------------------------------------------

    internal static bool TryGetFactory(Type type, out Func<object> factory)
        => Factories.TryGetValue(type, out factory!);



    internal static bool TryGetGetter(Type declaringType, string propertyName, out Func<object, object?> getter)
        => Getters.TryGetValue(new AccessorKey(declaringType, propertyName), out getter!);



    internal static bool TryGetSetter(Type declaringType, string propertyName, out Action<object, object?> setter)
        => Setters.TryGetValue(new AccessorKey(declaringType, propertyName), out setter!);
}
