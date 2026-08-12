using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill enabling <c>init</c>-only setters (used by the positional <c>record</c>
/// model type) on this netstandard2.0 generator project, where the framework does not
/// provide <see cref="IsExternalInit"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class IsExternalInit
{
}
