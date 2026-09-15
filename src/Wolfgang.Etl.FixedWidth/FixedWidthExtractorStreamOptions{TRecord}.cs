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
}
