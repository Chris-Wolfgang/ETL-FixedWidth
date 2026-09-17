using System;
using System.Text;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Configuration for a <see cref="FixedWidthLoader{TRecord}"/> constructed over a
/// <see cref="System.IO.Stream"/>: everything in <see cref="FixedWidthLoaderOptions"/> plus the
/// <see cref="Encoding"/> used to encode the bytes. The <see cref="System.IO.TextWriter"/> constructors
/// take the base record instead, because a writer already owns its encoding and an <c>Encoding</c>
/// there would be settable but inert.
/// </summary>
public sealed record FixedWidthLoaderStreamOptions : FixedWidthLoaderOptions
{
    /// <summary>
    /// The encoding used to encode the stream. Defaults to <see cref="Encoding.UTF8"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Encoding Encoding
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = Encoding.UTF8;



    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthLoaderStreamOptions"/> class.
    /// </summary>
    public FixedWidthLoaderStreamOptions()
    {
    }



    /// <summary>
    /// Copies the formatting settings of <paramref name="formatting"/> and adds the <paramref name="encoding"/> the stream is
    /// written with. Used by the pipeline builder, which accumulates the base record and only learns the output shape
    /// when it materializes.
    /// </summary>
    /// <param name="formatting">The formatting settings to copy.</param>
    /// <param name="encoding">The encoding to write the stream with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="formatting"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
    internal FixedWidthLoaderStreamOptions(FixedWidthLoaderOptions formatting, Encoding encoding)
        : base(formatting ?? throw new ArgumentNullException(nameof(formatting)))
    {
        Encoding = encoding;
    }
}
