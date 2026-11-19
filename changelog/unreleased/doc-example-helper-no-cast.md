type: internal

The doc-example test helper enumerates regex matches with an indexed loop instead of a LINQ cast call, clearing a RedundantEnumerableCastCall finding that the previous OfType rewrite re-created, and compiling without nullable warnings on netcoreapp3.1. Test-only; no library change.
