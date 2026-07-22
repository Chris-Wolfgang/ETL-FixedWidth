using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Diagnostics;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Reads a fixed-width text file and yields records of type <typeparamref name="TRecord"/>
/// as an asynchronous stream.
/// </summary>
/// <typeparam name="TRecord">
/// The POCO type representing a single record. Properties decorated with
/// <see cref="Attributes.FixedWidthFieldAttribute"/> are populated from each line.
/// The type must have a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// Two construction modes are supported, each with different ownership semantics:
/// </para>
/// <list type="bullet">
///   <item><b>TextReader constructor</b> — the caller owns the <see cref="TextReader"/>
///   lifetime. The extractor does not dispose it. Calling <see cref="System.IDisposable.Dispose"/> is
///   optional and has no effect.</item>
///   <item><b>Stream constructor</b> — the extractor creates an internal
///   <see cref="StreamReader"/> with a 64 KB buffer for improved throughput on large files.
///   The caller retains ownership of the <see cref="Stream"/> (it is not closed), but
///   <see cref="System.IDisposable.Dispose"/> must be called to release the internal reader.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Stream-based (preferred for files — 64 KB buffer reduces syscall overhead):
/// await using var stream = File.OpenRead("data.txt");
/// using var extractor = new FixedWidthExtractor&lt;CustomerRecord&gt;(stream);
///
/// // TextReader-based (caller owns the reader):
/// var extractor = new FixedWidthExtractor&lt;CustomerRecord&gt;(reader);
/// </code>
/// </example>
public class FixedWidthExtractor<TRecord> : ExtractorBase<TRecord, FixedWidthReport>
    where TRecord : notnull, new()
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    /// <summary>
    /// Default buffer size used when constructing a <see cref="StreamReader"/>
    /// from a <see cref="Stream"/>. 64 KB reduces syscall frequency compared
    /// to the <see cref="StreamReader"/> default of 1 KB.
    /// </summary>
    private const int DefaultBufferSize = 65536;

    private readonly TextReader _reader;
    private readonly bool _ownsReader;
    private readonly ILogger _logger;
    private readonly IProgressTimer? _progressTimer;
    private bool _progressTimerWired;
    private long _currentLineNumber;
    private int _currentRejectedItemCount;
    private int _currentFilteredLineCount;

    // _currentLineNumber is read by CreateProgressReport on a Timer threadpool thread
    // and written by ExtractWorkerAsync on the async continuation thread.
    // Interlocked.Read/Increment ensures atomicity on all targets including 32-bit net462.


    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader"/> to read fixed-width records from. This can be a
    /// <see cref="StreamReader"/> wrapping a file stream (local or network share), a
    /// <see cref="StringReader"/> for in-memory content, or any other <see cref="TextReader"/>
    /// implementation. Reading is performed synchronously for throughput; callers with
    /// slow or non-buffered sources should pre-buffer into a <see cref="StringReader"/>.
    /// The caller is responsible for the reader's lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    public FixedWidthExtractor(TextReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="TextReader"/> with diagnostic logging.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader"/> to read fixed-width records from.
    /// </param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="reader"/> or <paramref name="logger"/> is null.
    /// </exception>
    public FixedWidthExtractor
    (
        TextReader reader,
        ILogger<FixedWidthExtractor<TRecord>> logger
    )
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="TextReader"/> and uses the supplied
    /// <see cref="IProgressTimer"/> instead of the default system timer.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader"/> to read fixed-width records from.
    /// </param>
    /// <param name="timer">
    /// The <see cref="IProgressTimer"/> to use for progress reporting.
    /// </param>
    /// <param name="logger">
    /// An optional <see cref="ILogger{TCategoryName}"/> for diagnostic output.
    /// Pass <see langword="null"/> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="reader"/> or <paramref name="timer"/> is null.
    /// </exception>
    internal FixedWidthExtractor
    (
        TextReader reader,
        IProgressTimer timer,
        ILogger<FixedWidthExtractor<TRecord>>? logger = null
    )
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="Stream"/> using an internal <see cref="StreamReader"/>
    /// with a 64 KB buffer for improved throughput on large files.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to read fixed-width records from. The stream must be
    /// readable. The caller retains ownership — the extractor does not dispose the stream.
    /// </param>
    /// <param name="encoding">
    /// The <see cref="Encoding"/> used to decode the stream. Pass <see langword="null"/>
    /// (the default) to use <see cref="Encoding.UTF8"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public FixedWidthExtractor(Stream stream, Encoding? encoding = null)
    {
        _reader = CreateBufferedReader(stream, encoding);
        _ownsReader = true;
        _logger = NullLogger.Instance;
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="Stream"/> with diagnostic logging.
    /// The extractor creates an internal <see cref="StreamReader"/> with a 64 KB
    /// buffer for improved throughput on large files.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to read fixed-width records from. The stream must be
    /// readable. The caller retains ownership — the extractor does not dispose the stream.
    /// </param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="encoding">
    /// The <see cref="Encoding"/> used to decode the stream. Pass <see langword="null"/>
    /// (the default) to use <see cref="Encoding.UTF8"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> or <paramref name="logger"/> is null.
    /// </exception>
    public FixedWidthExtractor
    (
        Stream stream,
        ILogger<FixedWidthExtractor<TRecord>> logger,
        Encoding? encoding = null
    )
    {
        _reader = CreateBufferedReader(stream, encoding);
        _ownsReader = true;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord}"/> that reads
    /// from the specified <see cref="Stream"/> and uses the supplied
    /// <see cref="IProgressTimer"/> instead of the default system timer.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to read fixed-width records from.
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
    internal FixedWidthExtractor
    (
        Stream stream,
        IProgressTimer timer,
        ILogger<FixedWidthExtractor<TRecord>>? logger = null
    )
    {
        _reader = CreateBufferedReader(stream, encoding: null);
        _ownsReader = true;
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }



    /// <summary>
    /// Creates the internal <see cref="StreamReader"/> shared by the
    /// <see cref="Stream"/>-based constructors: the requested encoding (or
    /// <see cref="Encoding.UTF8"/>), BOM detection, a 64 KB buffer, and
    /// <c>leaveOpen: true</c> so the caller retains stream ownership.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    private static StreamReader CreateBufferedReader(Stream stream, Encoding? encoding)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        return new StreamReader(stream, encoding: encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: DefaultBufferSize, leaveOpen: true);
    }



    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    /// <summary>
    /// Specifies what happens when a line is encountered that is too short or whose
    /// field values cannot be converted to the target property type.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="MalformedLineHandling.ThrowException"/>.
    /// When set to <see cref="MalformedLineHandling.Skip"/>, the line is skipped and
    /// <c>CurrentSkippedItemCount</c> is incremented.
    /// When set to <see cref="MalformedLineHandling.ReturnDefault"/>, a default instance
    /// of <typeparamref name="TRecord"/> is yielded for the offending line.
    /// </remarks>
    public MalformedLineHandling MalformedLineHandling { get; set; } = MalformedLineHandling.ThrowException;



    /// <summary>
    /// Specifies what happens when a truly blank line (zero length) is encountered
    /// in the file. Evaluated before the skip budget and <see cref="ExtractorBase{TRecord,FixedWidthReport}.MaximumItemCount"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><see cref="BlankLineHandling.ThrowException"/> (default) — always throws
    ///   a <see cref="Exceptions.LineTooShortException"/> regardless of position.</item>
    ///   <item><see cref="BlankLineHandling.Skip"/> — the line is invisible to all counting
    ///   logic. Does not count toward <see cref="ExtractorBase{TRecord,FixedWidthReport}.SkipItemCount"/>
    ///   or <see cref="ExtractorBase{TRecord,FixedWidthReport}.MaximumItemCount"/>.</item>
    ///   <item><see cref="BlankLineHandling.ReturnDefault"/> — a default
    ///   <typeparamref name="TRecord"/> instance is yielded. Counts toward the skip budget
    ///   if within <see cref="ExtractorBase{TRecord,FixedWidthReport}.SkipItemCount"/>, otherwise
    ///   counts toward <see cref="ExtractorBase{TRecord,FixedWidthReport}.MaximumItemCount"/>.</item>
    /// </list>
    /// <para>
    /// Note: a line consisting entirely of spaces is not blank — it is a valid data line
    /// that will parse to a record with all whitespace-trimmed (empty/default) fields.
    /// </para>
    /// <para>
    /// <see cref="LineFilter"/> is not invoked for blank lines.
    /// </para>
    /// </remarks>
    public BlankLineHandling BlankLineHandling { get; set; } = BlankLineHandling.ThrowException;



    /// <summary>
    /// A delegate invoked for every data line (after header and separator lines have been
    /// skipped) before any parsing occurs. Return <see cref="LineAction.Process"/> to parse
    /// the line normally, <see cref="LineAction.Skip"/> to skip it, or
    /// <see cref="LineAction.Stop"/> to end the stream immediately without parsing the line.
    /// </summary>
    /// <remarks>
    /// Evaluated after <see cref="BlankLineHandling"/> — blank lines never reach the filter.
    /// Evaluated before the skip budget and <see cref="ExtractorBase{TRecord,FixedWidthReport}.MaximumItemCount"/>.
    /// Both <see cref="LineAction.Skip"/> and <see cref="LineAction.Stop"/> are invisible
    /// to all counting logic — they do not affect <see cref="ExtractorBase{TRecord,FixedWidthReport}.SkipItemCount"/>,
    /// <see cref="ExtractorBase{TRecord,FixedWidthReport}.MaximumItemCount"/>, or
    /// <c>CurrentSkippedItemCount</c>.
    /// Defaults to a function that always returns <see cref="LineAction.Process"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Footer string — stop when a known marker line is reached
    /// extractor.LineFilter = line => line == "END" ? LineAction.Stop : LineAction.Process;
    ///
    /// // Trailing separator — stop when a line consists entirely of dashes
    /// extractor.LineFilter = line => line.All(c => c == '-') ? LineAction.Stop : LineAction.Process;
    ///
    /// // EOF marker — stop when a line starts with a sentinel prefix
    /// extractor.LineFilter = line => line.StartsWith("$$") ? LineAction.Stop : LineAction.Process;
    ///
    /// // Comment lines — skip lines that begin with '#'
    /// extractor.LineFilter = line => line.StartsWith("#") ? LineAction.Skip : LineAction.Process;
    ///
    /// // Blank line as terminator — stop at the first empty line
    /// extractor.LineFilter = line => string.IsNullOrWhiteSpace(line) ? LineAction.Stop : LineAction.Process;
    /// </code>
    /// </example>
    public Func<string, LineAction> LineFilter { get; set; } = _ => LineAction.Process;



    /// <summary>
    /// An optional callback invoked for each fully parsed record, after
    /// <see cref="ValueParser"/> and field assignment but before the record is
    /// yielded. Return <see cref="ValidationResult.Accept"/> to yield it,
    /// <see cref="ValidationResult.Skip"/> to drop it (increments
    /// <c>CurrentSkippedItemCount</c>), or <see cref="ValidationResult.Stop"/> to
    /// end extraction. <see langword="null"/> (the default) applies no validation.
    /// </summary>
    /// <example>
    /// <code>
    /// extractor.RecordValidator = record =>
    ///     record.Balance &lt; 0
    ///         ? ValidationResult.Skip("Negative balance")
    ///         : ValidationResult.Accept();
    /// </code>
    /// </example>
    public Func<TRecord, ValidationResult>? RecordValidator { get; set; }



    /// <summary>
    /// A delegate that converts a raw string read from the file into the target property
    /// type. The <see cref="FieldContext"/> provides the property type, format string, and
    /// other field metadata needed to perform the conversion.
    /// Defaults to <see cref="FixedWidthConverter.DefaultParser"/>.
    /// Mirrors <see cref="FixedWidthLoader{TRecord}.ValueConverter"/>.
    /// </summary>
    /// <remarks>
    /// The delegate must return a value that is assignable to the property's CLR type.
    /// Returning an incompatible type will cause an <see cref="InvalidCastException"/>
    /// when the framework attempts to set the property value.
    /// </remarks>
    /// <exception cref="Exceptions.FieldConversionException">
    /// The default <see cref="FixedWidthConverter.DefaultParser"/> wraps any parse
    /// failure in a <see cref="Exceptions.FieldConversionException"/>. Custom parsers
    /// should do the same so that <see cref="MalformedLineHandling"/> can handle them
    /// uniformly.
    /// </exception>
    /// <example>
    /// <code>
    /// // Treat "Y"/"N" as bool, fall back to DefaultParser for everything else:
    /// extractor.ValueParser = (text, ctx) =>
    ///     ctx.PropertyType == typeof(bool)
    ///         ? (object)(text.Span.SequenceEqual("Y".AsSpan()))
    ///         : FixedWidthConverter.DefaultParser(text, ctx);
    ///
    /// // Parse a custom date format for a specific field:
    /// extractor.ValueParser = (text, ctx) =>
    ///     ctx.PropertyName == "BirthDate"
    ///         ? DateTime.ParseExact(text.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture)
    ///         : FixedWidthConverter.DefaultParser(text, ctx);
    /// </code>
    /// </example>
    public FixedWidthValueParser ValueParser { get; set; } = FixedWidthConverter.DefaultParser;



    /// <summary>
    /// The number of header lines to skip at the beginning of the file before
    /// extracting records. Defaults to 0.
    /// For the common single-header case, use <see cref="HasHeader"/> instead.
    /// </summary>
    /// <remarks>
    /// Lines 1 through <see cref="HeaderLineCount"/> are skipped entirely without
    /// parsing. If <see cref="FieldSeparator"/> is also set, the line immediately
    /// after the last header line is additionally skipped as a separator line.
    /// </remarks>
    /// <example>
    /// <code>
    /// // File has two header lines followed by data:
    /// extractor.HeaderLineCount = 2;
    ///
    /// // Equivalent shorthand for the common single-header case:
    /// extractor.HasHeader = true;
    /// </code>
    /// </example>
    public int HeaderLineCount { get; set; }



    /// <summary>
    /// Convenience property — when set to <see langword="true"/>, sets
    /// <see cref="HeaderLineCount"/> to 1. When set to <see langword="false"/>,
    /// sets <see cref="HeaderLineCount"/> to 0.
    /// Returns <see langword="true"/> if <see cref="HeaderLineCount"/> is greater than zero.
    /// Mirrors <see cref="FixedWidthLoader{TRecord}.WriteHeader"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// // Skip one header line before reading records:
    /// extractor.HasHeader = true;
    ///
    /// // Skip one header line and one separator line:
    /// extractor.HasHeader    = true;
    /// extractor.FieldSeparator = '-';
    /// </code>
    /// </example>
    public bool HasHeader
    {
        get => HeaderLineCount > 0;
        set => HeaderLineCount = value
            ? 1
            : 0;
    }



    /// <summary>
    /// When non-null, the line immediately following the last header line is treated
    /// as a separator and skipped. Has no effect if <see cref="HeaderLineCount"/> is 0.
    /// The value of the character is not used for parsing — only its presence matters.
    /// Set to <see langword="null"/> (default) for no separator.
    /// Mirrors <see cref="FixedWidthLoader{TRecord}.FieldSeparator"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// extractor.HasHeader      = true;
    /// extractor.FieldSeparator = '-';  // skips a "----------" separator line after the header
    /// extractor.FieldSeparator = null; // no separator line (default)
    /// </code>
    /// </example>
    public char? FieldSeparator { get; set; }



    /// <summary>
    /// The delimiter string present between fields in the source file, or
    /// <see langword="null"/> (default) for pure fixed-width input with no delimiter.
    /// Must match the <see cref="FixedWidthLoader{TRecord}.FieldDelimiter"/>
    /// used when the file was written.
    /// </summary>
    /// <remarks>
    /// When set, the extractor accounts for the delimiter width when calculating
    /// field start positions, ensuring each field is read from the correct offset.
    /// </remarks>
    /// <example>
    /// <code>
    /// // File was written with FieldDelimiter = " | " — set the same value on the extractor:
    /// extractor.FieldDelimiter = " | ";
    ///
    /// // Pure fixed-width file with no delimiter (default):
    /// extractor.FieldDelimiter = null;
    /// </code>
    /// </example>
    public string? FieldDelimiter { get; set; }



    /// <summary>
    /// The 1-based physical line number of the line most recently read from the file.
    /// Updated before each line is parsed so that if an exception is thrown,
    /// this value points to the offending line. Matches the line number shown
    /// in a text editor — no adjustment is needed for header or separator lines.
    /// </summary>
    /// <remarks>
    /// Thread-safe: reads are performed with <see cref="Interlocked"/>
    /// so this property may be sampled from a progress-reporting timer thread
    /// without a data race.
    /// </remarks>
    public long CurrentLineNumber => Interlocked.Read(ref _currentLineNumber);



    /// <summary>
    /// The number of parsed records rejected so far: records discarded via
    /// <see cref="Enums.MalformedLineHandling.Skip"/> and records rejected by
    /// <see cref="RecordValidator"/>. Distinct from
    /// <see cref="ExtractorBase{TRecord,FixedWidthReport}.CurrentSkippedItemCount"/>
    /// (the <c>SkipItemCount</c> pagination budget).
    /// </summary>
    public int CurrentRejectedItemCount => Volatile.Read(ref _currentRejectedItemCount);



    /// <summary>
    /// The number of physical lines read that did not produce a record and were
    /// neither skipped by the budget nor rejected: header lines, the separator line,
    /// blank lines dropped per <see cref="BlankLineHandling"/>, lines dropped by
    /// <see cref="LineFilter"/>, and the line that triggered early termination. With
    /// <see cref="CurrentLineNumber"/> this closes the line accounting:
    /// <c>CurrentLineNumber = CurrentItemCount + CurrentSkippedItemCount +
    /// CurrentRejectedItemCount + CurrentFilteredLineCount</c>.
    /// </summary>
    public int CurrentFilteredLineCount => Volatile.Read(ref _currentFilteredLineCount);



    private void IncrementRejectedItemCount() => Interlocked.Increment(ref _currentRejectedItemCount);



    private void IncrementFilteredLineCount() => Interlocked.Increment(ref _currentFilteredLineCount);



    /// <summary>
    /// Creates a progress report snapshot for the current extractor state.
    /// </summary>
    /// <returns>
    /// A <see cref="FixedWidthReport"/> snapshot containing
    /// <see cref="ExtractorBase{TRecord,FixedWidthReport}.CurrentItemCount"/>,
    /// <see cref="ExtractorBase{TRecord,FixedWidthReport}.CurrentSkippedItemCount"/>,
    /// <see cref="CurrentRejectedItemCount"/>, <see cref="CurrentFilteredLineCount"/>,
    /// and <see cref="CurrentLineNumber"/> at the moment of the call.
    /// </returns>
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
    /// Releases the internal <see cref="StreamReader"/> when this instance was
    /// constructed from a <see cref="Stream"/>, then defers to the base class.
    /// Has no effect on a caller-owned <see cref="TextReader"/>.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when called from <see cref="System.IDisposable"/>;
    /// <see langword="false"/> when called from a finalizer.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsReader)
        {
            _reader.Dispose();
        }

        base.Dispose(disposing);
    }



