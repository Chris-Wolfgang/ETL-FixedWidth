using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the optional <see cref="Encoding"/> parameter on the
/// <see cref="Stream"/>-based constructors of the extractor and loader (#16).
/// </summary>
public class FixedWidthEncodingTests
{
    private static readonly IReadOnlyList<PersonRecord> People = new List<PersonRecord>
    {
        new() { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new() { FirstName = "Bob", LastName = "Brown", Age = 30 },
    };



    [Fact]
    public async Task Loader_writes_in_the_supplied_encoding_and_extractor_reads_it_back()
    {
        var stream = new MemoryStream();

        var loader = new FixedWidthLoader<PersonRecord>(stream, Encoding.Unicode);
        await loader.LoadAsync(People.ToAsyncEnumerable(), CancellationToken.None);

        stream.Position = 0;
        var extractor = new FixedWidthExtractor<PersonRecord>(stream, Encoding.Unicode);
        var readBack = new List<PersonRecord>();
        await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
        {
            readBack.Add(person);
        }

        Assert.Equal(People, readBack);
    }



    [Fact]
    public async Task Loader_with_UTF8_no_BOM_encoding_omits_the_byte_order_mark()
    {
        var stream = new MemoryStream();

        using (var loader = new FixedWidthLoader<PersonRecord>(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            await loader.LoadAsync(People.ToAsyncEnumerable(), CancellationToken.None);
        }

        var bytes = stream.ToArray();
        var utf8Bom = Encoding.UTF8.GetPreamble();

        Assert.False
        (
            bytes.Take(utf8Bom.Length).SequenceEqual(utf8Bom)
        );
    }



    [Fact]
    public async Task Loader_with_default_encoding_writes_the_UTF8_BOM()
    {
        var stream = new MemoryStream();

        using (var loader = new FixedWidthLoader<PersonRecord>(stream))
        {
            await loader.LoadAsync(People.ToAsyncEnumerable(), CancellationToken.None);
        }

        var bytes = stream.ToArray();
        var utf8Bom = Encoding.UTF8.GetPreamble();

        Assert.True
        (
            bytes.Take(utf8Bom.Length).SequenceEqual(utf8Bom)
        );
    }
}
