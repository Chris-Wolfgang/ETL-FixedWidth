using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

// ------------------------------------------------------------------
// Shared test POCO for contract tests that need value equality
// ------------------------------------------------------------------

/// <summary>
/// A simple record with a single integer field that can round-trip through
/// fixed-width parsing. Available for any tests that need a minimal record type.
/// </summary>
[ExcludeFromCodeCoverage]
public record IntRecord
{
    [FixedWidthField(0, 10, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Value { get; set; }
}
