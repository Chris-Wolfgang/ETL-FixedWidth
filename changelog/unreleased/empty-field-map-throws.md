type: fix

A record type with no `[FixedWidthField]` properties now throws `InvalidOperationException` (naming the type and mentioning attribute trimming) from the text extractor, loader and data reader instead of silently yielding default records or writing blank lines.
