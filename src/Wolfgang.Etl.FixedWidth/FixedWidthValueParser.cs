using System;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Converts a raw field value read from the file into the target property type.
/// The <paramref name="text"/> is a zero-copy slice of the source line — call
/// <see cref="ReadOnlyMemory{T}.ToString()"/> only if you need a <see cref="string"/>.
/// </summary>
/// <param name="text">
/// The raw field value as a <see cref="ReadOnlyMemory{T}"/> slice of the source line.
/// Access the characters via <c>text.Span</c> for zero-allocation processing.
/// </param>
/// <param name="context">
/// Metadata about the field being parsed, including property type, format string,
/// and field length.
/// </param>
/// <returns>
/// A value assignable to the target property's CLR type.
/// </returns>
public delegate object FixedWidthValueParser(ReadOnlyMemory<char> text, FieldContext context);
