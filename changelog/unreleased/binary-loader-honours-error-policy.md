type: fix

`FixedWidthBinaryLoader` now consults `FixedWidthBinaryLoaderOptions.ErrorPolicy` when a record fails to encode: `Skip` writes nothing for that record, counts it in `CurrentErrorItemCount` and continues, while the default policy still aborts with the `FieldConversionException`.
