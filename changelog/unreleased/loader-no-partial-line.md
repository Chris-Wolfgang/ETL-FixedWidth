type: fix

`FixedWidthLoader` no longer leaves a truncated, unterminated line in the output when a field of a record fails mid-write (for example a `FieldOverflowException`); each line is formatted in a buffer and written only once every field has succeeded.
