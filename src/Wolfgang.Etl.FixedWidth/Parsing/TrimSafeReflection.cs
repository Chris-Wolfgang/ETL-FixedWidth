using System;
using System.ComponentModel;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// The two reflection calls on a record's <em>property types</em> (as opposed to the
/// record type itself, which every public entry point annotates with
/// <see cref="Diagnostics.TrimmedMembers.Record"/>). A property type cannot carry a
/// <c>[DynamicallyAccessedMembers]</c> annotation, so each call is isolated here with
/// its trimming justification, instead of being repeated at every call site (#358).
/// </summary>
internal static class TrimSafeReflection
{
    /// <summary>
    /// Returns the <see cref="TypeConverter"/> for a field's property type.
    /// </summary>
    /// <remarks>
    /// Used only as the fallback parser for types the built-in span parsers do not
    /// handle (see <c>FixedWidthConverter.ParseValue</c>): enums and consumer types
    /// with a <c>[TypeConverter]</c>. <see cref="Nullable{T}"/> is unwrapped before the
    /// fallback runs, so the generic <c>NullableConverter</c> case that the framework's
    /// <c>RequiresUnreferencedCode</c> warns about is not reached through this path.
    /// The framework keeps its intrinsic converters (primitives, enums, Guid, ...);
    /// a consumer type whose converter is only reachable through its
    /// <c>[TypeConverter]</c> attribute must keep that converter type under trimming.
    /// That is the one documented trimming caveat of the attribute-based mapper.
    /// </remarks>
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Fallback converter lookup for property types; Nullable<T> is unwrapped first and the intrinsic converters are rooted by TypeDescriptor. Custom [TypeConverter] types are the documented consumer caveat (#358).")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Same lookup: the property type cannot be annotated; see the IL2026 justification (#358).")]
#endif
    internal static TypeConverter GetConverter(Type propertyType)
        => TypeDescriptor.GetConverter(propertyType);



    /// <summary>
    /// Creates the boxed default value of a value type (<c>default(T)</c> boxed), used
    /// for empty fields of non-nullable value-type properties.
    /// </summary>
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Only called for value types; a struct without an explicit parameterless constructor (every built-in value type) needs no constructor metadata to create its default instance (#358).")]
#endif
    internal static object CreateValueTypeDefault(Type valueType)
        => Activator.CreateInstance(valueType)!;
}
