#if !NET5_0_OR_GREATER

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for <c>System.Runtime.CompilerServices.IsExternalInit</c> on target
/// frameworks that pre-date .NET 5. Required to enable C# 9 <c>init</c>-only setters
/// on the targets that pre-date .NET 5 (net462 / net481 / netstandard2.0).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
internal static class IsExternalInit
{
}

#endif
