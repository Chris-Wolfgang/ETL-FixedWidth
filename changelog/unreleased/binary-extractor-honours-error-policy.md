type: fix

`FixedWidthBinaryExtractor` now consults `FixedWidthBinaryExtractorOptions.ErrorPolicy` when a record fails to decode: `Skip` drops that record, counts it in `CurrentErrorItemCount` and continues, while the default policy still aborts with the `FieldConversionException`.
