type: internal

The options constructor assigns the stage's backing fields directly instead of going through the deprecated setters, so the `CS0618` suppressions that covered those writes are gone. The only validating setter (`StartByteOffset`) was already guarded on the record's init accessor, so no record change was needed; the four `ResolvedEncoding` observation reads stay as the issue scopes. (#441)
