# Architecture Decision Records

See [README.md](README.md) for the folder and [ADR 0000](0000-record-architecture-decisions.md) for the practice. Copy
[`TEMPLATE.md`](TEMPLATE.md) to add a record.

| ADR | Decision |
| --- | --- |
| [0000](0000-record-architecture-decisions.md) | Record architecture decisions |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions (superseded by [0000](0000-record-architecture-decisions.md)) |
| [0002](0002-numberstyles-per-type-default.md) | Per-type default `NumberStyles` for field parsing |
| [0003](0003-line-accounting-counters.md) | Four-way line accounting: items / skipped / rejected / filtered |
| [0004](0004-line-endings-via-textwriter-newline.md) | Line-ending control via `TextWriter.NewLine`, not a `LineEnding` enum |
| [0005](0005-trim-and-aot-annotations.md) | Trim / Native AOT annotations on the record-type entry points (proposed) |
| [0006](0006-pin-assemblyversion.md) | Pin `AssemblyVersion` at `1.0.0.0` |
| [0007](0007-async-only-io-and-synchronous-line-reads.md) | Ban blocking APIs with `BannedSymbols.txt`; read lines synchronously on purpose |
| [0008](0008-stream-reader-and-writer-ownership.md) | The caller keeps what it passes in; the library disposes only what it opens |
