using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Encoding _encoding;
    private readonly BinaryRecordMap _map;
    private readonly IProgressTimer? _progressTimer;
    private bool _progressTimerWired;
    private long _currentRecordNumber;



    /// <summary>
    /// Initializes a new <see cref="FixedWidthBinaryLoader{TRecord}"/> that writes fixed-length
    /// binary records to <paramref name="stream"/>. The caller retains ownership of the stream.
    /// </summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="encoding">
    /// The encoding used to write <see cref="Enums.BinaryFieldType.Text"/> fields. Pass
    /// <see langword="null"/> (the default) for <see cref="Encoding.ASCII"/>; pass a code-page
    /// encoding for EBCDIC output.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public FixedWidthBinaryLoader(Stream stream, Encoding? encoding = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        }

        _encoding = encoding ?? Encoding.ASCII;
        _map = BinaryFieldMap.GetResult<TRecord>();
    }



    // Test-only constructor that injects a deterministic progress timer.
    internal FixedWidthBinaryLoader(Stream stream, IProgressTimer timer, Encoding? encoding = null)
        : this(stream, encoding)
    {
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
    }



    /// <summary>The number of bytes in one record, derived from the field layout.</summary>
    public int RecordByteLength => _map.RecordByteLength;



    /// <inheritdoc/>
    protected override async Task LoadWorkerAsync(IAsyncEnumerable<TRecord> items, CancellationToken cancellationToken)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var buffer = new byte[_map.RecordByteLength];

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(buffer, 0, buffer.Length);
            foreach (var descriptor in _map.Descriptors)
            {
                descriptor.Encode(item, buffer, _encoding);
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
            CurrentItemCount,
            CurrentSkippedItemCount,
            currentRejectedItemCount: 0,
            currentFilteredLineCount: 0,
            currentLineNumber: Interlocked.Read(ref _currentRecordNumber)
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
