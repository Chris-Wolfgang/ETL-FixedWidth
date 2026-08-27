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
}
