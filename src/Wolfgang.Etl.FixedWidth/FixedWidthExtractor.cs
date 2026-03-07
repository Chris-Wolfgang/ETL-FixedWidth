using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Wolfgang.Etl.Abstractions;
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
/// <typeparam name="TProgress">
/// The type of the progress object reported during extraction.
/// Override <see cref="CreateProgressReport"/> to return an instance of this type.
/// If you do not need a custom progress type, use <see cref="FixedWidthReport"/>.
/// </typeparam>
/// <example>
/// <code>
/// // For most cases, no subclassing is needed — FixedWidthReport is supported directly:
/// var extractor = new FixedWidthExtractor&lt;CustomerRecord, FixedWidthReport&gt;(reader);
///
/// // To use a fully custom progress type, subclass and override CreateProgressReport:
/// public class CustomerExtractor : FixedWidthExtractor&lt;CustomerRecord, MyProgress&gt;
/// {
///     public CustomerExtractor(string filePath)
///         : base(new StreamReader(filePath)) { }
///
///     protected override MyProgress CreateProgressReport() =>
///         new MyProgress(CurrentItemCount, CurrentSkippedItemCount);
/// }
/// </code>
/// </example>
public class FixedWidthExtractor<TRecord, TProgress> : ExtractorBase<TRecord, TProgress>
    where TRecord : new()
    where TProgress : notnull
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    private readonly TextReader _reader;
    private long _currentLineNumber;

    // _currentLineNumber is read by CreateProgressReport on a Timer threadpool thread
    // and written by ExtractWorkerAsync on the async continuation thread.
    // Interlocked.Read/Increment ensures atomicity on all targets including 32-bit net462.


    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    /// <summary>
    /// Initializes a new <see cref="FixedWidthExtractor{TRecord,TProgress}"/> that reads
    /// from the specified <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader"/> to read fixed-width records from. This can be a
    /// <see cref="StreamReader"/> wrapping a file or network stream, a
    /// <see cref="StringReader"/> for in-memory content, or any other <see cref="TextReader"/>
    /// implementation. The caller is responsible for the reader's lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    public FixedWidthExtractor(TextReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
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
    /// in the file. Evaluated before the skip budget and <see cref="ExtractorBase{TRecord,TProgress}.MaximumItemCount"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><see cref="BlankLineHandling.ThrowException"/> (default) — always throws
    ///   a <see cref="Exceptions.LineTooShortException"/> regardless of position.</item>
    ///   <item><see cref="BlankLineHandling.Skip"/> — the line is invisible to all counting
    ///   logic. Does not count toward <see cref="ExtractorBase{TRecord,TProgress}.SkipItemCount"/>
    ///   or <see cref="ExtractorBase{TRecord,TProgress}.MaximumItemCount"/>.</item>
    ///   <item><see cref="BlankLineHandling.ReturnDefault"/> — a default
    ///   <typeparamref name="TRecord"/> instance is yielded. Counts toward the skip budget
    ///   if within <see cref="ExtractorBase{TRecord,TProgress}.SkipItemCount"/>, otherwise
    ///   counts toward <see cref="ExtractorBase{TRecord,TProgress}.MaximumItemCount"/>.</item>
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
    /// Evaluated before the skip budget and <see cref="ExtractorBase{TRecord,TProgress}.MaximumItemCount"/>.
    /// Both <see cref="LineAction.Skip"/> and <see cref="LineAction.Stop"/> are invisible
    /// to all counting logic — they do not affect <see cref="ExtractorBase{TRecord,TProgress}.SkipItemCount"/>,
    /// <see cref="ExtractorBase{TRecord,TProgress}.MaximumItemCount"/>, or
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
    /// A delegate that converts a raw string read from the file into the target property
    /// type. The <see cref="FieldContext"/> provides the property type, format string, and
    /// other field metadata needed to perform the conversion.
    /// Defaults to <see cref="FixedWidthConverter.DefaultParser"/>.
    /// Mirrors <see cref="FixedWidthLoader{TRecord,TProgress}.ValueConverter"/>.
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
    ///         ? (object)(text.Trim() == "Y")
    ///         : FixedWidthConverter.DefaultParser(text, ctx);
    ///
    /// // Parse a custom date format for a specific field:
    /// extractor.ValueParser = (text, ctx) =>
    ///     ctx.PropertyName == "BirthDate"
    ///         ? DateTime.ParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture)
    ///         : FixedWidthConverter.DefaultParser(text, ctx);
    /// </code>
    /// </example>
    public Func<string, FieldContext, object> ValueParser { get; set; } = FixedWidthConverter.DefaultParser;



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
    /// Mirrors <see cref="FixedWidthLoader{TRecord,TProgress}.WriteHeader"/>.
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
    /// Mirrors <see cref="FixedWidthLoader{TRecord,TProgress}.FieldSeparator"/>.
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
    /// Must match the <see cref="FixedWidthLoader{TRecord,TProgress}.FieldDelimiter"/>
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
    /// Thread-safe: reads are performed with <see cref="Interlocked.Read(ref long)"/>
    /// so this property may be sampled from a progress-reporting timer thread
    /// without a data race.
    /// </remarks>
    public long CurrentLineNumber => Interlocked.Read(ref _currentLineNumber);

    /// <summary>
    /// Creates a progress report snapshot for the current extractor state.
    /// Override in a derived class to return a custom <typeparamref name="TProgress"/>
    /// instance. The default implementation returns a <see cref="FixedWidthReport"/>
    /// when <typeparamref name="TProgress"/> is <see cref="FixedWidthReport"/> or the
    /// base <see cref="Report"/>, and throws <see cref="NotSupportedException"/> otherwise.
    /// </summary>
    /// <returns>
    /// A <typeparamref name="TProgress"/> snapshot containing
    /// <see cref="ExtractorBase{TRecord,TProgress}.CurrentItemCount"/>,
    /// <see cref="ExtractorBase{TRecord,TProgress}.CurrentSkippedItemCount"/>,
    /// and <see cref="CurrentLineNumber"/> at the moment of the call.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <typeparamref name="TProgress"/> is not <see cref="FixedWidthReport"/>
    /// or <see cref="Report"/> and <see cref="CreateProgressReport"/> has not been overridden.
    /// </exception>
    /// <example>
    /// <code>
    /// // To use a custom progress type, subclass and override:
    /// public class MyExtractor : FixedWidthExtractor&lt;MyRecord, MyProgress&gt;
    /// {
    ///     public MyExtractor(TextReader reader) : base(reader) { }
    ///
    ///     protected override MyProgress CreateProgressReport() =>
    ///         new MyProgress(CurrentItemCount, CurrentLineNumber);
    /// }
    /// </code>
    /// </example>
    protected override TProgress CreateProgressReport()
    {
        if (typeof(TProgress) == typeof(FixedWidthReport) || typeof(TProgress) == typeof(Report))
        {
            return (TProgress)(object)new FixedWidthReport
            (
                CurrentItemCount,
                CurrentSkippedItemCount,
                Interlocked.Read(ref _currentLineNumber)
            );
        }

        throw new NotSupportedException
        (
            $"Override {nameof(CreateProgressReport)} to supply a " +
            $"{typeof(TProgress).Name} instance."
        );
    }



    /// <summary>
    /// Returns a snapshot progress report. Visible to the test assembly via InternalsVisibleTo.
    /// </summary>
    internal TProgress GetProgressReport() => CreateProgressReport();



