using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Diagnostics;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Writes records of type <typeparamref name="TRecord"/> to a fixed-width text stream
/// as an asynchronous operation.
/// </summary>
/// <remarks>
/// <para>
/// Two construction modes are supported, each with different ownership semantics:
/// </para>
/// <list type="bullet">
///   <item><b>TextWriter constructor</b> — the caller owns the <see cref="TextWriter"/>
///   lifetime. The loader does not dispose it, and calling <see cref="System.IDisposable.Dispose"/> is
///   optional (no-op). The caller is responsible for flushing the writer.</item>
///   <item><b>Stream constructor</b> — the loader creates an internal
///   <see cref="StreamWriter"/> with a 64 KB buffer for improved throughput.
///   The caller retains ownership of the <see cref="Stream"/> (it is not closed).
///   The internal writer is flushed automatically at the end of
///   <c>LoadWorkerAsync</c>, and <see cref="System.IDisposable.Dispose"/> must be called to release it.</item>
/// </list>
/// <code>
/// // Stream-based (preferred for files — 64 KB buffer reduces syscall overhead):
/// await using var stream = File.OpenWrite("output.txt");
/// using var loader = new FixedWidthLoader&lt;MyRecord&gt;(stream);
///
/// // TextWriter-based (caller owns the writer):
/// var sw = new StringWriter();
/// var loader = new FixedWidthLoader&lt;MyRecord&gt;(sw);
/// var result = sw.ToString();
///
/// // Write a formatted table to the console
/// var loader = new FixedWidthLoader&lt;MyRecord&gt;(Console.Out);
/// loader.WriteHeader    = true;
/// loader.FieldSeparator = '-';
/// loader.FieldDelimiter = " | ";
/// </code>
/// </remarks>
public class FixedWidthLoader<TRecord> : LoaderBase<TRecord, FixedWidthReport>, ISupportDryRun
    where TRecord : notnull
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    /// <summary>
    /// Default buffer size used when constructing a <see cref="StreamWriter"/>
    /// from a <see cref="Stream"/>. 64 KB reduces syscall frequency compared
    /// to the <see cref="StreamWriter"/> default of 1 KB.
    /// </summary>
    private const int DefaultBufferSize = 65536;

    private readonly TextWriter _writer;
    private readonly bool _ownsWriter;
    private readonly ILogger _logger;
    private readonly IProgressTimer? _progressTimer;
    private bool _progressTimerWired;
    private long _currentLineNumber;

    // _currentLineNumber is read by CreateProgressReport on a Timer threadpool thread
    // and written by LoadWorkerAsync on the async continuation thread.
    // Interlocked.Read/Increment ensures atomicity on all targets including 32-bit net462.


    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter"/> to write fixed-width records to. This can be a
    /// <see cref="StreamWriter"/> wrapping a file or network stream, a
    /// <see cref="StringWriter"/> for in-memory content, <see cref="Console.Out"/> for
    /// formatted console table output, or any other <see cref="TextWriter"/> implementation.
    /// The caller is responsible for the writer's lifetime — the loader does not dispose it.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
    public FixedWidthLoader(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _logger = NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="TextWriter"/> with diagnostic logging.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter"/> to write fixed-width records to.
    /// </param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="writer"/> or <paramref name="logger"/> is null.
    /// </exception>
    public FixedWidthLoader
    (
        TextWriter writer,
        ILogger<FixedWidthLoader<TRecord>> logger
    )
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="TextWriter"/> and uses the supplied
    /// <see cref="IProgressTimer"/> instead of the default system timer.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter"/> to write fixed-width records to.
    /// </param>
    /// <param name="timer">
    /// The <see cref="IProgressTimer"/> to use for progress reporting.
    /// </param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output.
    /// Pass <see langword="null"/> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="writer"/> or <paramref name="timer"/> is null.
    /// </exception>
    internal FixedWidthLoader
    (
        TextWriter writer,
        IProgressTimer timer,
        ILogger<FixedWidthLoader<TRecord>>? logger = null
    )
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="Stream"/> using an internal <see cref="StreamWriter"/>
    /// with a 64 KB buffer for improved throughput on large files.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to write fixed-width records to. The stream must be
    /// writable. The caller retains ownership — the loader does not dispose the stream.
    /// </param>
    /// <param name="encoding">
    /// The <see cref="Encoding"/> used to encode the output. Pass <see langword="null"/>
    /// (the default) to use <see cref="Encoding.UTF8"/>. Use <c>new UTF8Encoding(false)</c>
    /// to write UTF-8 without a byte-order mark.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public FixedWidthLoader(Stream stream, Encoding? encoding = null)
    {
        _writer = CreateBufferedWriter(stream, encoding);
        _ownsWriter = true;
        _logger = NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="Stream"/> with diagnostic logging.
    /// The loader creates an internal <see cref="StreamWriter"/> with a 64 KB
    /// buffer for improved throughput on large files.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to write fixed-width records to. The stream must be
    /// writable. The caller retains ownership — the loader does not dispose the stream.
    /// </param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="encoding">
    /// The <see cref="Encoding"/> used to encode the output. Pass <see langword="null"/>
    /// (the default) to use <see cref="Encoding.UTF8"/>. Use <c>new UTF8Encoding(false)</c>
    /// to write UTF-8 without a byte-order mark.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> or <paramref name="logger"/> is null.
    /// </exception>
    public FixedWidthLoader
    (
        Stream stream,
        ILogger<FixedWidthLoader<TRecord>> logger,
        Encoding? encoding = null
    )
    {
        _writer = CreateBufferedWriter(stream, encoding);
        _ownsWriter = true;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord}"/> that writes
    /// to the specified <see cref="Stream"/> and uses the supplied
    /// <see cref="IProgressTimer"/> instead of the default system timer.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to write fixed-width records to.
    /// </param>
    /// <param name="timer">
    /// The <see cref="IProgressTimer"/> to use for progress reporting.
    /// </param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output.
    /// Pass <see langword="null"/> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> or <paramref name="timer"/> is null.
    /// </exception>
    internal FixedWidthLoader
    (
        Stream stream,
        IProgressTimer timer,
        ILogger<FixedWidthLoader<TRecord>>? logger = null
    )
    {
        _writer = CreateBufferedWriter(stream, encoding: null);
        _ownsWriter = true;
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    /// <summary>
    /// Creates the internal <see cref="StreamWriter"/> shared by the
    /// <see cref="Stream"/>-based constructors: the requested encoding (or
    /// <see cref="Encoding.UTF8"/>), a 64 KB buffer, and <c>leaveOpen: true</c>
    /// so the caller retains stream ownership.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    private static StreamWriter CreateBufferedWriter(Stream stream, Encoding? encoding)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        return new StreamWriter(stream, encoding: encoding ?? Encoding.UTF8, bufferSize: DefaultBufferSize, leaveOpen: true);
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The function used to convert a field value to its string representation
    /// before padding and writing. Defaults to <see cref="FixedWidthConverter.Strict"/>.
    /// </summary>
    /// <remarks>
    /// The converter receives the raw boxed property value and a <see cref="FieldContext"/>
    /// describing the field. It must return a string no longer than
    /// <see cref="FieldContext.FieldLength"/>; otherwise the loader's field-width
    /// safety-net will throw a
    /// <see cref="Exceptions.FieldOverflowException"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Write booleans as "Y"/"N" instead of "True"/"False":
    /// loader.ValueConverter = (value, ctx) =>
    ///     ctx.PropertyType == typeof(bool)
    ///         ? ((bool)value ? "Y" : "N")
    ///         : FixedWidthConverter.Strict(value, ctx);
    ///
    /// // Silently truncate all values instead of throwing on overflow:
    /// loader.ValueConverter = FixedWidthConverter.Truncate;
    /// </code>
    /// </example>
    public Func<object, FieldContext, string> ValueConverter { get; set; } = FixedWidthConverter.Strict;



    /// <summary>
    /// The function used to convert a header label to its string representation.
    /// Defaults to <see cref="FixedWidthConverter.StrictHeader"/>.
    /// </summary>
    /// <remarks>
    /// Only called when <see cref="WriteHeader"/> is <see langword="true"/>.
    /// Space-padding to <see cref="FieldContext.FieldLength"/> is applied by
    /// the framework after this converter returns — the converter must only
    /// ensure the returned string is not longer than the field width.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Render all headers in upper-case:
    /// loader.HeaderConverter = (label, ctx) =>
    ///     FixedWidthConverter.StrictHeader(label.ToUpperInvariant(), ctx);
    ///
    /// // Silently truncate headers that are too long:
    /// loader.HeaderConverter = FixedWidthConverter.TruncateHeader;
    /// </code>
    /// </example>
    public Func<string, FieldContext, string> HeaderConverter { get; set; } = FixedWidthConverter.StrictHeader;



    /// <summary>
    /// When <see langword="true"/>, a header line is written before any records.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The header label for each field is taken from
    /// <see cref="Attributes.FixedWidthFieldAttribute.Header"/> if set, or the
    /// property name otherwise. Labels are passed through
    /// <see cref="HeaderConverter"/> before being written.
    /// </remarks>
    /// <example>
    /// <code>
    /// loader.WriteHeader = true;
    /// // Produces a header line like: "FirstName LastName  Age  "
    /// </code>
    /// </example>
    public bool WriteHeader { get; set; }



    /// <inheritdoc />
    /// <remarks>
    /// When <see langword="true"/>, the loader enumerates the source and evaluates
    /// <see cref="Abstractions.LoaderBase{TDestination,TProgress}.SkipItemCount"/> /
    /// <see cref="Abstractions.LoaderBase{TDestination,TProgress}.MaximumItemCount"/>,
    /// increments progress counters, fires the progress-timer callback, and logs as
    /// usual — but writes nothing to the output fixed-width stream. Field-width
    /// validation still runs, so a dry run surfaces
    /// <see cref="Exceptions.FieldOverflowException"/> the same way a real run would.
    /// Defaults to <see langword="false"/>.
    /// </remarks>
    public bool IsDryRun { get; set; }



    /// <summary>
    /// When non-null, a separator line is written after the header, consisting of
    /// the specified character repeated to each field's width.
    /// Set to <see langword="null"/> (default) to write no separator.
    /// Has no effect if <see cref="WriteHeader"/> is <see langword="false"/>.
    /// Mirrors <see cref="FixedWidthExtractor{TRecord}.FieldSeparator"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// loader.FieldSeparator = '-';  // writes "----------"
    /// loader.FieldSeparator = '=';  // writes "=========="
    /// loader.FieldSeparator = null; // no separator (default)
    /// </code>
    /// </example>
    public char? FieldSeparator { get; set; }



    /// <summary>
    /// An optional string written between fields on every line including headers,
    /// separators, and data rows. Set to <see langword="null"/> (default) for pure
    /// fixed-width output with no delimiter. Use a value like <c>" | "</c> for
    /// human-readable report output.
    /// </summary>
    /// <remarks>
    /// When set, the delimiter is inserted between every adjacent pair of fields — it
    /// is not appended after the last field. The
    /// <see cref="FixedWidthExtractor{TRecord}.FieldDelimiter"/> on the
    /// corresponding extractor must be set to the same value so that field boundaries
    /// are correctly identified during extraction.
    /// </remarks>
    /// <example>
    /// <code>
    /// loader.FieldDelimiter = " | ";   // human-readable table: "John       | Smith      |  42 "
    /// loader.FieldDelimiter = null;    // pure fixed-width (default): "John      Smith        42 "
    /// </code>
    /// </example>
    public string? FieldDelimiter { get; set; }



    /// <summary>
    /// The 1-based physical line number of the line most recently written to the output.
    /// Updated after each line is written. Includes the header line and separator line
    /// if written. Matches the line number shown in a text editor.
    /// </summary>
    /// <remarks>
    /// Thread-safe: reads are performed with <see cref="Interlocked"/>
    /// so this property may be sampled from a progress-reporting timer thread
    /// without a data race.
    /// </remarks>
    public long CurrentLineNumber => Interlocked.Read(ref _currentLineNumber);

    /// <summary>
    /// Creates a progress report snapshot for the current loader state.
    /// </summary>
    /// <returns>
    /// A <see cref="FixedWidthReport"/> snapshot containing
    /// <see cref="LoaderBase{TRecord,FixedWidthReport}.CurrentItemCount"/>,
    /// <see cref="LoaderBase{TRecord,FixedWidthReport}.CurrentSkippedItemCount"/>,
    /// and <see cref="CurrentLineNumber"/> at the moment of the call.
    /// </returns>
    protected override FixedWidthReport CreateProgressReport()
    {
        return new FixedWidthReport
        (
            CurrentItemCount,
            CurrentSkippedItemCount,
            currentRejectedItemCount: 0,
            currentFilteredLineCount: 0,
            Interlocked.Read(ref _currentLineNumber)
        );
    }



    /// <summary>
    /// Returns a snapshot progress report. Visible to the test assembly via InternalsVisibleTo.
    /// </summary>
    internal FixedWidthReport GetProgressReport() => CreateProgressReport();



    /// <inheritdoc/>
    protected override IProgressTimer CreateProgressTimer(IProgress<FixedWidthReport> progress)
    {
        if (_progressTimer != null)
        {
            if (!_progressTimerWired)
            {
                _progressTimerWired = true;
                _progressTimer.Elapsed += () => progress.Report(CreateProgressReport());
            }

            return _progressTimer;
        }

        return base.CreateProgressTimer(progress);
    }



    /// <summary>
    /// Releases the internal <see cref="StreamWriter"/> when this instance was
    /// constructed from a <see cref="Stream"/>, then defers to the base class.
    /// Has no effect on a caller-owned <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="System.IDisposable"/>;
    /// <see langword="false"/> when called from a finalizer.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsWriter)
        {
            _writer.Dispose();
        }

        base.Dispose(disposing);
    }



    /// <inheritdoc/>
    protected override async Task LoadWorkerAsync
    (
        IAsyncEnumerable<TRecord> items,
        CancellationToken token
    )
    {
        var fieldMap = FieldMap.GetResult<TRecord>();
        LogLoadingStarted(fieldMap);

        // Metrics (#30): duration recorded on completion/throw; counters are no-ops without a listener.
        var metricTags = FixedWidthMetrics.CreateTags(FixedWidthMetrics.LoadOperation, typeof(TRecord));
        using var operationScope = FixedWidthMetrics.MeasureDuration(metricTags);

        // In dry-run mode, route all formatting through a throwaway writer so the
        // pipeline — including field-width validation — still runs, but nothing
        // reaches the output stream. The real writer is left untouched and so
        // flushes nothing below.
        var target = IsDryRun ? TextWriter.Null : _writer;

        if (WriteHeader)
        {
            await WriteHeaderAsync(fieldMap, target).ConfigureAwait(false);
        }

        await foreach (var item in items.WithCancellation(token).ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();

            if (EqualityComparer<TRecord>.Default.Equals(item, default!))
            {
                throw LogAndCreateNullRecordError();
            }

            if (CurrentSkippedItemCount < SkipItemCount)
            {
                IncrementCurrentSkippedItemCount();
                FixedWidthMetrics.RecordSkipped(metricTags);
                LogDebugItemSkipped();
                continue;
            }

            if (CurrentItemCount >= MaximumItemCount)
            {
                LogDebugMaxReached();
                break;
            }

            FixedWidthLineParser.WriteRecord
            (
                target,
                item,
                fieldMap,
                ValueConverter,
                FieldDelimiter
            );
            Interlocked.Increment(ref _currentLineNumber);
            await target.WriteLineAsync().ConfigureAwait(false);
            IncrementCurrentItemCount();
            FixedWidthMetrics.RecordLoaded(metricTags);
            LogDebugRecordWritten();
        }

        await FlushIfOwnedAsync(token).ConfigureAwait(false);

        LogLoadingCompleted();
    }



    /// <summary>
    /// Flushes the internal writer when the loader owns it (the <see cref="Stream"/>
    /// constructors). No-op for caller-supplied writers, which the caller flushes.
    /// In dry-run mode the writer received no writes, so this flushes nothing.
    /// </summary>
    private async Task FlushIfOwnedAsync(CancellationToken token)
    {
        // In dry-run mode nothing was written to the owned writer, so there is
        // nothing to flush. Flushing anyway would emit the StreamWriter's UTF-8
        // preamble (BOM) to the stream on .NET Framework, which the dry-run
        // contract treats as a side effect.
        if (!_ownsWriter || IsDryRun)
        {
            return;
        }

#if NET8_0_OR_GREATER
        await _writer.FlushAsync(token).ConfigureAwait(false);
#else
        token.ThrowIfCancellationRequested();
        await _writer.FlushAsync().ConfigureAwait(false);
#endif
    }



    /// <summary>
    /// Writes the header line and, if <see cref="FieldSeparator"/> is set, the
    /// separator line that follows it.
    /// </summary>
    private async Task WriteHeaderAsync(FieldMapResult fieldMap, TextWriter target)
    {
        FixedWidthLineParser.WriteHeader
        (
            target,
            fieldMap,
            HeaderConverter,
            FieldDelimiter
        );
        Interlocked.Increment(ref _currentLineNumber);
        await target.WriteLineAsync().ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Wrote header at line {LineNumber}", _currentLineNumber);
        }

        if (FieldSeparator.HasValue)
        {
            FixedWidthLineParser.WriteSeparator
            (
                target,
                fieldMap,
                FieldSeparator.Value,
                FieldDelimiter
            );
            Interlocked.Increment(ref _currentLineNumber);
            await target.WriteLineAsync().ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug
                (
                    "Wrote separator at line {LineNumber} (char='{SeparatorChar}')",
                    _currentLineNumber,
                    FieldSeparator.Value
                );
            }
        }
    }



    // ------------------------------------------------------------------
    // Logging helpers
    // ------------------------------------------------------------------

    private void LogLoadingStarted(FieldMapResult fieldMap)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Loading started for {RecordType}. WriteHeader={WriteHeader}, " +
                "FieldSeparator={FieldSeparator}, FieldDelimiter={FieldDelimiter}, " +
                "SkipItemCount={SkipItemCount}, MaximumItemCount={MaximumItemCount}",
                typeof(TRecord).Name,
                WriteHeader,
                FieldSeparator?.ToString() ?? "(none)",
                FieldDelimiter ?? "(none)",
                SkipItemCount,
                MaximumItemCount
            );
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Field map resolved for {RecordType}: {FieldCount} fields, " +
                "ExpectedLineWidth={ExpectedLineWidth}, TotalColumnCount={TotalColumnCount}",
                typeof(TRecord).Name,
                fieldMap.Descriptors.Count,
                fieldMap.ExpectedLineWidth,
                fieldMap.TotalColumnCount
            );
        }
    }



    private void LogLoadingCompleted()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Loading completed for {RecordType}: {ItemCount} items loaded, " +
                "{SkippedCount} skipped, {LineCount} lines written",
                typeof(TRecord).Name,
                CurrentItemCount,
                CurrentSkippedItemCount,
                Interlocked.Read(ref _currentLineNumber)
            );
        }
    }



    private void LogDebugMaxReached()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "MaximumItemCount ({MaximumItemCount}) reached, stopping loading",
                MaximumItemCount
            );
        }
    }



    private void LogDebugItemSkipped()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Skipping item ({SkippedCount}/{SkipItemCount})",
                CurrentSkippedItemCount,
                SkipItemCount
            );
        }
    }



    private void LogDebugRecordWritten()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Wrote record at line {LineNumber} (item #{ItemCount})",
                _currentLineNumber,
                CurrentItemCount
            );
        }
    }



    private InvalidOperationException LogAndCreateNullRecordError()
    {
        var ex = new InvalidOperationException
        (
            $"A null record was encountered at item {CurrentItemCount + CurrentSkippedItemCount + 1}. " +
            "The loader writes what it is given — null records are not permitted."
        );

        _logger.LogError
        (
            ex,
            "Null record encountered at item {ItemPosition}",
            CurrentItemCount + CurrentSkippedItemCount + 1
        );

        return ex;
    }



}
