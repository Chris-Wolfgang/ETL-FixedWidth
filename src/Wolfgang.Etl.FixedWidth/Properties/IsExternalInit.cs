#if !NET5_0_OR_GREATER

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for <c>System.Runtime.CompilerServices.IsExternalInit</c> on target
/// frameworks that pre-date .NET 5. Required to enable C# 9 <c>init</c>-only setters
/// on net462 / netstandard2.0 / netstandard2.1.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
internal static class IsExternalInit
{
}

#endif
