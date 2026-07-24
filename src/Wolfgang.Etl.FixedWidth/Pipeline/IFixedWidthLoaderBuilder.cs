using System;
using System.Text;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// A fluent builder for a fixed-width loader that terminates a generic <see cref="EtlPipeline"/>.
/// Returned by the <c>FixedWidthLoader</c> sink terminators in
/// <see cref="EtlPipelineFixedWidthExtensions"/>.
/// </summary>
/// <typeparam name="T">The record type consumed by the loader.</typeparam>
/// <remarks>
/// Each configuration method maps one-to-one to a settable property on
/// <see cref="T:Wolfgang.Etl.FixedWidth.FixedWidthLoader`1"/> and returns the same builder so calls
/// chain. Because the builder <em>is</em> an <see cref="IEtlPipelineSink"/>, the loader is materialized
/// and the pipeline runs when
/// <see cref="IEtlPipelineSink.RunAsync(IProgress{EtlPipelineProgress}, System.Threading.CancellationToken)"/>
/// is called.
/// </remarks>
public interface IFixedWidthLoaderBuilder<T> : IEtlPipelineSink
    where T : notnull
{
    /// <summary>
    /// Sets the text encoding used to encode the output. Applies only to the path- and stream-based
    /// terminators; a caller-supplied writer already carries its own encoding. Defaults to
    /// <see cref="System.Text.Encoding.UTF8"/>.
    /// </summary>
    /// <param name="encoding">The encoding to write with.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> Encoding(Encoding encoding);



    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.WriteHeader"/> — whether a header line is written before the
    /// records.
    /// </summary>
    /// <param name="writeHeader"><see langword="true"/> to write a header line.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> WriteHeader(bool writeHeader);


    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.ValueConverter"/> — the delegate that renders a field value to
    /// its string form before padding.
    /// </summary>
    /// <param name="converter">The value converter.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> ValueConverter(Func<object, FieldContext, string> converter);


    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.HeaderConverter"/> — the delegate that renders a header label
    /// to its string form. Only used when <see cref="WriteHeader(bool)"/> is enabled.
    /// </summary>
    /// <param name="converter">The header converter.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> HeaderConverter(Func<string, FieldContext, string> converter);


    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.FieldSeparator"/> — when non-null (and a header is written), a
    /// separator line of the given character is written after the header.
    /// </summary>
    /// <param name="separator">The separator character, or <see langword="null"/> for none.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> FieldSeparator(char? separator);


    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.FieldDelimiter"/> — a string written between adjacent fields on
    /// every line, or <see langword="null"/> for pure fixed-width output.
    /// </summary>
    /// <param name="delimiter">The field delimiter, or <see langword="null"/> for none.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> FieldDelimiter(string? delimiter);


    /// <summary>
    /// Sets <see cref="FixedWidthLoader{T}.IsDryRun"/> — when enabled the pipeline runs and validates but
    /// writes nothing to the output.
    /// </summary>
    /// <param name="isDryRun"><see langword="true"/> to run without writing output.</param>
    /// <returns>The same builder, for chaining.</returns>
    IFixedWidthLoaderBuilder<T> IsDryRun(bool isDryRun);
}
