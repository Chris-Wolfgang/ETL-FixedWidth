# 0005. Trim / Native AOT annotations on the record-type entry points

- **Status**: proposed (owner review needed: public-surface change, #358)
- **Date**: 2026-10-29

## Context

Repository baseline item 18 (#358) asks the library to declare `IsAotCompatible` /
`IsTrimmable`. Declaring them honestly means the trim analyzer must report nothing.
With the trim, AOT and single-file analyzer categories raised to `warning` (the
repository `.editorconfig` currently lowers every analyzer diagnostic to `suggestion`,
which hides them), `src/Wolfgang.Etl.FixedWidth` reported 17 diagnostics on both
net8.0 and net10.0:

| Code | Count | Where |
| --- | --- | --- |
| IL2026 | 3 | `TypeDescriptor.GetConverter` in `FixedWidthConverter`, `Parsing/FieldDescriptor`, `Binary/BinaryFieldDescriptor` |
| IL2067 / IL2072 | 2 / 3 | the same `TypeDescriptor` calls, plus `Activator.CreateInstance` for value-type defaults |
| IL2070 | 4 | `GetProperties` / `GetConstructor` on an unannotated `Type` in `FieldMap`, `FieldMapResult`, `BinaryFieldMap` |
| IL2090 | 3 | `FixedWidthTransformer.ByMatchingProperties` on unannotated `TSource` / `TDestination` |
| IL2093 | 1 | `FixedWidthDataReader.GetFieldType` lacks the return annotation `IDataRecord` declares |
| IL2111 | 1 | `GetSchemaTable` passes `typeof(Type)` to `DataColumnCollection.Add` |

A trimmed (ILLink) publish of the `tests/AotSmoke` consumer reported the same root
causes as 10 whole-program warnings.

The mapper reflects over a **record type** (public properties, their attributes, the
public parameterless constructor) and over each mapped **property type** (a
`TypeConverter` fallback and boxed defaults). The record type flows in through public
generic parameters (`FixedWidthExtractor<TRecord>`, ...) and `Type` parameters
(`FixedWidthSchema.For(Type)`, `FixedWidthMultiRecordExtractor.When/Otherwise`). The
source generator from #13 already removes `Expression.Compile` from the hot path; it
does not remove the reflection over attributes and property types.

## Decision

Annotate, do not restrict. Every public entry point that hands a record type to the
mapper declares what the mapper reads, so the trimmer keeps exactly those members:

1. `[DynamicallyAccessedMembers(PublicProperties | PublicParameterlessConstructor)]`
   (`TrimmedMembers.Record`) on:
   `FixedWidthExtractor<TRecord>`, `FixedWidthLoader<TRecord>`,
   `FixedWidthBinaryExtractor<TRecord>`, `FixedWidthBinaryLoader<TRecord>`,
   `FixedWidthDataReader<TRecord>`, `FixedWidthSchemaBuilder<T>`,
   `FixedWidthSchema.For<T>()`, `FixedWidthSchema.For(Type)`,
   `FixedWidthMultiRecordExtractor.When(Func<string, bool>, Type)`,
   `FixedWidthMultiRecordExtractor.Otherwise(Type)`, the seven
   `EtlPipelineFixedWidthExtensions.FixedWidthExtractor<T>` / `FixedWidthLoader<T>`
   methods, and the internal builders and mapper methods they reach.
2. `FixedWidthTransformer<TSource, TDestination>`: `PublicProperties` on `TSource`,
   `TrimmedMembers.Record` on `TDestination` (only `ByMatchingProperties` reflects).
3. `FixedWidthDataReader<TRecord>.GetFieldType` gains the
   `[return: DynamicallyAccessedMembers(PublicFields | PublicProperties)]` that
   `IDataRecord.GetFieldType` declares (net8.0+).
4. The two reflection calls on **property types**, which cannot be annotated, move into
   one internal class (`Parsing/TrimSafeReflection`) with an
   `UnconditionalSuppressMessage` and a written justification each:
   `TypeDescriptor.GetConverter` (the fallback parser; `Nullable<T>` is unwrapped before
   it runs, the intrinsic converters are rooted by `TypeDescriptor`) and
   `Activator.CreateInstance` for value-type defaults. `GetSchemaTable` gets one
   suppression for the framework-internal IL2111.
5. `IsAotCompatible` / `IsTrimmable` are set for net8.0 and later (the TFMs the analyzer
   runs on). net462 / net481 / netstandard2.0 get internal polyfills of
   `DynamicallyAccessedMembersAttribute` / `DynamicallyAccessedMemberTypes` so the same
   source compiles everywhere; the trimmer matches the attribute by name.

Alternatives considered:

- **`[RequiresUnreferencedCode]` on the constructors.** Honest but useless: every
  consumer call site would warn, and `IsTrimmable` would advertise nothing.
- **Source-generated metadata only (#12 / #13 extended).** Removes the reflection over
  attributes too, but a generator only sees types that exist in source at compile time;
  `When(predicate, Type)` / `For(Type)` with a runtime `Type` would still need the
  reflection path. It is a follow-up, not a replacement.

## Consequences

- **Public-surface change (no signature change).** No member is added, removed or
  renamed; `PublicAPI.*.txt` does not change (attributes are not part of that baseline).
  The annotations are, however, visible to consumers' trim analysis: a consumer that
  passes its **own unannotated generic parameter** through (for example
  `class Repo<T> { FixedWidthExtractor<T> ... }`) gets IL2091 in a trimmed/AOT build
  until it annotates its `T` the same way. Consumers with concrete record types see no
  change, and JIT consumers see nothing at all.
- **One documented caveat.** A property type whose parsing relies on a custom
  `[TypeConverter]` must keep that converter type under trimming (for example with
  `[DynamicDependency]` or a `TrimmerRootDescriptor`). Built-in scalar types, enums and
  `Nullable<T>` of them need nothing.
- **Analyzer visibility is a separate, protected change.** The repository
  `.editorconfig` line `dotnet_analyzer_diagnostic.severity = suggestion` still hides
  IL2xxx/IL3xxx. Raising `category-Trimming` / `category-AOT` / `category-SingleFile` to
  `warning` (so a regression fails the Release build) is the next stacked PR after this
  design is accepted. Verification for this change was done with that elevation applied
  locally.
- **Follow-ups after acceptance:** the `.editorconfig` elevation above; dropping the
  `TrimmerRootAssembly` items from `tests/AotSmoke` once a Linux Native AOT run confirms
  the annotations keep the `[FixedWidthField]` attribute metadata on their own.
