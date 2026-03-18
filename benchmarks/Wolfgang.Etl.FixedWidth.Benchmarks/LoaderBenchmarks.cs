using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Wolfgang.Etl.Abstractions;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

[MemoryDiagnoser]
public class LoaderBenchmarks
{
    private BenchmarkRecord[] _records = Array.Empty<BenchmarkRecord>();
    private string _filePath = string.Empty;



    [Params(1_000, 10_000, 100_000)]
    public int RecordCount { get; set; }



    [GlobalSetup]
    public void Setup()
    {
        _records = new BenchmarkRecord[RecordCount];
        for (var i = 0; i < RecordCount; i++)
        {
            _records[i] = new BenchmarkRecord
            {
                FirstName = "John",
                LastName = "Smith",
                City = "Seattle",
                ZipCode = 98101,
                Age = 42,
            };
        }

        _filePath = Path.Combine(Path.GetTempPath(), $"fw_bench_load_{RecordCount}.txt");
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
    // In-memory (MemoryStream) — isolates formatting cost from I/O
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public async Task Memory_TextWriter()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        var loader = new FixedWidthLoader<BenchmarkRecord, Report>(writer);

        await loader.LoadAsync(ToAsyncEnumerable(_records));
        await writer.FlushAsync();
    }



    [Benchmark]
    public async Task Memory_Stream()
    {
        using var stream = new MemoryStream();
        using var loader = new FixedWidthLoader<BenchmarkRecord, Report>(stream);

        await loader.LoadAsync(ToAsyncEnumerable(_records));
    }



    // ------------------------------------------------------------------
    // File-backed — shows real I/O benefit of 64 KB buffer
    // ------------------------------------------------------------------

    [Benchmark]
    public async Task File_TextWriter_1KB()
    {
        using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096);
        using var writer = new StreamWriter(stream, bufferSize: 1024);
        var loader = new FixedWidthLoader<BenchmarkRecord, Report>(writer);

        await loader.LoadAsync(ToAsyncEnumerable(_records));
        await writer.FlushAsync();
    }



    [Benchmark]
    public async Task File_Stream_64KB()
    {
        using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096);
        using var loader = new FixedWidthLoader<BenchmarkRecord, Report>(stream);

        await loader.LoadAsync(ToAsyncEnumerable(_records));
    }



    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
