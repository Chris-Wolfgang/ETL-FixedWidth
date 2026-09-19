type: internal

`FixedWidthExtractor` / `FixedWidthLoader` / `FixedWidthTransformer`: the `(source, logger = null)` overloads drop their dead default (the hidden single-argument constructor already wins that shape) and the `(source, options = null, …)` overloads drop the options default that could only be reached by name (S3427). Binary signatures unchanged; PublicAPI text updated. The RS0026 that remains on the Stream overloads and on `FixedWidthMultiRecordExtractor` is suppressed with a justification pointing at the 12/15 wave (#373 / #343).
