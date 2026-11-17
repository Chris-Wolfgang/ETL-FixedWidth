type: fix

`FixedWidthExtractor` and `FixedWidthMultiRecordExtractor` now consult the configured `ErrorPolicy` when `MalformedLineHandling` is `ThrowException` (the default, whose default policy still aborts), so a caller-supplied `ErrorPolicy` such as `Skip` is honoured instead of ignored.
