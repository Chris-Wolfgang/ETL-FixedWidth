type: feature

The net8.0 and net10.0 assemblies declare `IsAotCompatible` / `IsTrimmable`: the record-type generic parameters and `Type` parameters carry `[DynamicallyAccessedMembers(PublicProperties | PublicParameterlessConstructor)]`, so a trimmed or Native AOT app keeps the members the mapper reads. A consumer that forwards its own unannotated generic `T` into these types gets IL2091 in a trimmed build until it annotates `T` the same way. See ADR 0005.
