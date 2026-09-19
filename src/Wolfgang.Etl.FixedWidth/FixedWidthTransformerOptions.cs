using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Configuration for a <see cref="FixedWidthTransformer{TSource, TDestination}"/>, supplied to its constructor.
/// The transformer has no settings of its own beyond the base-stage ones it inherits
/// (<c>ReportingInterval</c>, <c>MaximumItemCount</c>, <c>SkipItemCount</c>, <c>ErrorPolicy</c>); the record
/// exists so the transformer is configured the same way as every other stage (ADR-0009).
/// </summary>
/// <remarks>
/// Every property is <see langword="init"/>-only: settle the configuration before a run rather than
/// mutating it during one. A <see langword="null"/> options argument means "all defaults".
/// </remarks>
[SuppressMessage("Major Code Smell", "S2094:Classes should not be empty", Justification = "ADR-0009: every stage takes its own options record so provider-specific settings can be added without a breaking constructor change; the transformer simply has none yet.")]
public sealed record FixedWidthTransformerOptions : TransformerOptions;
