using System;
using System.IO;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.TestKit.Xunit;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Argument validation for every public and internal constructor in the package.
/// </summary>
/// <remarks>
/// <para>
/// Each constructor gets three kinds of assertion: every required reference parameter rejects
/// <see langword="null"/>, the reported <see cref="ArgumentNullException.ParamName"/> names the
/// parameter the caller actually passed, and every optional parameter accepts
/// <see langword="null"/> without throwing.
/// </para>
/// <para>
/// The <c>ParamName</c> assertions are the point, not decoration. The types with two input shapes
/// route both through one private constructor that takes each source as a separate nullable
/// parameter. When only that core validated, a null <see cref="Stream"/> fell through to the
/// reader/writer branch and was reported as <c>reader</c> or <c>writer</c> — a parameter the
/// caller never passed. These tests pin the boundary behaviour so collapsing constructors into a
/// shared core cannot silently reintroduce it.
/// </para>
/// </remarks>
public class ConstructorArgumentTests
{
    private sealed class BinaryAccount
    {
        [FixedWidthBinaryField(0, 8, BinaryFieldType.Text)]
        public string AccountId { get; set; } = string.Empty;

        [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
        public int TransactionCount { get; set; }
    }



    private static MemoryStream NewStream() => new MemoryStream();

    private static StringReader NewReader() => new StringReader(string.Empty);

    private static StringWriter NewWriter() => new StringWriter();



    // ------------------------------------------------------------------
    // FixedWidthExtractor<TRecord>
    // ------------------------------------------------------------------

    [Fact]
    public void Extractor_reader_ctor_when_reader_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>((TextReader)null!));

