# 0008. The caller keeps what it passes in; the library disposes only what it opens

- **Status**: accepted
- **Date**: 2026-11-17

## Context

An extractor or loader can be given its data three ways: a `TextReader` or
`TextWriter`, a `Stream`, or (through the `EtlPipeline` factories) a file path. Each
raises the same question: who closes it, and who flushes it. A library that closes a
stream the caller still needs breaks a `MemoryStream` that is read back after the
run. A library that leaves a file it opened itself leaks the handle.

The rule the code follows was written in the XML documentation of each type and in
comments in the pipeline builders, but not in one place.

## Decision

The library disposes only what it created. Nothing the caller passes in is ever
closed, and there is no `leaveOpen` option to change that.

| The caller passes | The library creates | On dispose |
| --- | --- | --- |
| A `TextReader` or `TextWriter` | Nothing | Nothing. `Dispose` is optional and has no effect. The caller flushes a writer. |
| A `Stream` (text extractor or loader) | A `StreamReader` or `StreamWriter` with a 64 KB buffer and `leaveOpen: true` | The internal reader or writer. The stream stays open. The loader flushes its writer at the end of the load. |
| A `Stream` (binary extractor or loader) | Nothing | The stream stays open. |
| A file path (`EtlPipeline` factory) | The reader or writer for that file | The pipeline disposes it after the run, on success and on failure. Disposing a writer flushes the file and closes it. |
| A pre-built extractor (`EtlPipeline` factory) | Nothing | Nothing. The caller owns it. |

## Consequences

- A caller can read back a `MemoryStream`, or keep writing to a stream, after the
  run.
- A stream-based extractor or loader must still be disposed, to release its internal
  reader or writer. Forgetting to does not close the caller's stream, but for a
  loader that did not finish its load the buffered output is never flushed.
- A caller who passes a `TextWriter` has to flush it. The loader does not.
- A caller who wants the stream closed with the stage has to close it themselves.
  ETL-Xml takes the other approach for caller-supplied streams and honours a
  `LeaveOpen` option (ETL-Xml ADR-003). The two libraries differ here.
- The path-based factories are the only place the library owns a file handle, so
  they are the only place a handle can leak from.

*Recorded after the fact: the behaviour was already in force when this ADR was
written.*
