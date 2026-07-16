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
/// The default (<see langword="null"/>) uses the target type's natural style —
/// <c>Integer</c> for integral types, <c>Number</c> for decimal / floating-point —
/// so the permissive forms (currency, exponent, parentheses) must be opted into.
/// </summary>
public class FixedWidthNumberStylesTests
{
    [ExcludeFromCodeCoverage]
    private class MoneyRecord
    {
        // Default (null) -> Number for decimal: allows sign, decimal, thousands.
        [FixedWidthField(0, 12)]
        public decimal Amount { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class PlainIntRecord
    {
        // Default (null) -> Integer for int: no decimals, no thousands.
        [FixedWidthField(0, 12)]
        public int Value { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class AnyMoneyRecord
    {
        // Explicit opt-in to the permissive forms (currency, parentheses, ...).
        [FixedWidthField(0, 12, NumberStyles = NumberStyles.Any)]
        public decimal Amount { get; set; }
    }



    private enum Priority
    {
        Low,
        Medium,
        High,
    }



    [ExcludeFromCodeCoverage]
    private class TicketRecord
    {
        [FixedWidthField(0, 8)]
        public Priority Priority { get; set; }
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
    public async Task Default_for_decimal_allows_thousands_and_decimal_point()
    {
        var records = await ExtractAsync<MoneyRecord>("1,234.56    ");

        Assert.Equal(1234.56m, Assert.Single(records).Amount);
    }



    [Fact]
    public async Task Default_for_decimal_rejects_a_parenthesized_negative()
    {
        // Parentheses are not part of the natural (Number) style — rejected.
        var extractor = new FixedWidthExtractor<MoneyRecord>(new StringReader("(500)       "))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        var results = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Empty(results);
        Assert.Equal(1, extractor.CurrentRejectedItemCount);
    }



    [Fact]
    public async Task Default_for_int_parses_a_plain_integer()
    {
        var records = await ExtractAsync<PlainIntRecord>("1234        ");

        Assert.Equal(1234, Assert.Single(records).Value);
    }



    [Fact]
    public async Task Default_for_int_rejects_a_decimal_point()
    {
        // Integer (the natural style for int) disallows the decimal point.
        var extractor = new FixedWidthExtractor<PlainIntRecord>(new StringReader("12.5        "))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        var results = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Empty(results);
        Assert.Equal(1, extractor.CurrentRejectedItemCount);
    }



    [Fact]
    public async Task Explicit_NumberStyles_Any_allows_a_parenthesized_negative()
    {
        var records = await ExtractAsync<AnyMoneyRecord>("(500)       ");

        Assert.Equal(-500m, Assert.Single(records).Amount);
    }



    [Fact]
    public async Task Enum_field_parses_by_member_name_not_its_underlying_integer()
    {
        // Enums share a TypeCode with their underlying integral type; the parser
        // must route them to the TypeConverter (which parses "High"), not to
        // int.Parse.
        var records = await ExtractAsync<TicketRecord>("High    ");

        Assert.Equal(Priority.High, Assert.Single(records).Priority);
    }
}
