# 0004. Line-ending control via TextWriter.NewLine, not a LineEnding enum

- **Status**: accepted
- **Date**: 2026-07-16
- **Supersedes**: the `LineEnding` enum proposed in #17

## Context

Consumers asked to control the line ending the loader writes (#17). The proposed
API was a `LineEnding { Lf, CrLf, ... }` enum on `FixedWidthLoader`. Two problems
surfaced: (1) .NET has no standard line-ending enum, so we would be inventing one
that every consumer must learn; and (2) the enum conflated *which* ending with
*whether a trailing* ending is written, and it would have shadowed the
`TextWriter.NewLine` mechanism the BCL already provides.

## Decision

We do not add a `LineEnding` enum. The loader honours the `TextWriter.NewLine`
of the writer it is given, which is the idiomatic BCL mechanism:

```csharp
await using var writer = new StreamWriter(stream) { NewLine = "\n" }; // force LF
using var loader = new FixedWidthLoader<PersonRecord>(writer);
```

The extractor already reads `\n`, `\r`, and `\r\n` transparently via
`TextReader.ReadLine()`, so no reader-side option is needed. This is documented in
the README and DocFX getting-started (#222).

## Consequences

- No new public surface to version or explain; consumers use a mechanism they
  already know.
- Output line endings are controlled at writer-construction time rather than via
  a loader property.
- #17 is closed as addressed-by-documentation rather than implemented; PR #219
  (the enum implementation) was dropped.