        Assert.Equal("reader", ex.ParamName);
    }



    [Fact]
    public void Extractor_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>((Stream)null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void Extractor_reader_timer_ctor_when_reader_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>((TextReader)null!, new ManualProgressTimer()));

        Assert.Equal("reader", ex.ParamName);
    }



    [Fact]
    public void Extractor_reader_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>(NewReader(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void Extractor_stream_timer_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>((Stream)null!, new ManualProgressTimer()));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void Extractor_stream_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>(NewStream(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void Extractor_reader_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthExtractor<PersonRecord>(NewReader(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Extractor_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthExtractor<PersonRecord>(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Extractor_reader_timer_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthExtractor<PersonRecord>(NewReader(), new ManualProgressTimer(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Extractor_stream_timer_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthExtractor<PersonRecord>(NewStream(), new ManualProgressTimer(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthLoader<TRecord>
    // ------------------------------------------------------------------

    [Fact]
    public void Loader_writer_ctor_when_writer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>((TextWriter)null!));

        Assert.Equal("writer", ex.ParamName);
    }



    [Fact]
    public void Loader_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>((Stream)null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void Loader_writer_timer_ctor_when_writer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>((TextWriter)null!, new ManualProgressTimer()));

        Assert.Equal("writer", ex.ParamName);
    }



    [Fact]
    public void Loader_writer_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>(NewWriter(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void Loader_stream_timer_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>((Stream)null!, new ManualProgressTimer()));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void Loader_stream_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoader<PersonRecord>(NewStream(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void Loader_writer_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(NewWriter(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Loader_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Loader_writer_timer_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(NewWriter(), new ManualProgressTimer(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Loader_stream_timer_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(NewStream(), new ManualProgressTimer(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthMultiRecordExtractor
    // ------------------------------------------------------------------

    [Fact]
    public void MultiRecord_reader_ctor_when_reader_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((TextReader)null!));

        Assert.Equal("reader", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((Stream)null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_reader_timer_ctor_when_reader_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((TextReader)null!, new ManualProgressTimer()));

        Assert.Equal("reader", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_reader_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor(NewReader(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_stream_timer_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor((Stream)null!, new ManualProgressTimer()));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_stream_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractor(NewStream(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void MultiRecord_reader_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthMultiRecordExtractor(NewReader(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void MultiRecord_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthMultiRecordExtractor(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void MultiRecord_reader_timer_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthMultiRecordExtractor(NewReader(), new ManualProgressTimer(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void MultiRecord_stream_timer_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthMultiRecordExtractor(NewStream(), new ManualProgressTimer(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthDataReader<TRecord>
    // ------------------------------------------------------------------

    [Fact]
    public void DataReader_reader_ctor_when_reader_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthDataReader<PersonRecord>((TextReader)null!));

        Assert.Equal("reader", ex.ParamName);
    }



    [Fact]
    public void DataReader_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthDataReader<PersonRecord>((Stream)null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void DataReader_reader_ctor_accepts_a_null_logger()
    {
        using var sut = new FixedWidthDataReader<PersonRecord>(NewReader(), logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void DataReader_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthDataReader<PersonRecord>(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthBinaryExtractor<TRecord> / FixedWidthBinaryLoader<TRecord>
    // ------------------------------------------------------------------

    [Fact]
    public void BinaryExtractor_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryExtractor<BinaryAccount>(null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void BinaryExtractor_stream_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryExtractor<BinaryAccount>(NewStream(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void BinaryExtractor_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthBinaryExtractor<BinaryAccount>(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void BinaryExtractor_stream_timer_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthBinaryExtractor<BinaryAccount>(NewStream(), new ManualProgressTimer(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void BinaryLoader_stream_ctor_when_stream_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryLoader<BinaryAccount>(null!));

        Assert.Equal("stream", ex.ParamName);
    }



    [Fact]
    public void BinaryLoader_stream_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryLoader<BinaryAccount>(NewStream(), (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void BinaryLoader_stream_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthBinaryLoader<BinaryAccount>(NewStream(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void BinaryLoader_stream_timer_ctor_accepts_null_options_and_logger()
    {
        using var sut = new FixedWidthBinaryLoader<BinaryAccount>(NewStream(), new ManualProgressTimer(), options: null, logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthTransformer<TSource, TDestination>
    // ------------------------------------------------------------------

    [Fact]
    public void Transformer_ctor_when_transform_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthTransformer<PersonRecord, PersonRecord>(null!));

        Assert.Equal("transform", ex.ParamName);
    }



    [Fact]
    public void Transformer_timer_ctor_when_transform_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthTransformer<PersonRecord, PersonRecord>(null!, new ManualProgressTimer()));

        Assert.Equal("transform", ex.ParamName);
    }



    [Fact]
    public void Transformer_timer_ctor_when_timer_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthTransformer<PersonRecord, PersonRecord>(r => r, (IProgressTimer)null!));

        Assert.Equal("timer", ex.ParamName);
    }



    [Fact]
    public void Transformer_ctor_accepts_a_null_logger()
    {
        var sut = new FixedWidthTransformer<PersonRecord, PersonRecord>(r => r, logger: null);

        Assert.NotNull(sut);
    }



    [Fact]
    public void Transformer_timer_ctor_accepts_a_null_logger()
    {
        var sut = new FixedWidthTransformer<PersonRecord, PersonRecord>(r => r, new ManualProgressTimer(), logger: null);

        Assert.NotNull(sut);
    }



    // ------------------------------------------------------------------
    // FixedWidthReport / FixedWidthError
    // ------------------------------------------------------------------

    [Fact]
    public void Report_options_ctor_when_options_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthReport(null!));

        Assert.Equal("options", ex.ParamName);
    }



    [Fact]
    public void Report_options_ctor_accepts_a_default_options_instance()
    {
        var sut = new FixedWidthReport(new FixedWidthReportOptions());

        Assert.NotNull(sut);
    }



    [Fact]
    public void Error_ctor_when_exception_is_null_throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthError(1, "raw", null!));

        Assert.Equal("exception", ex.ParamName);
    }



    [Fact]
    public void Error_ctor_accepts_a_null_raw_content()
    {
        var sut = new FixedWidthError(1, null, new InvalidOperationException());

        Assert.NotNull(sut);
    }
}