#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc/>
#pragma warning disable MA0051 // async iterator methods cannot delegate 'yield return' to sub-methods
    protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync([EnumeratorCancellation] CancellationToken token)
#else
    /// <inheritdoc/>
#pragma warning disable MA0051 // async iterator methods cannot delegate 'yield return' to sub-methods
    protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
#endif
#pragma warning restore MA0051
    {
        var fieldMap = FieldMap.GetResult<TRecord>();
        long dataLinesSkipped = 0;
        var separatorLineNo = HeaderLineCount > 0 && FieldSeparator.HasValue
            ? HeaderLineCount + 1
            : -1;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        while (await _reader.ReadLineAsync(token).ConfigureAwait(false) is { } line)
#else
        while (await _reader.ReadLineAsync().ConfigureAwait(false) is { } line)
#endif
        {
            token.ThrowIfCancellationRequested();

            // Update before any processing so that if an exception is thrown,
            // CurrentLineNumber points to the offending line in the file.
            Interlocked.Increment(ref _currentLineNumber);

            if (IsStructuralLine(separatorLineNo))
            {
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                if (!HandleBlankLine(fieldMap, out var defaultRecord))
                {
                    // BlankLineHandling.Skip — invisible to all counting logic.
                    continue;
                }

                // BlankLineHandling.ReturnDefault — participates in skip/max budgets
                // exactly like a normal data line.
                if (dataLinesSkipped < SkipItemCount)
                {
                    dataLinesSkipped++;
                    IncrementCurrentSkippedItemCount();
                    continue;
                }

                if (CurrentItemCount >= MaximumItemCount)
                {
                    yield break;
                }

                IncrementCurrentItemCount();
                yield return defaultRecord;
                continue;
            }

            var filterAction = LineFilter(line);
            if (filterAction == LineAction.Stop)
            {
                yield break;
            }
            if (filterAction == LineAction.Skip)
            {
                continue;
            }

            if (dataLinesSkipped < SkipItemCount)
            {
                dataLinesSkipped++;
                IncrementCurrentSkippedItemCount();
                continue;
            }

            if (CurrentItemCount >= MaximumItemCount)
            {
                yield break;
            }

            if (!TryParseLine(line, fieldMap, out var record))
            {
                continue;
            }

            IncrementCurrentItemCount();
            yield return record;
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
    /// Handles a blank line according to <see cref="BlankLineHandling"/>.
    /// Returns <see langword="true"/> when a default record should be yielded,
    /// passing it back via <paramref name="defaultRecord"/>.
    /// Returns <see langword="false"/> when the line should be silently skipped.
    /// Throws <see cref="LineTooShortException"/> when the policy is
    /// <see cref="BlankLineHandling.ThrowException"/>.
    /// </summary>
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
                defaultRecord = new TRecord();
                return true;

            default:
                throw new LineTooShortException
                (
                    $"Blank line encountered at line {_currentLineNumber}.",
                    _currentLineNumber,
                    string.Empty,
                    fieldMap.ExpectedLineWidth,
                    0
                );
        }
    }



    /// <summary>
    /// Attempts to parse <paramref name="line"/> into a <typeparamref name="TRecord"/>.
    /// Returns <see langword="true"/> and sets <paramref name="record"/> on success.
    /// Returns <see langword="false"/> when <see cref="MalformedLineHandling"/> is
    /// <see cref="MalformedLineHandling.Skip"/> and the line cannot be parsed.
    /// Yields a default record and returns <see langword="false"/> when the policy
    /// is <see cref="MalformedLineHandling.ReturnDefault"/>.
    /// Re-throws on <see cref="MalformedLineHandling.ThrowException"/>.
    /// </summary>
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
        catch (MalformedLineException)
        {
            switch (MalformedLineHandling)
            {
                case MalformedLineHandling.Skip:
                    IncrementCurrentSkippedItemCount();
                    return false;

                case MalformedLineHandling.ReturnDefault:
                    // Cannot yield inside catch — caller handles the yield.
                    IncrementCurrentItemCount();
                    record = new TRecord();
                    return true;

                default:
                    throw;
            }
        }
    }
}
