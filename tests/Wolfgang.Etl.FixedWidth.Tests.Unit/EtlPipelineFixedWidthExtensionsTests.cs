using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Tests for the class-named <see cref="EtlPipelineFixedWidthExtensions"/> source factories and sink
/// terminators that hang fixed-width extraction/loading off the generic <see cref="EtlPipeline"/> chain
/// (issue #253). Pipeline operators are supplied as inline <c>Through</c> stages so the tests take no
/// dependency on the LINQ-flavored operators shipped by <c>Wolfgang.Etl.Transformers</c>. Assertions are
/// newline-agnostic (records are round-tripped, or text is normalized) so they hold on every platform.
/// </summary>
public sealed class EtlPipelineFixedWidthExtensionsTests : IDisposable
{
    private readonly string _tempDir;


    public EtlPipelineFixedWidthExtensionsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "etl-fixedwidth-pipeline-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }


    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leaked handle would surface as its own test failure.
        }
    }


    [Fact]
    public async Task Extractor_from_path_through_two_stages_to_Loader_path_round_trips_records()
    {
        var source = WriteTempFile
        (
            "source.txt",
            Content(("Alice", "Smith", 30), ("Bob", "Jones", 25), ("Carol", "White", 35))
        );
        var target = Path.Combine(_tempDir, "target.txt");

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .Through(FilterAdults)
            .Through(UppercaseLastNames)
            .FixedWidthLoader<PersonRecord>(target)
            .RunAsync();

        var written = await ExtractAllAsync(target);

        Assert.Equal
        (
            new[]
            {
                new PersonRecord { FirstName = "Alice", LastName = "SMITH", Age = 30 },
                new PersonRecord { FirstName = "Carol", LastName = "WHITE", Age = 35 },
            },
            written
        );
    }


    [Fact]
    public async Task Extractor_from_stream_and_Loader_to_writer_leave_caller_resources_open()
    {
        using var sourceStream = StreamOver(Content(("Alice", "Smith", 30)));
        using var targetWriter = new StringWriter();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(sourceStream)
            .FixedWidthLoader<PersonRecord>(targetWriter)
            .RunAsync();

        Assert.Equal("Alice     Smith     030", Normalize(targetWriter.ToString()).TrimEnd('\n'));

        // Caller-owned resources are left open — the stream is still readable and the writer still writable.
        Assert.True(sourceStream.CanRead);
        targetWriter.Write("still-open");
    }


    [Fact]
    public async Task Extractor_from_reader_and_Loader_to_stream_round_trips_and_leaves_stream_open()
    {
        using var reader = ReaderOver(Content(("Alice", "Smith", 30), ("Bob", "Jones", 25)));
        using var targetStream = new MemoryStream();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(reader)
            .FixedWidthLoader<PersonRecord>(targetStream)
            .Encoding(Utf8NoBom)
            .RunAsync();

        Assert.True(targetStream.CanWrite, "the loader must leave the caller-owned stream open");
        Assert.Equal
        (
            "Alice     Smith     030\nBob       Jones     025",
            Normalize(Utf8NoBom.GetString(targetStream.ToArray())).TrimEnd('\n')
        );
    }


    [Fact]
    public async Task Extractor_from_existing_instance_runs_and_is_not_disposed_by_the_pipeline()
    {
        using var reader = ReaderOver(Content(("Alice", "Smith", 30)));
        var extractor = new FixedWidthExtractor<PersonRecord>(reader);
        using var targetWriter = new StringWriter();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(extractor)
            .FixedWidthLoader<PersonRecord>(targetWriter)
            .RunAsync();

        Assert.Equal("Alice     Smith     030", Normalize(targetWriter.ToString()).TrimEnd('\n'));

        // The caller still owns the reader — it must remain usable.
        Assert.True(reader.BaseStream.CanRead);
    }


    [Fact]
    public async Task Header_separator_and_delimiter_setters_round_trip_through_a_file()
    {
        // Start from a plain fixed-width source; the loader emits the header/separator/delimited form and
        // the second extractor reads it back with matching configuration — no hand-authored layout.
        var source = WriteTempFile("plain.txt", Content(("Alice", "Smith", 30), ("Bob", "Jones", 25)));
        var target = Path.Combine(_tempDir, "delimited-out.txt");

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .FixedWidthLoader<PersonRecord>(target)
            .WriteHeader(true)
            .FieldSeparator('-')
            .FieldDelimiter(" | ")
            .RunAsync();

        // Read the output back with the matching header/separator/delimiter configuration.
        var records = await ToListAsync
        (
            EtlPipeline
                .Create()
                .FixedWidthExtractor<PersonRecord>(target)
                .HasHeader(true)
                .FieldSeparator('-')
                .FieldDelimiter(" | ")
                .AsAsyncEnumerable()
        );

        Assert.Equal
        (
            new[]
            {
                new PersonRecord { FirstName = "Alice", LastName = "Smith", Age = 30 },
                new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 25 },
            },
            records
        );
    }


    [Fact]
    public async Task Extractor_MalformedLineHandling_Skip_and_BlankLineHandling_Skip_drop_bad_lines()
    {
        // A too-short line and a blank line sit among the valid records.
        var source = WriteTempFile
        (
            "messy.txt",
            "Alice     Smith     030\nshort\n\nCarol     White     035\n"
        );
        using var targetWriter = new StringWriter();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .MalformedLineHandling(MalformedLineHandling.Skip)
            .BlankLineHandling(BlankLineHandling.Skip)
            .FixedWidthLoader<PersonRecord>(targetWriter)
            .RunAsync();

        Assert.Equal
        (
            "Alice     Smith     030\nCarol     White     035",
            Normalize(targetWriter.ToString()).TrimEnd('\n')
        );
    }


    [Fact]
    public async Task Extractor_LineFilter_RecordValidator_and_ValueParser_setters_apply()
    {
        var source = WriteTempFile
        (
            "filtered.txt",
            "#comment  ignore     000\nAlice     Smith     030\nBob       Jones     025\n"
        );
        using var targetWriter = new StringWriter();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .LineFilter(line => line.StartsWith("#", StringComparison.Ordinal) ? LineAction.Skip : LineAction.Process)
            .RecordValidator(p => p.Age >= 30 ? ValidationResult.Accept() : ValidationResult.Skip("under 30"))
            .ValueParser(FixedWidthConverter.DefaultParser)
            .FixedWidthLoader<PersonRecord>(targetWriter)
            .RunAsync();

        // The comment line is filtered, Bob is rejected by the validator; only Alice survives.
        Assert.Equal("Alice     Smith     030", Normalize(targetWriter.ToString()).TrimEnd('\n'));
    }


    [Fact]
    public async Task Loader_setters_including_dry_run_and_converters_are_applied()
    {
        var source = WriteTempFile("dry.txt", Content(("Alice", "Smith", 30)));
        var target = Path.Combine(_tempDir, "dry-out.txt");

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .FixedWidthLoader<PersonRecord>(target)
            .ValueConverter(FixedWidthConverter.Strict)
            .HeaderConverter(FixedWidthConverter.StrictHeader)
            .FieldSeparator(null)
            .FieldDelimiter(null)
            .IsDryRun(true)
            .RunAsync();

        // A dry run validates but writes nothing — the file is created empty.
        Assert.Equal(string.Empty, File.ReadAllText(target));
    }


    [Fact]
    public async Task Loader_Encoding_setter_binds_the_output_stream_encoding()
    {
        var source = WriteTempFile("enc.txt", Content(("Alice", "Smith", 30)));
        var target = Path.Combine(_tempDir, "enc-out.txt");

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .FixedWidthLoader<PersonRecord>(target)
            .Encoding(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
            .RunAsync();

        var bytes = File.ReadAllBytes(target);

        Assert.True
        (
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "Expected a UTF-8 BOM, proving the loader used the configured encoding to open the file."
        );
    }


    [Fact]
    public async Task Extractor_Encoding_setter_binds_the_input_stream_encoding()
    {
        // Write the source as UTF-16 so a mismatched decode would corrupt it.
        var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        var target = Path.Combine(_tempDir, "utf16.txt");
        File.WriteAllText(target, Content(("Alice", "Smith", 30)), utf16);

        using var targetWriter = new StringWriter();

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(target)
            .Encoding(utf16)
            .FixedWidthLoader<PersonRecord>(targetWriter)
            .RunAsync();

        Assert.Equal("Alice     Smith     030", Normalize(targetWriter.ToString()).TrimEnd('\n'));
    }


    [Fact]
    public async Task Path_based_source_and_sink_release_their_file_handles_after_a_successful_run()
    {
        var source = WriteTempFile("release.txt", Content(("Alice", "Smith", 30)));
        var target = Path.Combine(_tempDir, "release-out.txt");

        await EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .FixedWidthLoader<PersonRecord>(target)
            .RunAsync();

        // A locked file would throw IOException here.
        File.Delete(source);
        File.Delete(target);

        Assert.False(File.Exists(source));
        Assert.False(File.Exists(target));
    }


    [Fact]
    public async Task Path_based_source_releases_its_file_handle_after_a_faulted_run()
    {
        var source = WriteTempFile("fault.txt", Content(("Alice", "Smith", 30)));
        var target = Path.Combine(_tempDir, "fault-out.txt");

        var run = EtlPipeline
            .Create()
            .FixedWidthExtractor<PersonRecord>(source)
            .Through(ThrowOnFirst)
            .FixedWidthLoader<PersonRecord>(target)
            .RunAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => run);

        // The reader must have been disposed even though the pipeline threw.
        File.Delete(source);
        Assert.False(File.Exists(source));
    }


    [Fact]
    public void First_pipeline_operator_narrows_the_builder_off_the_configuration_surface()
    {
        var source = WriteTempFile("narrow.txt", Content(("Alice", "Smith", 30)));

        var builder = EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(source);
        IEtlPipeline<PersonRecord> narrowed = builder.Through(FilterAdults);

        Assert.False(narrowed is IFixedWidthExtractorBuilder<PersonRecord>);
    }


    [Fact]
    public void Configuring_the_extractor_after_it_is_materialized_throws()
    {
        var source = WriteTempFile("late.txt", Content(("Alice", "Smith", 30)));

        var builder = EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(source);
        _ = builder.Through(FilterAdults);

        Assert.Throws<InvalidOperationException>(() => builder.HeaderLineCount(1));
        Assert.Throws<InvalidOperationException>(() => builder.Encoding(Utf8NoBom));
    }


    [Fact]
    public void Extractor_factories_reject_null_arguments()
    {
        var pipeline = EtlPipeline.Create();

        // Null receiver — every overload.
        Assert.Throws<ArgumentNullException>(() => ((EtlPipeline)null!).FixedWidthExtractor<PersonRecord>("x.txt"));
        Assert.Throws<ArgumentNullException>(() => ((EtlPipeline)null!).FixedWidthExtractor<PersonRecord>(Stream.Null));
        Assert.Throws<ArgumentNullException>(() => ((EtlPipeline)null!).FixedWidthExtractor<PersonRecord>(TextReader.Null));
        Assert.Throws<ArgumentNullException>(() => ((EtlPipeline)null!).FixedWidthExtractor<PersonRecord>(new FixedWidthExtractor<PersonRecord>(TextReader.Null)));

        // Null argument — every overload.
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthExtractor<PersonRecord>((string)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthExtractor<PersonRecord>((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthExtractor<PersonRecord>((TextReader)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthExtractor<PersonRecord>((FixedWidthExtractor<PersonRecord>)null!));
    }


    [Fact]
    public void Loader_terminators_reject_null_arguments()
    {
        var source = WriteTempFile("guard.txt", Content(("Alice", "Smith", 30)));
        var pipeline = EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(source);

        // Null receiver — every overload.
        Assert.Throws<ArgumentNullException>(() => ((IEtlPipeline<PersonRecord>)null!).FixedWidthLoader<PersonRecord>("x.txt"));
        Assert.Throws<ArgumentNullException>(() => ((IEtlPipeline<PersonRecord>)null!).FixedWidthLoader<PersonRecord>(Stream.Null));
        Assert.Throws<ArgumentNullException>(() => ((IEtlPipeline<PersonRecord>)null!).FixedWidthLoader<PersonRecord>(TextWriter.Null));

        // Null argument — every overload.
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthLoader<PersonRecord>((string)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthLoader<PersonRecord>((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.FixedWidthLoader<PersonRecord>((TextWriter)null!));
    }


    [Fact]
    public void Extractor_and_loader_setters_reject_null_delegates_and_encodings()
    {
        var source = WriteTempFile("nulls.txt", Content(("Alice", "Smith", 30)));
        var extractor = EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(source);

        Assert.Throws<ArgumentNullException>(() => extractor.Encoding(null!));
        Assert.Throws<ArgumentNullException>(() => extractor.LineFilter(null!));
        Assert.Throws<ArgumentNullException>(() => extractor.RecordValidator(null!));
        Assert.Throws<ArgumentNullException>(() => extractor.ValueParser(null!));

        var loader = EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(source).FixedWidthLoader<PersonRecord>(new StringWriter());

        Assert.Throws<ArgumentNullException>(() => loader.Encoding(null!));
        Assert.Throws<ArgumentNullException>(() => loader.ValueConverter(null!));
        Assert.Throws<ArgumentNullException>(() => loader.HeaderConverter(null!));
    }


    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);


    private static string Line(string first, string last, int age)
        => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}", first, last, age);


    private static string Content(params (string First, string Last, int Age)[] people)
        => string.Concat(people.Select(p => Line(p.First, p.Last, p.Age) + "\n"));


    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");


    private async Task<IReadOnlyList<PersonRecord>> ExtractAllAsync(string path)
        => await ToListAsync(EtlPipeline.Create().FixedWidthExtractor<PersonRecord>(path).AsAsyncEnumerable());


    private static async Task<IReadOnlyList<PersonRecord>> ToListAsync(IAsyncEnumerable<PersonRecord> source)
    {
        var list = new List<PersonRecord>();
        await foreach (var item in source.ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }


    private static async IAsyncEnumerable<PersonRecord> FilterAdults(IAsyncEnumerable<PersonRecord> source)
    {
        await foreach (var person in source.ConfigureAwait(false))
        {
            if (person.Age >= 30)
            {
                yield return person;
            }
        }
    }


    private static async IAsyncEnumerable<PersonRecord> UppercaseLastNames(IAsyncEnumerable<PersonRecord> source)
    {
        await foreach (var person in source.ConfigureAwait(false))
        {
            yield return person with { LastName = person.LastName.ToUpperInvariant() };
        }
    }


    private static async IAsyncEnumerable<PersonRecord> ThrowOnFirst(IAsyncEnumerable<PersonRecord> source)
    {
        await foreach (var _ in source.ConfigureAwait(false))
        {
            throw new InvalidOperationException("boom");
        }

        yield break;
    }


    private string WriteTempFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Utf8NoBom);
        return path;
    }


    private static MemoryStream StreamOver(string content) => new(Utf8NoBom.GetBytes(content));


    private static StreamReader ReaderOver(string content) => new(StreamOver(content), Utf8NoBom);
}
