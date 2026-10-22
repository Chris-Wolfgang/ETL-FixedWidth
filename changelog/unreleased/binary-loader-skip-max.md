type: fix

`FixedWidthBinaryLoader` now honours `SkipItemCount` and `MaximumItemCount`, and rejects a null record with an `InvalidOperationException` instead of a `NullReferenceException`.
