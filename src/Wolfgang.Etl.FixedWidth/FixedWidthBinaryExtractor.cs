using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Binary;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Reads a fixed-length <b>binary</b> record file (mainframe / COBOL, #21) and yields records of
/// type <typeparamref name="TRecord"/>. Unlike <see cref="FixedWidthExtractor{TRecord}"/>, records
/// are <b>not</b> newline-delimited — each record is a fixed number of bytes (the sum of the
/// <see cref="Attributes.FixedWidthBinaryFieldAttribute.ByteLength"/> of every field), read directly
/// from the stream, so packed-decimal and binary bytes that happen to be <c>0x0A</c>/<c>0x0D</c> are
/// never mistaken for record separators.
/// </summary>
/// <typeparam name="TRecord">
/// The record type, whose <see cref="Attributes.FixedWidthBinaryFieldAttribute"/> properties define
/// the byte layout. Requires a public parameterless constructor.
/// </typeparam>
/// <example>
/// <code>
/// await using var stream = File.OpenRead("accounts.dat");
/// using var extractor = new FixedWidthBinaryExtractor&lt;AccountRecord&gt;(stream);
/// await foreach (var account in extractor.ExtractAsync(token))
/// {
///     // account.Balance decoded from COMP-3, account.TransactionCount from COMP, …
/// }
/// </code>
/// </example>
public sealed class FixedWidthBinaryExtractor<TRecord> : ExtractorBase<TRecord, FixedWidthReport>
    where TRecord : notnull, new()
{
    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly BinaryRecordMap _map;
    private readonly IProgressTimer? _progressTimer;
    private bool _progressTimerWired;
    private long _currentRecordNumber;



    /// <summary>
    /// Initializes a new <see cref="FixedWidthBinaryExtractor{TRecord}"/> that reads fixed-length
    /// binary records from <paramref name="stream"/>. The caller retains ownership of the stream.
    /// </summary>
    /// <param name="stream">The readable binary record stream.</param>
    /// <param name="encoding">
    /// The encoding used to decode <see cref="Enums.BinaryFieldType.Text"/> fields. Pass
    /// <see langword="null"/> (the default) for <see cref="Encoding.ASCII"/>; pass a code-page
    /// encoding (e.g. IBM037) for EBCDIC data.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable.</exception>
    public FixedWidthBinaryExtractor(Stream stream, Encoding? encoding = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        _encoding = encoding ?? Encoding.ASCII;
        _map = BinaryFieldMap.GetResult<TRecord>();
    }



    // Test-only constructor that injects a deterministic progress timer.
    internal FixedWidthBinaryExtractor(Stream stream, IProgressTimer timer, Encoding? encoding = null)
        : this(stream, encoding)
    {
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
    }



    /// <summary>The number of bytes in one record, derived from the field layout.</summary>
    public int RecordByteLength => _map.RecordByteLength;



    /// <inheritdoc/>
    protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync
    (
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // Honor an already-cancelled token before touching the stream (TestKit cancellation contract).
        cancellationToken.ThrowIfCancellationRequested();

        var recordLength = _map.RecordByteLength;
        var buffer = new byte[recordLength];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (CurrentItemCount >= MaximumItemCount)
            {
                break;
            }

            var read = await ReadFullRecordAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;   // clean end of stream
            }

            if (read < recordLength)
            {
                throw new EndOfStreamException($"Unexpected end of stream: a partial record of {read} bytes was read but {recordLength} were expected.");
            }

            Interlocked.Increment(ref _currentRecordNumber);

            if (CurrentSkippedItemCount < SkipItemCount)
            {
                IncrementCurrentSkippedItemCount();
                continue;
            }

            var record = (TRecord)_map.Factory();
            foreach (var descriptor in _map.Descriptors)
            {
                descriptor.Setter(record, descriptor.Decode(buffer, _encoding));
            }

            IncrementCurrentItemCount();
            yield return record;
        }
    }



    private async Task<int> ReadFullRecordAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await _stream.ReadAsync(buffer, total, buffer.Length - total, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
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
