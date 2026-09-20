using System;
using Wolfgang.Etl.Abstractions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// <see cref="FixedWidthBinaryExtractorOptions"/>, <see cref="FixedWidthBinaryLoaderOptions"/> and
/// <see cref="FixedWidthMultiRecordExtractorOptions"/> carry the four shared stage settings themselves instead
/// of deriving from the Abstractions base records: on net462 / net481 / netstandard2.0 a derived record's
/// <c>&lt;Clone&gt;$</c> returns the base type, which breaks callers compiled against 0.12.0 that use a
/// <c>with</c> expression. Deriving is scheduled for the 2026-12-15 wave (#373); until then these tests pin
/// the standalone shape, the validation the base records would have done, and the projection the constructors
/// hand to the base stage.
/// </summary>
public class FixedWidthStandaloneOptionsRecordTests
{
    private static Func<ItemErrorContext, ItemErrorAction> AnyPolicy => _ => default;



    [Fact]
    public void Records_do_not_derive_from_the_base_records_until_the_removal_wave()
    {
        Assert.IsNotAssignableFrom<ExtractorOptions>(new FixedWidthBinaryExtractorOptions());
        Assert.IsNotAssignableFrom<LoaderOptions>(new FixedWidthBinaryLoaderOptions());
        Assert.IsNotAssignableFrom<ExtractorOptions>(new FixedWidthMultiRecordExtractorOptions());
    }



    [Fact]
    public void Defaults_match_the_base_records()
    {
        var expectedExtractor = new ExtractorOptions();
        var expectedLoader = new LoaderOptions();

        foreach (var (actualInterval, actualMax, actualSkip) in new[]
        {
            (new FixedWidthBinaryExtractorOptions().ReportingInterval, new FixedWidthBinaryExtractorOptions().MaximumItemCount, new FixedWidthBinaryExtractorOptions().SkipItemCount),
            (new FixedWidthMultiRecordExtractorOptions().ReportingInterval, new FixedWidthMultiRecordExtractorOptions().MaximumItemCount, new FixedWidthMultiRecordExtractorOptions().SkipItemCount),
            (new FixedWidthBinaryLoaderOptions().ReportingInterval, new FixedWidthBinaryLoaderOptions().MaximumItemCount, new FixedWidthBinaryLoaderOptions().SkipItemCount),
        })
        {
            Assert.Equal(expectedExtractor.ReportingInterval, actualInterval);
            Assert.Equal(expectedExtractor.MaximumItemCount, actualMax);
            Assert.Equal(expectedExtractor.SkipItemCount, actualSkip);
        }

        Assert.Same(expectedExtractor.ErrorPolicy, new FixedWidthBinaryExtractorOptions().ErrorPolicy);
        Assert.Same(expectedExtractor.ErrorPolicy, new FixedWidthMultiRecordExtractorOptions().ErrorPolicy);
        Assert.Same(expectedLoader.ErrorPolicy, new FixedWidthBinaryLoaderOptions().ErrorPolicy);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReportingInterval_and_MaximumItemCount_below_one_throw_at_init(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryExtractorOptions { ReportingInterval = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryExtractorOptions { MaximumItemCount = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryLoaderOptions { ReportingInterval = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryLoaderOptions { MaximumItemCount = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthMultiRecordExtractorOptions { ReportingInterval = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthMultiRecordExtractorOptions { MaximumItemCount = value });
    }



    [Fact]
    public void Negative_SkipItemCount_and_null_ErrorPolicy_throw_at_init()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryExtractorOptions { SkipItemCount = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthBinaryLoaderOptions { SkipItemCount = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthMultiRecordExtractorOptions { SkipItemCount = -1 });
        Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryExtractorOptions { ErrorPolicy = null! });
        Assert.Throws<ArgumentNullException>(() => new FixedWidthBinaryLoaderOptions { ErrorPolicy = null! });
        Assert.Throws<ArgumentNullException>(() => new FixedWidthMultiRecordExtractorOptions { ErrorPolicy = null! });
    }



    [Fact]
    public void Projection_carries_every_shared_setting_to_the_base_record()
    {
        var policy = AnyPolicy;

        var extractor = new FixedWidthBinaryExtractorOptions { ReportingInterval = 7, MaximumItemCount = 8, SkipItemCount = 9, ErrorPolicy = policy }.ToExtractorOptions();
        var multi = new FixedWidthMultiRecordExtractorOptions { ReportingInterval = 7, MaximumItemCount = 8, SkipItemCount = 9, ErrorPolicy = policy }.ToExtractorOptions();
        var loader = new FixedWidthBinaryLoaderOptions { ReportingInterval = 7, MaximumItemCount = 8, SkipItemCount = 9, ErrorPolicy = policy }.ToLoaderOptions();

        foreach (var (interval, max, skip, errorPolicy) in new[]
        {
            (extractor.ReportingInterval, extractor.MaximumItemCount, extractor.SkipItemCount, extractor.ErrorPolicy),
            (multi.ReportingInterval, multi.MaximumItemCount, multi.SkipItemCount, multi.ErrorPolicy),
            (loader.ReportingInterval, loader.MaximumItemCount, loader.SkipItemCount, loader.ErrorPolicy),
        })
        {
            Assert.Equal(7, interval);
            Assert.Equal(8, max);
            Assert.Equal(9, skip);
            Assert.Same(policy, errorPolicy);
        }
    }



    [Fact]
    public void With_expression_keeps_the_shared_settings_and_the_record_type()
    {
        var original = new FixedWidthBinaryExtractorOptions { SkipItemCount = 2, MaximumItemCount = 3 };

        var copy = original with { Encoding = System.Text.Encoding.UTF8 };

        Assert.IsType<FixedWidthBinaryExtractorOptions>(copy);
        Assert.Equal(2, copy.SkipItemCount);
        Assert.Equal(3, copy.MaximumItemCount);
        Assert.Same(System.Text.Encoding.UTF8, copy.Encoding);
    }
}
