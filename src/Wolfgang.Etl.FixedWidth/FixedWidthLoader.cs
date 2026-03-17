using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// Writes records of type <typeparamref name="TRecord"/> to a fixed-width text stream
/// as an asynchronous operation.
/// </summary>
/// <remarks>
/// <para>
/// The loader writes to any <see cref="TextWriter"/>, making it suitable for files,
/// in-memory strings, network streams, and console output:
/// </para>
/// <code>
/// // Write to a file
/// await using var writer = new StreamWriter("output.txt");
/// var loader = new FixedWidthLoader&lt;MyRecord, Report&gt;(writer);
///
/// // Write to a string (useful for testing or building string output)
/// var sw = new StringWriter();
/// var loader = new FixedWidthLoader&lt;MyRecord, Report&gt;(sw);
/// var result = sw.ToString();
///
/// // Write a formatted table to the console
/// var loader = new FixedWidthLoader&lt;MyRecord, Report&gt;(Console.Out);
/// loader.WriteHeader    = true;
/// loader.FieldSeparator = '-';
/// loader.FieldDelimiter = " | ";
/// </code>
/// <para>
/// The caller owns the <see cref="TextWriter"/> lifetime. The loader does not dispose it.
/// </para>
/// </remarks>
public class FixedWidthLoader<TRecord, TProgress> : LoaderBase<TRecord, TProgress>
    where TRecord : notnull
    where TProgress : notnull
{
    // ------------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------------

    private readonly TextWriter _writer;
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
    /// Initializes a new <see cref="FixedWidthLoader{TRecord,TProgress}"/> that writes
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
    }



    /// <summary>
    /// Initializes a new <see cref="FixedWidthLoader{TRecord,TProgress}"/> that writes
    /// to the specified <see cref="TextWriter"/> and uses the supplied
    /// <see cref="IProgressTimer"/> instead of the default system timer.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter"/> to write fixed-width records to.
    /// </param>
    /// <param name="timer">
    /// The <see cref="IProgressTimer"/> to use for progress reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="writer"/> or <paramref name="timer"/> is null.
    /// </exception>
    internal FixedWidthLoader(TextWriter writer, IProgressTimer timer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _progressTimer = timer ?? throw new ArgumentNullException(nameof(timer));
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
    /// <see cref="FieldContext.FieldLength"/>; otherwise the safety-net inside
    /// <c>FormatSegment</c> will throw a
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



    /// <summary>
    /// When non-null, a separator line is written after the header, consisting of
    /// the specified character repeated to each field's width.
    /// Set to <see langword="null"/> (default) to write no separator.
    /// Has no effect if <see cref="WriteHeader"/> is <see langword="false"/>.
    /// Mirrors <see cref="FixedWidthExtractor{TRecord,TProgress}.FieldSeparator"/>.
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
    /// <see cref="FixedWidthExtractor{TRecord,TProgress}.FieldDelimiter"/> on the
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
    /// Override in a derived class to return a custom <typeparamref name="TProgress"/>
    /// instance. The default implementation returns a <see cref="FixedWidthReport"/>
    /// when <typeparamref name="TProgress"/> is <see cref="FixedWidthReport"/> or the
    /// base <see cref="Report"/>, and throws <see cref="NotSupportedException"/> otherwise.
    /// </summary>
    /// <returns>
    /// A <typeparamref name="TProgress"/> snapshot containing
    /// <see cref="LoaderBase{TRecord,TProgress}.CurrentItemCount"/>,
    /// <see cref="LoaderBase{TRecord,TProgress}.CurrentSkippedItemCount"/>,
    /// and <see cref="CurrentLineNumber"/> at the moment of the call.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <typeparamref name="TProgress"/> is not <see cref="FixedWidthReport"/>
    /// or <see cref="Report"/> and <see cref="CreateProgressReport"/> has not been overridden.
    /// </exception>
    /// <example>
    /// <code>
    /// // To use a custom progress type, subclass and override:
    /// public class MyLoader : FixedWidthLoader&lt;MyRecord, MyProgress&gt;
    /// {
    ///     public MyLoader(TextWriter writer) : base(writer) { }
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



    /// <inheritdoc/>
    protected override IProgressTimer CreateProgressTimer(IProgress<TProgress> progress)
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
    protected override async Task LoadWorkerAsync
    (
        IAsyncEnumerable<TRecord> items,
        CancellationToken token
    )
    {
        var fieldMap = FieldMap.GetResult<TRecord>();

        if (WriteHeader)
        {
            await WriteHeaderAsync(fieldMap).ConfigureAwait(false);
        }

        await foreach (var item in items.WithCancellation(token).ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();

            if (CurrentItemCount >= MaximumItemCount)
            {
                break;
            }

            if (item == null)
            {
                throw new InvalidOperationException( $"A null record was encountered at item {CurrentItemCount + CurrentSkippedItemCount + 1}. " + $"The loader writes what it is given — null records are not permitted.");
            }

            if (CurrentSkippedItemCount < SkipItemCount)
            {
                IncrementCurrentSkippedItemCount();
                continue;
            }

            var segments = FixedWidthLineParser.FormatSegments
            (
                item,
                fieldMap,
                ValueConverter
            );
            Interlocked.Increment(ref _currentLineNumber);
            await _writer.WriteLineAsync(Join(segments)).ConfigureAwait(false);
            IncrementCurrentItemCount();
        }
    }



    /// <summary>
    /// Writes the header line and, if <see cref="FieldSeparator"/> is set, the
    /// separator line that follows it.
    /// </summary>
    private async Task WriteHeaderAsync(FieldMapResult fieldMap)
    {
        var headerSegments = FixedWidthLineParser.FormatHeaderSegments
        (
            fieldMap,
            HeaderConverter
        );
        Interlocked.Increment(ref _currentLineNumber);
        await _writer.WriteLineAsync(Join(headerSegments)).ConfigureAwait(false);

        if (FieldSeparator.HasValue)
        {
            var separatorSegments = FixedWidthLineParser.FormatSeparatorSegments
            (
                fieldMap,
                FieldSeparator.Value
            );
            Interlocked.Increment(ref _currentLineNumber);
            await _writer.WriteLineAsync(Join(separatorSegments)).ConfigureAwait(false);
        }
    }



    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Joins field segments with <see cref="FieldDelimiter"/> between them,
    /// or concatenates them directly when no delimiter is set.
    /// </summary>
    private string Join(IReadOnlyList<string> segments)
    {
        if (string.IsNullOrEmpty(FieldDelimiter))
        {
            return string.Concat(segments);
        }

        return string.Join
        (
            FieldDelimiter,
            segments
        );
    }
}
