type: fix

`[FixedWidthField(Format = ...)]` is now honoured when parsing `DateOnly` and `TimeOnly` (net6.0 and later), so loader output written with a compact format such as `yyyyMMdd` can be read back by the extractor.
