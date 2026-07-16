using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit.Globalization;

/// <summary>
/// Verifies that the extractor and loader are culture-invariant (#155): the same
/// input must produce the same output regardless of the ambient
/// <see cref="CultureInfo.CurrentCulture"/> / <see cref="CultureInfo.CurrentUICulture"/>.
/// The suite runs under a matrix of hostile cultures — <c>en-US</c>,
/// <c>tr-TR</c> (dotted-I), <c>de-DE</c> (decimal comma), <c>zh-CN</c>,
/// <c>ar-SA</c> (Hindi-Arabic digits, RTL), and <c>ja-JP</c> (full-width digits) —
/// exercising the number-separator, digit-shape, and case-folding bug classes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Culture-sensitivity allowlist.</b> This library exposes <em>no</em> public
/// method that is intentionally culture-sensitive. Every parse routes through
/// <see cref="CultureInfo.InvariantCulture"/> (numeric <c>Parse</c>, <c>DateTime.ParseExact</c>,
/// and <c>TypeConverter.ConvertFromInvariantString</c>) and every format writes with
/// <see cref="CultureInfo.InvariantCulture"/>. The allowlist is therefore empty and
/// these tests assert the whole surface is invariant by contract — if a future edit
/// drops a <c>culture</c> argument, the non-<c>en-US</c> rows fail.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public class CultureInvarianceTests
{
    /// <summary>
    /// Hostile cultures the round-trip runs under. <c>en-US</c> is the baseline;
    /// the rest each stress a distinct globalization bug class.
    /// </summary>
    public static readonly IEnumerable<object[]> HostileCultures = new[]
    {
        new object[] { "en-US" },
        new object[] { "tr-TR" },
        new object[] { "de-DE" },
        new object[] { "zh-CN" },
        new object[] { "ar-SA" },
        new object[] { "ja-JP" },
    };



    private enum Priority
    {
        Low,
        Medium,
        High,
    }



    [ExcludeFromCodeCoverage]
    private record SampleRecord
    {
        // decimal + double exercise the separator classes (de-DE comma vs dot).
        [FixedWidthField(0, 12)]
        public decimal Amount { get; set; }

        [FixedWidthField(1, 12)]
        public double Rate { get; set; }

        // ParseExact / ToString(format) with a fixed pattern.
        [FixedWidthField(2, 8, Format = "yyyyMMdd")]
        public DateTime BirthDate { get; set; }

        [FixedWidthField(3, 6, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Count { get; set; }

        // Enum member-name round-trip via the TypeConverter fallback (tr-TR case-folding).
        [FixedWidthField(4, 8)]
        public Priority Priority { get; set; }
    }



    private static readonly IReadOnlyList<SampleRecord> Records = new[]
    {
        new SampleRecord { Amount = 1234.56m, Rate = 3.14159d, BirthDate = new DateTime(2026, 7, 16), Count = 42, Priority = Priority.High },
        new SampleRecord { Amount = -0.5m, Rate = 1000.25d, BirthDate = new DateTime(1999, 12, 31), Count = 7, Priority = Priority.Low },
    };



    private static async Task<string> SerializeAsync(IEnumerable<SampleRecord> records)
    {
        var writer = new StringWriter();
        using var loader = new FixedWidthLoader<SampleRecord>(writer);
        await loader.LoadAsync(records.ToAsyncEnumerable(), CancellationToken.None);
        return writer.ToString();
    }



    private static async Task<List<SampleRecord>> DeserializeAsync(string text)
    {
        var results = new List<SampleRecord>();
        using var extractor = new FixedWidthExtractor<SampleRecord>(new StringReader(text));
        await foreach (var record in extractor.ExtractAsync(CancellationToken.None))
        {
            results.Add(record);
        }

        return results;
    }



    // The invariant baseline: what the loader emits with no culture swap in effect.
    private static readonly Lazy<string> InvariantBaseline =
        new(() => SerializeAsync(Records).GetAwaiter().GetResult());



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task Loader_emits_identical_text_under_every_culture(string cultureName)
    {
        using var _ = new CultureSwapper(cultureName);

        var text = await SerializeAsync(Records);

        Assert.Equal(InvariantBaseline.Value, text);
    }



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task Extractor_round_trips_every_field_under_every_culture(string cultureName)
    {
        using var _ = new CultureSwapper(cultureName);

        var text = await SerializeAsync(Records);
        var roundTripped = await DeserializeAsync(text);

        Assert.Equal(Records, roundTripped);
    }



    [Fact]
    public async Task Decimal_point_is_the_decimal_separator_even_under_de_DE()
    {
        // In de-DE, ',' is the decimal separator and '.' is the group separator.
        // Invariant parsing must treat '.' as the decimal point: "1,234.56" -> 1234.56m,
        // NOT 123456m. A dropped InvariantCulture argument would flip this.
        using var _ = new CultureSwapper("de-DE");

        // Build the 46-char line from correctly-sized fields: Amount(12) Rate(12)
        // BirthDate(8) Count(6) Priority(8). Only Amount carries the '.'-vs-',' probe.
        var line = "1,234.56".PadRight(12)   // Amount -> 1234.56 (invariant), not 123456
            + "0".PadRight(12)               // Rate
            + "20260716"                     // BirthDate
            + "42".PadLeft(6, '0')           // Count
            + "Low".PadRight(8);             // Priority

        var records = await DeserializeAsync(line);

        Assert.Equal(1234.56m, Assert.Single(records).Amount);
    }



    [Fact]
    public async Task Enum_member_name_round_trips_under_tr_TR()
    {
        // The Turkish dotted/dotless-I is the classic case-folding hazard. The
        // enum round-trip must not depend on culture-sensitive casing.
        using var _ = new CultureSwapper("tr-TR");

        var text = await SerializeAsync(Records);
        var roundTripped = await DeserializeAsync(text);

        Assert.Equal(Records.Select(r => r.Priority), roundTripped.Select(r => r.Priority));
    }
}
