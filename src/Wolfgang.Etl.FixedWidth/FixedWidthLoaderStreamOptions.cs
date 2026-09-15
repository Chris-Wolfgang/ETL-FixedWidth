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
}
