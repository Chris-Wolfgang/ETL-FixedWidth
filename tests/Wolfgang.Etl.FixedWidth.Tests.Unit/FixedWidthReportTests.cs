using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthReportTests
{
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
            report.CurrentCount
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
            report.CurrentCount
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
}
