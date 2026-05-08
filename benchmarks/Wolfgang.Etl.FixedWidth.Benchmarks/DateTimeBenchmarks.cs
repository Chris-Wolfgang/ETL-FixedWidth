using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

/// <summary>
/// Exercises the reader/writer hot paths with a record that contains a
/// <see cref="DateTime"/> field, isolating the span-based DateTime parse path
/// added in the perf optimization branch.
/// </summary>
[MemoryDiagnoser]
public class DateTimeBenchmarks
{
    private byte[] _data = Array.Empty<byte>();
    private BenchmarkRecordWithDate[] _records = Array.Empty<BenchmarkRecordWithDate>();



    [Params(10_000)]
    public int RecordCount { get; set; }



    [GlobalSetup]
    public void Setup()
    {
        _records = new BenchmarkRecordWithDate[RecordCount];
        var sb = new StringBuilder(RecordCount * 60);
        for (var i = 0; i < RecordCount; i++)
        {
            _records[i] = new BenchmarkRecordWithDate
            {
                FirstName = "John",
                LastName = "Smith",
                BirthDate = new DateTime(1980, 1, 1).AddDays(i % 10000),
                ZipCode = 98101,
            };

            sb.Append("John                ");                                         // 20
            sb.Append("Smith               ");                                         // 20
            sb.Append(_records[i].BirthDate.ToString("yyyyMMdd"));                     //  8
            sb.Append("98101");                                                        //  5
            sb.AppendLine();
        }

        _data = Encoding.UTF8.GetBytes(sb.ToString());
    }



    [Benchmark]
    public async Task<int> Extract_Memory()
    {
        using var stream = new MemoryStream(_data);
        using var extractor = new FixedWidthExtractor<BenchmarkRecordWithDate>(stream);
        var count = 0;
        await foreach (var _ in extractor.ExtractAsync())
        {
            count++;
        }
        return count;
    }



    [Benchmark]
    public async Task Load_Memory()
    {
        using var stream = new MemoryStream();
        using var loader = new FixedWidthLoader<BenchmarkRecordWithDate>(stream);
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
