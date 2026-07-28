using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Default <see cref="IFixedWidthExtractorBuilder{T}"/> implementation. Records configuration until the
/// first pipeline operator, then materializes a <see cref="FixedWidthExtractor{T}"/> and delegates to a
/// generic <see cref="EtlPipeline"/> built from it. Any resource the builder itself opened (a file
/// reader for a path source, the extractor's internal reader for a stream source) is disposed once the
/// source is drained — on success, failure, or cancellation.
/// </summary>
/// <typeparam name="T">The record type produced by the extractor.</typeparam>
internal sealed class FixedWidthExtractorBuilder<T> : IFixedWidthExtractorBuilder<T>
    where T : notnull, new()
{
    private readonly string? _path;
    private readonly Stream? _stream;
    private readonly TextReader? _reader;
    private readonly FixedWidthExtractor<T>? _existing;
    private readonly List<Action<FixedWidthExtractor<T>>> _mutations = new();

    private Encoding _encoding = System.Text.Encoding.UTF8;
    private IEtlPipeline<T>? _pipeline;


    private FixedWidthExtractorBuilder
    (
        string? path,
        Stream? stream,
        TextReader? reader,
        FixedWidthExtractor<T>? existing
    )
    {
        _path = path;
        _stream = stream;
        _reader = reader;
        _existing = existing;
    }


    internal static IFixedWidthExtractorBuilder<T> FromPath(string path)
        => new FixedWidthExtractorBuilder<T>(path, stream: null, reader: null, existing: null);


    internal static IFixedWidthExtractorBuilder<T> FromStream(Stream stream)
        => new FixedWidthExtractorBuilder<T>(path: null, stream, reader: null, existing: null);


    internal static IFixedWidthExtractorBuilder<T> FromReader(TextReader reader)
        => new FixedWidthExtractorBuilder<T>(path: null, stream: null, reader, existing: null);


    internal static IFixedWidthExtractorBuilder<T> FromExtractor(FixedWidthExtractor<T> extractor)
        => new FixedWidthExtractorBuilder<T>(path: null, stream: null, reader: null, extractor);


    public IFixedWidthExtractorBuilder<T> Encoding(Encoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        ThrowIfMaterialized();
        _encoding = encoding;
        return this;
    }


    public IFixedWidthExtractorBuilder<T> HeaderLineCount(int count) => Configure(e => e.HeaderLineCount = count);


    public IFixedWidthExtractorBuilder<T> HasHeader(bool hasHeader) => Configure(e => e.HasHeader = hasHeader);


    public IFixedWidthExtractorBuilder<T> MalformedLineHandling(MalformedLineHandling handling) => Configure(e => e.MalformedLineHandling = handling);


    public IFixedWidthExtractorBuilder<T> BlankLineHandling(BlankLineHandling handling) => Configure(e => e.BlankLineHandling = handling);


    public IFixedWidthExtractorBuilder<T> LineFilter(Func<string, LineAction> filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        return Configure(e => e.LineFilter = filter);
    }


    public IFixedWidthExtractorBuilder<T> RecordValidator(Func<T, ValidationResult> validator)
    {
        if (validator is null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        return Configure(e => e.RecordValidator = validator);
    }


    public IFixedWidthExtractorBuilder<T> ValueParser(FixedWidthValueParser parser)
    {
        if (parser is null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return Configure(e => e.ValueParser = parser);
    }


    public IFixedWidthExtractorBuilder<T> FieldSeparator(char? separator) => Configure(e => e.FieldSeparator = separator);


    public IFixedWidthExtractorBuilder<T> FieldDelimiter(string? delimiter) => Configure(e => e.FieldDelimiter = delimiter);


    public IEtlPipeline<TOut> Through<TOut>(ITransformAsync<T, TOut> transformer) where TOut : notnull => Pipeline().Through(transformer);


    public IEtlPipeline<TOut> Through<TOut>(ITransformWithCancellationAsync<T, TOut> transformer) where TOut : notnull => Pipeline().Through(transformer);


    public IEtlPipeline<TOut> Through<TOut>(Func<IAsyncEnumerable<T>, IAsyncEnumerable<TOut>> stage) where TOut : notnull => Pipeline().Through(stage);


    public IEtlPipeline<TOut> Through<TOut>(Func<IAsyncEnumerable<T>, CancellationToken, IAsyncEnumerable<TOut>> stage) where TOut : notnull => Pipeline().Through(stage);


    public IEtlPipelineSink To<TProgress>(LoaderBase<T, TProgress> loader) where TProgress : notnull => Pipeline().To(loader);


    public IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken token = default) => Pipeline().AsAsyncEnumerable(token);


    private IFixedWidthExtractorBuilder<T> Configure(Action<FixedWidthExtractor<T>> mutation)
    {
        ThrowIfMaterialized();
        _mutations.Add(mutation);
        return this;
    }


    private void ThrowIfMaterialized()
    {
        if (_pipeline is not null)
        {
            throw new InvalidOperationException
            (
                "The extractor has already been materialized by a pipeline operator; configuration setters can no longer be applied."
            );
        }
    }


    private IEtlPipeline<T> Pipeline()
    {
        if (_pipeline is null)
        {
            var extractor = BuildExtractor(out var ownedResources);
            _pipeline = EtlPipeline.Create().From(DrainThenDisposeAsync(extractor, ownedResources));
        }

        return _pipeline;
    }


    private FixedWidthExtractor<T> BuildExtractor(out object?[] ownedResources)
    {
        FixedWidthExtractor<T> extractor;

        if (_existing is not null)
        {
            // Caller-supplied instance — the caller owns its lifetime; dispose nothing.
            extractor = _existing;
            ownedResources = Array.Empty<object?>();
        }
        else if (_reader is not null)
        {
            // Caller owns the reader; the extractor's Dispose is a no-op. Dispose nothing.
            extractor = new FixedWidthExtractor<T>(_reader);
            ownedResources = Array.Empty<object?>();
        }
        else if (_stream is not null)
        {
            // The extractor wraps the caller's stream with leaveOpen:true, so dispose only the
            // extractor (to release its internal reader); the caller retains the stream.
            extractor = new FixedWidthExtractor<T>(_stream, _encoding);
            ownedResources = new object?[] { extractor };
        }
        else
        {
            // Path source: the builder owns the reader it opens, so dispose it once drained.
            var reader = new StreamReader(_path!, _encoding, detectEncodingFromByteOrderMarks: true);
            extractor = new FixedWidthExtractor<T>(reader);
            ownedResources = new object?[] { reader };
        }

        foreach (var mutation in _mutations)
        {
            mutation(extractor);
        }

        return extractor;
    }


    /// <summary>
    /// Yields every record from the extractor, then disposes the owned resources in a
    /// <see langword="finally"/> block so they are released whether the run succeeds, throws, or is
    /// cancelled. The pipeline's cancellation token is forwarded here by the generic core.
    /// </summary>
    private static async IAsyncEnumerable<T> DrainThenDisposeAsync
    (
        FixedWidthExtractor<T> extractor,
        object?[] ownedResources,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        try
        {
            await foreach (var item in extractor.ExtractAsync(token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            foreach (var resource in ownedResources)
            {
                (resource as IDisposable)?.Dispose();
            }
        }
    }
}
