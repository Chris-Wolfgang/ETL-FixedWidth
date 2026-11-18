# 0007. Ban blocking APIs with BannedSymbols.txt; read lines synchronously on purpose

- **Status**: accepted
- **Date**: 2026-11-17

## Context

Every extractor and loader here is a stage in an `IAsyncEnumerable<T>` pipeline. A
blocking call on that path (`Task.Result`, `Thread.Sleep`, a synchronous
`Stream.Read`) holds a thread-pool thread for the length of the I/O and can deadlock
a consumer that runs under a single-threaded synchronization context. Such a call
compiles without complaint and is easy to miss in review.

The repository already carries the mechanism: a `BannedSymbols.txt` at the root,
read by `Microsoft.CodeAnalysis.BannedApiAnalyzers`. On the date of this ADR the
list had the same 65 entries in all eight ETL repositories.

A fixed-width file is read one line at a time, often millions of lines. An awaited
`ReadLineAsync` per line pays for an async state machine on every line, while the
`StreamReader` underneath already reads the file in large buffered blocks. The text
extractors therefore read lines synchronously, and that choice was recorded only as
a code comment.

## Decision

We enforce the async-only rule with the banned-API analyzer instead of relying on
review. `BannedSymbols.txt` bans `Task.Wait`, `Task.WaitAll`, `Task.WaitAny`,
`Task<T>.Result`, `GetAwaiter().GetResult()`, `Thread.Sleep`, the synchronous `File`
helpers, `Stream.Read` / `Write` / `Flush` / `CopyTo`, `Parallel.For` / `ForEach`
and the blocking `Console` reads, each with its replacement in the message.
`.editorconfig` sets `RS0030` to `error` for the shipped code and to `none` for
tests, benchmarks and examples. The shipped code has no `RS0030` suppression.

Within that rule, the text path works on a buffered `TextReader` / `TextWriter`
synchronously where it is per line or per field:

- `FixedWidthExtractor`, `FixedWidthMultiRecordExtractor` and `FixedWidthDataReader`
  call `TextReader.ReadLine()`. The extract method stays an async iterator, as the
  base class requires.
- `FixedWidthLoader` writes each field with `TextWriter.Write`, ends the line with
  `WriteLineAsync`, and flushes with `FlushAsync` at the end of the load.

`TextReader` and `TextWriter` members are not on the banned list, so `RS0030` is not
involved. The synchronous `ReadLine` is suppressed for `CA1849`, `VSTHRD103` and
`S6966` with a `#pragma` and a comment at the call.

The binary extractor and loader use `Stream.ReadAsync` and `WriteAsync`.

## Consequences

- A direct call to a banned member cannot merge into shipped code, and the build
  error names the replacement.
- The line loop has no per-line async overhead.
- When the reader's buffer is empty, `ReadLine` refills it with a synchronous read on
  the underlying stream. For a file or an in-memory stream that is short. For a slow
  source, such as a network stream, the calling thread blocks for the length of that
  read.
- A cancellation request is observed between lines, not during a read.
- The rule covers the listed members only. Nothing but review stops a new
  synchronous `TextReader` or `TextWriter` call somewhere it does matter.
- `BannedSymbols.txt` is a protected file, so a change to the list goes in its own
  pull request.
- Sibling records of the rule: ETL-SqlBulkCopy ADR 0003, ETL-Transformers ADR-0001.

*Recorded after the fact: both the rule and the synchronous line reads were already
in force when this ADR was written.*
