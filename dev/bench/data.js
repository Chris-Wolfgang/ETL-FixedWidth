window.BENCHMARK_DATA = {
  "lastUpdate": 1778338266986,
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
      },
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
          "id": "3459d7b55f0481551270b17b13f46ed11b5d5a1b",
          "message": "perf: span-based writes + DateTime parse to cut allocations\n\nEliminates intermediate string allocations on the writer hot path and\nthe DateTime/DateTimeOffset/TimeSpan parse path:\n\n- WriteFieldSegment / WriteHeaderSegmentTo replace text.PadLeft/PadRight\n  with a stack-allocated padded span (net8+) or a pooled char[] fallback,\n  then a single TextWriter.Write call per field.\n- WritePadding helper replaces the new string(' ', n) and\n  new string(separatorChar, n) calls used for skip-column gaps,\n  trailing padding, and separator lines.\n- ParseDateTimeValueSpan adds a span-based net8+ path for DateTime,\n  DateTimeOffset, and TimeSpan, avoiding the .ToString() allocation.\n\nMeasured with BenchmarkDotNet ShortRun on net10.0\n(LoaderBenchmarks.Memory_TextWriter):\n\n| Records | Allocated (baseline) | Allocated (this branch) |\n|---------|----------------------|--------------------------|\n| 1k      | 414 KB               | 211 KB  (-49%)           |\n| 10k     | 4865 KB              | 2834 KB (-42%)           |\n| 100k    | 44514 KB             | 24202 KB (-46%)          |\n\nDateTime-bearing records (DateTimeBenchmarks, 10k):\n\n| Method         | Allocated baseline | Allocated this branch |\n|----------------|--------------------|------------------------|\n| Extract_Memory | 3.70 MB            | 3.32 MB (-10%)         |\n| Load_Memory    | 4.62 MB            | 3.40 MB (-26%)         |\n\nMean times are within ShortRun noise (overlapping CIs) on the writer\nside and modestly faster on DateTime loading.\n\nAll 284 existing unit tests pass on net10.0 and net8.0.",
          "timestamp": "2026-05-08T21:16:51-04:00",
          "tree_id": "b7aee2c3b559ba10ecf15b0b2d3e680edf3b4274",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/3459d7b55f0481551270b17b13f46ed11b5d5a1b"
        },
        "date": 1778289702359,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4216654.0703125,
            "unit": "ns",
            "range": "± 21493.96151748553"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3266485.9973958335,
            "unit": "ns",
            "range": "± 62352.497316427245"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 402713.6380208333,
            "unit": "ns",
            "range": "± 1857.3030784993464"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 503332.5452473958,
            "unit": "ns",
            "range": "± 5414.93243621461"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 442728.58837890625,
            "unit": "ns",
            "range": "± 1670.6447032313163"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 497204.7731119792,
            "unit": "ns",
            "range": "± 5525.579775894468"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4138450.8385416665,
            "unit": "ns",
            "range": "± 7510.225524085662"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4201155.8828125,
            "unit": "ns",
            "range": "± 77914.8733532161"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4310558.083333333,
            "unit": "ns",
            "range": "± 23788.402272181003"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4242266.666666667,
            "unit": "ns",
            "range": "± 33565.6364888011"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 42068885.13888889,
            "unit": "ns",
            "range": "± 74377.92531270135"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 42161264.138888896,
            "unit": "ns",
            "range": "± 384407.2058763389"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 46855351.30303031,
            "unit": "ns",
            "range": "± 175378.54104864248"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 42902522.472222224,
            "unit": "ns",
            "range": "± 81157.05362248544"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 233335.1472981771,
            "unit": "ns",
            "range": "± 3038.9611827939507"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 342637.8642578125,
            "unit": "ns",
            "range": "± 1234.2842319571396"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 654039.0690104166,
            "unit": "ns",
            "range": "± 5050.678781115413"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 629988.0810546875,
            "unit": "ns",
            "range": "± 12256.86091887072"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3341738.4114583335,
            "unit": "ns",
            "range": "± 19736.324064813696"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2845411.22265625,
            "unit": "ns",
            "range": "± 12247.886513011083"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3590364.2708333335,
            "unit": "ns",
            "range": "± 28576.209498044755"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3508552.41015625,
            "unit": "ns",
            "range": "± 21185.787485073964"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 24852544.041666668,
            "unit": "ns",
            "range": "± 80689.20139030507"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25083898.875,
            "unit": "ns",
            "range": "± 76564.91509043593"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 41439276.888888896,
            "unit": "ns",
            "range": "± 2999218.9303766396"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 31759313.604166668,
            "unit": "ns",
            "range": "± 377816.5008422643"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 433835.86246744794,
            "unit": "ns",
            "range": "± 22433.531349980127"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 436311.51871744794,
            "unit": "ns",
            "range": "± 17877.37639236097"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 998214.4521484375,
            "unit": "ns",
            "range": "± 22945.390374484454"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4789709.1171875,
            "unit": "ns",
            "range": "± 17784.552516459145"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 42202302.86111111,
            "unit": "ns",
            "range": "± 413193.29327156156"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 417873110.6666667,
            "unit": "ns",
            "range": "± 5088506.430467621"
          }
        ]
      },
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
          "id": "6dbffc3598d734622e678c7ac6fcdca584fc3497",
          "message": "test: address Copilot feedback on review naming + threshold robustness\n\n- Rename ParseValue numeric/bool tests to describe behavior\n  (\"returns_parsed_value\") instead of implementation detail\n  (\"uses_span_fast_path\"). The tests run on every TFM and exercise\n  the span path on net8+ or the TypeDescriptor fallback on older TFMs;\n  either way the observable behavior is the same.\n- Widen the >stackalloc-threshold tests' field length from 300 to 1024\n  and reword the comment to be honest about the dependency on the\n  WriteFieldSegment stackalloc cap. The test still requires the field\n  width to exceed the cap; 1024 just leaves more headroom.",
          "timestamp": "2026-05-09T10:46:18-04:00",
          "tree_id": "0e6cdedefdee0ccecab8cfc13c7496497dea4459",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/6dbffc3598d734622e678c7ac6fcdca584fc3497"
        },
        "date": 1778338266037,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 3942746.5651041665,
            "unit": "ns",
            "range": "± 22667.225032962608"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3041857.078125,
            "unit": "ns",
            "range": "± 38179.32252355142"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 407828.6165364583,
            "unit": "ns",
            "range": "± 2575.5482833380483"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 513970.2353515625,
            "unit": "ns",
            "range": "± 8623.596452312679"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 436093.29573567706,
            "unit": "ns",
            "range": "± 441.51228246915235"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 505143.001953125,
            "unit": "ns",
            "range": "± 2069.6854758083255"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 3930599.7291666665,
            "unit": "ns",
            "range": "± 31347.8662923844"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4063511.0104166665,
            "unit": "ns",
            "range": "± 27824.480073356637"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4126135.46875,
            "unit": "ns",
            "range": "± 13829.779595901144"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4161590.6276041665,
            "unit": "ns",
            "range": "± 30342.90377549906"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 41627050.05128205,
            "unit": "ns",
            "range": "± 852055.8785878181"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 39305051.69230769,
            "unit": "ns",
            "range": "± 76182.39404572928"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 41903504.80555556,
            "unit": "ns",
            "range": "± 277448.6941799685"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 40815809.589743584,
            "unit": "ns",
            "range": "± 432692.1970560855"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 231232.955078125,
            "unit": "ns",
            "range": "± 1591.5464049593556"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 354817.0296223958,
            "unit": "ns",
            "range": "± 1322.6248020107919"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 534315.5166015625,
            "unit": "ns",
            "range": "± 5428.245100518981"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 600675.9449869791,
            "unit": "ns",
            "range": "± 27006.85121019023"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3508497.2213541665,
            "unit": "ns",
            "range": "± 27042.817046142918"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2998870.8854166665,
            "unit": "ns",
            "range": "± 4541.9113826200355"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3516834.2552083335,
            "unit": "ns",
            "range": "± 88349.17426378651"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3195672.390625,
            "unit": "ns",
            "range": "± 25711.388411858963"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 25247183.447916668,
            "unit": "ns",
            "range": "± 683225.4820548052"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 26394036.270833332,
            "unit": "ns",
            "range": "± 922182.1451952781"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 36535382.77777778,
            "unit": "ns",
            "range": "± 1323826.9787235283"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 26957460.395833332,
            "unit": "ns",
            "range": "± 122146.35821302021"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 401982.7532552083,
            "unit": "ns",
            "range": "± 5615.5936937582765"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 412685.23828125,
            "unit": "ns",
            "range": "± 18341.084266481725"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 890905.2685546875,
            "unit": "ns",
            "range": "± 6514.074176217474"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4567763.450520833,
            "unit": "ns",
            "range": "± 27125.720887769465"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 42766467.916666664,
            "unit": "ns",
            "range": "± 866047.8272933477"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 397023249.3333333,
            "unit": "ns",
            "range": "± 3096678.930127619"
          }
        ]
      }
    ]
  }
}