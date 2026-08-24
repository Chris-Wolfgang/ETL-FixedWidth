using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

// ------------------------------------------------------------------
// Extractor — Stream constructor
// ------------------------------------------------------------------

public class FixedWidthExtractorStreamCtorTests
{
    private static readonly string PersonLine = "John      Smith     042";



    private static MemoryStream ToStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }



    [Fact]
    public void Constructor_when_stream_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>
        (
            () => new FixedWidthExtractor<PersonRecord>((Stream)null!)
        );
    }



    [Fact]
    public void Constructor_TextReader_Logger_when_logger_is_null_uses_NullLogger()
    {
        // logger is now an optional trailing parameter: null means "no logging"
        // (NullLogger.Instance) rather than an argument error.
        var sut = new FixedWidthExtractor<PersonRecord>
        (
            new StringReader(PersonLine),
            logger: null
        );

        Assert.NotNull(sut);
    }



    [Fact]
    public void Constructor_Stream_named_encoding_and_logger_nulls_is_unambiguous()
    {
        // Regression guard. While the (Stream, ILogger, Encoding?) overload coexisted with
        // (Stream, Encoding?, ILogger?), this call matched BOTH exactly, so neither won and it did
        // not compile (CS0121). With a single candidate remaining it is unambiguous. If this stops
        // compiling, a competing overload has been reintroduced.
        using var stream = ToStream(PersonLine);

        var sut = new FixedWidthExtractor<PersonRecord>
        (
            stream,
            encoding: null,
            logger: null
        );

        Assert.NotNull(sut);
    }



    [Fact]
    public void Constructor_Stream_Encoding_Logger_when_logger_is_null_uses_NullLogger()
    {
        using var stream = ToStream(PersonLine);

        // A concrete Encoding is passed rather than null: while the [Obsolete]
        // (Stream, ILogger, Encoding?) overload still exists, `encoding: null, logger: null`
        // matches both overloads exactly and is ambiguous (CS0121). Supplying a real Encoding
        // makes the obsolete overload inapplicable. The ambiguity disappears when that
        // overload is removed.
        var sut = new FixedWidthExtractor<PersonRecord>
        (
            stream,
            Encoding.UTF8,
            logger: null
        );

        Assert.NotNull(sut);
    }



    [Fact]
    public async Task ExtractAsync_from_Stream_yields_records()
    {
        using var stream = ToStream(PersonLine + "\n" + PersonLine);
        using var extractor = new FixedWidthExtractor<PersonRecord>(stream);

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("John", results[0].FirstName);
    }



    [Fact]
    public void Dispose_disposes_internal_StreamReader()
    {
        var stream = ToStream(PersonLine);
        var extractor = new FixedWidthExtractor<PersonRecord>(stream);

        extractor.Dispose();

        // The internal StreamReader is disposed. The Stream itself remains
        // open because leaveOpen: true — verify the stream is still accessible.
        Assert.True(stream.CanRead);
    }



    [Fact]
    public void Dispose_when_constructed_from_TextReader_does_not_dispose_reader()
    {
        var reader = new StringReader(PersonLine);
        var extractor = new FixedWidthExtractor<PersonRecord>(reader);

        extractor.Dispose();

        // The caller-owned reader should still be usable.
        var line = reader.ReadLine();
        Assert.Equal(PersonLine, line);
    }



    [Fact]
    public void Dispose_can_be_called_multiple_times_without_error()
    {
        using var stream = ToStream(PersonLine);
        var extractor = new FixedWidthExtractor<PersonRecord>(stream);

        extractor.Dispose();
        extractor.Dispose();
    }
}



// ------------------------------------------------------------------
// Loader — Stream constructor
// ------------------------------------------------------------------

public class FixedWidthLoaderStreamCtorTests
{
    [Fact]
    public void Constructor_when_stream_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>
        (
            () => new FixedWidthLoader<PersonRecord>((Stream)null!)
        );
    }



    [Fact]
    public void Constructor_TextWriter_Logger_when_logger_is_null_uses_NullLogger()
    {
        // logger is now an optional trailing parameter: null means "no logging"
        // (NullLogger.Instance) rather than an argument error.
        var sut = new FixedWidthLoader<PersonRecord>
        (
            new StringWriter(),
            logger: null
        );

        Assert.NotNull(sut);
    }



    [Fact]
    public void Constructor_Stream_Encoding_Logger_when_logger_is_null_uses_NullLogger()
    {
        using var stream = new MemoryStream();

        // A concrete Encoding is passed rather than null: while the [Obsolete]
        // (Stream, ILogger, Encoding?) overload still exists, `encoding: null, logger: null`
        // matches both overloads exactly and is ambiguous (CS0121). Supplying a real Encoding
        // makes the obsolete overload inapplicable. The ambiguity disappears when that
        // overload is removed.
        var sut = new FixedWidthLoader<PersonRecord>
        (
            stream,
            Encoding.UTF8,
            logger: null
        );

        Assert.NotNull(sut);
    }



    [Fact]
    public async Task LoadAsync_to_Stream_writes_records()
    {
        using var stream = new MemoryStream();
        using var loader = new FixedWidthLoader<PersonRecord>(stream);

        await loader.LoadAsync
        (
            new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }.ToAsyncEnumerable()
        );

        // Data should be in the stream after LoadAsync flushes.
        Assert.True(stream.Length > 0);
    }



    [Fact]
    public async Task LoadAsync_flushes_owned_StreamWriter_at_completion()
    {
        using var stream = new MemoryStream();
        var loader = new FixedWidthLoader<PersonRecord>(stream);

        await loader.LoadAsync
        (
            new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }.ToAsyncEnumerable()
        );

        // Verify content is flushed to the stream without needing manual flush.
        stream.Position = 0;
        using var reader = new StreamReader(stream, encoding: System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        Assert.Contains("John", content, StringComparison.Ordinal);

        loader.Dispose();
    }



    [Fact]
    public void Dispose_disposes_internal_StreamWriter()
    {
        var stream = new MemoryStream();
        var loader = new FixedWidthLoader<PersonRecord>(stream);

        loader.Dispose();

        // The internal StreamWriter is disposed. The Stream itself remains
        // open because leaveOpen: true — verify the stream is still accessible.
        Assert.True(stream.CanWrite);
    }



    [Fact]
    public void Dispose_when_constructed_from_TextWriter_does_not_dispose_writer()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer);

        loader.Dispose();

        // The caller-owned writer should still be usable.
        writer.Write("test");
        Assert.Contains("test", writer.ToString(), StringComparison.Ordinal);
    }



    [Fact]
    public void Dispose_can_be_called_multiple_times_without_error()
    {
        using var stream = new MemoryStream();
        var loader = new FixedWidthLoader<PersonRecord>(stream);

        loader.Dispose();
        loader.Dispose();
    }
}
