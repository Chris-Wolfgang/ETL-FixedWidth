window.BENCHMARK_DATA = {
  "lastUpdate": 1779155710696,
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
      }
    ]
  }
}