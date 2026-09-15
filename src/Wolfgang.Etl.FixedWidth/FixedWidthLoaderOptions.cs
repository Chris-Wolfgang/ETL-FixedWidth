using System;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Configuration for a <see cref="FixedWidthLoader{TRecord}"/>, supplied to its constructor. Carries
/// every formatting setting that is independent of the output shape; the <see cref="System.IO.Stream"/>
/// constructors take the derived <see cref="FixedWidthLoaderStreamOptions"/>, which adds the
/// byte-encoding <c>Encoding</c> a <see cref="System.IO.TextWriter"/> caller has already decided.
/// </summary>
/// <remarks>
/// Every property is <see langword="init"/>-only: settle the configuration before a run rather than
/// mutating it during one (ADR-0009). The documented defaults live on the property initializers, so
/// no constructor can diverge from them; a <see langword="null"/> options argument means "all defaults".
/// </remarks>
public record FixedWidthLoaderOptions
{
    /// <summary>
    /// Converts a field value to its written text. Defaults to <see cref="FixedWidthConverter.Strict"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<object, FieldContext, string> ValueConverter
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = FixedWidthConverter.Strict;



    /// <summary>
    /// Converts a header label to its written text. Defaults to
    /// <see cref="FixedWidthConverter.StrictHeader"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<string, FieldContext, string> HeaderConverter
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = FixedWidthConverter.StrictHeader;



    /// <summary>
    /// Whether a header line is written before any records. Defaults to <see langword="false"/>.
    /// </summary>
    public bool WriteHeader { get; init; }



    /// <summary>
    /// Whether the load runs as a dry run: the loader enumerates the source, evaluates every record,
    /// counts and reports exactly as a real load would, but writes nothing. Defaults to
    /// <see langword="false"/>.
    /// </summary>
    public bool IsDryRun { get; init; }



    /// <summary>
    /// When non-null, a separator line of this character is written after the header. Defaults to
    /// <see langword="null"/>.
    /// </summary>
    public char? FieldSeparator { get; init; }



    /// <summary>
    /// When non-null, inserted between every adjacent pair of fields. Defaults to
    /// <see langword="null"/>.
    /// </summary>
    public string? FieldDelimiter { get; init; }



    /// <summary>
    /// An explicit schema overriding the attribute-derived layout of the record type. Defaults to
    /// <see langword="null"/>.
    /// </summary>
    public FixedWidthSchema? Schema { get; init; }
}
