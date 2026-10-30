using System.Diagnostics.CodeAnalysis;

namespace Wolfgang.Etl.FixedWidth.Diagnostics;

/// <summary>
/// The <c>DynamicallyAccessedMemberTypes</c> a record type must keep under
/// trimming / Native AOT for the attribute-based mapper to work (#358).
/// </summary>
/// <remarks>
/// The mapper enumerates the public instance properties of a record type to read
/// its <c>[FixedWidthField]</c> / <c>[FixedWidthSkip]</c> / <c>[FixedWidthBinaryField]</c>
/// attributes and to get / set the values, and calls the public parameterless
/// constructor to create a record on extraction. Every public generic parameter and
/// <see cref="System.Type"/> parameter that reaches the mapper carries this annotation,
/// so the trimmer keeps exactly those members of the consumer's record types.
/// </remarks>
internal static class TrimmedMembers
{
    /// <summary>
    /// Public properties plus the public parameterless constructor.
    /// </summary>
    internal const DynamicallyAccessedMemberTypes Record =
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
}
