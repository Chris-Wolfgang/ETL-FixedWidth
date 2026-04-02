using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

[MemoryDiagnoser]
public class ExtractorBenchmarks
{
    private byte[] _data = Array.Empty<byte>();
    private string _filePath = string.Empty;



    [Params(1_000, 10_000, 100_000)]
    public int RecordCount { get; set; }



    [GlobalSetup]
    public async Task Setup()
    {
        // Each line: FirstName(20) + LastName(20) + City(10) + ZipCode(5) + Age(3) = 58 chars
        var sb = new StringBuilder(RecordCount * 60);
        for (var i = 0; i < RecordCount; i++)
        {
            sb.Append("John                ");   // 20
            sb.Append("Smith               ");   // 20
            sb.Append("Seattle   ");             // 10
            sb.Append("98101");                  // 5
            sb.Append("042");                    // 3
            sb.AppendLine();
        }

        _data = Encoding.UTF8.GetBytes(sb.ToString());

        _filePath = Path.Combine(Path.GetTempPath(), $"fw_bench_extract_{RecordCount}.txt");
        await File.WriteAllBytesAsync(_filePath, _data);
    }



    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }



    // ------------------------------------------------------------------
    // In-memory (MemoryStream) — isolates parsing cost from I/O
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public async Task<int> Memory_TextReader()
    {
        using var reader = new StreamReader
        (
            new MemoryStream(_data),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: false
        );
        var extractor = new FixedWidthExtractor<BenchmarkRecord>(reader);

        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }

        return count;
    }



    [Benchmark]
    public async Task<int> Memory_Stream()
    {
        using var stream = new MemoryStream(_data);
        using var extractor = new FixedWidthExtractor<BenchmarkRecord>(stream);

        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }

        return count;
    }



    // ------------------------------------------------------------------
    // File-backed — shows real I/O benefit of 64 KB buffer
    // ------------------------------------------------------------------

    [Benchmark]
    public async Task<int> File_TextReader_1KB()
    {
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024);
        var extractor = new FixedWidthExtractor<BenchmarkRecord>(reader);

        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }

        return count;
    }



    [Benchmark]
    public async Task<int> File_Stream_64KB()
    {
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        using var extractor = new FixedWidthExtractor<BenchmarkRecord>(stream);

        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }

        return count;
    }
}
