using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Binary;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Writes records of type <typeparamref name="TRecord"/> to a fixed-length <b>binary</b> record
/// stream (mainframe / COBOL, #21) — the write counterpart of
/// <see cref="FixedWidthBinaryExtractor{TRecord}"/>. Each record is encoded to a fixed number of
/// bytes (the field byte widths) and written back-to-back with no delimiters, encoding
/// <c>COMP-3</c> packed-decimal and <c>COMP</c> binary-integer fields alongside text.
/// </summary>
/// <typeparam name="TRecord">
/// The record type, whose <see cref="Attributes.FixedWidthBinaryFieldAttribute"/> properties define
/// the byte layout.
/// </typeparam>
public sealed class FixedWidthBinaryLoader<TRecord> : LoaderBase<TRecord, FixedWidthReport>
    where TRecord : notnull
{
    private readonly Stream _stream;
    // Null when the caller supplied no options - the signal to fall back to the
    // [Obsolete] Encoding property.
    private readonly Encoding? _optionsEncoding;
    private readonly ILogger _logger;
    private readonly BinaryRecordMap _map;
    private readonly IProgressTimer? _progressTimer;
    private bool _progressTimerWired;
    private long _currentRecordNumber;


    /// <summary>
    /// Initializes a new <see cref="FixedWidthBinaryLoader{TRecord}"/> from a <see cref="Stream"/> using the
    /// default options, with diagnostic logging.
    /// </summary>
    /// <param name="stream">The stream to use.</param>
    /// <param name="logger">The logger to use for diagnostic output.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    [Obsolete("Use the constructor that takes FixedWidthBinaryLoaderOptions. This overload will be removed in a future release.")]
    public FixedWidthBinaryLoader(Stream stream, ILogger<FixedWidthBinaryLoader<TRecord>> logger)
        : this(stream, options: null, logger: logger)
    {
    }








    /// <summary>
    /// Initializes a new <see cref="FixedWidthBinaryLoader{TRecord}"/> that writes fixed-length
    /// binary records to <paramref name="stream"/>. The caller retains ownership of the stream.
    /// </summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">
    /// Options that control behaviour, including the encoding. When <c>null</c> — or omitted —
    /// the documented defaults apply.
    /// </param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output. Pass
    /// <see langword="null"/> (the default) to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public FixedWidthBinaryLoader
    (
        Stream stream,
        FixedWidthBinaryLoaderOptions? options = null,
        ILogger<FixedWidthBinaryLoader<TRecord>>? logger = null
    )
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        }

        _optionsEncoding = options?.Encoding;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _map = BinaryFieldMap.GetResult<TRecord>();
    }



    // Test-only constructor that injects a deterministic progress timer.
    internal FixedWidthBinaryLoader
    (
        Stream stream,
        IProgressTimer timer,
        FixedWidthBinaryLoaderOptions? options = null,
        ILogger<FixedWidthBinaryLoader<TRecord>>? logger = null
    )
        : this(stream, options, logger)
    {
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
    }


    /// <summary>
    /// The <see cref="System.Text.Encoding"/> used by this instance. Superseded by
    /// <see cref="FixedWidthBinaryLoaderOptions.Encoding"/>.
    /// </summary>
    /// <remarks>
    /// Retained for source compatibility with 0.10.x. It is honoured only when the constructor was
    /// given no options object; a caller who passes options is using the supported route and that
    /// value wins. The two cannot conflict in existing code, because the options constructor did
    /// not exist before 0.11.0.
    /// </remarks>
    [Obsolete("Set Encoding on FixedWidthBinaryLoaderOptions and pass it to the constructor instead. This property will be removed in a future release.")]
    public Encoding Encoding { get; init; } = Encoding.ASCII;



    // Options win when supplied; otherwise fall back to the obsolete property. Resolved at the
    // point of use rather than in the constructor: an init property is assigned AFTER the
    // constructor body runs, so capturing it there would always read the default.
#pragma warning disable CS0618 // reading the obsolete property is the entire point of this member
    private Encoding ResolvedEncoding => _optionsEncoding ?? Encoding;
#pragma warning restore CS0618




    /// <summary>The number of bytes in one record, derived from the field layout.</summary>
    public int RecordByteLength => _map.RecordByteLength;



    /// <inheritdoc/>
    // Keep the descriptive `cancellationToken` name at the override site; the base
    // class shortens it to `token` but the longer form is the fleet-wide convention
    // in this repo.
#pragma warning disable S927 // Parameter names should match base declaration
    protected override async Task LoadWorkerAsync(IAsyncEnumerable<TRecord> items, CancellationToken cancellationToken)
#pragma warning restore S927
    {
        // items is guaranteed non-null by the LoaderBase.LoadAsync entry point.
        cancellationToken.ThrowIfCancellationRequested();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Binary load started for {RecordType}: RecordByteLength={RecordByteLength}",
                typeof(TRecord).Name,
                _map.RecordByteLength
            );
        }

        var buffer = new byte[_map.RecordByteLength];

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(buffer, 0, buffer.Length);
            foreach (var descriptor in _map.Descriptors)
            {
                descriptor.Encode(item, buffer, ResolvedEncoding);
            }

            await _stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

            Interlocked.Increment(ref _currentRecordNumber);
            IncrementCurrentItemCount();
        }

        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }



    /// <inheritdoc/>
    protected override FixedWidthReport CreateProgressReport()
    {
        return new FixedWidthReport
        (
            new FixedWidthReportOptions
            {
                CurrentCount = CurrentItemCount,
                CurrentSkippedItemCount = CurrentSkippedItemCount,
                CurrentLineNumber = Interlocked.Read(ref _currentRecordNumber)
            }
        );
    }



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
}
