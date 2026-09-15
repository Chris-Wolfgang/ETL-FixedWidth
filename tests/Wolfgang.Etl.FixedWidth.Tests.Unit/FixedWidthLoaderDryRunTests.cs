using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Dry-run behaviour, configured through <see cref="FixedWidthLoaderOptions.IsDryRun"/>: a dry run
/// enumerates the source and counts, but writes nothing. Replaces the TestKit contract base, which
/// (at 0.23.x) constrains its subject to the retired <c>ISupportDryRun</c> interface.
/// </summary>
public class FixedWidthLoaderDryRunTests
{
    private static readonly IReadOnlyList<PersonRecord> SourceItems = new List<PersonRecord>
    {
        new() { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new() { FirstName = "Bob", LastName = "Brown", Age = 30 },
    };



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
    public async Task LoadAsync_when_not_a_dry_run_writes_to_the_stream()
    {
        var stream = new MemoryStream();
        var sut = new FixedWidthLoader<PersonRecord>(stream, new FixedWidthLoaderStreamOptions());

        await sut.LoadAsync(SourceItems.ToAsyncEnumerable());

        Assert.True(stream.Length > 0);
    }



    [Fact]
    public async Task LoadAsync_when_IsDryRun_is_set_through_options_writes_nothing_but_counts()
    {
        var stream = new MemoryStream();
        var sut = new FixedWidthLoader<PersonRecord>(stream, new FixedWidthLoaderStreamOptions { IsDryRun = true });

        await sut.LoadAsync(SourceItems.ToAsyncEnumerable());

        // The Stream constructor owns (and flushes) the writer, so any bytes written show up here.
        Assert.Equal(0, stream.Length);
        Assert.Equal(SourceItems.Count, sut.CurrentItemCount);
    }
}
