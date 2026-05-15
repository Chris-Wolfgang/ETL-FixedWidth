window.BENCHMARK_DATA = {
  "lastUpdate": 1778883987328,
  "repoUrl": "https://github.com/Chris-Wolfgang/ETL-FixedWidth",
  "entries": {
    "BenchmarkDotNet": [
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "distinct": true,
          "id": "c7e42d68ea9505302d43f578752a2066ad9d2360",
          "message": "Bump the dotnet-dependencies group with 3 updates\n\nBumps Meziantou.Analyzer from 3.0.76 to 3.0.85\nBumps Microsoft.Bcl.AsyncInterfaces from 10.0.7 to 10.0.8\nBumps Microsoft.Extensions.Logging.Abstractions from 10.0.7 to 10.0.8\n\n---\nupdated-dependencies:\n- dependency-name: Meziantou.Analyzer\n  dependency-version: 3.0.85\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: dotnet-dependencies\n- dependency-name: Microsoft.Bcl.AsyncInterfaces\n  dependency-version: 10.0.8\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: dotnet-dependencies\n- dependency-name: Microsoft.Bcl.AsyncInterfaces\n  dependency-version: 10.0.8\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: dotnet-dependencies\n- dependency-name: Microsoft.Extensions.Logging.Abstractions\n  dependency-version: 10.0.8\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: dotnet-dependencies\n- dependency-name: Microsoft.Extensions.Logging.Abstractions\n  dependency-version: 10.0.8\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: dotnet-dependencies\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>",
          "timestamp": "2026-05-15T18:21:31-04:00",
          "tree_id": "de52dab8ec367b39dea099e66dbc73a283933643",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/c7e42d68ea9505302d43f578752a2066ad9d2360"
        },
        "date": 1778883986246,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4201554.643229167,
            "unit": "ns",
            "range": "± 51193.98294145984"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3067095.8723958335,
            "unit": "ns",
            "range": "± 66629.92115761866"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 413116.50472005206,
            "unit": "ns",
            "range": "± 3565.0513016481896"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 490531.9010416667,
            "unit": "ns",
            "range": "± 13974.849794300762"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 434110.4348958333,
            "unit": "ns",
            "range": "± 1139.068708625726"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 506742.0143229167,
            "unit": "ns",
            "range": "± 93.59216295190603"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4001213.7630208335,
            "unit": "ns",
            "range": "± 12545.117460341708"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4058041.5546875,
            "unit": "ns",
            "range": "± 3814.6954289480577"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4261858.2578125,
            "unit": "ns",
            "range": "± 58051.95970726994"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4404888.192708333,
            "unit": "ns",
            "range": "± 28494.488189144144"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 40615070.102564104,
            "unit": "ns",
            "range": "± 255040.64728492996"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 41734674.611111104,
            "unit": "ns",
            "range": "± 114465.85101783939"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 43276241.02777778,
            "unit": "ns",
            "range": "± 113959.8781483436"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 40480557.384615384,
            "unit": "ns",
            "range": "± 227018.48815166467"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 229836.85400390625,
            "unit": "ns",
            "range": "± 202.57895058369147"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 360939.2633463542,
            "unit": "ns",
            "range": "± 2967.489169151492"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 519748.5315755208,
            "unit": "ns",
            "range": "± 2049.3817699334127"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 643632.9973958334,
            "unit": "ns",
            "range": "± 12644.362354427134"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3594252.1692708335,
            "unit": "ns",
            "range": "± 14316.550279790255"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2905447.2877604165,
            "unit": "ns",
            "range": "± 9667.235686110655"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3513638.7265625,
            "unit": "ns",
            "range": "± 70867.07887833596"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3185674.6302083335,
            "unit": "ns",
            "range": "± 6647.356433699468"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 25183012.4375,
            "unit": "ns",
            "range": "± 811499.3293858647"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25037409.84375,
            "unit": "ns",
            "range": "± 142319.73457655963"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 31848041.354166668,
            "unit": "ns",
            "range": "± 168645.11551883345"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 27198189.697916668,
            "unit": "ns",
            "range": "± 364389.49775029504"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 401631.4729817708,
            "unit": "ns",
            "range": "± 8394.864166603214"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 398011.2438151042,
            "unit": "ns",
            "range": "± 9097.979672887246"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 896162.1575520834,
            "unit": "ns",
            "range": "± 8505.131064059979"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4472221.341145833,
            "unit": "ns",
            "range": "± 13701.217709016204"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 42187580.083333336,
            "unit": "ns",
            "range": "± 463577.50869158097"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 418388817.3333333,
            "unit": "ns",
            "range": "± 1880606.782510723"
          }
        ]
      }
    ]
  }
}