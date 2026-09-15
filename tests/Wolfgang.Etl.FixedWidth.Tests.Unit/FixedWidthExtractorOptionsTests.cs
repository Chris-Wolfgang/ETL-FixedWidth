using System;
using System.IO;
using System.Text;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthExtractorOptionsTests
{
    private static readonly Func<string, LineAction> StopOnEnd = line => string.Equals(line, "END", StringComparison.Ordinal) ? LineAction.Stop : LineAction.Process;
    private static readonly Func<PersonRecord, ValidationResult> AlwaysValid = _ => ValidationResult.Accept();
    private static readonly Action<FixedWidthError> Ignore = _ => { };
    private static readonly FixedWidthValueParser Parser = (text, _) => text;



    [Fact]
    public void Defaults_match_the_extractor_defaults()
    {
        var options = new FixedWidthExtractorOptions<PersonRecord>();
        using var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader(string.Empty));

        Assert.Equal(extractor.MalformedLineHandling, options.MalformedLineHandling);
        Assert.Equal(extractor.BlankLineHandling, options.BlankLineHandling);
        Assert.Equal(LineAction.Process, options.LineFilter("anything"));
        Assert.Null(options.RecordValidator);
        Assert.Null(options.OnError);
        Assert.Same(extractor.ValueParser, options.ValueParser);
        Assert.Equal(extractor.HeaderLineCount, options.HeaderLineCount);
        Assert.Equal(extractor.FieldSeparator, options.FieldSeparator);
        Assert.Equal(extractor.FieldDelimiter, options.FieldDelimiter);
        Assert.Equal(extractor.Schema, options.Schema);
        Assert.Equal(extractor.TrackByteOffset, options.TrackByteOffset);
        Assert.Equal(extractor.StartByteOffset, options.StartByteOffset);
    }



    [Fact]
    public void Reader_constructor_when_passed_options_applies_every_member()
    {
        var options = new FixedWidthExtractorOptions<PersonRecord>
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
            BlankLineHandling = BlankLineHandling.Skip,
            LineFilter = StopOnEnd,
            RecordValidator = AlwaysValid,
            OnError = Ignore,
            ValueParser = Parser,
            HeaderLineCount = 2,
            FieldSeparator = '-',
            FieldDelimiter = " | ",
            TrackByteOffset = false,
            StartByteOffset = 0,
        };

        using var sut = new FixedWidthExtractor<PersonRecord>(new StringReader(string.Empty), options);

        Assert.Equal(MalformedLineHandling.Skip, sut.MalformedLineHandling);
        Assert.Equal(BlankLineHandling.Skip, sut.BlankLineHandling);
        Assert.Same(StopOnEnd, sut.LineFilter);
        Assert.Same(AlwaysValid, sut.RecordValidator);
        Assert.Same(Ignore, sut.OnError);
        Assert.Same(Parser, sut.ValueParser);
        Assert.Equal(2, sut.HeaderLineCount);
        Assert.True(sut.HasHeader);
        Assert.Equal('-', sut.FieldSeparator);
        Assert.Equal(" | ", sut.FieldDelimiter);
    }



    [Fact]
    public void Stream_constructor_when_passed_stream_options_applies_encoding_and_checkpoint_members()
    {
        var options = new FixedWidthExtractorStreamOptions<PersonRecord>
        {
            Encoding = Encoding.Unicode,
            TrackByteOffset = true,
            StartByteOffset = 12,
            HeaderLineCount = 1,
        };

        using var sut = new FixedWidthExtractor<PersonRecord>(new MemoryStream(), options);

        Assert.True(sut.TrackByteOffset);
        Assert.Equal(12, sut.StartByteOffset);
        Assert.Equal(1, sut.HeaderLineCount);
    }



    [Fact]
    public void Stream_constructor_when_options_are_null_keeps_the_defaults()
    {
        using var sut = new FixedWidthExtractor<PersonRecord>(new MemoryStream(), options: null);

        Assert.Equal(MalformedLineHandling.ThrowException, sut.MalformedLineHandling);
        Assert.Equal(0, sut.HeaderLineCount);
        Assert.False(sut.TrackByteOffset);
    }



    [Fact]
    public void Reader_constructor_when_options_are_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractor<PersonRecord>(new StringReader(string.Empty), (FixedWidthExtractorOptions<PersonRecord>)null!));

        Assert.Equal("options", ex.ParamName);
    }



    [Theory]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void StartByteOffset_when_negative_throws_ArgumentOutOfRangeException(long value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthExtractorOptions<PersonRecord> { StartByteOffset = value });

        Assert.Equal("value", ex.ParamName);
        Assert.Equal(value, ex.ActualValue);
    }



    [Fact]
    public void HeaderLineCount_when_negative_throws_ArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthExtractorOptions<PersonRecord> { HeaderLineCount = -1 });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void LineFilter_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractorOptions<PersonRecord> { LineFilter = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void ValueParser_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractorOptions<PersonRecord> { ValueParser = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void Encoding_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthExtractorStreamOptions<PersonRecord> { Encoding = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void With_expression_preserves_unset_members()
    {
        var baseline = new FixedWidthExtractorOptions<PersonRecord> { HeaderLineCount = 3, FieldSeparator = '-' };

        var changed = baseline with { FieldDelimiter = "," };

        Assert.Equal(3, changed.HeaderLineCount);
        Assert.Equal('-', changed.FieldSeparator);
        Assert.Equal(",", changed.FieldDelimiter);
    }
}