#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
#pragma warning disable MA0051 // async iterator methods cannot delegate 'yield return' to sub-methods
#pragma warning disable CS1998 // async method lacks 'await' — intentionally synchronous; see comment below
    protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync([EnumeratorCancellation] CancellationToken token)
#else
    /// <inheritdoc/>
#pragma warning disable MA0051 // async iterator methods cannot delegate 'yield return' to sub-methods
#pragma warning disable CS1998 // async method lacks 'await' — intentionally synchronous; see comment below
    protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
#endif
#pragma warning restore CS1998
#pragma warning restore MA0051
    {
        // Use synchronous ReadLine to avoid async state machine overhead per line.
        // The TextReader/StreamReader already buffers internally, so async I/O adds
        // cost without benefit for file-based and memory-based streams.
        // The method remains async IAsyncEnumerable as required by the base class.

        var fieldMap = FieldMap.GetResult<TRecord>();
        long dataLinesSkipped = 0;
        var separatorLineNo = HeaderLineCount > 0 && FieldSeparator.HasValue
            ? HeaderLineCount + 1
            : -1;

        // Metrics (#30): records the operation duration when the enumerator completes, ends early, or
        // throws; the per-item/line counters below are no-ops unless a MeterListener is subscribed.
        var metricTags = FixedWidthMetrics.CreateTags(FixedWidthMetrics.ExtractOperation, typeof(TRecord));
        using var operationScope = FixedWidthMetrics.MeasureDuration(metricTags);

        LogExtractionStarted(fieldMap);

        token.ThrowIfCancellationRequested();

        string? line;
#pragma warning disable CA1849, VSTHRD103, S6966 // ReadLine is intentionally synchronous — see comment above
        while ((line = _reader.ReadLine()) != null)
#pragma warning restore CA1849, VSTHRD103, S6966
        {
            token.ThrowIfCancellationRequested();

            // Update before any processing so that if an exception is thrown,
            // CurrentLineNumber points to the offending line in the file.
            Interlocked.Increment(ref _currentLineNumber);
            FixedWidthMetrics.LinesRead.Add(1, metricTags);

            if (IsStructuralLine(separatorLineNo))
            {
                IncrementFilteredLineCount();
                LogDebugStructuralLineSkipped();
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                if (!HandleBlankLine(fieldMap, out var defaultRecord))
                {
                    IncrementFilteredLineCount();
                    LogDebugBlankLineSkipped();
                    continue;
                }

                // BlankLineHandling.ReturnDefault — participates in skip/max budgets
                // exactly like a normal data line.
                if (dataLinesSkipped < SkipItemCount)
                {
                    dataLinesSkipped++;
                    IncrementCurrentSkippedItemCount();
                    FixedWidthMetrics.ItemsSkipped.Add(1, metricTags);
                    LogDebugBlankLineInSkipBudget(dataLinesSkipped);
                    continue;
                }

                if (CurrentItemCount >= MaximumItemCount)
                {
                    IncrementFilteredLineCount();
                    LogDebugMaxReached();
                    LogExtractionCompleted();
                    yield break;
                }

                LogDebugBlankLineYieldedAsDefault();
                IncrementCurrentItemCount();
                FixedWidthMetrics.ItemsExtracted.Add(1, metricTags);
                yield return defaultRecord;
                continue;
            }

            var filterAction = LineFilter(line);
            if (filterAction == LineAction.Stop)
            {
                IncrementFilteredLineCount();
                LogDebugLineFilterStop();
                LogExtractionCompleted();
                yield break;
            }
            if (filterAction == LineAction.Skip)
            {
                IncrementFilteredLineCount();
                LogDebugLineFilterSkip();
                continue;
            }

            if (dataLinesSkipped < SkipItemCount)
            {
                dataLinesSkipped++;
                IncrementCurrentSkippedItemCount();
                FixedWidthMetrics.ItemsSkipped.Add(1, metricTags);
                LogDebugDataLineSkipped(dataLinesSkipped);
                continue;
            }

            if (CurrentItemCount >= MaximumItemCount)
            {
                IncrementFilteredLineCount();
                LogDebugMaxReached();
                LogExtractionCompleted();
                yield break;
            }

            if (!TryParseLine(line, fieldMap, out var record))
            {
                continue;
            }

            var validation = ApplyRecordValidation(record);
            if (validation == ValidationAction.Skip)
            {
                continue;
            }
            if (validation == ValidationAction.Stop)
            {
                IncrementFilteredLineCount();
                LogExtractionCompleted();
                yield break;
            }

            LogDebugRecordParsed();
            IncrementCurrentItemCount();
            FixedWidthMetrics.ItemsExtracted.Add(1, metricTags);
            yield return record;
        }

        LogExtractionCompleted();
    }



    /// <summary>
    /// Runs the optional <see cref="RecordValidator"/> against a parsed record.
    /// For <see cref="ValidationAction.Skip"/> it increments the rejected count and
    /// logs; the caller performs the actual skip/stop control flow.
    /// </summary>
    private ValidationAction ApplyRecordValidation(TRecord record)
    {
        if (RecordValidator == null)
        {
            return ValidationAction.Accept;
        }

        var result = RecordValidator(record);
        switch (result.Action)
        {
            case ValidationAction.Skip:
                IncrementRejectedItemCount();
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug
                    (
                        "Record at line {LineNumber} skipped by validator: {Reason}",
                        _currentLineNumber,
                        result.Reason ?? "(no reason given)"
                    );
                }
                return ValidationAction.Skip;

            case ValidationAction.Stop:
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug
                    (
                        "Extraction stopped by validator at line {LineNumber}: {Reason}",
                        _currentLineNumber,
                        result.Reason ?? "(no reason given)"
                    );
                }
                return ValidationAction.Stop;

            default:
                return ValidationAction.Accept;
        }
    }



    // ------------------------------------------------------------------
    // Logging helpers
    // ------------------------------------------------------------------

    private void LogExtractionStarted(FieldMapResult fieldMap)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Extraction started for {RecordType}. HeaderLineCount={HeaderLineCount}, " +
                "FieldSeparator={FieldSeparator}, FieldDelimiter={FieldDelimiter}, " +
                "SkipItemCount={SkipItemCount}, MaximumItemCount={MaximumItemCount}",
                typeof(TRecord).Name,
                HeaderLineCount,
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



    private void LogExtractionCompleted()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation
            (
                "Extraction completed for {RecordType}: {ItemCount} items extracted, " +
                "{SkippedCount} skipped, {LineCount} lines read",
                typeof(TRecord).Name,
                CurrentItemCount,
                CurrentSkippedItemCount,
                Interlocked.Read(ref _currentLineNumber)
            );
        }
    }



    private void LogDebugStructuralLineSkipped()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Skipping structural line {LineNumber} (header or separator)",
                _currentLineNumber
            );
        }
    }



    private void LogDebugBlankLineSkipped()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Blank line at line {LineNumber} skipped (BlankLineHandling=Skip)",
                _currentLineNumber
            );
        }
    }



    private void LogDebugBlankLineInSkipBudget(long dataLinesSkipped)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Blank line at line {LineNumber} counted toward skip budget " +
                "({DataLinesSkipped}/{SkipItemCount})",
                _currentLineNumber,
                dataLinesSkipped,
                SkipItemCount
            );
        }
    }



    private void LogDebugBlankLineYieldedAsDefault()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Blank line at line {LineNumber} yielded as default {RecordType} " +
                "(BlankLineHandling=ReturnDefault, item #{ItemCount})",
                _currentLineNumber,
                typeof(TRecord).Name,
                CurrentItemCount + 1
            );
        }
    }



    private void LogDebugMaxReached()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "MaximumItemCount ({MaximumItemCount}) reached at line {LineNumber}, " +
                "stopping extraction",
                MaximumItemCount,
                _currentLineNumber
            );
        }
    }



    private void LogDebugLineFilterStop()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "LineFilter returned Stop at line {LineNumber}, ending extraction",
                _currentLineNumber
            );
        }
    }



    private void LogDebugLineFilterSkip()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "LineFilter returned Skip at line {LineNumber}",
                _currentLineNumber
            );
        }
    }



    private void LogDebugDataLineSkipped(long dataLinesSkipped)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Skipping data line {LineNumber} ({DataLinesSkipped}/{SkipItemCount})",
                _currentLineNumber,
                dataLinesSkipped,
                SkipItemCount
            );
        }
    }



    private void LogDebugRecordParsed()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                "Parsed record at line {LineNumber} (item #{ItemCount})",
                _currentLineNumber,
                CurrentItemCount + 1
            );
        }
    }



    private void LogMalformedLine(MalformedLineException ex)
    {
        if (MalformedLineHandling == MalformedLineHandling.ThrowException)
        {
            _logger.LogError
            (
                ex,
                "Malformed line {LineNumber}: {ErrorMessage}",
                _currentLineNumber,
                ex.Message
            );
        }
        else if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug
            (
                ex,
                "Malformed line {LineNumber} handled with {MalformedLineHandling}",
                _currentLineNumber,
                MalformedLineHandling
            );
        }
    }



    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns <see langword="true"/> if the current line is a header or separator
    /// line that should be skipped without any further processing.
    /// </summary>
    private bool IsStructuralLine(long separatorLineNo)
    {
        return _currentLineNumber <= HeaderLineCount
            || _currentLineNumber == separatorLineNo;
    }


    /// <summary>
    /// Creates a default <typeparamref name="TRecord"/> instance via the field map's
    /// compiled factory. Used by the <see cref="BlankLineHandling.ReturnDefault"/> and
    /// <see cref="MalformedLineHandling.ReturnDefault"/> paths.
    /// </summary>
    private static TRecord CreateDefaultRecord(FieldMapResult fieldMap) => (TRecord)fieldMap.Factory();



    /// <summary>
    /// Handles a blank line according to <see cref="BlankLineHandling"/>.
    /// Returns <see langword="true"/> when a default record should be yielded,
    /// passing it back via <paramref name="defaultRecord"/>.
    /// Returns <see langword="false"/> when the line should be silently skipped.
    /// Throws <see cref="LineTooShortException"/> when the policy is
    /// <see cref="BlankLineHandling.ThrowException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BlankLineHandling"/> holds an unrecognized value.
    /// </exception>
    /// <exception cref="LineTooShortException">
    /// Thrown when <see cref="BlankLineHandling"/> is
    /// <see cref="BlankLineHandling.ThrowException"/> (the default).
    /// </exception>
    private bool HandleBlankLine(FieldMapResult fieldMap, out TRecord defaultRecord)
    {
        defaultRecord = default!;

        switch (BlankLineHandling)
        {
            case BlankLineHandling.Skip:
                return false;

            case BlankLineHandling.ReturnDefault:
                defaultRecord = CreateDefaultRecord(fieldMap);
                return true;

            case BlankLineHandling.ThrowException:
                var delimiterWidth = string.IsNullOrEmpty(FieldDelimiter) ? 0 : FieldDelimiter!.Length;
                var delimiterCount = Math.Max(0, fieldMap.TotalColumnCount - 1);
                var expectedWidth = fieldMap.ExpectedLineWidth + delimiterWidth * delimiterCount;

                var ex = new LineTooShortException
                (
                    $"Blank line encountered at line {_currentLineNumber}.",
                    _currentLineNumber,
                    string.Empty,
                    expectedWidth,
                    0
                );

                _logger.LogError
                (
                    ex,
                    "Blank line at line {LineNumber}. Expected width {ExpectedWidth}, got 0",
                    _currentLineNumber,
                    expectedWidth
                );

                throw ex;
            default:
                throw new InvalidOperationException
                (
                    $"Unexpected {nameof(BlankLineHandling)} value: {BlankLineHandling}"
                );
        }
    }



    /// <summary>
    /// Attempts to parse <paramref name="line"/> into a <typeparamref name="TRecord"/>.
    /// Returns <see langword="true"/> and sets <paramref name="record"/> on success.
    /// Returns <see langword="false"/> when <see cref="MalformedLineHandling"/> is
    /// <see cref="MalformedLineHandling.Skip"/> and the line cannot be parsed.
    /// Sets a default record and returns <see langword="true"/> when the policy
    /// is <see cref="MalformedLineHandling.ReturnDefault"/> (the caller performs
    /// the yield).
    /// Re-throws on <see cref="MalformedLineHandling.ThrowException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="MalformedLineHandling"/> holds an unrecognized value.
    /// </exception>
    private bool TryParseLine(string line, FieldMapResult fieldMap, out TRecord record)
    {
        record = default!;
        try
        {
            record = FixedWidthLineParser.ParseLine<TRecord>
            (
                line,
                _currentLineNumber,
                fieldMap,
                FieldDelimiter,
                ValueParser
            );
            return true;
        }
        catch (MalformedLineException ex)
        {
            LogMalformedLine(ex);

            switch (MalformedLineHandling)
            {
                case MalformedLineHandling.Skip:
                    IncrementRejectedItemCount();
                    return false;

                case MalformedLineHandling.ReturnDefault:
                    // Cannot yield inside catch — caller handles the yield and increment.
                    record = CreateDefaultRecord(fieldMap);
                    return true;

                case MalformedLineHandling.ThrowException:
                    throw;

                default:
                    throw new InvalidOperationException
                    (
                        $"Unexpected {nameof(MalformedLineHandling)} value: {MalformedLineHandling}"
                    );
            }
        }
    }
}
