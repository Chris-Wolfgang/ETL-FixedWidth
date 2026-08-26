using System.Text;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Options for the <see cref="System.IO.Stream"/>-based <see cref="FixedWidthDataReader{TRecord}"/> constructor.
/// </summary>
/// <remarks>
/// Supplied as the second constructor parameter, ahead of the optional logger. When the whole
/// options object is <see langword="null"/>, or an individual property is left unset, the
/// documented defaults below apply — defaults live on the property initializers here rather than
/// in constructor bodies, so no constructor can accidentally diverge from them.
/// <para>
/// Options are scoped to the <em>input shape</em> they configure, not to the type as a whole, so
/// every property here is meaningful for the constructor it is passed to. This is why there is no
/// counterpart record for the <see cref="System.IO.TextReader"/> constructor: a caller-supplied
/// reader already carries its own encoding, so the setting would be inert on that path.
/// </para>
/// </remarks>
public sealed record FixedWidthDataReaderOptions
{
    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> used to decode the input stream.
    /// Defaults to <see cref="System.Text.Encoding.UTF8"/>.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;
}
