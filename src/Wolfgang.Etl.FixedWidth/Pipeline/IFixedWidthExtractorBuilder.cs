using System;
using System.Text;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A fluent builder for a fixed-width extraction stage of a generic <see cref="EtlPipeline"/>. Returned
/// by the <c>FixedWidthExtractor</c> source factories in <see cref="EtlPipelineFixedWidthExtensions"/>.
/// </summary>
/// <typeparam name="T">The record type produced by the extractor.</typeparam>
/// <remarks>
/// Each configuration method maps one-to-one to a settable property on
/// <see cref="T:Wolfgang.Etl.FixedWidth.FixedWidthExtractor`1"/> and returns the same builder so calls
/// chain. Configuration is recorded and applied when the first pipeline operation — a <c>Through</c>
/// stage, a sink terminator such as <c>FixedWidthLoader</c>, or
/// <see cref="IEtlPipeline{T}.AsAsyncEnumerable"/> — materializes the extractor; calling a setter after
/// that throws <see cref="InvalidOperationException"/>.
/// </remarks>
public interface IFixedWidthExtractorBuilder<T> : IEtlPipeline<T>
    where T : notnull, new()
{
    /// <summary>
    /// Sets the text encoding used to decode the source. Applies only to the path- and stream-based
    /// factories; a caller-supplied reader or extractor already carries its own encoding. Defaults to
    /// <see cref="System.Text.Encoding.UTF8"/>.
    /// </summary>
    /// <param name="encoding">The encoding to decode with.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> Encoding(Encoding encoding);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.HeaderLineCount"/> — the number of header lines to skip
    /// before the first record.
    /// </summary>
    /// <param name="count">The number of leading lines to skip.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> HeaderLineCount(int count);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.HasHeader"/> — the single-header-line convenience over
    /// <see cref="HeaderLineCount(int)"/>.
    /// </summary>
    /// <param name="hasHeader"><see langword="true"/> to skip one header line; otherwise none.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> HasHeader(bool hasHeader);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.MalformedLineHandling"/> — how a line that is too short or
    /// cannot be converted is handled.
    /// </summary>
    /// <param name="handling">The malformed-line policy.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> MalformedLineHandling(MalformedLineHandling handling);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.BlankLineHandling"/> — how a zero-length line is handled.
    /// </summary>
    /// <param name="handling">The blank-line policy.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> BlankLineHandling(BlankLineHandling handling);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.LineFilter"/> — a per-line predicate evaluated before
    /// parsing that can process, skip, or stop.
    /// </summary>
    /// <param name="filter">The line filter.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> LineFilter(Func<string, LineAction> filter);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.RecordValidator"/> — a per-record callback that can accept,
    /// skip, or stop after parsing.
    /// </summary>
    /// <param name="validator">The record validator.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> RecordValidator(Func<T, ValidationResult> validator);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.ValueParser"/> — the delegate that converts a raw field
    /// string into the target property type.
    /// </summary>
    /// <param name="parser">The value parser.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> ValueParser(FixedWidthValueParser parser);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.FieldSeparator"/> — when non-null, the line after the
    /// header is treated as a separator and skipped.
    /// </summary>
    /// <param name="separator">The separator character, or <see langword="null"/> for none.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> FieldSeparator(char? separator);


    /// <summary>
    /// Sets <see cref="FixedWidthExtractor{T}.FieldDelimiter"/> — the delimiter present between fields in
    /// the source, or <see langword="null"/> for pure fixed-width input.
    /// </summary>
    /// <param name="delimiter">The field delimiter, or <see langword="null"/> for none.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthExtractorBuilder<T> FieldDelimiter(string? delimiter);
}
