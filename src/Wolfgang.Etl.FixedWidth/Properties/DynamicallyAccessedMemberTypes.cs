#if !NET5_0_OR_GREATER

using System.ComponentModel;

// ReSharper disable once CheckNamespace
// The namespace is deliberate: the trimmer and the trim analyzer match these
// attributes by full name, so the polyfills must live in the framework's own
// namespace to be recognised.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Polyfill of <see cref="DynamicallyAccessedMemberTypes"/> for target frameworks
/// that predate it (net462, net481, netstandard2.0). Only the members this
/// library uses are declared; the values match the framework enum.
/// </summary>
[Flags]
[EditorBrowsable(EditorBrowsableState.Never)]
internal enum DynamicallyAccessedMemberTypes
{
    /// <summary>No members.</summary>
    None = 0,

    /// <summary>The public parameterless constructor.</summary>
    PublicParameterlessConstructor = 0x0001,

    /// <summary>All public properties.</summary>
    PublicProperties = 0x0200,
}

#endif
