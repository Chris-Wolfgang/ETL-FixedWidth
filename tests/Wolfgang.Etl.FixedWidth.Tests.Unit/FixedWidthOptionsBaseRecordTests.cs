using System;
using System.IO;
using Wolfgang.Etl.Abstractions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// The options records inherit the per-stage-kind base records from Wolfgang.Etl.Abstractions 0.24 (ADR-0009),
/// so the four settings every base stage shares travel through the same record as the fixed-width settings and
/// are applied by the base constructor.
/// </summary>
public class FixedWidthOptionsBaseRecordTests
{
    private static Func<ItemErrorContext, ItemErrorAction> AnyPolicy => _ => default;



    [Fact]
    public void Extractor_records_inherit_ExtractorOptions()
    {
        Assert.IsAssignableFrom<ExtractorOptions>(new FixedWidthExtractorOptions<PersonRecord>());
        Assert.IsAssignableFrom<ExtractorOptions>(new FixedWidthExtractorStreamOptions<PersonRecord>());
    }



    [Fact]
    public void Loader_records_inherit_LoaderOptions()
    {
        Assert.IsAssignableFrom<LoaderOptions>(new FixedWidthLoaderOptions());
        Assert.IsAssignableFrom<LoaderOptions>(new FixedWidthLoaderStreamOptions());
    }



    [Fact]
    public void FixedWidthExtractor_when_inherited_settings_are_set_through_options_applies_them()
    {
        var policy = AnyPolicy;
        var options = new FixedWidthExtractorOptions<PersonRecord>
        {
            ReportingInterval = 5,
            SkipItemCount = 2,
            MaximumItemCount = 3,
            ErrorPolicy = policy,
            HeaderLineCount = 1,
        };

        using var sut = new FixedWidthExtractor<PersonRecord>(new StringReader(string.Empty), options);

        Assert.Equal(5, sut.ReportingInterval);
        Assert.Equal(2, sut.SkipItemCount);
        Assert.Equal(3, sut.MaximumItemCount);
        Assert.Same(policy, sut.ErrorPolicy);
        Assert.Equal(1, sut.HeaderLineCount);
    }



    [Fact]
    public void FixedWidthLoader_when_inherited_settings_are_set_through_options_applies_them()
    {
        var policy = AnyPolicy;
        var options = new FixedWidthLoaderOptions
        {
            ReportingInterval = 5,
            SkipItemCount = 2,
            MaximumItemCount = 3,
            ErrorPolicy = policy,
            IsDryRun = true,
        };

        using var sut = new FixedWidthLoader<PersonRecord>(new StringWriter(), options);

        Assert.Equal(5, sut.ReportingInterval);
        Assert.Equal(2, sut.SkipItemCount);
        Assert.Equal(3, sut.MaximumItemCount);
        Assert.Same(policy, sut.ErrorPolicy);
        Assert.True(sut.IsDryRun);
    }



    [Fact]
    public void FixedWidthExtractor_when_options_are_omitted_keeps_the_base_defaults()
    {
        var defaults = new ExtractorOptions();

        using var sut = new FixedWidthExtractor<PersonRecord>(new StringReader(string.Empty));

        Assert.Equal(defaults.ReportingInterval, sut.ReportingInterval);
        Assert.Equal(defaults.SkipItemCount, sut.SkipItemCount);
        Assert.Equal(defaults.MaximumItemCount, sut.MaximumItemCount);
    }



    [Fact]
    public void With_expression_preserves_the_inherited_settings()
    {
        var original = new FixedWidthExtractorStreamOptions<PersonRecord> { SkipItemCount = 2, MaximumItemCount = 3 };

        var copy = original with { HeaderLineCount = 1 };

        Assert.Equal(2, copy.SkipItemCount);
        Assert.Equal(3, copy.MaximumItemCount);
        Assert.Equal(1, copy.HeaderLineCount);
    }
}
