type: internal

The `(reader|writer, logger = null)` and `(…, options = null, …)` constructor defaults on `FixedWidthExtractor`, `FixedWidthLoader` and `FixedWidthTransformer` stay as shipped in 0.12; dropping them (S3427) rewrites recorded public signatures and is scheduled for the 2026-12-15 wave (#495), with the rule silenced for those three files until then. The RS0026 justifications from the overload review remain.
