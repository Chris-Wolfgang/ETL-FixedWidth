using System.IO;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Compile-time guards for the constructor overload set after the options record became optional on the
/// <see cref="TextReader"/> / <see cref="TextWriter"/> constructors. Each call below is a shape that a reviewer
/// has claimed is ambiguous; if any of them ever became ambiguous this file would fail to compile (CS0121).
/// </summary>
public class ConstructorOverloadResolutionTests
{
    [Fact]
    public void Positional_null_second_argument_binds_the_shipped_logger_overload()
    {
        // (reader, null): the two-parameter (TextReader, ILogger?) candidate has every argument supplied, so it
        // beats (TextReader, Options?, ILogger?), which would need a default for the logger.
        using var reader = new StringReader(string.Empty);
        using var writer = new StringWriter();

        var extractor = new FixedWidthExtractor<PersonRecord>(reader, null);
        var loader = new FixedWidthLoader<PersonRecord>(writer, null);

        Assert.NotNull(extractor);
        Assert.NotNull(loader);
    }



    [Fact]
    public void Single_argument_call_binds_the_shipped_single_argument_overload()
    {
        // (reader): the exact-arity (TextReader) candidate beats both optional-parameter overloads.
        using var reader = new StringReader(string.Empty);
        using var writer = new StringWriter();

        var extractor = new FixedWidthExtractor<PersonRecord>(reader);
        var loader = new FixedWidthLoader<PersonRecord>(writer);

        Assert.NotNull(extractor);
        Assert.NotNull(loader);
    }



    [Fact]
    public void Record_and_named_logger_shapes_bind_the_options_overload()
    {
        using var reader = new StringReader(string.Empty);
        using var writer = new StringWriter();

        var viaRecord = new FixedWidthExtractor<PersonRecord>(reader, new FixedWidthExtractorOptions<PersonRecord> { SkipItemCount = 1 });
        var viaNamedNull = new FixedWidthLoader<PersonRecord>(writer, options: null, logger: null);

        Assert.Equal(1, viaRecord.SkipItemCount);
        Assert.NotNull(viaNamedNull);
    }
}
