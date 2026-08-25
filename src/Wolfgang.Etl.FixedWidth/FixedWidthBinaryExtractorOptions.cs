using System.Text;

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
/// every property here is meaningful for the constructor it is passed to.
/// </para>
/// </remarks>
public sealed record FixedWidthBinaryExtractorOptions
{
    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> used to decode text fields out of each binary record.
    /// Defaults to <see cref="System.Text.Encoding.ASCII"/>.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.ASCII;
}
