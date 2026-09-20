using System;
using System.Text;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Options for the <see cref="System.IO.Stream"/>-based <see cref="FixedWidthBinaryExtractor{TRecord}"/> constructors.
/// </summary>
/// <remarks>
/// Supplied as the second constructor parameter, ahead of the optional logger. When the whole
/// options object is <see langword="null"/>, or an individual property is left unset, the
/// documented defaults below apply — defaults live on the property initializers here rather than
/// in constructor bodies, so no constructor can accidentally diverge from them.
/// <para>
/// Options are scoped to the <em>input shape</em> they configure, not to the type as a whole, so
/// every property here is meaningful for the constructor it is passed to. The base-stage
/// configuration (<c>ReportingInterval</c>, <c>MaximumItemCount</c>, <c>SkipItemCount</c>, <c>ErrorPolicy</c>)
/// is inherited from the Abstractions record (ADR-0009).
/// </para>
/// </remarks>
public sealed record FixedWidthBinaryExtractorOptions
{
    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> used to decode text fields out of each binary record.
    /// Defaults to <see cref="System.Text.Encoding.ASCII"/>.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.ASCII;



    /// <summary>
    /// Gets the number of items between progress reports. Defaults to <c>1000</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than <c>1</c>.</exception>
    public int ReportingInterval
    {
        get;
        init
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "ReportingInterval must be 1 or greater.");
            }

            field = value;
        }
    } = 1_000;



    /// <summary>
    /// Gets the maximum number of items the stage extracts. Defaults to <see cref="int.MaxValue"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than <c>1</c>.</exception>
    public int MaximumItemCount
    {
        get;
        init
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MaximumItemCount must be 1 or greater.");
            }

            field = value;
        }
    } = int.MaxValue;



    /// <summary>
    /// Gets the number of items the stage skips before the first is yielded. Defaults to <c>0</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int SkipItemCount
    {
        get;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "SkipItemCount cannot be negative.");
            }

            field = value;
        }
    }



    // The base record owns the default policy (an internal type there); take it from a base instance.
    private static readonly Func<ItemErrorContext, ItemErrorAction> DefaultErrorPolicy = new ExtractorOptions().ErrorPolicy;



    /// <summary>
    /// Gets the per-item error policy. Defaults to the abort policy the base stages use.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<ItemErrorContext, ItemErrorAction> ErrorPolicy
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = DefaultErrorPolicy;



    /// <summary>
    /// Projects the shared stage settings onto the base record the constructor hands to
    /// <see cref="ExtractorBase{TSource, TProgress}"/>. The record does not <i>derive</i> from
    /// <see cref="ExtractorOptions"/> on purpose: on net462 / net481 / netstandard2.0 a derived record's
    /// synthesized <c>&lt;Clone&gt;$</c> would return the base type, which breaks callers compiled
    /// against 0.12.0 that use a <c>with</c> expression (ApiCompat CP0002). Deriving is scheduled
    /// for the 2026-12-15 wave (#373).
    /// </summary>
    internal ExtractorOptions ToExtractorOptions() =>
        new()
        {
            ReportingInterval = ReportingInterval,
            MaximumItemCount = MaximumItemCount,
            SkipItemCount = SkipItemCount,
            ErrorPolicy = ErrorPolicy,
        };
}
