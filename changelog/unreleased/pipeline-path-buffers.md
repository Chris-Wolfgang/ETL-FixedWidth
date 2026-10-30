type: internal

The fluent pipeline path source and sink (`FixedWidthExtractor<T>(path)` / `FixedWidthLoader<T>(path)`) open their `StreamReader` / `StreamWriter` with the same 64 KB buffer the extractor and loader Stream constructors use, instead of the 1 K-char default.
