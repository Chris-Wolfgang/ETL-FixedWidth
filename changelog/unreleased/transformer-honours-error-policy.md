type: fix

`FixedWidthTransformer` now consults `FixedWidthTransformerOptions.ErrorPolicy` when the projection throws: `Skip` drops that item, counts it in `CurrentErrorItemCount` and continues, while the default policy still aborts with the projection's exception. With this, every stage in the package honours `ErrorPolicy`.
