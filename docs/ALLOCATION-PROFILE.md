# Allocation profile

This document is the snapshot the thorough-review pass ([#157]) asks for: an
explicit statement of what allocates, what does not, and how allocation is
guarded against regression.

## Is any public method zero-allocation?

**No.** ETL-FixedWidth is a materializing library by design. Every public
operation produces managed objects that must be allocated:

| Surface | Unavoidable allocations |
| --- | --- |
| `FixedWidthExtractor<T>.ExtractAsync` | one `T` record per line; a `string` per string-typed field; a boxed `object` per value-typed field (the converter returns `object`); the `IAsyncEnumerator` state machine. |
| `FixedWidthLoader<T>.LoadAsync` | the formatted `string`/`char[]` segments written per record; the async state machine. |

Because the field converter returns `object`, even the span-based fast path
(`int.Parse(span, …)` on net5.0+) boxes its result. There is therefore **no call
site with a zero-allocation contract to enforce**, so — per the [#157] acceptance
criteria — this repo intentionally does **not** carry
`GC.GetAllocatedBytesForCurrentThread()` unit tests. A half-implemented "assert
zero bytes" test would either be trivially false or pinned to an arbitrary
threshold that adds maintenance noise without protecting a real contract.

## What the fast path *does* optimize

On `net5.0`+ the numeric and `DateTime` parse paths read directly from the line's
`ReadOnlySpan<char>` (`ParseNumericSpan` / `ParseDateTimeValueSpan`), avoiding the
**intermediate per-field substring** that the netstandard2.0 build must allocate
before parsing. That is a real, measured reduction — but the boxed result and the
record itself remain, so it is "fewer allocations", not "zero".

## How allocation is guarded

Allocation regressions are caught at the **benchmark** level, not by unit tests.
Every benchmark class carries BenchmarkDotNet's `[MemoryDiagnoser]`, so the
`Allocated` column is captured on every run and published to the trend chart:

- `ExtractorBenchmarks`
- `LoaderBenchmarks`
- `DateTimeBenchmarks`
- `PeakMemoryBenchmarks`

Chart: <https://chris-wolfgang.github.io/ETL-FixedWidth/dev/bench/>

A change that regresses per-record allocation shows up as a step in the
`Allocated` series on that chart. That is the appropriate granularity for a
library whose hot path is inherently allocating.

## Comparing runtimes

The benchmark workflows run every class on `net10.0` only. To measure the span
fast path on an older runtime (the net5.0-7.0 assemblies used to take the
substring path, [#594]), the benchmark project also targets `net6.0` and `net8.0`,
so BenchmarkDotNet can run the same cases side by side on each runtime:

```bash
cd benchmarks/Wolfgang.Etl.FixedWidth.Benchmarks
dotnet build -c Release
dotnet run -c Release -f net10.0 --no-build -- --filter "*ExtractorBenchmarks.Memory_TextReader*" --runtimes net6.0 net8.0 net10.0 --job short --memory
```

The matching .NET runtimes must be installed. One local ShortRun (Windows 11,
2026-10-31, machine under load, so read the `Allocated` column, not the times):

| RecordCount | .NET 6.0 | .NET 8.0 | .NET 10.0 |
| ---: | ---: | ---: | ---: |
| 1,000 | 418.22 KB | 402.57 KB | 402.57 KB |
| 10,000 | 4,144.26 KB | 3,988.54 KB | 3,988.55 KB |
| 100,000 | 41,407.61 KB | 39,848.25 KB | 39,848.27 KB |

net6.0 allocates about 16 bytes more per record than net8.0/net10.0 (4%). The
benchmark record has two `int` fields (`ZipCode`, `Age`); the substring path would
add two short strings per record (about 32 bytes each on 64-bit), more than the
measured gap, which is consistent with the span path being taken on net6.0. The
remaining 16 bytes are not attributed here.

[#157]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/157
[#594]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/594
