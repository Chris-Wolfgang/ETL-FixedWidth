using System;
using System.IO;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Class-named fixed-width source factories and sink terminators for the fluent <see cref="EtlPipeline"/>
/// chain (issue #253). Source factories hang off <see cref="EtlPipeline"/> so a pipeline reads
/// <c>EtlPipeline.Create().FixedWidthExtractor&lt;Person&gt;("people.txt")</c>; sink terminators hang off
/// <see cref="IEtlPipeline{T}"/> so it ends
/// <c>… .FixedWidthLoader&lt;Person&gt;("out.txt").RunAsync()</c>.
/// </summary>
/// <remarks>
/// Path-based factories own the file stream they open and dispose it after the run (success or
/// failure). Factories that accept a caller-supplied <see cref="Stream"/>, <see cref="TextReader"/>,
/// <see cref="TextWriter"/>, or a pre-built extractor do not dispose it — the caller owns the lifecycle
/// and, for a writer, is responsible for flushing it. The fluent setters returned by these factories map
/// 1:1 to the underlying <c>FixedWidthExtractor&lt;T&gt;</c> / <c>FixedWidthLoader&lt;T&gt;</c>
/// properties; encoding is set via the builder's <c>Encoding</c> method.
/// </remarks>
public static class EtlPipelineFixedWidthExtensions
{
    // ------------------------------------------------------------------
    // Source factories (on EtlPipeline)
    // ------------------------------------------------------------------

    /// <summary>
    /// Begins a pipeline that reads fixed-width records from the file at <paramref name="path"/>. The
    /// opened file reader is owned by the pipeline and disposed when the run finishes.
    /// </summary>
    /// <typeparam name="T">The record type to produce.</typeparam>
    /// <param name="pipeline">The seed returned by <see cref="EtlPipeline.Create"/>.</param>
    /// <param name="path">The path of the fixed-width file to read.</param>
    /// <returns>A fluent <see cref="IFixedWidthExtractorBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public static IFixedWidthExtractorBuilder<T> FixedWidthExtractor<T>(this EtlPipeline pipeline, string path)
        where T : notnull, new()
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return FixedWidthExtractorBuilder<T>.FromPath(path);
    }


    /// <summary>
    /// Begins a pipeline that reads fixed-width records from <paramref name="stream"/>. The caller
    /// retains ownership of the stream; only the extractor's internal reader is disposed.
    /// </summary>
    /// <typeparam name="T">The record type to produce.</typeparam>
    /// <param name="pipeline">The seed returned by <see cref="EtlPipeline.Create"/>.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A fluent <see cref="IFixedWidthExtractorBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static IFixedWidthExtractorBuilder<T> FixedWidthExtractor<T>(this EtlPipeline pipeline, Stream stream)
        where T : notnull, new()
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        return FixedWidthExtractorBuilder<T>.FromStream(stream);
    }


    /// <summary>
    /// Begins a pipeline that reads fixed-width records from <paramref name="reader"/>. The caller
    /// retains ownership of the reader; nothing is disposed by the pipeline.
    /// </summary>
    /// <typeparam name="T">The record type to produce.</typeparam>
    /// <param name="pipeline">The seed returned by <see cref="EtlPipeline.Create"/>.</param>
    /// <param name="reader">The reader to read from.</param>
    /// <returns>A fluent <see cref="IFixedWidthExtractorBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="reader"/> is <see langword="null"/>.</exception>
    public static IFixedWidthExtractorBuilder<T> FixedWidthExtractor<T>(this EtlPipeline pipeline, TextReader reader)
        where T : notnull, new()
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return FixedWidthExtractorBuilder<T>.FromReader(reader);
    }


    /// <summary>
    /// Begins a pipeline from an already-configured <see cref="T:Wolfgang.Etl.FixedWidth.FixedWidthExtractor`1"/>.
    /// The caller retains ownership of the instance; nothing is disposed by the pipeline. The returned
    /// builder can still layer further configuration over the supplied extractor.
    /// </summary>
    /// <typeparam name="T">The record type to produce.</typeparam>
    /// <param name="pipeline">The seed returned by <see cref="EtlPipeline.Create"/>.</param>
    /// <param name="extractor">The extractor that seeds the pipeline.</param>
    /// <returns>A fluent <see cref="IFixedWidthExtractorBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="extractor"/> is <see langword="null"/>.</exception>
    public static IFixedWidthExtractorBuilder<T> FixedWidthExtractor<T>(this EtlPipeline pipeline, FixedWidthExtractor<T> extractor)
        where T : notnull, new()
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (extractor is null)
        {
            throw new ArgumentNullException(nameof(extractor));
        }

        return FixedWidthExtractorBuilder<T>.FromExtractor(extractor);
    }


    // ------------------------------------------------------------------
    // Sink terminators (on IEtlPipeline<T>)
    // ------------------------------------------------------------------

    /// <summary>
    /// Terminates the pipeline by writing fixed-width records to the file at <paramref name="path"/>.
    /// The opened file writer is owned by the pipeline and disposed after the run, flushing to disk.
    /// </summary>
    /// <typeparam name="T">The record type to write.</typeparam>
    /// <param name="pipeline">The pipeline to terminate.</param>
    /// <param name="path">The path of the fixed-width file to write. An existing file is overwritten.</param>
    /// <returns>A fluent <see cref="IFixedWidthLoaderBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public static IFixedWidthLoaderBuilder<T> FixedWidthLoader<T>(this IEtlPipeline<T> pipeline, string path)
        where T : notnull
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return FixedWidthLoaderBuilder<T>.FromPath(pipeline, path);
    }


    /// <summary>
    /// Terminates the pipeline by writing fixed-width records to <paramref name="stream"/>. The caller
    /// retains ownership of the stream; only the loader's internal writer is disposed.
    /// </summary>
    /// <typeparam name="T">The record type to write.</typeparam>
    /// <param name="pipeline">The pipeline to terminate.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <returns>A fluent <see cref="IFixedWidthLoaderBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static IFixedWidthLoaderBuilder<T> FixedWidthLoader<T>(this IEtlPipeline<T> pipeline, Stream stream)
        where T : notnull
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        return FixedWidthLoaderBuilder<T>.FromStream(pipeline, stream);
    }


    /// <summary>
    /// Terminates the pipeline by writing fixed-width records to <paramref name="writer"/>. The caller
    /// retains ownership of the writer and is responsible for flushing it; nothing is disposed by the
    /// pipeline.
    /// </summary>
    /// <typeparam name="T">The record type to write.</typeparam>
    /// <param name="pipeline">The pipeline to terminate.</param>
    /// <param name="writer">The writer to write to.</param>
    /// <returns>A fluent <see cref="IFixedWidthLoaderBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeline"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    public static IFixedWidthLoaderBuilder<T> FixedWidthLoader<T>(this IEtlPipeline<T> pipeline, TextWriter writer)
        where T : notnull
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        return FixedWidthLoaderBuilder<T>.FromWriter(pipeline, writer);
    }
}
