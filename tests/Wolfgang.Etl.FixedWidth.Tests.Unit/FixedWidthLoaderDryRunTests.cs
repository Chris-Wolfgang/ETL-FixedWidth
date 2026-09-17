using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wolfgang.Etl.TestKit.Xunit;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Dry-run behaviour, configured through <see cref="FixedWidthLoaderOptions.IsDryRun"/>. The behavioural
/// contract (a dry run skips the side effect, a normal run performs it) comes from the TestKit base, which
/// is non-generic as of Abstractions 0.24; the facts below cover what the contract does not: the option's
/// default, its application, and that a dry run still counts.
/// </summary>
public class FixedWidthLoaderDryRunTests : SupportsDryRunContractTests
{
    private static readonly IReadOnlyList<PersonRecord> SourceItems = new List<PersonRecord>
    {
        new() { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new() { FirstName = "Bob", LastName = "Brown", Age = 30 },
    };



    /// <inheritdoc />
    protected override async Task<bool> RunAndReportSideEffectAsync(bool isDryRun)
    {
        var stream = new MemoryStream();
        var sut = new FixedWidthLoader<PersonRecord>(stream, new FixedWidthLoaderStreamOptions { IsDryRun = isDryRun });

        await sut.LoadAsync(SourceItems.ToAsyncEnumerable());

        // The Stream constructor owns (and flushes) the writer, so any bytes written show up here.
        return stream.Length > 0;
    }



    [Fact]
    public void IsDryRun_when_options_leave_it_unset_defaults_to_false()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(new MemoryStream(), new FixedWidthLoaderStreamOptions());

        Assert.False(sut.IsDryRun);
    }



    [Fact]
    public void IsDryRun_when_set_through_options_is_applied()
    {
        using var sut = new FixedWidthLoader<PersonRecord>(new MemoryStream(), new FixedWidthLoaderStreamOptions { IsDryRun = true });

        Assert.True(sut.IsDryRun);
    }



    [Fact]
    public async Task LoadAsync_when_IsDryRun_is_set_through_options_still_counts_the_items()
    {
        var sut = new FixedWidthLoader<PersonRecord>(new MemoryStream(), new FixedWidthLoaderStreamOptions { IsDryRun = true });

        await sut.LoadAsync(SourceItems.ToAsyncEnumerable());

        Assert.Equal(SourceItems.Count, sut.CurrentItemCount);
    }
}
