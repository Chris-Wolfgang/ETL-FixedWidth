using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;
using Wolfgang.Etl.TestKit.Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Struct records pass the generic constraints, so they must work end to end: the field setters
/// have to mutate the boxed instance rather than a copy (#680).
/// </summary>
public sealed class FixedWidthStructRecordTests
{
    private record struct TextStruct
    {
        [FixedWidthField(0, 5)]
        public string Name { get; set; }



        [FixedWidthField(1, 5, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Num { get; set; }
    }



    private record struct BinaryStruct
    {
        [FixedWidthBinaryField(0, 4, BinaryFieldType.Text)]
        public string Code { get; set; }



        [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
        public int Count { get; set; }
    }



    [Fact]
    public async Task Text_extractor_populates_a_struct_record()
    {
        using var extractor = new FixedWidthExtractor<TextStruct>(new StringReader("ABCDE00042\nFGHIJ00007\n"));

        var records = await extractor.ExtractAsync(CancellationToken.None).ToListAsync();

        Assert.Equal(new[] { "ABCDE", "FGHIJ" }, records.Select(r => r.Name));
        Assert.Equal(new[] { 42, 7 }, records.Select(r => r.Num));
    }



    [Fact]
    public async Task Text_loader_and_extractor_round_trip_a_struct_record()
    {
        using var writer = new StringWriter { NewLine = "\n" };
        using (var loader = new FixedWidthLoader<TextStruct>(writer))
        {
            await loader.LoadAsync(new[] { new TextStruct { Name = "AB", Num = 9 } }.ToAsyncEnumerable(), CancellationToken.None);
        }

        using var extractor = new FixedWidthExtractor<TextStruct>(new StringReader(writer.ToString()));
        var read = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal("AB", read.Name);
        Assert.Equal(9, read.Num);
    }



    [Fact]
    public async Task Binary_loader_and_extractor_round_trip_a_struct_record()
    {
        using var stream = new MemoryStream();
        using (var loader = new FixedWidthBinaryLoader<BinaryStruct>(stream, options: null))
        {
            await loader.LoadAsync(new[] { new BinaryStruct { Code = "XY", Count = 5 } }.ToAsyncEnumerable(), CancellationToken.None);
        }

        Assert.Equal(8, stream.Length);

        using var read = new MemoryStream(stream.ToArray());
        using var extractor = new FixedWidthBinaryExtractor<BinaryStruct>(read, options: null);
        var record = Assert.Single(await extractor.ExtractAsync(CancellationToken.None).ToListAsync());

        Assert.Equal("XY", record.Code);
        Assert.Equal(5, record.Count);
    }
}
