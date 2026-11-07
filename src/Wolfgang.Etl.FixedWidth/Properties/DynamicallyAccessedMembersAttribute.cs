#if !NET5_0_OR_GREATER

using System.ComponentModel;

// ReSharper disable once CheckNamespace
// The namespace is deliberate: the trimmer and the trim analyzer match these
// attributes by full name, so the polyfills must live in the framework's own
// namespace to be recognised.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Polyfill of <see cref="DynamicallyAccessedMembersAttribute"/> for target
/// frameworks that predate it (net462, net481, netstandard2.0). It has no effect
/// there; it lets the generic parameters carry the same annotation on every TFM.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[AttributeUsage
(
    AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false
)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class.
    /// </summary>
    /// <param name="memberTypes">The members that are accessed dynamically.</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }



    /// <summary>Gets the members that are accessed dynamically.</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#endif
