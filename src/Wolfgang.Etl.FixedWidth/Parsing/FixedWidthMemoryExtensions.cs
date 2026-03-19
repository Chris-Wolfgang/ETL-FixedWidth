using System;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// Extension methods for <see cref="ReadOnlyMemory{T}"/> to support
/// zero-allocation trimming of whitespace.
/// </summary>
internal static class FixedWidthMemoryExtensions
{
    /// <summary>
    /// Returns a <see cref="ReadOnlyMemory{T}"/> with leading and trailing
    /// whitespace removed, without allocating a new string. The returned
    /// memory is a slice of the original.
    /// </summary>
    internal static ReadOnlyMemory<char> TrimMemory(this ReadOnlyMemory<char> memory)
    {
        var span = memory.Span;
        var start = 0;
        while (start < span.Length && char.IsWhiteSpace(span[start]))
        {
            start++;
        }

        var end = span.Length - 1;
        while (end > start && char.IsWhiteSpace(span[end]))
        {
            end--;
        }

        return memory.Slice(start, end - start + 1);
    }
}
