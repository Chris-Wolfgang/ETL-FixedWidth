using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the <see cref="FixedWidthLoader{TRecord}.LineEnding"/> property (#17).
/// </summary>
public class FixedWidthLineEndingTests
{
    private static readonly IReadOnlyList<PersonRecord> People = new List<PersonRecord>
    {
        new() { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new() { FirstName = "Bob", LastName = "Brown", Age = 30 },
    };



    private static async Task<string> LoadAsync(LineEnding lineEnding)
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer) { LineEnding = lineEnding };

        await loader.LoadAsync(People.ToAsyncEnumerable(), CancellationToken.None);

        return writer.ToString();
    }



    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }

        return count;
    }



    [Fact]
    public async Task LineEnding_Lf_uses_line_feed_with_a_trailing_newline()
    {
        var output = await LoadAsync(LineEnding.Lf);

        Assert.DoesNotContain("\r", output);
        Assert.EndsWith("\n", output);
        // Two records, each followed by a line feed (trailing included).
        Assert.Equal(2, output.Count(c => c == '\n'));
    }



    [Fact]
    public async Task LineEnding_CrLf_uses_carriage_return_line_feed()
    {
        var output = await LoadAsync(LineEnding.CrLf);

        Assert.EndsWith("\r\n", output);
        Assert.Equal(2, CountOccurrences(output, "\r\n"));
    }



    [Fact]
    public async Task LineEnding_None_omits_the_trailing_newline()
    {
        var output = await LoadAsync(LineEnding.None);

        Assert.False(output.EndsWith("\n", StringComparison.Ordinal));
        // Two records → a single inter-record separator, no trailing newline.
        Assert.Equal(1, CountOccurrences(output, Environment.NewLine));
    }



    [Fact]
    public async Task LineEnding_Default_ends_with_the_platform_newline()
    {
        var output = await LoadAsync(LineEnding.Default);

        Assert.EndsWith(Environment.NewLine, output);
        Assert.Equal(2, CountOccurrences(output, Environment.NewLine));
    }
}
