using System.Text;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Options that control <see cref="FixedWidthLoader{TRecord}"/> behaviour.
/// </summary>
/// <remarks>
/// Supplied as the second constructor parameter, ahead of the optional logger. When the whole
/// options object is <see langword="null"/>, or an individual property is left unset, the
/// documented defaults below apply — defaults live on the property initializers here rather than
/// in constructor bodies, so no constructor can accidentally diverge from them.
/// </remarks>
public sealed record FixedWidthLoaderOptions
{
    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> used to encode the output stream.
    /// Defaults to <see langword="null"/>, meaning <see cref="System.Text.Encoding.UTF8"/>.
    /// </summary>
    /// <remarks>
    /// Applies only to the <see cref="System.IO.Stream"/>-based constructors. The
    /// <see cref="System.IO.TextWriter"/>-based constructors already carry their own encoding,
    /// so this value is not consulted on that path.
    /// </remarks>
    public Encoding? Encoding { get; init; }
}
