using System;
using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit.TestModels;

/// <summary>
/// A fixed-width record used by the source-generator tests. It is a public, non-generic
/// class with a public parameterless constructor and a spread of field types — reference
/// (<see cref="Name"/>), value (<see cref="Age"/>, <see cref="HireDate"/>) and nullable
/// value (<see cref="Salary"/>) — so the generated factory, getters and setters can be
/// checked against the reflection fallback for each shape. The
/// <c>Wolfgang.Etl.FixedWidth.Analyzers</c> generator, referenced by this test project,
/// emits accessors for this type and registers them from a module initializer.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GeneratedAccessorFixture
{
    [FixedWidthField(0, 10)]
    public string Name { get; set; } = string.Empty;

    [FixedWidthField(1, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }

    [FixedWidthField(2, 8, Format = "yyyyMMdd")]
    public DateTime HireDate { get; set; }

    [FixedWidthField(3, 6)]
    public decimal? Salary { get; set; }
}



/// <summary>
/// A fixed-width record with an <c>init</c>-only mapped property. The generator must NOT
/// emit a setter for it — a plain <c>obj.Prop = value</c> assignment is invalid C# for an
/// init-only property (CS8852) and would break this project's build. Extraction still works
/// through the reflection/Expression setter fallback. The fact that this file compiles is
/// itself the regression guard.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GeneratedAccessorInitOnlyFixture
{
    [FixedWidthField(0, 5)]
    public string Code { get; init; } = string.Empty;
}
