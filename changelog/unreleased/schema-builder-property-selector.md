type: internal

FixedWidthSchemaBuilder now resolves its property selector with Wolfgang.Etl.Abstractions PropertySelector.Resolve instead of a private copy; the same selectors are accepted and rejected (ArgumentException), only the exception message text comes from Abstractions.
