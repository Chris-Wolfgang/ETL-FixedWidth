using System;
using System.IO;
using System.Text;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthLoaderOptionsTests
{
    private static readonly Func<object, FieldContext, string> Converter = (value, _) => value.ToString() ?? string.Empty;
    private static readonly Func<string, FieldContext, string> HeaderConverter = (label, _) => label.ToUpperInvariant();



    [Fact]
    public void Defaults_match_the_loader_defaults()
    {
        var options = new FixedWidthLoaderOptions();
        using var loader = new FixedWidthLoader<PersonRecord>(new StringWriter());

        Assert.Same(loader.ValueConverter, options.ValueConverter);
        Assert.Same(loader.HeaderConverter, options.HeaderConverter);
        Assert.Equal(loader.WriteHeader, options.WriteHeader);
        Assert.Equal(loader.IsDryRun, options.IsDryRun);
        Assert.Equal(loader.FieldSeparator, options.FieldSeparator);
        Assert.Equal(loader.FieldDelimiter, options.FieldDelimiter);
        Assert.Equal(loader.Schema, options.Schema);
    }



    [Fact]
    public void Writer_constructor_when_passed_options_applies_every_member()
    {
        var options = new FixedWidthLoaderOptions
        {
            ValueConverter = Converter,
            HeaderConverter = HeaderConverter,
            WriteHeader = true,
            IsDryRun = true,
            FieldSeparator = '=',
            FieldDelimiter = "|",
        };

        using var sut = new FixedWidthLoader<PersonRecord>(new StringWriter(), options);

        Assert.Same(Converter, sut.ValueConverter);
        Assert.Same(HeaderConverter, sut.HeaderConverter);
        Assert.True(sut.WriteHeader);
        Assert.True(sut.IsDryRun);
        Assert.Equal('=', sut.FieldSeparator);
        Assert.Equal("|", sut.FieldDelimiter);
    }



    [Fact]
    public void Stream_constructor_when_passed_stream_options_applies_encoding_and_base_members()
    {
        var options = new FixedWidthLoaderStreamOptions { Encoding = Encoding.Unicode, WriteHeader = true };

        using var sut = new FixedWidthLoader<PersonRecord>(new MemoryStream(), options);

        Assert.True(sut.WriteHeader);
    }



    [Fact]
    public void Stream_constructor_when_options_are_null_keeps_the_defaults()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(new MemoryStream(), options: null);

        Assert.False(sut.WriteHeader);
        Assert.False(sut.IsDryRun);
        Assert.Null(sut.FieldDelimiter);
    }



    [Fact]
    public void Writer_constructor_when_options_are_null_keeps_the_defaults()
    {
        var defaults = new FixedWidthLoaderOptions();

        using var sut = new FixedWidthLoader<PersonRecord>(new StringWriter(), (FixedWidthLoaderOptions?)null);

        Assert.Equal(defaults.WriteHeader, sut.WriteHeader);
        Assert.Equal(defaults.IsDryRun, sut.IsDryRun);
        Assert.Equal(defaults.FieldSeparator, sut.FieldSeparator);
        Assert.Equal(defaults.FieldDelimiter, sut.FieldDelimiter);
    }



    [Fact]
    public void ValueConverter_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoaderOptions { ValueConverter = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void HeaderConverter_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoaderOptions { HeaderConverter = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void Encoding_when_null_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new FixedWidthLoaderStreamOptions { Encoding = null! });

        Assert.Equal("value", ex.ParamName);
    }



    [Fact]
    public void With_expression_preserves_unset_members()
    {
        var baseline = new FixedWidthLoaderStreamOptions { WriteHeader = true, Encoding = Encoding.Unicode };

        var changed = baseline with { FieldDelimiter = "," };

        Assert.True(changed.WriteHeader);
        Assert.Same(Encoding.Unicode, changed.Encoding);
        Assert.Equal(",", changed.FieldDelimiter);
    }
}
