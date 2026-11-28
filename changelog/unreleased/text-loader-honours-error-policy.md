type: fix

`FixedWidthLoader` now consults `ErrorPolicy` when a record fails to format (a value wider than its field, or an exception from the `ValueConverter`): `Skip` writes no line for that record, counts it in `CurrentErrorItemCount` and continues, while the default policy still aborts.
