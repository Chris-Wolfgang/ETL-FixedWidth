type: fix

`FixedWidthBinaryExtractorOptions`, `FixedWidthBinaryLoaderOptions` and `FixedWidthMultiRecordExtractorOptions` carry `ReportingInterval` / `MaximumItemCount` / `SkipItemCount` / `ErrorPolicy` themselves instead of deriving from the Abstractions base records, so a caller compiled against 0.12.0 that uses a `with` expression on them keeps working on net462 / net481 / netstandard2.0 (deriving is scheduled for the 2026-12-15 wave, #373).
