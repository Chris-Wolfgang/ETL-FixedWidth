using System;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Configuration for a <see cref="FixedWidthExtractor{TRecord}"/>, supplied to its constructor.
/// Carries every parsing setting that is independent of the input shape; the
/// <see cref="System.IO.Stream"/> constructors take the derived <see cref="FixedWidthExtractorStreamOptions{TRecord}"/>,
/// which adds the byte-decoding <c>Encoding</c> a <see cref="System.IO.TextReader"/> caller has
/// already decided.
/// </summary>
/// <remarks>
/// Every property is <see langword="init"/>-only: settle the configuration before a run rather than
/// mutating it during one (ADR-0009). The documented defaults live on the property initializers, so
/// no constructor can diverge from them; a <see langword="null"/> options argument means "all defaults".
/// Members that reject a value do so from the <see langword="init"/> accessor, so an invalid record
/// cannot be constructed.
/// </remarks>
/// <typeparam name="TRecord">The record type the extractor produces.</typeparam>
public record FixedWidthExtractorOptions<TRecord> : ExtractorOptions
    where TRecord : notnull
{
    /// <summary>
    /// How a line whose length does not match the schema is handled. Defaults to
    /// <see cref="MalformedLineHandling.ThrowException"/>.
    /// </summary>
    public MalformedLineHandling MalformedLineHandling { get; init; } = MalformedLineHandling.ThrowException;



    /// <summary>
    /// How a blank line is handled. Defaults to <see cref="BlankLineHandling.ThrowException"/>.
    /// </summary>
    public BlankLineHandling BlankLineHandling { get; init; } = BlankLineHandling.ThrowException;



    /// <summary>
    /// Decides, per line, whether it is processed, skipped, or ends extraction. Defaults to a
    /// filter that processes every line.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<string, LineAction> LineFilter
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = static _ => LineAction.Process;



    /// <summary>
    /// Optional validation applied to each parsed record before it is yielded. Defaults to
    /// <see langword="null"/> (no validation).
    /// </summary>
    public Func<TRecord, ValidationResult>? RecordValidator { get; init; }



    /// <summary>
    /// Optional callback invoked for each recoverable error. Defaults to <see langword="null"/>.
    /// </summary>
    public Action<FixedWidthError>? OnError { get; init; }



    /// <summary>
    /// Converts a field's text to the property's CLR type. Defaults to
    /// <see cref="FixedWidthConverter.DefaultParser"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public FixedWidthValueParser ValueParser
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = FixedWidthConverter.DefaultParser;



    /// <summary>
    /// The number of header lines skipped before the first record. Defaults to <c>0</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int HeaderLineCount
    {
        get;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "HeaderLineCount cannot be negative.");
            }

            field = value;
        }
    }



    /// <summary>
    /// When non-null, the line after the last header line is treated as a separator and skipped.
    /// Only its presence matters, not its value. Defaults to <see langword="null"/>.
    /// </summary>
    public char? FieldSeparator { get; init; }



    /// <summary>
    /// The delimiter present between fields in the source, accounted for when computing field
    /// positions. Defaults to <see langword="null"/> (no delimiter).
    /// </summary>
    public string? FieldDelimiter { get; init; }



    /// <summary>
    /// An explicit schema overriding the attribute-derived layout of <typeparamref name="TRecord"/>.
    /// Defaults to <see langword="null"/>.
    /// </summary>
    public FixedWidthSchema? Schema { get; init; }



    /// <summary>
    /// Whether byte offsets are tracked so a run can be checkpointed and resumed. Requires a
    /// seekable <see cref="System.IO.Stream"/> source. Defaults to <see langword="false"/>.
    /// </summary>
    public bool TrackByteOffset { get; init; }



    /// <summary>
    /// The byte offset to seek to before extraction begins — a checkpoint from a prior run.
    /// Defaults to <c>0</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public long StartByteOffset
    {
        get;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "StartByteOffset cannot be negative.");
            }

            field = value;
        }
    }
}
