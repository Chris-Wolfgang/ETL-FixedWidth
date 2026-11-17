#if NET6_0_OR_GREATER
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// <c>Format</c> is applied when writing any <see cref="IFormattable"/>; these tests pin that
/// <see cref="DateOnly"/> and <see cref="TimeOnly"/> honour it on read too, so loader output
/// round-trips through the extractor.
/// </summary>
public sealed class FixedWidthDateOnlyTimeOnlyTests
{
    private sealed class DateRecord
    {
        [FixedWidthField(0, 8, Format = "yyyyMMdd")]
        public DateOnly Day { get; set; }



        [FixedWidthField(1, 6, Format = "HHmmss")]
        public TimeOnly Time { get; set; }



        [FixedWidthField(2, 8, Format = "yyyyMMdd")]
        public DateOnly? OptionalDay { get; set; }
    }



    [Fact]
    public async Task DateOnly_and_TimeOnly_with_a_Format_round_trip_through_loader_and_extractor()
    {
        var original = new DateRecord
        {
            Day = new DateOnly(2026, 11, 16),
            Time = new TimeOnly(13, 5, 9),
            OptionalDay = new DateOnly(2027, 1, 2),
        };

        using var writer = new StringWriter();
        using (var loader = new FixedWidthLoader<DateRecord>(writer))
        {
            await loader.LoadAsync(new[] { original }.ToAsyncEnumerable(), CancellationToken.None);
        }

        Assert.StartsWith("2026111613050920270102", writer.ToString(), StringComparison.Ordinal);

        using var extractor = new FixedWidthExtractor<DateRecord>(new StringReader(writer.ToString()));
        var read = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal(original.Day, read.Day);
        Assert.Equal(original.Time, read.Time);
        Assert.Equal(original.OptionalDay, read.OptionalDay);
    }



    [Fact]
    public async Task A_blank_nullable_DateOnly_reads_back_as_null()
    {
        using var extractor = new FixedWidthExtractor<DateRecord>(new StringReader("20261116130509" + "        "));
        var read = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Null(read.OptionalDay);
    }
}
#endif
