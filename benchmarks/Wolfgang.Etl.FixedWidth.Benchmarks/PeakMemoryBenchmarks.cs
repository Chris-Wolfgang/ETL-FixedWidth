using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

[MemoryDiagnoser]
public class PeakMemoryBenchmarks
{
    private byte[][] _dataBySize = Array.Empty<byte[]>();



    [Params(0, 1, 1_000, 10_000, 100_000, 1_000_000)]
    public int RecordCount { get; set; }



    [GlobalSetup]
    public void Setup()
    {
        _dataBySize = new byte[1][];
        _dataBySize[0] = BuildData(RecordCount);
    }



    private static byte[] BuildData(int count)
    {
        if (count == 0) return Encoding.UTF8.GetBytes("");

        var sb = new StringBuilder(count * 60);
        for (var i = 0; i < count; i++)
        {
            sb.Append("John                ");   // 20
            sb.Append("Smith               ");   // 20
            sb.Append("Seattle   ");             // 10
            sb.Append("98101");                  // 5
            sb.Append("042");                    // 3
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }



    [Benchmark]
    public async Task<long> Extract_PeakMemory()
    {
        var before = GC.GetTotalMemory(forceFullCollection: true);

        using var stream = new MemoryStream(_dataBySize[0]);
        var extractor = new FixedWidthExtractor<BenchmarkRecord>(stream);

        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);
        return after - before;
    }
}
