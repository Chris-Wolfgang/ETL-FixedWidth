type: feature

`FixedWidthBinaryExtractor` and `FixedWidthBinaryLoader` now throw `FieldConversionException` with the record number, the field's byte offset (new `FieldConversionException.ByteOffset`), the property name and the original error as `InnerException` when a field cannot be decoded or encoded; this changes the exception type from the bare `FormatException` / `OverflowException` / `ArgumentException` thrown before, so catch blocks for those types must catch `FieldConversionException` instead.
