using System;
using System.Text;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Configuration for a <see cref="FixedWidthExtractor{TRecord}"/> constructed over a
/// <see cref="System.IO.Stream"/>: everything in <see cref="FixedWidthExtractorOptions{TRecord}"/> plus
/// the <see cref="Encoding"/> used to decode the bytes. The <see cref="System.IO.TextReader"/>
/// constructors take the base record instead, because a reader has already decoded its bytes and an
/// <c>Encoding</c> there would be settable but inert.
/// </summary>
/// <typeparam name="TRecord">The record type the extractor produces.</typeparam>
public sealed record FixedWidthExtractorStreamOptions<TRecord> : FixedWidthExtractorOptions<TRecord>
    where TRecord : notnull
{
    /// <summary>
    /// The encoding used to decode the stream. Defaults to <see cref="Encoding.UTF8"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Encoding Encoding
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = Encoding.UTF8;



    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthExtractorStreamOptions{TRecord}"/> class.
    /// </summary>
    public FixedWidthExtractorStreamOptions()
    {
    }



    /// <summary>
    /// Copies the parsing settings of <paramref name="parsing"/> and adds the <paramref name="encoding"/> the stream is
    /// decoded with. Used by the pipeline builder, which accumulates the base record and only learns the input shape
    /// when it materializes.
    /// </summary>
    /// <param name="parsing">The parsing settings to copy.</param>
    /// <param name="encoding">The encoding to decode the stream with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parsing"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
    internal FixedWidthExtractorStreamOptions(FixedWidthExtractorOptions<TRecord> parsing, Encoding encoding)
        : base(parsing ?? throw new ArgumentNullException(nameof(parsing)))
    {
        Encoding = encoding;
    }
}
