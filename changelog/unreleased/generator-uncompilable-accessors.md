type: fix

The accessor source generator no longer emits uncompilable code for struct records, types with `required` members, or properties named with a C# keyword; those shapes now fall back to the reflection path (structs and `required` types) or are emitted with an escaped identifier (keyword names).
