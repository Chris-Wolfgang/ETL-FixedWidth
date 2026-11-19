type: internal

The doc-example test helper enumerates regex matches with an explicitly typed foreach instead of a LINQ cast call, clearing a RedundantEnumerableCastCall finding that the previous OfType rewrite re-created. Test-only; no library change.
