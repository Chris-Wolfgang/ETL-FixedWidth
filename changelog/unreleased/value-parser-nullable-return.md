type: feature

`FixedWidthValueParser` now returns `object?`, so a custom parser can return `null` for a field with no value without a null-forgiving `!`; code that assigns the result to a non-nullable `object` may see new nullable warnings.
