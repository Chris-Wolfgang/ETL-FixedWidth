using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth;


/// <summary>
/// Default <see cref="IFixedWidthLoaderBuilder{T}"/> implementation. Records configuration up front,
/// then materializes a <see cref="FixedWidthLoader{T}"/> and terminates the upstream pipeline when
/// <see cref="RunAsync"/> is called. Any resource the builder itself opened (a file writer for a path
/// sink) is disposed after the run, on success or failure, flushing the output to disk.
/// </summary>
/// <typeparam name="T">The record type consumed by the loader.</typeparam>
internal sealed class FixedWidthLoaderBuilder<T> : IFixedWidthLoaderBuilder<T>
    where T : notnull
{
    private readonly IEtlPipeline<T> _pipeline;
    private readonly string? _path;
    private readonly Stream? _stream;
    private readonly TextWriter? _writer;
    private FixedWidthLoaderOptions _options = new();

    private Encoding _encoding = System.Text.Encoding.UTF8;


    private FixedWidthLoaderBuilder
    (
        IEtlPipeline<T> pipeline,
        string? path,
        Stream? stream,
        TextWriter? writer
    )
    {
        _pipeline = pipeline;
        _path = path;
        _stream = stream;
        _writer = writer;
    }


    internal static IFixedWidthLoaderBuilder<T> FromPath(IEtlPipeline<T> pipeline, string path)
        => new FixedWidthLoaderBuilder<T>(pipeline, path, stream: null, writer: null);


    internal static IFixedWidthLoaderBuilder<T> FromStream(IEtlPipeline<T> pipeline, Stream stream)
        => new FixedWidthLoaderBuilder<T>(pipeline, path: null, stream, writer: null);


    internal static IFixedWidthLoaderBuilder<T> FromWriter(IEtlPipeline<T> pipeline, TextWriter writer)
        => new FixedWidthLoaderBuilder<T>(pipeline, path: null, stream: null, writer);


    public IFixedWidthLoaderBuilder<T> Encoding(Encoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        _encoding = encoding;
        return this;
    }


    public IFixedWidthLoaderBuilder<T> WriteHeader(bool writeHeader) => Set(o => o with { WriteHeader = writeHeader });


    public IFixedWidthLoaderBuilder<T> ValueConverter(Func<object, FieldContext, string> converter)
    {
        if (converter is null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        return Set(o => o with { ValueConverter = converter });
    }


    public IFixedWidthLoaderBuilder<T> HeaderConverter(Func<string, FieldContext, string> converter)
    {
        if (converter is null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        return Set(o => o with { HeaderConverter = converter });
    }


    public IFixedWidthLoaderBuilder<T> FieldSeparator(char? separator) => Set(o => o with { FieldSeparator = separator });


    public IFixedWidthLoaderBuilder<T> FieldDelimiter(string? delimiter) => Set(o => o with { FieldDelimiter = delimiter });


    public IFixedWidthLoaderBuilder<T> IsDryRun(bool isDryRun) => Set(o => o with { IsDryRun = isDryRun });


    public Task RunAsync(IProgress<EtlPipelineProgress>? progress = null, CancellationToken token = default)
    {
        FixedWidthLoader<T> loader;
        IDisposable? owned = null;

        if (_writer is not null)
        {
            // Caller owns the writer and flushes it; dispose nothing.
            loader = new FixedWidthLoader<T>(_writer, _options);
        }
        else if (_stream is not null)
        {
            // The loader wraps the caller's stream without taking ownership and flushes it during the run.
            // Disposing the loader afterwards releases its internal writer; the caller's stream itself is left open.
            loader = new FixedWidthLoader<T>(_stream, new FixedWidthLoaderStreamOptions(_options, _encoding));
            owned = loader;
        }
        else
        {
            // Path sink: the builder owns the writer it opens. Disposing it after the run flushes the
            // file to disk and closes it.
            var writer = new StreamWriter(_path!, append: false, _encoding);
            loader = new FixedWidthLoader<T>(writer, _options);
            owned = writer;
        }

        var sink = _pipeline.To(loader);

        if (owned is not null)
        {
            sink = sink.DisposingOwned(owned);
        }

        return sink.RunAsync(progress, token);
    }


    private IFixedWidthLoaderBuilder<T> Set(Func<FixedWidthLoaderOptions, FixedWidthLoaderOptions> update)
    {
        _options = update(_options);
        return this;
    }
}
