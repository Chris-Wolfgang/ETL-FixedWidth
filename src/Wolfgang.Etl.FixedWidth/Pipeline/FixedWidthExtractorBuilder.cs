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
    private FixedWidthExtractorOptions<T> _options = new();
    private Touched _touched;

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


    public IFixedWidthExtractorBuilder<T> HeaderLineCount(int count) => Set(o => o with { HeaderLineCount = count }, Touched.HeaderLineCount);


    public IFixedWidthExtractorBuilder<T> HasHeader(bool hasHeader) => Set(o => o with { HeaderLineCount = hasHeader ? 1 : 0 }, Touched.HeaderLineCount);


    public IFixedWidthExtractorBuilder<T> MalformedLineHandling(MalformedLineHandling handling) => Set(o => o with { MalformedLineHandling = handling }, Touched.MalformedLineHandling);


    public IFixedWidthExtractorBuilder<T> BlankLineHandling(BlankLineHandling handling) => Set(o => o with { BlankLineHandling = handling }, Touched.BlankLineHandling);


    public IFixedWidthExtractorBuilder<T> LineFilter(Func<string, LineAction> filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        return Set(o => o with { LineFilter = filter }, Touched.LineFilter);
    }


    public IFixedWidthExtractorBuilder<T> RecordValidator(Func<T, ValidationResult> validator)
    {
        if (validator is null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        return Set(o => o with { RecordValidator = validator }, Touched.RecordValidator);
    }


    public IFixedWidthExtractorBuilder<T> ValueParser(FixedWidthValueParser parser)
    {
        if (parser is null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return Set(o => o with { ValueParser = parser }, Touched.ValueParser);
    }


    public IFixedWidthExtractorBuilder<T> FieldSeparator(char? separator) => Set(o => o with { FieldSeparator = separator }, Touched.FieldSeparator);


    public IFixedWidthExtractorBuilder<T> FieldDelimiter(string? delimiter) => Set(o => o with { FieldDelimiter = delimiter }, Touched.FieldDelimiter);


    public IEtlPipeline<TOut> Through<TOut>(ITransformAsync<T, TOut> transformer) where TOut : notnull => Pipeline().Through(transformer);


    public IEtlPipeline<TOut> Through<TOut>(ITransformWithCancellationAsync<T, TOut> transformer) where TOut : notnull => Pipeline().Through(transformer);


    public IEtlPipeline<TOut> Through<TOut>(Func<IAsyncEnumerable<T>, IAsyncEnumerable<TOut>> stage) where TOut : notnull => Pipeline().Through(stage);


    public IEtlPipeline<TOut> Through<TOut>(Func<IAsyncEnumerable<T>, CancellationToken, IAsyncEnumerable<TOut>> stage) where TOut : notnull => Pipeline().Through(stage);


    public IEtlPipelineSink To<TProgress>(LoaderBase<T, TProgress> loader) where TProgress : notnull => Pipeline().To(loader);


    public IAsyncEnumerable<T> AsAsyncEnumerable(CancellationToken token = default) => Pipeline().AsAsyncEnumerable(token);


    private IFixedWidthExtractorBuilder<T> Set
    (
        Func<FixedWidthExtractorOptions<T>, FixedWidthExtractorOptions<T>> update,
        Touched member
    )
    {
        ThrowIfMaterialized();
        _options = update(_options);
        _touched |= member;
        return this;
    }



#pragma warning disable CS0618 // The only place the builder writes the deprecated setters: a caller-supplied instance (FromExtractor) is already configured, and a whole record cannot be applied to it without resetting what the caller set, so the members configured through the builder are copied one by one. Goes with the setters (#342). Constructed sources take the record.
    private void ApplyTo(FixedWidthExtractor<T> existing)
    {
        if (_touched.HasFlag(Touched.HeaderLineCount)) { existing.HeaderLineCount = _options.HeaderLineCount; }
        if (_touched.HasFlag(Touched.MalformedLineHandling)) { existing.MalformedLineHandling = _options.MalformedLineHandling; }
        if (_touched.HasFlag(Touched.BlankLineHandling)) { existing.BlankLineHandling = _options.BlankLineHandling; }
        if (_touched.HasFlag(Touched.LineFilter)) { existing.LineFilter = _options.LineFilter; }
        if (_touched.HasFlag(Touched.RecordValidator)) { existing.RecordValidator = _options.RecordValidator; }
        if (_touched.HasFlag(Touched.ValueParser)) { existing.ValueParser = _options.ValueParser; }
        if (_touched.HasFlag(Touched.FieldSeparator)) { existing.FieldSeparator = _options.FieldSeparator; }
        if (_touched.HasFlag(Touched.FieldDelimiter)) { existing.FieldDelimiter = _options.FieldDelimiter; }
    }
#pragma warning restore CS0618



    [Flags]
    private enum Touched
    {
        None = 0,
        HeaderLineCount = 1 << 0,
        MalformedLineHandling = 1 << 1,
        BlankLineHandling = 1 << 2,
        LineFilter = 1 << 3,
        RecordValidator = 1 << 4,
        ValueParser = 1 << 5,
        FieldSeparator = 1 << 6,
        FieldDelimiter = 1 << 7,
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
            ApplyTo(extractor);
        }
        else if (_reader is not null)
        {
            // Caller owns the reader; the extractor's Dispose is a no-op. Dispose nothing.
            extractor = new FixedWidthExtractor<T>(_reader, _options);
            ownedResources = Array.Empty<object?>();
        }
        else if (_stream is not null)
        {
            // The extractor wraps the caller's stream with leaveOpen:true, so dispose only the
            // extractor (to release its internal reader); the caller retains the stream.
            extractor = new FixedWidthExtractor<T>(_stream, new FixedWidthExtractorStreamOptions<T>(_options, _encoding));
            ownedResources = new object?[] { extractor };
        }
        else
        {
            // Path source: the builder owns the reader it opens, so dispose it once drained.
            var reader = new StreamReader(_path!, _encoding, detectEncodingFromByteOrderMarks: true);
            extractor = new FixedWidthExtractor<T>(reader, _options);
            ownedResources = new object?[] { reader };
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
            await foreach (var item in extractor.ExtractAsync(token).ConfigureAwait(false))
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
