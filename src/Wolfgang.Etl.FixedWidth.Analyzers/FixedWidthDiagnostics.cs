using Microsoft.CodeAnalysis;

namespace Wolfgang.Etl.FixedWidth.Analyzers;

/// <summary>
/// The <see cref="DiagnosticDescriptor"/>s reported by <see cref="FixedWidthFieldAnalyzer"/>
/// for <c>[FixedWidthField]</c> layout mistakes (#27). The identifiers follow the
/// <c>FW0NN</c> scheme from the issue; rules that assume an explicit-byte-position layout
/// (FW001 overlap, FW002 gap) do not apply to this library's index-based model — start
/// positions are derived by summing preceding lengths, so columns are contiguous by
/// construction and gaps are expressed explicitly with <c>[FixedWidthSkip]</c>.
/// </summary>
internal static class FixedWidthDiagnostics
{
    private const string Category = "Wolfgang.Etl.FixedWidth";
    private const string HelpLinkBase = "https://github.com/Chris-Wolfgang/ETL-FixedWidth";

    /// <summary>FW003 — two columns declare the same <c>Index</c>.</summary>
    internal static readonly DiagnosticDescriptor DuplicateIndex = new
    (
        id: "FW003",
        title: "Duplicate fixed-width column index",
        messageFormat: "Index {0} is declared by more than one [FixedWidthField] / [FixedWidthSkip] on '{1}'; each index must be unique or field mapping throws at runtime",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each [FixedWidthField] and [FixedWidthSkip] on a record type must declare a unique Index. Duplicate indexes cause FieldMap to throw an InvalidOperationException at runtime.",
        helpLinkUri: HelpLinkBase
    );

    /// <summary>FW004 — a date/time field has no <c>Format</c>.</summary>
    internal static readonly DiagnosticDescriptor MissingTemporalFormat = new
    (
        id: "FW004",
        title: "Date/time fixed-width field requires a Format",
        messageFormat: "Property '{0}' of type '{1}' is a [FixedWidthField] with no Format; DateTime, DateTimeOffset and TimeSpan require an explicit Format or both extraction and loading throw at runtime",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "DateTime, DateTimeOffset and TimeSpan have no safe culture-neutral fixed-width representation, so the converter requires an explicit Format. Without one, ParseValue and the writer throw.",
        helpLinkUri: HelpLinkBase
    );

    /// <summary>FW005 — a temporal <c>Format</c> pattern is wider than the field.</summary>
    internal static readonly DiagnosticDescriptor FormatWiderThanField = new
    (
        id: "FW005",
        title: "Format pattern is wider than the fixed-width field",
        messageFormat: "The Format '{0}' ({1} characters) on property '{2}' is wider than the field length {3}; the formatted value overflows the field when writing",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "For a date/time field, the literal Format pattern is the width of the written value. A pattern wider than the declared field length produces a value that overflows the field, throwing FieldOverflowException on write.",
        helpLinkUri: HelpLinkBase
    );

    /// <summary>FW007 — a mapped property has no public setter.</summary>
    internal static readonly DiagnosticDescriptor NoPublicSetter = new
    (
        id: "FW007",
        title: "Fixed-width field property has no public setter",
        messageFormat: "Property '{0}' is marked [FixedWidthField] but has no public setter; extraction throws at runtime because the value cannot be assigned",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FieldMap requires a public setter on every [FixedWidthField] property and throws an InvalidOperationException when one is missing.",
        helpLinkUri: HelpLinkBase
    );

    /// <summary>FW008 — a mapped property has no public getter.</summary>
    internal static readonly DiagnosticDescriptor NoPublicGetter = new
    (
        id: "FW008",
        title: "Fixed-width field property has no public getter",
        messageFormat: "Property '{0}' is marked [FixedWidthField] but has no public getter; loading (writing) throws at runtime because the value cannot be read",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The loader reads each [FixedWidthField] property through a getter. A property with no public getter throws an InvalidOperationException when written.",
        helpLinkUri: HelpLinkBase
    );
}
