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
(`int.Parse(span, …)` on net8+) boxes its result. There is therefore **no call
site with a zero-allocation contract to enforce**, so — per the [#157] acceptance
criteria — this repo intentionally does **not** carry
`GC.GetAllocatedBytesForCurrentThread()` unit tests. A half-implemented "assert
zero bytes" test would either be trivially false or pinned to an arbitrary
threshold that adds maintenance noise without protecting a real contract.

## What the fast path *does* optimize

On `net8.0`+ the numeric and `DateTime` parse paths read directly from the line's
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

[#157]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/157
