type: fix

`FixedWidthTransformer.ByMatchingProperties()` no longer throws for same-named properties that need a conversion (`int` to `int?`, value type to `object`) and ignores static properties and indexers.
