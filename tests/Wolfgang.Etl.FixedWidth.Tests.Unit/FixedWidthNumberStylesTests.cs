using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the <see cref="FixedWidthFieldAttribute.NumberStyles"/> property (#9).
/// </summary>
public class FixedWidthNumberStylesTests
{
    [ExcludeFromCodeCoverage]
    private class MoneyRecord
    {
        // Default NumberStyles.Any — permissive.
        [FixedWidthField(0, 12)]
        public decimal Amount { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class StrictIntRecord
    {
        // Integer only — no decimals, no thousands separators.
        [FixedWidthField(0, 12, NumberStyles = NumberStyles.Integer)]
        public int Value { get; set; }
    }



    private static async Task<T[]> ExtractAsync<T>(string line, MalformedLineHandling malformed = MalformedLineHandling.ThrowException)
        where T : notnull, new()
    {
        var extractor = new FixedWidthExtractor<T>(new StringReader(line))
        {
            MalformedLineHandling = malformed,
        };

        var results = new System.Collections.Generic.List<T>();
        await foreach (var record in extractor.ExtractAsync(CancellationToken.None))
        {
            results.Add(record);
        }

        return results.ToArray();
    }



    [Fact]
    public async Task Default_Any_parses_thousands_and_decimal()
    {
        var records = await ExtractAsync<MoneyRecord>("1,234.56    ");

        Assert.Equal(1234.56m, Assert.Single(records).Amount);
    }



    [Fact]
    public async Task Default_Any_parses_parenthesized_negative()
    {
        var records = await ExtractAsync<MoneyRecord>("(500)       ");

        Assert.Equal(-500m, Assert.Single(records).Amount);
    }



    [Fact]
    public async Task NumberStyles_Integer_parses_a_plain_integer()
    {
        var records = await ExtractAsync<StrictIntRecord>("1234        ");

        Assert.Equal(1234, Assert.Single(records).Value);
    }



    [Fact]
    public async Task NumberStyles_Integer_rejects_a_decimal_value()
    {
        // The Integer style disallows the decimal point, so parsing fails and the
        // malformed line is skipped (rejected) rather than yielded.
        var extractor = new FixedWidthExtractor<StrictIntRecord>(new StringReader("12.5        "))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        var results = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Empty(results);
        Assert.Equal(1, extractor.CurrentRejectedItemCount);
    }
}
