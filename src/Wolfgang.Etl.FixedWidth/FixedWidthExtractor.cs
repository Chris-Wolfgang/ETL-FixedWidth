using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;

namespace Wolfgang.Etl.FixedWidth
{
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
        /// in the file. Evaluated before the skip budget and <see cref="MaximumItemCount"/>.
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
        /// <example>
        /// <code>
        /// // Custom parser — treat "Y"/"N" as bool
        /// extractor.ValueParser = (text, ctx) =>
        ///     ctx.PropertyType == typeof(bool) ? (object)(text.Trim() == "Y") :
        ///     FixedWidthConverter.DefaultParser(text, ctx);
        /// </code>
        /// </example>
        public Func<string, FieldContext, object> ValueParser { get; set; } = FixedWidthConverter.DefaultParser;



        /// <summary>
        /// The number of header lines to skip at the beginning of the file before
        /// extracting records. Defaults to 0.
        /// For the common single-header case, use <see cref="HasHeader"/> instead.
        /// </summary>
        public int HeaderLineCount { get; set; } = 0;



        /// <summary>
        /// Convenience property — when set to <see langword="true"/>, sets
        /// <see cref="HeaderLineCount"/> to 1. When set to <see langword="false"/>,
        /// sets <see cref="HeaderLineCount"/> to 0.
        /// Returns <see langword="true"/> if <see cref="HeaderLineCount"/> is greater than zero.
        /// Mirrors <see cref="FixedWidthLoader{TRecord,TProgress}.WriteHeader"/>.
        /// </summary>
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
        public char? FieldSeparator { get; set; } = null;



        /// <summary>
        /// The delimiter string written between fields by the loader, or <see langword="null"/>
        /// (default) for pure fixed-width input with no delimiter. Must match the
        /// <see cref="FixedWidthLoader{TRecord,TProgress}.FieldDelimiter"/> used when the
        /// file was written.
        /// </summary>
        public string FieldDelimiter { get; set; } = null;



        /// <summary>
        /// The 1-based physical line number of the line most recently read from the file.
        /// Updated before each line is parsed so that if an exception is thrown,
        /// this value points to the offending line. Matches the line number shown
        /// in a text editor — no adjustment is needed for header or separator lines.
        /// </summary>
        public long CurrentLineNumber => Interlocked.Read(ref _currentLineNumber);

        /// <summary>
        /// Creates a progress report. Override in a derived class to return a custom
        /// <typeparamref name="TProgress"/> instance. The default implementation returns a
        /// <see cref="FixedWidthReport"/> if <typeparamref name="TProgress"/> is
        /// <see cref="FixedWidthReport"/> or the base <see cref="Report"/>, and throws
        /// <see cref="NotImplementedException"/> otherwise.
        /// </summary>
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

            throw new NotImplementedException( $"Override {nameof(CreateProgressReport)} to supply a {typeof(TProgress).Name} instance.");
        }



        /// <summary>
        /// Returns a snapshot progress report. Visible to the test assembly via InternalsVisibleTo.
        /// </summary>
        internal TProgress GetProgressReport() => CreateProgressReport();



#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
        protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync( [EnumeratorCancellation] CancellationToken token)
#else
        protected override async IAsyncEnumerable<TRecord> ExtractWorkerAsync(CancellationToken token)
#endif
        {
            var fieldMap = FieldMap.GetResult<TRecord>();
            long dataLinesSkipped = 0;
            var separatorLineNo = HeaderLineCount > 0 && FieldSeparator.HasValue
                ? HeaderLineCount + 1
                : -1;

            string line;
            while ((line = await _reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                token.ThrowIfCancellationRequested();

                // Update before any processing so that if an exception is thrown,
                // CurrentLineNumber points to the offending line in the file.
                Interlocked.Increment(ref _currentLineNumber);

                // Skip header lines.
                if (_currentLineNumber <= HeaderLineCount)
                {
                    continue;
                }

                // Skip the separator line that immediately follows the header.
                if (_currentLineNumber == separatorLineNo)
                {
                    continue;
                }

                // Handle blank lines before any counting logic.
                // - ThrowException: always throws regardless of position in file.
                // - Skip: line is invisible — does not count toward skip budget or MaximumItemCount.
                // - ReturnDefault: counts toward skip budget or MaximumItemCount depending on position.
                if (string.IsNullOrEmpty(line))
                {
                    switch (BlankLineHandling)
                    {
                        case BlankLineHandling.Skip:
                            continue;

                        case BlankLineHandling.ReturnDefault:
                            // Falls through to counting logic below.
                            break;

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



                // Apply user-defined line filter. Skip and Stop do not count toward
                // skip budget, MaximumItemCount, or CurrentSkippedItemCount —
                // the line is treated as invisible.
                if (!string.IsNullOrEmpty(line))
                {
                    var action = LineFilter(line);
                    if (action == LineAction.Stop)
                    {
                        yield break;
                    }
                    if (action == LineAction.Skip)
                    {
                        continue;
                    }
                }



                // Skip the first SkipItemCount data lines. Blank lines with
                // ReturnDefault count toward the skip budget.
                if (dataLinesSkipped < SkipItemCount)
                {
                    dataLinesSkipped++;
                    IncrementCurrentSkippedItemCount();
                    continue;
                }



                // Stop when MaximumItemCount is reached.
                if (CurrentItemCount >= MaximumItemCount)
                {
                    yield break;
                }

                // If the line is blank and ReturnDefault, yield a default record.
                if (string.IsNullOrEmpty(line))
                {
                    IncrementCurrentItemCount();
                    yield return new TRecord();
                    continue;
                }



                // Parse the line.
                TRecord record = default!;
                bool malformedReturnDefault = false;
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
                }
                catch (MalformedLineException)
                {
                    switch (MalformedLineHandling)
                    {
                        case MalformedLineHandling.Skip:
                            IncrementCurrentSkippedItemCount();
                            continue;

                        case MalformedLineHandling.ReturnDefault:
                            malformedReturnDefault = true;
                            break;

                        default:
                            throw;
                    }
                }



                // Cannot yield inside a catch block — handle ReturnDefault after the try/catch.
                if (malformedReturnDefault)
                {
                    IncrementCurrentItemCount();
                    yield return new TRecord();
                    continue;
                }

                IncrementCurrentItemCount();
                yield return record;
            }
        }
    }
}
