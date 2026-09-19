type: feature

`FixedWidthBinaryExtractorOptions`, `FixedWidthBinaryLoaderOptions` and `FixedWidthMultiRecordExtractorOptions` now derive from the Abstractions base records, so those three stages take `ReportingInterval` / `MaximumItemCount` / `SkipItemCount` / `ErrorPolicy` through their record like every other stage (ADR-0009); `FixedWidthMultiRecordExtractor` gains a `(TextReader, options, logger)` constructor. New `FixedWidthTransformerOptions` record and `FixedWidthTransformer(transform, options, logger)` constructor for the same reason.
