window.BENCHMARK_DATA = {
  "lastUpdate": 1778288207393,
  "repoUrl": "https://github.com/Chris-Wolfgang/ETL-FixedWidth",
  "entries": {
    "BenchmarkDotNet": [
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "distinct": true,
          "id": "c23e029afd9476ce0c78ea099c47821caf889638",
          "message": "ci: fix BDN report glob, merge per-class reports, pin action by SHA\n\nBoth follow-up review findings on PR #83:\n\n- BDN's --exporters json actually emits *-report-full-compressed.json\n  (not -compact); also one file per benchmark class, not a single\n  joined report. Verified locally against BDN 0.15.8 output.\n  Use jq to combine the per-class .Benchmarks arrays into one synthetic\n  report that github-action-benchmark can consume.\n- Pin benchmark-action/github-action-benchmark to v1.22.1 commit SHA,\n  matching the repo convention for third-party actions that publish to\n  gh-pages (see peaceiris/actions-gh-pages and softprops/action-gh-release).",
          "timestamp": "2026-05-08T20:52:04-04:00",
          "tree_id": "53b9435c87da3b8fa77cda671c19e9bbac106deb",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/c23e029afd9476ce0c78ea099c47821caf889638"
        },
        "date": 1778288206454,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 427356.92171223956,
            "unit": "ns",
            "range": "± 7394.806530448705"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 532432.619140625,
            "unit": "ns",
            "range": "± 12466.509683762033"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 459977.77652994794,
            "unit": "ns",
            "range": "± 760.6092329233128"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 512135.7119140625,
            "unit": "ns",
            "range": "± 10212.265805199973"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4144199.6588541665,
            "unit": "ns",
            "range": "± 3528.9213938193207"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4089750.21875,
            "unit": "ns",
            "range": "± 13228.434092580574"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4435602.700520833,
            "unit": "ns",
            "range": "± 10346.715769124598"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4130034.0885416665,
            "unit": "ns",
            "range": "± 20056.229243602702"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 42305883.61111111,
            "unit": "ns",
            "range": "± 316648.7088684513"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 41133611.58974359,
            "unit": "ns",
            "range": "± 151644.69688918145"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 40850562.15384615,
            "unit": "ns",
            "range": "± 74710.85795298382"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 40768306.58974359,
            "unit": "ns",
            "range": "± 58348.3941670873"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 258019.89892578125,
            "unit": "ns",
            "range": "± 1945.4202535645295"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 391955.0494791667,
            "unit": "ns",
            "range": "± 3596.2135525552126"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 614085.8499348959,
            "unit": "ns",
            "range": "± 1254.8461228621316"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 738338.9876302084,
            "unit": "ns",
            "range": "± 2067.0246096523083"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3723737.9557291665,
            "unit": "ns",
            "range": "± 75710.65167422095"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2968907.2981770835,
            "unit": "ns",
            "range": "± 25895.17238046954"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3947695.5598958335,
            "unit": "ns",
            "range": "± 53503.73025400911"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3506873.9010416665,
            "unit": "ns",
            "range": "± 10062.604943098237"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 33279349.5625,
            "unit": "ns",
            "range": "± 345875.75202041486"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 29294429.90625,
            "unit": "ns",
            "range": "± 248288.92844929872"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 34666538.177777775,
            "unit": "ns",
            "range": "± 219337.84720050576"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 30463801.833333332,
            "unit": "ns",
            "range": "± 388074.9791848989"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 444091.27132161456,
            "unit": "ns",
            "range": "± 30944.64475586541"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 453194.2242838542,
            "unit": "ns",
            "range": "± 11772.027490042408"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 958903.2522786459,
            "unit": "ns",
            "range": "± 24997.24933872326"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4981805.90625,
            "unit": "ns",
            "range": "± 24305.66268832613"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 42713160.86111111,
            "unit": "ns",
            "range": "± 474139.1778264839"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 428157404.6666667,
            "unit": "ns",
            "range": "± 1123139.7456774171"
          }
        ]
      }
    ]
  }
}