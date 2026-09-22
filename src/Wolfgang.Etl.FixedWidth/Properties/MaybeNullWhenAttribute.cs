#if !NET5_0_OR_GREATER

using System.ComponentModel;

// ReSharper disable once CheckNamespace
// The namespace is deliberate: the polyfill must live in the framework's own
// namespace for the compiler to recognise it.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Polyfill of <see cref="MaybeNullWhenAttribute"/> for target frameworks that
/// predate it (net462, net481, netstandard2.0).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaybeNullWhenAttribute"/> class.
    /// </summary>
    /// <param name="returnValue">
    /// The return value condition. If the method returns this value, the associated
    /// parameter may be <see langword="null"/>.
    /// </param>
    public MaybeNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }



    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}

#endif
