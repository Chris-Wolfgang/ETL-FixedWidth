using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Reads a fixed-width file that interleaves <b>multiple record types</b> — for example a
/// mainframe batch file with a header, detail, and trailer layout on different lines — and
/// yields each line as the <see cref="object"/> it maps to (#19).
/// </summary>
/// <remarks>
/// <para>
/// Register one rule per record type with <see cref="When"/>: a predicate over the raw line
/// (typically a discriminator character such as <c>line[0] == 'D'</c>) and the POCO type to
/// materialize when it matches. Rules are evaluated in registration order — the first match
/// wins. A line that matches no rule is handled per <see cref="UnmatchedLineHandling"/>, unless
/// a fallback type was registered with <see cref="Otherwise"/>.
/// </para>
/// <para>
/// Each record type keeps its own independent <c>[FixedWidthField]</c> layout. The yielded
/// records are the concrete types you registered — pattern-match on them at the call site.
/// </para>
/// <para>
/// Ownership semantics match <see cref="FixedWidthExtractor{TRecord}"/>: a caller-supplied
/// <see cref="TextReader"/> is not disposed; a <see cref="Stream"/> is wrapped in an internal
/// 64&#160;KB <see cref="StreamReader"/> that <see cref="System.IDisposable.Dispose"/> releases
/// while the stream itself stays open.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var extractor = new FixedWidthMultiRecordExtractor(reader)
///     .When(line => line[0] == 'H', typeof(HeaderRecord))
///     .When(line => line[0] == 'D', typeof(DetailRecord))
///     .When(line => line[0] == 'T', typeof(TrailerRecord));
///
/// await foreach (var record in extractor.ExtractAsync(token))
/// {
///     switch (record)
///     {
///         case HeaderRecord h: /* ... */ break;
///         case DetailRecord d: /* ... */ break;
///         case TrailerRecord t: /* ... */ break;
///     }
/// }
/// </code>
/// </example>
public sealed class FixedWidthMultiRecordExtractor : ExtractorBase<object, FixedWidthReport>
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    /// <summary>
    /// Default buffer size used when constructing a <see cref="StreamReader"/> from a
    /// <see cref="Stream"/>. 64&#160;KB reduces syscall frequency compared to the
    /// <see cref="StreamReader"/> default of 1&#160;KB.
    /// </summary>
    private const int DefaultBufferSize = 65536;

    private readonly Stream? _stream;   // set by the Stream constructor; wrapped lazily using Encoding
    private readonly bool _ownsReader;
    private readonly ILogger _logger;
    private TextReader? _reader;         // supplied directly (TextReader) or created from _stream at enumeration
    private readonly IProgressTimer? _progressTimer;
    private readonly List<Rule> _rules = new();
    private bool _progressTimerWired;
    private Rule? _fallback;
    private long _currentLineNumber;
    private int _currentRejectedItemCount;
    private int _currentFilteredLineCount;

    // _currentLineNumber is read by CreateProgressReport on a Timer threadpool thread and
    // written by ExtractWorkerAsync on the async continuation thread. Interlocked keeps the
    // read/write atomic on all targets including 32-bit net462.


    // ------------------------------------------------------------------
    // Constructors
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new <see cref="FixedWidthMultiRecordExtractor"/> that reads from the specified
    /// <see cref="TextReader"/>. The caller owns the reader's lifetime.
    /// </summary>
    /// <param name="reader">The reader to pull fixed-width lines from.</param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output. Pass
    /// <see langword="null"/> (the default) to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
    public FixedWidthMultiRecordExtractor(TextReader reader, ILogger<FixedWidthMultiRecordExtractor>? logger = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthMultiRecordExtractor"/> that reads from the specified
    /// <see cref="Stream"/> using an internal <see cref="StreamReader"/> with a 64&#160;KB buffer.
    /// The caller retains ownership of the stream. Set <see cref="Encoding"/> to decode with a
    /// specific encoding (defaults to <see cref="Encoding.UTF8"/>).
    /// </summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output. Pass
    /// <see langword="null"/> (the default) to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public FixedWidthMultiRecordExtractor(Stream stream, ILogger<FixedWidthMultiRecordExtractor>? logger = null)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        // We create the internal StreamReader that wraps the caller's stream, so we own (and dispose)
        // that reader. It is created with leaveOpen:true, so the caller's stream itself is never closed
        // — the caller retains ownership of the Stream. (A caller-supplied TextReader leaves this false.)
        _ownsReader = true;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    // Test-only constructor that injects a deterministic progress timer.
    internal FixedWidthMultiRecordExtractor(TextReader reader, IProgressTimer timer)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
        _logger = NullLogger.Instance;
    }



    private static StreamReader CreateBufferedReader(Stream stream, Encoding encoding)
    {
        return new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: DefaultBufferSize, leaveOpen: true);
    }



    // ------------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers a rule: when <paramref name="predicate"/> returns <see langword="true"/> for a
    /// line, that line is parsed as <paramref name="recordType"/>. Rules are evaluated in the
    /// order they are registered and the first match wins. Returns this extractor so calls can
    /// be chained.
    /// </summary>
    /// <param name="predicate">
    /// A discriminator over the raw line — for example <c>line =&gt; line[0] == 'D'</c>. Blank
    /// lines are not passed to the predicate when <see cref="SkipBlankLines"/> is
    /// <see langword="true"/> (the default), so a discriminator may index the line safely.
    /// </param>
    /// <param name="recordType">
    /// The POCO type to materialize, decorated with <c>[FixedWidthField]</c> attributes and
    /// having a public parameterless constructor.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="predicate"/> or <paramref name="recordType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="recordType"/> has an invalid layout (for example duplicate column indexes
    /// or a mapped property with no public setter).
    /// </exception>
    public FixedWidthMultiRecordExtractor When(Func<string, bool> predicate, Type recordType)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (recordType == null)
        {
            throw new ArgumentNullException(nameof(recordType));
        }

        _rules.Add(new Rule(predicate, recordType, FieldMap.GetResult(recordType)));
        return this;
    }



    /// <summary>
    /// Registers a fallback record type for lines that match no <see cref="When"/> rule. When a
    /// fallback is set it takes precedence over <see cref="UnmatchedLineHandling"/> — the
    /// otherwise-unmatched line is parsed as <paramref name="recordType"/> instead of being
    /// thrown or skipped. Returns this extractor so calls can be chained.
    /// </summary>
    /// <param name="recordType">The catch-all POCO type for unmatched lines.</param>
    /// <exception cref="ArgumentNullException"><paramref name="recordType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="recordType"/> has an invalid layout.</exception>
    public FixedWidthMultiRecordExtractor Otherwise(Type recordType)
    {
        if (recordType == null)
        {
            throw new ArgumentNullException(nameof(recordType));
        }

        _fallback = new Rule(_ => true, recordType, FieldMap.GetResult(recordType));
        return this;
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// The encoding used to decode the source when constructed from a <see cref="Stream"/>. Defaults
    /// to <see cref="Encoding.UTF8"/>. Ignored when constructed from a <see cref="TextReader"/> (the
    /// reader already decodes). The stream is wrapped lazily when extraction begins, so this value is
    /// read then — set it in the object initializer.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;



    /// <summary>
    /// What to do with a data line that matches no <see cref="When"/> rule and for which no
    /// <see cref="Otherwise"/> fallback was registered. Defaults to
    /// <see cref="UnmatchedLineHandling.ThrowException"/>.
    /// </summary>
    public UnmatchedLineHandling UnmatchedLineHandling { get; init; } = UnmatchedLineHandling.ThrowException;



    /// <summary>
    /// What to do when a matched line cannot be parsed into its record type — too short, or a
    /// field value that will not convert. Defaults to
    /// <see cref="MalformedLineHandling.ThrowException"/>; <see cref="MalformedLineHandling.Skip"/>
    /// drops the line and continues. <see cref="MalformedLineHandling.ReturnDefault"/> is not
    /// supported here (the substitute type would be ambiguous) and throws
    /// <see cref="InvalidOperationException"/> if set.
    /// </summary>
    public MalformedLineHandling MalformedLineHandling { get; init; } = MalformedLineHandling.ThrowException;



    /// <summary>
    /// When <see langword="true"/> (the default), zero-length lines are skipped before any
    /// predicate runs, so discriminators may index the line without guarding against empty
    /// input. When <see langword="false"/>, a blank line is treated as an unmatched line.
    /// </summary>
    public bool SkipBlankLines { get; init; } = true;



    /// <summary>
    /// The number of header lines to skip at the start of the file before routing begins.
    /// Defaults to 0. Use this only for banner lines that precede the record body; a leading
    /// <c>H</c> record that you want to capture should be registered with <see cref="When"/>
    /// instead.
    /// </summary>
    public int HeaderLineCount { get; init; }



    /// <summary>
    /// Convenience wrapper over <see cref="HeaderLineCount"/> — <see langword="true"/> maps to 1,
    /// <see langword="false"/> to 0.
    /// </summary>
    public bool HasHeader
    {
        get => HeaderLineCount > 0;
        init => HeaderLineCount = value ? 1 : 0;
    }



    /// <summary>
    /// An optional delimiter present between columns in the source file, or <see langword="null"/>
    /// (the default) for pure fixed-width input. Applies to every registered record type.
    /// </summary>
    public string? FieldDelimiter { get; init; }



    /// <summary>
    /// The value parser applied to every field of every record type. Defaults to
    /// <see cref="FixedWidthConverter.DefaultParser"/>.
    /// </summary>
    public FixedWidthValueParser ValueParser { get; init; } = FixedWidthConverter.DefaultParser;



    /// <summary>
    /// An optional dead-letter sink invoked once for each line that fails to parse (#29). With
    /// <see cref="MalformedLineHandling.Skip"/> the line is reported and dropped; with the default
    /// <see cref="MalformedLineHandling.ThrowException"/> it is reported before the exception is
    /// re-thrown.
    /// </summary>
    public Action<FixedWidthError>? OnError { get; init; }



    /// <summary>
    /// The 1-based physical line number of the line most recently read. Thread-safe so it may be
    /// sampled from a progress timer thread.
    /// </summary>
    public long CurrentLineNumber => Interlocked.Read(ref _currentLineNumber);



    /// <summary>
    /// The number of matched lines dropped by <see cref="MalformedLineHandling.Skip"/>. Distinct
    /// from the <c>SkipItemCount</c> pagination budget.
    /// </summary>
    public int CurrentRejectedItemCount => Volatile.Read(ref _currentRejectedItemCount);



    /// <summary>
    /// The number of physical lines read that produced no record and were not counted as skipped
    /// or rejected: header lines, blank lines dropped by <see cref="SkipBlankLines"/>, and
    /// unmatched lines dropped by <see cref="UnmatchedLineHandling.Skip"/>.
    /// </summary>
    public int CurrentFilteredLineCount => Volatile.Read(ref _currentFilteredLineCount);



    // ------------------------------------------------------------------
    // ExtractorBase overrides
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    protected override FixedWidthReport CreateProgressReport()
    {
        return new FixedWidthReport
        (
            CurrentItemCount,
            CurrentSkippedItemCount,
            CurrentRejectedItemCount,
            CurrentFilteredLineCount,
            Interlocked.Read(ref _currentLineNumber)
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



    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsReader)
        {
            // _reader is null if the extractor was never enumerated; the caller-owned stream stays open.
            _reader?.Dispose();
        }

        base.Dispose(disposing);
    }



#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
#pragma warning disable MA0051 // async iterator methods cannot delegate 'yield return' to sub-methods
#pragma warning disable CS1998 // async method lacks 'await' — intentionally synchronous; see comment below
    protected override async IAsyncEnumerable<object> ExtractWorkerAsync([EnumeratorCancellation] CancellationToken token)
#else
    /// <inheritdoc/>
#pragma warning disable MA0051
#pragma warning disable CS1998
    protected override async IAsyncEnumerable<object> ExtractWorkerAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
#endif
#pragma warning restore CS1998
#pragma warning restore MA0051
    {
        // Synchronous ReadLine — the TextReader/StreamReader buffers internally, so async I/O
        // adds state-machine cost without benefit for file- and memory-based sources.
        if (_rules.Count == 0 && _fallback == null)
        {
            throw new InvalidOperationException
            (
                "No record-type rules registered. Call When(...) at least once (or Otherwise(...)) " +
                "before extracting."
            );
        }

        if (MalformedLineHandling == MalformedLineHandling.ReturnDefault)
        {
            throw new InvalidOperationException
            (
                $"{nameof(MalformedLineHandling)}.{nameof(MalformedLineHandling.ReturnDefault)} is not " +
                $"supported by {nameof(FixedWidthMultiRecordExtractor)} — the substitute record type is ambiguous."
            );
        }

        long dataLinesSkipped = 0;

        // Wrap the stream lazily so the Encoding init property is read here (after the object
        // initializer has run), not in the constructor. A TextReader source is used as supplied.
        var reader = _reader ??= CreateBufferedReader(_stream!, Encoding);

        LogExtractionStarted();
        token.ThrowIfCancellationRequested();

        string? line;
#pragma warning disable CA1849, VSTHRD103, S6966 // ReadLine is intentionally synchronous
        while ((line = reader.ReadLine()) != null)
#pragma warning restore CA1849, VSTHRD103, S6966
        {
            token.ThrowIfCancellationRequested();

            Interlocked.Increment(ref _currentLineNumber);

            if (_currentLineNumber <= HeaderLineCount)
            {
                IncrementFilteredLineCount();
                continue;
            }

            if (SkipBlankLines && line.Length == 0)
            {
                IncrementFilteredLineCount();
                continue;
            }

            var rule = MatchRule(line);
            if (rule == null)
            {
                if (UnmatchedLineHandling == UnmatchedLineHandling.Skip)
                {
                    IncrementFilteredLineCount();
                    continue;
                }

                throw new InvalidDataException
                (
                    $"Line {_currentLineNumber} matched no registered record type: '{line}'."
                );
            }

            if (dataLinesSkipped < SkipItemCount)
            {
                dataLinesSkipped++;
                IncrementCurrentSkippedItemCount();
                continue;
            }

            if (CurrentItemCount >= MaximumItemCount)
            {
                // Mirror FixedWidthExtractor: the line was read but won't be yielded, so count it
                // as filtered and log completion before ending early.
                IncrementFilteredLineCount();
                LogExtractionCompleted();
                yield break;
            }

            if (!TryParseLine(line, rule, out var record))
            {
                continue;
            }

            IncrementCurrentItemCount();
            yield return record;
        }

        LogExtractionCompleted();
    }



    // ------------------------------------------------------------------
    // Logging helpers
    // ------------------------------------------------------------------

    private void LogExtractionStarted()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Multi-record extraction started. Rules={RuleCount}, Fallback={Fallback}, " +
                "HeaderLineCount={HeaderLineCount}, UnmatchedLineHandling={UnmatchedLineHandling}",
                _rules.Count,
                _fallback?.RecordType.Name ?? "(none)",
                HeaderLineCount,
                UnmatchedLineHandling
            );
        }
    }



    private void LogExtractionCompleted()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Multi-record extraction completed: {ItemCount} extracted, {SkippedCount} skipped, " +
                "{RejectedCount} rejected, {LineCount} lines read",
                CurrentItemCount,
                CurrentSkippedItemCount,
                CurrentRejectedItemCount,
                Interlocked.Read(ref _currentLineNumber)
            );
        }
    }



    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns the first rule whose predicate matches <paramref name="line"/>, the registered
    /// <see cref="Otherwise"/> fallback if none match, or <see langword="null"/> when neither
    /// applies.
    /// </summary>
    private Rule? MatchRule(string line)
    {
        for (var i = 0; i < _rules.Count; i++)
        {
            if (_rules[i].Predicate(line))
            {
                return _rules[i];
            }
        }

        return _fallback;
    }



    /// <summary>
    /// Parses <paramref name="line"/> into <paramref name="record"/> using the matched rule.
    /// Returns <see langword="true"/> on success. On a parse failure, routes the error through the
    /// base per-item error policy (<see cref="OnItemError"/>), then either skips (returns
    /// <see langword="false"/>) or re-throws, per <see cref="MalformedLineHandling"/>.
    /// </summary>
    private bool TryParseLine(string line, Rule rule, out object record)
    {
        record = null!;
        try
        {
            record = FixedWidthLineParser.ParseLine<object>
            (
                line,
                _currentLineNumber,
                rule.Map,
                FieldDelimiter,
                ValueParser
            );
            return true;
        }
        catch (MalformedLineException ex)
        {
            // Route through the base per-item error policy (OnItemError, translated from
            // MalformedLineHandling) so the failure is counted (CurrentErrorItemCount) and surfaced in
            // the pipeline (ErrorItemCount) — matching FixedWidthExtractor. OnError is invoked from
            // OnItemError, so the dead-letter sink still sees every failure.
            if (HandleItemError(new ItemErrorContext(_currentLineNumber, ex, () => line)) == ItemErrorAction.Abort)
            {
                throw;
            }

            IncrementRejectedItemCount();
            return false;
        }
    }



    /// <summary>
    /// Translates <see cref="MalformedLineHandling"/> into the base per-item error policy and reports
    /// the failure to the <see cref="OnError"/> dead-letter sink. <see cref="MalformedLineHandling.Skip"/>
    /// maps to <see cref="ItemErrorAction.Skip"/>; <see cref="MalformedLineHandling.ThrowException"/>
    /// (the default) maps to <see cref="ItemErrorAction.Abort"/>. <see cref="MalformedLineHandling.ReturnDefault"/>
    /// is rejected up front, so it never reaches this hook.
    /// </summary>
    protected override ItemErrorAction OnItemError(ItemErrorContext context)
    {
        if (context != null)
        {
            OnError?.Invoke(new FixedWidthError(context.ItemNumber, context.RawContent?.Invoke(), context.Exception));
        }

        return MalformedLineHandling == MalformedLineHandling.Skip
            ? ItemErrorAction.Skip
            : ItemErrorAction.Abort;
    }



    private void IncrementRejectedItemCount() => Interlocked.Increment(ref _currentRejectedItemCount);



    private void IncrementFilteredLineCount() => Interlocked.Increment(ref _currentFilteredLineCount);



    /// <summary>A registered discriminator rule: predicate, target type, and its resolved map.</summary>
    private sealed class Rule
    {
        public Rule(Func<string, bool> predicate, Type recordType, FieldMapResult map)
        {
            Predicate = predicate;
            RecordType = recordType;
            Map = map;
        }

        public Func<string, bool> Predicate { get; }

        public Type RecordType { get; }

        public FieldMapResult Map { get; }
    }
}
