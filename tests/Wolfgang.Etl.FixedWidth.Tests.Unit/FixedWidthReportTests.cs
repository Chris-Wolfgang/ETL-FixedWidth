using System;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthReportTests
{
    [Fact]
    public void Constructor_from_options_sets_all_properties()
    {
        var report = new FixedWidthReport
        (
            new FixedWidthReportOptions
            {
                CurrentCount = 10,
                CurrentSkippedItemCount = 2,
                CurrentRejectedItemCount = 3,
                CurrentFilteredLineCount = 4,
                CurrentLineNumber = 20L
            }
        );

        Assert.Equal(10, report.CurrentItemCount);
        Assert.Equal(2, report.CurrentSkippedItemCount);
        Assert.Equal(3, report.CurrentRejectedItemCount);
        Assert.Equal(4, report.CurrentFilteredLineCount);
        Assert.Equal(20L, report.CurrentLineNumber);
    }



    [Fact]
    public void Constructor_from_options_defaults_every_unset_count_to_zero()
    {
        var report = new FixedWidthReport(new FixedWidthReportOptions());

        Assert.Equal(0, report.CurrentItemCount);
        Assert.Equal(0, report.CurrentSkippedItemCount);
        Assert.Equal(0, report.CurrentRejectedItemCount);
        Assert.Equal(0, report.CurrentFilteredLineCount);
        Assert.Equal(0L, report.CurrentLineNumber);
    }



    [Fact]
    public void Constructor_when_options_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FixedWidthReport(null!));
    }



    // The two positional constructors below are obsolete but still shipped, so they stay under
    // test until they are removed. CS0618 is suppressed here for exactly that reason.
#pragma warning disable CS0618 // Type or member is obsolete

    [Fact]
    public void Constructor_sets_all_properties()
    {
        var report = new FixedWidthReport
        (
            currentCount: 10,
            currentSkippedItemCount: 2,
            currentLineNumber: 15L
        );

        Assert.Equal
        (
            10,
            report.CurrentItemCount
        );
        Assert.Equal
        (
            2,
            report.CurrentSkippedItemCount
        );
        Assert.Equal
        (
            15L,
            report.CurrentLineNumber
        );
        // The three-parameter overload defaults the rejected/filtered counts to zero.
        Assert.Equal(0, report.CurrentRejectedItemCount);
        Assert.Equal(0, report.CurrentFilteredLineCount);
    }



    [Fact]
    public void Five_parameter_constructor_sets_rejected_and_filtered_counts()
    {
        var report = new FixedWidthReport
        (
            currentCount: 10,
            currentSkippedItemCount: 2,
            currentRejectedItemCount: 3,
            currentFilteredLineCount: 4,
            currentLineNumber: 20L
        );

        Assert.Equal(10, report.CurrentItemCount);
        Assert.Equal(2, report.CurrentSkippedItemCount);
        Assert.Equal(3, report.CurrentRejectedItemCount);
        Assert.Equal(4, report.CurrentFilteredLineCount);
        Assert.Equal(20L, report.CurrentLineNumber);
    }



    [Fact]
    public void Constructor_with_zero_values_is_valid()
    {
        var report = new FixedWidthReport
        (
            currentCount: 0,
            currentSkippedItemCount: 0,
            currentLineNumber: 0L
        );

        Assert.Equal
        (
            0,
            report.CurrentItemCount
        );
        Assert.Equal
        (
            0,
            report.CurrentSkippedItemCount
        );
        Assert.Equal
        (
            0L,
            report.CurrentLineNumber
        );
    }

#pragma warning restore CS0618
}
