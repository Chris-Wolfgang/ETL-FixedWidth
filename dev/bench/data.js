window.BENCHMARK_DATA = {
  "lastUpdate": 1782591312040,
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
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "id": "c23e029afd9476ce0c78ea099c47821caf889638",
          "message": "ci: fix BDN report glob, merge per-class reports, pin action by SHA\n\nBoth follow-up review findings on PR #83:\n\n- BDN's --exporters json actually emits *-report-full-compressed.json\n  (not -compact); also one file per benchmark class, not a single\n  joined report. Verified locally against BDN 0.15.8 output.\n  Use jq to combine the per-class .Benchmarks arrays into one synthetic\n  report that github-action-benchmark can consume.\n- Pin benchmark-action/github-action-benchmark to v1.22.1 commit SHA,\n  matching the repo convention for third-party actions that publish to\n  gh-pages (see peaceiris/actions-gh-pages and softprops/action-gh-release).",
          "timestamp": "2026-05-09T00:36:01Z",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/c23e029afd9476ce0c78ea099c47821caf889638"
        },
        "date": 1779155005037,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 404820.8346354167,
            "unit": "ns",
            "range": "± 9077.337571566626"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 491367.4912109375,
            "unit": "ns",
            "range": "± 11678.286971093174"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 440093.2568359375,
            "unit": "ns",
            "range": "± 1131.9700650979785"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 522348.1878255208,
            "unit": "ns",
            "range": "± 19659.90676140321"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4017451.0651041665,
            "unit": "ns",
            "range": "± 15910.095768437732"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4092489.7135416665,
            "unit": "ns",
            "range": "± 79785.7849525659"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4518500.908854167,
            "unit": "ns",
            "range": "± 58314.737629378775"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4035564.0729166665,
            "unit": "ns",
            "range": "± 317.3287925502194"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 42918516.666666664,
            "unit": "ns",
            "range": "± 162766.02667754667"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 40135899,
            "unit": "ns",
            "range": "± 243057.24124685695"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 42791213.75,
            "unit": "ns",
            "range": "± 246291.12473744867"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 41903337.55555555,
            "unit": "ns",
            "range": "± 168460.44803483866"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 244885.9189453125,
            "unit": "ns",
            "range": "± 3906.0590202735316"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 397007.4007161458,
            "unit": "ns",
            "range": "± 279.0564769070675"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 544897.8844401041,
            "unit": "ns",
            "range": "± 5670.291990977501"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 661037.0921223959,
            "unit": "ns",
            "range": "± 37672.59732089716"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3822059.4869791665,
            "unit": "ns",
            "range": "± 26612.76796475462"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 3333933.8151041665,
            "unit": "ns",
            "range": "± 140033.52767379553"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3848918.4348958335,
            "unit": "ns",
            "range": "± 37932.03129258904"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3460756.4947916665,
            "unit": "ns",
            "range": "± 41212.34706770922"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 29699596.645833332,
            "unit": "ns",
            "range": "± 180455.611161613"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 29263244.208333332,
            "unit": "ns",
            "range": "± 129164.23000311348"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 33260725.844444443,
            "unit": "ns",
            "range": "± 237110.97249531126"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 29028406.25,
            "unit": "ns",
            "range": "± 71612.3623221193"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 395412.94059244794,
            "unit": "ns",
            "range": "± 8438.685136116377"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 404074.603515625,
            "unit": "ns",
            "range": "± 8560.985398593173"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 947940.0305989584,
            "unit": "ns",
            "range": "± 21051.754338829578"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4489685.309895833,
            "unit": "ns",
            "range": "± 23027.451681730377"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 40835434,
            "unit": "ns",
            "range": "± 226138.5644317403"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 401266760.6666667,
            "unit": "ns",
            "range": "± 2140489.1264251573"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "id": "3459d7b55f0481551270b17b13f46ed11b5d5a1b",
          "message": "perf: span-based writes + DateTime parse to cut allocations\n\nEliminates intermediate string allocations on the writer hot path and\nthe DateTime/DateTimeOffset/TimeSpan parse path:\n\n- WriteFieldSegment / WriteHeaderSegmentTo replace text.PadLeft/PadRight\n  with a stack-allocated padded span (net8+) or a pooled char[] fallback,\n  then a single TextWriter.Write call per field.\n- WritePadding helper replaces the new string(' ', n) and\n  new string(separatorChar, n) calls used for skip-column gaps,\n  trailing padding, and separator lines.\n- ParseDateTimeValueSpan adds a span-based net8+ path for DateTime,\n  DateTimeOffset, and TimeSpan, avoiding the .ToString() allocation.\n\nMeasured with BenchmarkDotNet ShortRun on net10.0\n(LoaderBenchmarks.Memory_TextWriter):\n\n| Records | Allocated (baseline) | Allocated (this branch) |\n|---------|----------------------|--------------------------|\n| 1k      | 414 KB               | 211 KB  (-49%)           |\n| 10k     | 4865 KB              | 2834 KB (-42%)           |\n| 100k    | 44514 KB             | 24202 KB (-46%)          |\n\nDateTime-bearing records (DateTimeBenchmarks, 10k):\n\n| Method         | Allocated baseline | Allocated this branch |\n|----------------|--------------------|------------------------|\n| Extract_Memory | 3.70 MB            | 3.32 MB (-10%)         |\n| Load_Memory    | 4.62 MB            | 3.40 MB (-26%)         |\n\nMean times are within ShortRun noise (overlapping CIs) on the writer\nside and modestly faster on DateTime loading.\n\nAll 284 existing unit tests pass on net10.0 and net8.0.",
          "timestamp": "2026-05-08T23:31:39Z",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/3459d7b55f0481551270b17b13f46ed11b5d5a1b"
        },
        "date": 1779155352138,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4298081.580729167,
            "unit": "ns",
            "range": "± 64885.278903267084"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3304413.3216145835,
            "unit": "ns",
            "range": "± 29364.72728133162"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 415567.66015625,
            "unit": "ns",
            "range": "± 4103.886608364988"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 503945.8759765625,
            "unit": "ns",
            "range": "± 11005.125242670265"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 426519.5380859375,
            "unit": "ns",
            "range": "± 1663.7466962372403"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 500433.7229817708,
            "unit": "ns",
            "range": "± 10265.896443414216"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4283786.015625,
            "unit": "ns",
            "range": "± 37392.22578417833"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 3957491.3567708335,
            "unit": "ns",
            "range": "± 12645.996293844799"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4168800.2135416665,
            "unit": "ns",
            "range": "± 36208.48833186619"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4313231.109375,
            "unit": "ns",
            "range": "± 24832.18448159547"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 39958175.41025641,
            "unit": "ns",
            "range": "± 257505.74786971748"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 39997145.41025641,
            "unit": "ns",
            "range": "± 46358.364153584014"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 41755406.615384616,
            "unit": "ns",
            "range": "± 184471.5572257258"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 40409636.74358974,
            "unit": "ns",
            "range": "± 81631.01688509586"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 225603.21394856772,
            "unit": "ns",
            "range": "± 1814.368624794429"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 371687.9895833333,
            "unit": "ns",
            "range": "± 1669.9149630192592"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 568494.6341145834,
            "unit": "ns",
            "range": "± 30654.90086886506"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 636615.6233723959,
            "unit": "ns",
            "range": "± 6394.5667886217925"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3521159.9088541665,
            "unit": "ns",
            "range": "± 10736.463623924197"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 3040017.9440104165,
            "unit": "ns",
            "range": "± 11260.409676956722"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3573455.9427083335,
            "unit": "ns",
            "range": "± 56661.68860969581"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3082757.15234375,
            "unit": "ns",
            "range": "± 6344.792198363988"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 25733243.791666668,
            "unit": "ns",
            "range": "± 786956.9012307953"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25380994.447916668,
            "unit": "ns",
            "range": "± 533988.4635309328"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 32300696.5,
            "unit": "ns",
            "range": "± 670590.141558305"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 27094205.020833332,
            "unit": "ns",
            "range": "± 150185.4251086007"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 401149.57421875,
            "unit": "ns",
            "range": "± 8936.7847373017"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 399250.96923828125,
            "unit": "ns",
            "range": "± 1572.0892738860014"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 874494.0432942709,
            "unit": "ns",
            "range": "± 17782.172849802744"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4782195.84375,
            "unit": "ns",
            "range": "± 19847.01723461601"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 43281261.361111104,
            "unit": "ns",
            "range": "± 712924.4742719276"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 400191884.6666667,
            "unit": "ns",
            "range": "± 6275816.586435134"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "id": "6dbffc3598d734622e678c7ac6fcdca584fc3497",
          "message": "test: address Copilot feedback on review naming + threshold robustness\n\n- Rename ParseValue numeric/bool tests to describe behavior\n  (\"returns_parsed_value\") instead of implementation detail\n  (\"uses_span_fast_path\"). The tests run on every TFM and exercise\n  the span path on net8+ or the TypeDescriptor fallback on older TFMs;\n  either way the observable behavior is the same.\n- Widen the >stackalloc-threshold tests' field length from 300 to 1024\n  and reword the comment to be honest about the dependency on the\n  WriteFieldSegment stackalloc cap. The test still requires the field\n  width to exceed the cap; 1024 just leaves more headroom.",
          "timestamp": "2026-05-09T14:31:56Z",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/6dbffc3598d734622e678c7ac6fcdca584fc3497"
        },
        "date": 1779155708630,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4145582.03125,
            "unit": "ns",
            "range": "± 31000.42900002921"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3085008.296875,
            "unit": "ns",
            "range": "± 31829.831889909627"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 398665.7604166667,
            "unit": "ns",
            "range": "± 1211.3595056139573"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 501076.8551432292,
            "unit": "ns",
            "range": "± 9683.042530127756"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 421319.72867838544,
            "unit": "ns",
            "range": "± 1611.415800548259"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 503656.8821614583,
            "unit": "ns",
            "range": "± 11191.593514802069"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4220623.111979167,
            "unit": "ns",
            "range": "± 228661.17110258923"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4044679.3411458335,
            "unit": "ns",
            "range": "± 15575.084718710696"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4267236.997395833,
            "unit": "ns",
            "range": "± 20426.84915041839"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4037035.546875,
            "unit": "ns",
            "range": "± 21754.441448501202"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 39498676.461538464,
            "unit": "ns",
            "range": "± 133426.8832119802"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 42900632.5,
            "unit": "ns",
            "range": "± 37992.29614050202"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 41395262.769230776,
            "unit": "ns",
            "range": "± 295141.78917402925"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 39349244.20512821,
            "unit": "ns",
            "range": "± 56619.26560727493"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 226304.44978841147,
            "unit": "ns",
            "range": "± 302.9997727467151"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 382535.73649088544,
            "unit": "ns",
            "range": "± 25706.18537635276"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 515565.6539713542,
            "unit": "ns",
            "range": "± 25556.541798010625"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 642128.4720052084,
            "unit": "ns",
            "range": "± 854.302294411455"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3495064.3802083335,
            "unit": "ns",
            "range": "± 2864.9218742871935"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2927148.7578125,
            "unit": "ns",
            "range": "± 56512.97385726196"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3473276.3098958335,
            "unit": "ns",
            "range": "± 52012.40601924861"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3168012.4908854165,
            "unit": "ns",
            "range": "± 10492.213257196498"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 25135970.572916668,
            "unit": "ns",
            "range": "± 777157.4804047773"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25038997,
            "unit": "ns",
            "range": "± 59253.63398300886"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 34500698.11111111,
            "unit": "ns",
            "range": "± 2388008.22290503"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 26777045.71875,
            "unit": "ns",
            "range": "± 76091.6216519534"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 392365.0934244792,
            "unit": "ns",
            "range": "± 8996.673156611043"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 415684.5279947917,
            "unit": "ns",
            "range": "± 19978.773546752374"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 869398.6591796875,
            "unit": "ns",
            "range": "± 18163.430434017693"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4424896.497395833,
            "unit": "ns",
            "range": "± 40958.130206152615"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 41463406.82051282,
            "unit": "ns",
            "range": "± 215497.15261081365"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 398955452.6666667,
            "unit": "ns",
            "range": "± 2018774.4806323298"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "committer": {
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang",
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com"
          },
          "id": "1f215218bc7b7f73dc6e1cf367e33a7e74d504c8",
          "message": "chore: bump Wolfgang.Etl.FixedWidth to 0.2.1\n\nPatch bump for the perf-only improvements landed in #84\n(span-based writes + DateTime parse) — no public API change,\noutput is byte-for-byte identical to 0.2.0.",
          "timestamp": "2026-05-09T13:11:39Z",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/1f215218bc7b7f73dc6e1cf367e33a7e74d504c8"
        },
        "date": 1779156297786,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4451528.309895833,
            "unit": "ns",
            "range": "± 70695.47891751143"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3064823.5768229165,
            "unit": "ns",
            "range": "± 33524.33987691612"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 428618.81363932294,
            "unit": "ns",
            "range": "± 2682.133641873538"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 542805.849609375,
            "unit": "ns",
            "range": "± 5022.426526364942"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 442865.4557291667,
            "unit": "ns",
            "range": "± 1554.6411199218073"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 515394.3636067708,
            "unit": "ns",
            "range": "± 6982.003117634836"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4108684.0989583335,
            "unit": "ns",
            "range": "± 21654.03499186924"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4426536.067708333,
            "unit": "ns",
            "range": "± 36674.45509727732"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4454445.684895833,
            "unit": "ns",
            "range": "± 49061.63491797129"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4485369.6640625,
            "unit": "ns",
            "range": "± 21146.591790133985"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 41772611.27777777,
            "unit": "ns",
            "range": "± 197372.1871898889"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 40244963.76923077,
            "unit": "ns",
            "range": "± 173170.11235656717"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 44186225.88888889,
            "unit": "ns",
            "range": "± 110368.60538630944"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 42218854.72222222,
            "unit": "ns",
            "range": "± 716284.9338601533"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 230564.72550455728,
            "unit": "ns",
            "range": "± 121.46854395178704"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 355354.10628255206,
            "unit": "ns",
            "range": "± 1150.0735154025615"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 647001.3206380209,
            "unit": "ns",
            "range": "± 5438.190616293818"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 668747.7177734375,
            "unit": "ns",
            "range": "± 14635.597689812621"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3290230.1328125,
            "unit": "ns",
            "range": "± 7524.445055574757"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2848041.0403645835,
            "unit": "ns",
            "range": "± 57702.49907139804"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3709531.5390625,
            "unit": "ns",
            "range": "± 131316.80245137578"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3324437.7239583335,
            "unit": "ns",
            "range": "± 49657.62602116145"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 26186751.71875,
            "unit": "ns",
            "range": "± 775657.4333264789"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 26589437.458333332,
            "unit": "ns",
            "range": "± 930545.2814362574"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 32666820,
            "unit": "ns",
            "range": "± 1253910.1360044829"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 29278542.354166668,
            "unit": "ns",
            "range": "± 378405.2546627377"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 457318.9596354167,
            "unit": "ns",
            "range": "± 4921.5513451092565"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 448077.02620442706,
            "unit": "ns",
            "range": "± 14826.297006227947"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 926259.7024739584,
            "unit": "ns",
            "range": "± 9795.188799810847"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4739055.734375,
            "unit": "ns",
            "range": "± 30482.101239756812"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 43865169.94444444,
            "unit": "ns",
            "range": "± 254804.75654345463"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 414343647.6666667,
            "unit": "ns",
            "range": "± 3559372.7032623785"
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
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "558367284823f36a9821a2b3c84d0ca4e7e7c7a4",
          "message": "Merge pull request #173 from Chris-Wolfgang/dependabot/nuget/dotnet-dependencies-0286896065\n\nBump the dotnet-dependencies group with 5 updates",
          "timestamp": "2026-06-19T13:31:58-04:00",
          "tree_id": "62d7c66bbbc1153655b8bc42c843727eaf27e22c",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/558367284823f36a9821a2b3c84d0ca4e7e7c7a4"
        },
        "date": 1781890610357,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4227165.625,
            "unit": "ns",
            "range": "± 13124.46095564388"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3088088.25,
            "unit": "ns",
            "range": "± 34586.17134762607"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 395911.60432942706,
            "unit": "ns",
            "range": "± 4705.57723671047"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 508774.7854817708,
            "unit": "ns",
            "range": "± 9621.13206794136"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 420953.87581380206,
            "unit": "ns",
            "range": "± 3713.2076097160893"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 490723.9599609375,
            "unit": "ns",
            "range": "± 14448.309315855193"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 3906463.6328125,
            "unit": "ns",
            "range": "± 7463.2976591719735"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4246526.5,
            "unit": "ns",
            "range": "± 15443.9861519274"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4161214.7057291665,
            "unit": "ns",
            "range": "± 16065.060355689733"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4175403.3203125,
            "unit": "ns",
            "range": "± 24955.419358895913"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 40726283.87179487,
            "unit": "ns",
            "range": "± 234545.13639524597"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 40768335.48717949,
            "unit": "ns",
            "range": "± 414690.3361953369"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 43161764.30555556,
            "unit": "ns",
            "range": "± 102461.2478759373"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 41397242,
            "unit": "ns",
            "range": "± 128452.07233925811"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 229818.38159179688,
            "unit": "ns",
            "range": "± 781.247484762072"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 348278.40771484375,
            "unit": "ns",
            "range": "± 2137.025909902019"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 517225.8994140625,
            "unit": "ns",
            "range": "± 3393.2151468709167"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 694505.3271484375,
            "unit": "ns",
            "range": "± 9911.281097894802"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3400817.8385416665,
            "unit": "ns",
            "range": "± 24902.218425702515"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2898823.27734375,
            "unit": "ns",
            "range": "± 62340.874188353584"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3591002.8489583335,
            "unit": "ns",
            "range": "± 98894.57454150423"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3475053.0169270835,
            "unit": "ns",
            "range": "± 18109.013348393837"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 27902443.75,
            "unit": "ns",
            "range": "± 1118615.6222293666"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 26918602.895833332,
            "unit": "ns",
            "range": "± 1194308.322745603"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 32001558.916666668,
            "unit": "ns",
            "range": "± 54617.612925368354"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 27594390.583333332,
            "unit": "ns",
            "range": "± 127714.09446457957"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 466046.91748046875,
            "unit": "ns",
            "range": "± 10957.108884041512"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 469256.00309244794,
            "unit": "ns",
            "range": "± 22694.380795221612"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 964230.9814453125,
            "unit": "ns",
            "range": "± 19066.901971972886"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4584398.958333333,
            "unit": "ns",
            "range": "± 38980.331469257755"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 44785191.166666664,
            "unit": "ns",
            "range": "± 204460.60605796296"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 435008076.6666667,
            "unit": "ns",
            "range": "± 1593284.4549879765"
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
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1864696fc51439e533d2bf11911bc9bc66e30276",
          "message": "Merge pull request #176 from Chris-Wolfgang/dependabot/github_actions/github-actions-91b150d450\n\nbuild(deps): bump the github-actions group with 3 updates",
          "timestamp": "2026-06-26T18:08:50-04:00",
          "tree_id": "a8dd6d979ff157e97580674d254be6d1c2041b5e",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/1864696fc51439e533d2bf11911bc9bc66e30276"
        },
        "date": 1782512023874,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4587277.927083333,
            "unit": "ns",
            "range": "± 99219.42115777562"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3207184.9921875,
            "unit": "ns",
            "range": "± 83804.56045349891"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 409470.2154947917,
            "unit": "ns",
            "range": "± 2844.0712366705056"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 502041.6429036458,
            "unit": "ns",
            "range": "± 8552.960383420163"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 460785.15283203125,
            "unit": "ns",
            "range": "± 2610.2797071071036"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 531635.2434895834,
            "unit": "ns",
            "range": "± 5496.537575015127"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 3937795.65234375,
            "unit": "ns",
            "range": "± 6516.525315145919"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4526899.239583333,
            "unit": "ns",
            "range": "± 28775.975193196813"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4469457.473958333,
            "unit": "ns",
            "range": "± 18593.970298662673"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4222698.684895833,
            "unit": "ns",
            "range": "± 38366.13042676977"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 42829592.5,
            "unit": "ns",
            "range": "± 269447.7311401663"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 42730189.861111104,
            "unit": "ns",
            "range": "± 150358.34314961405"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 44094914,
            "unit": "ns",
            "range": "± 196009.66784586865"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 42132349.30555555,
            "unit": "ns",
            "range": "± 223031.87686483006"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 246608.94694010416,
            "unit": "ns",
            "range": "± 865.3526234531312"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 354918.04215494794,
            "unit": "ns",
            "range": "± 8772.745259731822"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 586797.7568359375,
            "unit": "ns",
            "range": "± 69518.86734173224"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 755770.3063151041,
            "unit": "ns",
            "range": "± 3133.0179712017743"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3314464.45703125,
            "unit": "ns",
            "range": "± 18068.759290079997"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2875035.17578125,
            "unit": "ns",
            "range": "± 20152.21215731388"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3604131.890625,
            "unit": "ns",
            "range": "± 47645.40933027735"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3421591.6575520835,
            "unit": "ns",
            "range": "± 8602.703529940627"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 25120074.302083332,
            "unit": "ns",
            "range": "± 230415.90948110382"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 27225460.71875,
            "unit": "ns",
            "range": "± 625579.3037785557"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 31345248.125,
            "unit": "ns",
            "range": "± 26393.940093146855"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 27858682.34375,
            "unit": "ns",
            "range": "± 150202.4381471507"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 445146.63720703125,
            "unit": "ns",
            "range": "± 14598.120636899761"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 433938.748046875,
            "unit": "ns",
            "range": "± 2817.920886012692"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 969904.9127604166,
            "unit": "ns",
            "range": "± 21668.031651834"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4530122.138020833,
            "unit": "ns",
            "range": "± 63994.42480968504"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 44351102.02777778,
            "unit": "ns",
            "range": "± 28974.467166192626"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 416659909,
            "unit": "ns",
            "range": "± 2293913.704794276"
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
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "ca42939f934d155ed8751545f307858340263a4f",
          "message": "Merge pull request #187 from Chris-Wolfgang/protected/v022-protected-files\n\nchore: protected-file changes for v0.2.2 (split from #134)",
          "timestamp": "2026-06-26T19:36:56-04:00",
          "tree_id": "58ce42eb2e1626360cf2bd7ecfed0712b1e07eb1",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/ca42939f934d155ed8751545f307858340263a4f"
        },
        "date": 1782517304992,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4076562.2135416665,
            "unit": "ns",
            "range": "± 31203.27893426282"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3145156.8125,
            "unit": "ns",
            "range": "± 48453.977876414334"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 414954.2819010417,
            "unit": "ns",
            "range": "± 3454.948323877392"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 510543.1350911458,
            "unit": "ns",
            "range": "± 14731.55095614492"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 421423.41585286456,
            "unit": "ns",
            "range": "± 455.2841072538355"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 510316.4371744792,
            "unit": "ns",
            "range": "± 15811.519128084059"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4054631.3802083335,
            "unit": "ns",
            "range": "± 50578.32678061359"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4082956.5026041665,
            "unit": "ns",
            "range": "± 20698.627942315823"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4216146.791666667,
            "unit": "ns",
            "range": "± 13701.896311684495"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4181790.0755208335,
            "unit": "ns",
            "range": "± 10191.118957275432"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 39221407.102564104,
            "unit": "ns",
            "range": "± 306960.9656537035"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 39025853.79487179,
            "unit": "ns",
            "range": "± 32052.472680079973"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 41576615.277777776,
            "unit": "ns",
            "range": "± 169915.86628882843"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 40910952.07692307,
            "unit": "ns",
            "range": "± 53393.83291450272"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 226030.44116210938,
            "unit": "ns",
            "range": "± 352.02239820930174"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 360797.65738932294,
            "unit": "ns",
            "range": "± 4407.057365874478"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 534193.9244791666,
            "unit": "ns",
            "range": "± 8393.663722136911"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 657491.2542317709,
            "unit": "ns",
            "range": "± 16657.795053282276"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3489652.1497395835,
            "unit": "ns",
            "range": "± 12834.425313887703"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2926752.0390625,
            "unit": "ns",
            "range": "± 17647.6621174147"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3554005.2565104165,
            "unit": "ns",
            "range": "± 52028.181350093146"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3212984.2447916665,
            "unit": "ns",
            "range": "± 9321.091663035802"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 24905228.385416668,
            "unit": "ns",
            "range": "± 197610.86283604696"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25102462.0625,
            "unit": "ns",
            "range": "± 369567.9393897212"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 32289688.25,
            "unit": "ns",
            "range": "± 327890.22526553547"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 26778261.302083332,
            "unit": "ns",
            "range": "± 199271.32943151813"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 385434.4003092448,
            "unit": "ns",
            "range": "± 6861.958704870768"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 402503.8053385417,
            "unit": "ns",
            "range": "± 2669.299357236071"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 874892.6497395834,
            "unit": "ns",
            "range": "± 4403.446615420743"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4722054.8046875,
            "unit": "ns",
            "range": "± 41237.66726815047"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 40444957.15384615,
            "unit": "ns",
            "range": "± 184235.035253837"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 428254298.6666667,
            "unit": "ns",
            "range": "± 3471635.658563602"
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
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d5263305e0cf93934543b1ba0f628fdf581e52ae",
          "message": "Merge pull request #134 from Chris-Wolfgang/vNext\n\nRelease v0.2.2: canonical maintenance round + AssemblyVersion fix",
          "timestamp": "2026-06-26T21:57:29-04:00",
          "tree_id": "e205503b58c1c3bcb3ba95d36934410f1892a3d0",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/d5263305e0cf93934543b1ba0f628fdf581e52ae"
        },
        "date": 1782525746035,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4720484.71875,
            "unit": "ns",
            "range": "± 16557.487662207182"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3165916.546875,
            "unit": "ns",
            "range": "± 21756.80585891342"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 451044.8837890625,
            "unit": "ns",
            "range": "± 565.2110013908896"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 570290.244140625,
            "unit": "ns",
            "range": "± 8696.17279832186"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 503992.1891276042,
            "unit": "ns",
            "range": "± 1969.3523150020555"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 596400.3209635416,
            "unit": "ns",
            "range": "± 4518.697599301911"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4503943.25,
            "unit": "ns",
            "range": "± 7738.92097899891"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4795110.020833333,
            "unit": "ns",
            "range": "± 10554.182479221583"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4599461.28125,
            "unit": "ns",
            "range": "± 10096.77296710103"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4460902.859375,
            "unit": "ns",
            "range": "± 5331.153661817089"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 47688844.424242415,
            "unit": "ns",
            "range": "± 551639.5475259932"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 45533738.63636365,
            "unit": "ns",
            "range": "± 80227.3795373501"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 47645877.57575757,
            "unit": "ns",
            "range": "± 204262.84319982814"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 44893476.111111104,
            "unit": "ns",
            "range": "± 260486.0104503947"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 234708.89095052084,
            "unit": "ns",
            "range": "± 1699.568298249577"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 348283.462890625,
            "unit": "ns",
            "range": "± 830.71007580769"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 627911.3649088541,
            "unit": "ns",
            "range": "± 13383.753198595248"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 746105.1653645834,
            "unit": "ns",
            "range": "± 15181.636991175712"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3443376.39453125,
            "unit": "ns",
            "range": "± 17954.844941892574"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2846603.5026041665,
            "unit": "ns",
            "range": "± 38161.3794206448"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3644023.9466145835,
            "unit": "ns",
            "range": "± 26240.827454327115"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3378843.8489583335,
            "unit": "ns",
            "range": "± 27018.124537237574"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 26548784.083333332,
            "unit": "ns",
            "range": "± 89175.95553109658"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25840913.5,
            "unit": "ns",
            "range": "± 68784.69382618286"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 32738985.979166668,
            "unit": "ns",
            "range": "± 326610.6099946173"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 28259970.40625,
            "unit": "ns",
            "range": "± 245112.00968073256"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 425848.46175130206,
            "unit": "ns",
            "range": "± 7971.132720935127"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 452793.02034505206,
            "unit": "ns",
            "range": "± 14610.2022880905"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 1006138.841796875,
            "unit": "ns",
            "range": "± 8107.327414689691"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 5321150.096354167,
            "unit": "ns",
            "range": "± 32815.443767360885"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 48847428.81818181,
            "unit": "ns",
            "range": "± 153034.3746933761"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 436442121.6666667,
            "unit": "ns",
            "range": "± 3551143.11437674"
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
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f9c81f9dce2c202df428d91dbf1acf8013250686",
          "message": "Merge pull request #192 from Chris-Wolfgang/vNext\n\nRelease v0.2.2: code-review fixes + CHANGELOG",
          "timestamp": "2026-06-27T16:10:27-04:00",
          "tree_id": "1902ace9d0b65839ddc1af68967a18bca83be2d0",
          "url": "https://github.com/Chris-Wolfgang/ETL-FixedWidth/commit/f9c81f9dce2c202df428d91dbf1acf8013250686"
        },
        "date": 1782591310084,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Extract_Memory(RecordCount: 10000)",
            "value": 4892813.395833333,
            "unit": "ns",
            "range": "± 23830.730416268652"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.DateTimeBenchmarks.Load_Memory(RecordCount: 10000)",
            "value": 3150260.96875,
            "unit": "ns",
            "range": "± 24928.463835580467"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 1000)",
            "value": 434756.52587890625,
            "unit": "ns",
            "range": "± 4649.950753485282"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 519446.388671875,
            "unit": "ns",
            "range": "± 10348.730643506637"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 1000)",
            "value": 490633.4514973958,
            "unit": "ns",
            "range": "± 6821.259721942293"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 518907.7249348958,
            "unit": "ns",
            "range": "± 12799.835235281147"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 10000)",
            "value": 4291925.234375,
            "unit": "ns",
            "range": "± 14239.607826771133"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 4437828.934895833,
            "unit": "ns",
            "range": "± 10851.811800559235"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 10000)",
            "value": 4988561.0546875,
            "unit": "ns",
            "range": "± 41865.13055478511"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 4502498.140625,
            "unit": "ns",
            "range": "± 7501.6898380762705"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_TextReader(RecordCount: 100000)",
            "value": 45891294.72727273,
            "unit": "ns",
            "range": "± 183866.5018322852"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 47292721.81818181,
            "unit": "ns",
            "range": "± 82501.97069306448"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_TextReader_1KB(RecordCount: 100000)",
            "value": 46915033.72727273,
            "unit": "ns",
            "range": "± 154898.10661152183"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.ExtractorBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 48874484.03030303,
            "unit": "ns",
            "range": "± 795518.9221719919"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 1000)",
            "value": 227681.36726888022,
            "unit": "ns",
            "range": "± 449.43949803053806"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 1000)",
            "value": 346744.24267578125,
            "unit": "ns",
            "range": "± 201.31390913132907"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 1000)",
            "value": 546298.2682291666,
            "unit": "ns",
            "range": "± 51930.40730504326"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 1000)",
            "value": 723865.0240885416,
            "unit": "ns",
            "range": "± 10776.597826890196"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 10000)",
            "value": 3295053.1158854165,
            "unit": "ns",
            "range": "± 3246.999960545041"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 10000)",
            "value": 2892570.6328125,
            "unit": "ns",
            "range": "± 61021.31127877299"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 10000)",
            "value": 3560777.7630208335,
            "unit": "ns",
            "range": "± 16301.216537239443"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 10000)",
            "value": 3399978.59375,
            "unit": "ns",
            "range": "± 13174.266724703983"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_TextWriter(RecordCount: 100000)",
            "value": 24887281.697916668,
            "unit": "ns",
            "range": "± 260268.8955719123"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.Memory_Stream(RecordCount: 100000)",
            "value": 25368424.739583332,
            "unit": "ns",
            "range": "± 206112.8838923947"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_TextWriter_1KB(RecordCount: 100000)",
            "value": 33278373.333333332,
            "unit": "ns",
            "range": "± 713352.7163914095"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.LoaderBenchmarks.File_Stream_64KB(RecordCount: 100000)",
            "value": 28185220.03125,
            "unit": "ns",
            "range": "± 91740.20683713075"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 0)",
            "value": 453185.7278645833,
            "unit": "ns",
            "range": "± 22743.290873284368"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1)",
            "value": 477006.48486328125,
            "unit": "ns",
            "range": "± 23093.677211040726"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000)",
            "value": 960785.12109375,
            "unit": "ns",
            "range": "± 18276.0776995131"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 10000)",
            "value": 4900050.153645833,
            "unit": "ns",
            "range": "± 45000.788538860994"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 100000)",
            "value": 44308700.05555556,
            "unit": "ns",
            "range": "± 304774.0675235287"
          },
          {
            "name": "Wolfgang.Etl.FixedWidth.Benchmarks.PeakMemoryBenchmarks.Extract_PeakMemory(RecordCount: 1000000)",
            "value": 440637437.3333333,
            "unit": "ns",
            "range": "± 2839798.521244832"
          }
        ]
      }
    ]
  }
}