using System;
using System.IO;
using System.Text;

namespace Wolfgang.Etl.FixedWidth.Parsing;

/// <summary>
/// A <see cref="TextReader"/> decorator that reproduces <see cref="TextReader.ReadLine"/> exactly
/// while tracking how many <b>bytes</b> of the underlying stream each line consumed — line content
/// plus its terminator (<c>\n</c>, <c>\r</c>, or <c>\r\n</c>) — using the stream's <see cref="Encoding"/>.
/// This is what makes byte-offset checkpoint/resume possible (#31): <see cref="StreamReader"/> buffers
/// ahead and strips the terminator, so its <see cref="StreamReader.BaseStream"/> position cannot tell
/// you where the next unread line begins.
/// </summary>
/// <remarks>
/// Used only when byte-offset tracking is enabled; the default extraction path reads through the
/// unwrapped reader unchanged. Only <see cref="ReadLine"/> (and disposal) are exercised by the
/// extractor — the other <see cref="TextReader"/> members read from the same buffer for correctness
/// but are not on the hot path.
/// </remarks>
internal sealed class ByteCountingLineReader : TextReader
{
    private readonly TextReader _inner;
    private readonly Encoding _encoding;
    private readonly char[] _buffer;
    private readonly int _lfBytes;
    private readonly int _crBytes;
    private readonly int _crlfBytes;
    private int _bufferPosition;
    private int _bufferLength;
    private long _bytesConsumed;



    /// <summary>
    /// Initializes a new <see cref="ByteCountingLineReader"/>.
    /// </summary>
    /// <param name="inner">The reader to pull characters from (typically a <see cref="StreamReader"/>).</param>
    /// <param name="encoding">The encoding used to compute the byte width of each line and terminator.</param>
    /// <param name="initialByteOffset">
    /// The byte offset the underlying stream was already positioned at (a resume seek target, or a
    /// consumed byte-order-mark preamble), added to every reported offset.
    /// </param>
    /// <param name="bufferSize">The character read-ahead buffer size.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> or <paramref name="encoding"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> is not positive.</exception>
    public ByteCountingLineReader(TextReader inner, Encoding encoding, long initialByteOffset = 0, int bufferSize = 8192)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be greater than zero.");
        }

        _buffer = new char[bufferSize];
        _bytesConsumed = initialByteOffset;
        _lfBytes = encoding.GetByteCount("\n");
        _crBytes = encoding.GetByteCount("\r");
        _crlfBytes = encoding.GetByteCount("\r\n");
    }



    /// <summary>
    /// The total number of underlying-stream bytes consumed so far — the byte offset of the start of
    /// the next unread line. Includes the <c>initialByteOffset</c> passed to the constructor.
    /// </summary>
    public long BytesConsumed => _bytesConsumed;



    private bool FillBuffer()
    {
        _bufferLength = _inner.Read(_buffer, 0, _buffer.Length);
        _bufferPosition = 0;
        return _bufferLength > 0;
    }



    /// <inheritdoc/>
#pragma warning disable MA0051 // single scan/terminator state machine — splitting it would obscure the logic
    public override string? ReadLine()
#pragma warning restore MA0051
    {
        StringBuilder? line = null;

        while (true)
        {
            if (_bufferPosition >= _bufferLength && !FillBuffer())
            {
                // End of input. A pending builder is the final unterminated line; otherwise EOF.
                if (line == null)
                {
                    return null;
                }

                var last = line.ToString();
                _bytesConsumed += _encoding.GetByteCount(last);
                return last;
            }

            // Scan the buffer for the next carriage-return or line-feed.
            var scan = _bufferPosition;
            while (scan < _bufferLength && _buffer[scan] != '\r' && _buffer[scan] != '\n')
            {
                scan++;
            }

            line ??= new StringBuilder();
            line.Append(_buffer, _bufferPosition, scan - _bufferPosition);
            _bufferPosition = scan;

            if (scan >= _bufferLength)
            {
                // Ran off the end without a terminator — refill and keep accumulating.
                continue;
            }

            var terminator = _buffer[_bufferPosition];
            _bufferPosition++;
            var text = line.ToString();

            if (terminator == '\n')
            {
                _bytesConsumed += _encoding.GetByteCount(text) + _lfBytes;
                return text;
            }

            // A carriage return may stand alone or be the first half of a CR-LF pair — the '\n'
            // can sit in the next buffer, so refill if needed before peeking.
            if (_bufferPosition >= _bufferLength && !FillBuffer())
            {
                _bytesConsumed += _encoding.GetByteCount(text) + _crBytes;   // lone CR at end of input
                return text;
            }

            if (_buffer[_bufferPosition] == '\n')
            {
                _bufferPosition++;
                _bytesConsumed += _encoding.GetByteCount(text) + _crlfBytes;
            }
            else
            {
                _bytesConsumed += _encoding.GetByteCount(text) + _crBytes;
            }

            return text;
        }
    }



    /// <inheritdoc/>
    public override int Peek()
    {
        if (_bufferPosition >= _bufferLength && !FillBuffer())
        {
            return -1;
        }

        return _buffer[_bufferPosition];
    }



    /// <inheritdoc/>
    public override int Read()
    {
        if (_bufferPosition >= _bufferLength && !FillBuffer())
        {
            return -1;
        }

        var c = _buffer[_bufferPosition];
        _bufferPosition++;
        _bytesConsumed += _encoding.GetByteCount(new[] { c });
        return c;
    }



    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
