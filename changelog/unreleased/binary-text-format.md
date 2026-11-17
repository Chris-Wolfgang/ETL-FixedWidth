type: feature

`[FixedWidthBinaryField]` gains `Format` so binary `Text` fields mapped to `DateTime`, `DateTimeOffset` or `TimeSpan` (and any `IFormattable`) can be written and read back; a temporal text field without a `Format` now fails at map build with a message naming the property.
