# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Binary / mainframe field support ([#21]): `FixedWidthBinaryExtractor<TRecord>` and
  `FixedWidthBinaryLoader<TRecord>` read and write fixed-length **binary** records (no newline
  delimiters — each record is a fixed number of bytes) over a `Stream`, decoding/encoding COBOL
  `COMP-3` packed-decimal and `COMP` binary-integer fields alongside text (a round-trip is
  symmetric). Fields are declared with the new
  `[FixedWidthBinaryField(index, byteLength, BinaryFieldType, Scale/Signed)]` attribute
  (byte-based, type-driven); text fields use the extractor/loader's `Encoding` init property (ASCII
  default; set a code-page encoding for EBCDIC). Both take an optional `ILogger` as the last ctor
  argument. Unsigned `COMP` fields (`Signed = false`) decode over the
  full unsigned range: an 8-byte value above `Int64.MaxValue` maps cleanly to a `ulong` property,
  and overflows (throws) rather than wrapping negative if the target property is signed. A
  `COMP-3` field with a fractional part (`Scale > 0`) mapped to an integral property throws rather
  than silently rounding it away (map it to a `decimal`/floating-point property, or declare
  `Scale = 0`). The existing text extractor and loader are unchanged.
- `FixedWidthDataReader<TRecord>` — a forward-only, read-only `IDataReader` over a
  fixed-width source, with the layout taken from `TRecord`'s `[FixedWidthField]`
  attributes. It serves each field value directly from the parsed line — **no
  `TRecord` is allocated per row** — the optimal shape for `SqlBulkCopy`,
  `DataTable.Load`, and other ADO.NET consumers that would otherwise discard a POCO
  per row. Supports the extractor's configuration (`HeaderLineCount`,
  `SkipItemCount`, `MaximumItemCount`, `BlankLineHandling`, `MalformedLineHandling`,
  `FieldDelimiter`), typed accessors, and `GetSchemaTable()` ([#26]).
- `FixedWidthMultiRecordExtractor` — reads a file that interleaves **multiple record types**
  (a mainframe header/detail/trailer batch, for example) and yields each line as the
  concrete POCO it maps to. Register one rule per type with
  `.When(line => line[0] == 'D', typeof(DetailRecord))`; the first matching predicate
  wins. Lines that match no rule are handled per `UnmatchedLineHandling` (throw or skip)
  or routed to a fallback type via `.Otherwise(...)`. Each record type keeps its own
  independent `[FixedWidthField]` layout, and the extractor shares the family's
  `HeaderLineCount`, `FieldDelimiter`, `ValueParser`, `SkipItemCount`/`MaximumItemCount`,
  malformed-line dead-lettering (`OnError`), and progress reporting. Its configuration
  properties are `init`-only — set them in the object initializer, so config is fixed for
  the run ([#19]).

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.8.0] - 2026-08-07

Code-first schema building, error dead-lettering, and zero-config metrics.
Additive public API — no breaking changes.

### Added

- `FixedWidthSchemaBuilder<T>` — define a fixed-width layout with a fluent,
  type-safe code API (`.Field(r => r.Name, index, length, …)` / `.Skip(index, length)`
  / `.Build()`) instead of `[FixedWidthField]` attributes, for record types you
  cannot decorate or layouts built at runtime. Assign the resulting
  `FixedWidthSchema` to the new `FixedWidthExtractor<T>.Schema` /
  `FixedWidthLoader<T>.Schema` property to override attribute resolution; a
  built schema is equivalent to (and introspectable like) an attribute-resolved
  one ([#23]).
- Malformed-line handling now flows through the Abstractions #84 policy. `FixedWidthExtractor`
  overrides `OnItemError` to translate the existing `MalformedLineHandling` knob (`Skip` → skip,
  `ThrowException` → abort) and calls the base `HandleItemError`, so a genuine parse failure is
  counted in `CurrentErrorItemCount` and surfaced in the pipeline's `ErrorItemCount` — kept
  distinct from `RecordValidator` business rejects (which `CurrentRejectedItemCount` counts).
  `MalformedLineHandling.ReturnDefault` recovers before the give-up decision, so it never enters
  the error policy. Part of #29.
- `OnError` dead-letter sink (`Action<FixedWidthError>?`) on `FixedWidthExtractor<T>`: each record
  that fails to parse is reported as a `FixedWidthError` (1-based `ItemNumber`, `RawContent`,
  `Exception`) instead of only aborting. With `MalformedLineHandling.Skip` it is capture-and-continue;
  even on the default `ThrowException` the failure is reported before the throw. `RecordValidator`
  business rejects are **not** reported here (they are not parse errors). Closes #29.
- Native-AOT / trim-compatibility smoke test ([#153]): a `PublishAot` console
  consumer (`tests/AotSmoke`) exercises every public path against a concrete
  record type and asserts the results, and the `aot-smoke.yaml` workflow
  publishes it on Linux and runs the native binary so an AOT/trim regression
  fails before merge. The `Expression.Compile` accessor sites carry documented
  `IL3050` suppressions — under Native AOT they fall back to the interpreter, so
  the library runs correctly (without JIT speed). **Known limitation surfaced by
  the smoke:** attribute-based mapping reads `[FixedWidthField]` by reflection,
  and Native-AOT trimming strips those attribute instances unless the record's
  assembly is rooted (`TrimmerRootAssembly`) — a consumer using attribute mapping
  under AOT must root its record types today; removing that need is the
  source-generated-accessors follow-up.
- Concurrency / race-condition stress suite ([#147]):
  `tests/Wolfgang.Etl.FixedWidth.Tests.Concurrency` asserts correctness under
  contention — concurrent first-use of the process-global caches (`FieldMap`
  cache, `FixedWidthTransformer` static property-mapper), racing disposal, and
  cross-thread cancellation mid-enumeration. A weekly `concurrency.yaml` sweep
  cranks the iteration budget up via `STRESS_ITERATIONS`. (Coyote is not used —
  its `IAsyncEnumerable` support is rough and its CLI is net8-only; the xunit
  stress suite is the gate.)
- Sustained-load GC / allocation profiling ([#152]): `tools/GcProfile` runs
  extract → transform → load in a tight loop for a configurable duration and
  reports allocated bytes per record and gen2 collections per million records.
  A monthly `gc-profile.yaml` sweep gates the run against `docs/gc-baseline.json`
  — a per-record allocation jump (hot-path regression) or gen2 promotion
  (retention leak) fails the job. Complements the per-call allocation snapshot in
  `docs/ALLOCATION-PROFILE.md` (#157).
- Shadow-testing sample workloads ([#140]): `samples/ShadowWorkloads` runs
  realistic end-to-end scenarios (streaming round trip, reformat transform,
  pipeline composition) that double as usage documentation. A nightly
  `shadow.yaml` measures per-scenario latency + allocation and gates the result
  against `docs/shadow-baseline.json` — allocation is the hard gate (a >50% jump
  fails); latency is advisory (reported, not gated, since shared-runner wall-clock
  is too noisy to fail on reliably).

### Changed

- The #30 metrics no longer run unless a listener is subscribed to the
  `Wolfgang.Etl.FixedWidth` meter. The extract/load loop samples the instruments'
  `Enabled` state **once per operation** and executes no metric code when nothing
  is listening — removing the always-on per-line/per-record overhead that made
  extraction ~1.5–1.95× slower in 0.7.0 — with **no public API and no opt-in
  flag** (zero-config, consistent with the rest of the ETL family) ([#275]).
- Bumped `Wolfgang.Etl.Abstractions` 0.17.0 → 0.21.0 (and `Wolfgang.Etl.TestKit`
  0.10.0 → 0.14.0), adopting the renamed
  `EtlPipelineProgress.{Extracted,Loaded,Error}ItemCount` counters (formerly
  `Records{Extracted,Loaded,Errored}`). The loader and transformer
  now honor an already-cancelled `CancellationToken` before consuming their
  source — a pre-cancelled `LoadAsync` / `TransformAsync` reads nothing — matching
  the extractor and the TestKit base cancellation contract.

### Deprecated

### Removed

### Fixed

### Security

- Consumer-side reproducible-build verification ([#165]): each release now
  attaches a `reproducible-build-manifest.json` (expected per-framework assembly
  SHA-256 hashes + the exact toolchain), and `docs/REPRODUCIBLE-BUILD.md` plus a
  README "Verify the build" section document how a third party rebuilds the tag,
  compares hashes, files a discrepancy, and publishes an independent verification
  attestation.

## [0.7.0] - 2026-07-24

Pipeline composition and observability. Additive — no breaking changes.

### Added

- Fixed-width source factories and sink terminators for the generic `EtlPipeline`
  fluent chain: `EtlPipeline.Create().FixedWidthExtractor<T>(path | stream | reader | extractor)`
  and `… .FixedWidthLoader<T>(path | stream | writer)`. The returned
  `IFixedWidthExtractorBuilder<T>` / `IFixedWidthLoaderBuilder<T>` expose every
  extractor/loader setting as inline fluent methods (`HeaderLineCount`,
  `MalformedLineHandling`, `FieldDelimiter`, `Encoding`, `WriteHeader`,
  `ValueConverter`, `IsDryRun`, …). Path-based factories own the file stream they
  open and dispose it after the run (success or failure); caller-supplied
  streams/readers/writers are left open. Requires `Wolfgang.Etl.Abstractions`
  0.16.0 ([#253]).
- Built-in `System.Diagnostics.Metrics` instrumentation on the extractor and
  loader, emitted from the meter **`Wolfgang.Etl.FixedWidth`**: counters
  `wolfgang.etl.fixedwidth.items.extracted` / `.items.loaded` / `.items.skipped`
  / `.lines.read` and the histogram `wolfgang.etl.fixedwidth.operation.duration`
  (ms). Every measurement is tagged `etl.operation` (`extract`/`load`) and
  `etl.record_type`. Zero-config — subscribe with OpenTelemetry
  (`AddMeter("Wolfgang.Etl.FixedWidth")`) or a `MeterListener`; a no-op with no
  listener registered ([#30]).

## [0.6.0] - 2026-07-18

Layout introspection and format transformation. Additive — no breaking changes.

### Added

- `FixedWidthSchema.For<T>()` / `For(Type)` — a read-only view over the resolved
  field layout: `Fields`, `ExpectedLineWidth`, `TotalColumnCount`, `FieldCount`,
  `SkipCount`. Each `FixedWidthFieldInfo` exposes the name, position range, length,
  column index, type, alignment, pad, format, header, and `NumberStyles`; skipped
  columns carry `IsSkip` and `SkipMessage`. Useful for generating documentation,
  building validation tooling, or debugging a mapping ([#22]).
- `FixedWidthSchema.ToDiagram()` — renders the resolved layout as a human-readable
  text table for logging, tickets, and documentation ([#24]).
- `FixedWidthTransformer<TSource, TDestination>` — projects one fixed-width layout
  to another in a single streaming pass (the transform stage of an
  extract → transform → load pipeline), via a projection constructor or the
  `ByMatchingProperties()` same-name auto-mapping factory ([#14]).

## [0.5.1] - 2026-07-17

Quality, supply-chain, and CI hardening. **No public API or runtime behaviour
changes** — the shipped library is unchanged from 0.5.0.

### Security

- Release packages now carry a keyless **SLSA build-provenance attestation**
  (via `actions/attest-build-provenance`), verifiable with
  `gh attestation verify <package> --owner Chris-Wolfgang`. `SECURITY.md` gains a
  "Release path & compromise scope" appendix and documents the verification
  procedure ([#148], [#161]).
- Added **PackageValidation** as a release gate that diffs each pack against the
  previously published version and fails on an ABI break ([#146]).

### Changed

- Internal quality and CI hardening only, with no change to shipped code:
  `CultureInfo` invariance test matrix ([#155]), CsCheck property-based fuzz
  suite ([#139]), Verify snapshot tests ([#150]), and an XML-doc `<example>`
  API-rot guard ([#151]); CI additions — workflow-security via actionlint +
  zizmor ([#163]), OSSF Scorecard ([#162]), transitive-dependency license audit
  ([#158]), Semgrep SAST ([#141]), build-reproducibility verification ([#156]),
  and a cross-platform / ARM64 differential ([#149]); and new documentation —
  Architecture Decision Records ([#160]), a major-version migration-guide
  convention ([#159]), and an allocation-profile snapshot ([#157]).

## [0.5.0] - 2026-07-16

### Added

- Optional `Encoding` parameter on the `Stream`-based constructors of
  `FixedWidthExtractor` and `FixedWidthLoader`. Defaults to `Encoding.UTF8`
  (non-breaking); pass e.g. `new UTF8Encoding(false)` to write without a BOM,
  or a code-page encoding for EBCDIC/mainframe data ([#16]).
- `NumberStyles` property on `[FixedWidthField]` controlling how a numeric field
  is parsed during extraction. Defaults to `null`, using the target type's
  natural style — `Integer` for integral types, `Number` for
  `decimal`/`double`/`float` (matching `int.Parse` / `decimal.Parse`, parsed with
  `InvariantCulture`). Set it explicitly — e.g. `NumberStyles.Currency` — to
  accept currency symbols, scientific notation, or parenthesized negatives ([#9]).
- `RecordValidator` callback on `FixedWidthExtractor` (`Func<TRecord,
  ValidationResult>?`) invoked after a record is parsed but before it is
  yielded. Return `ValidationResult.Accept()`, `.Skip(reason)` (rejects the
  record), or `.Stop(reason)` (ends extraction). Defaults to `null` (no
  validation) ([#18]).
- Line-accounting counters on `FixedWidthExtractor` (surfaced on
  `FixedWidthReport`): `CurrentRejectedItemCount` (records dropped by
  `MalformedLineHandling.Skip` or `RecordValidator.Skip`) and
  `CurrentFilteredLineCount` (non-record lines: headers, the separator, blank
  lines dropped per `BlankLineHandling`, `LineFilter`-skipped lines, and the
  early-termination trigger line). Together they close the line accounting:
  `CurrentLineNumber = CurrentItemCount + CurrentSkippedItemCount +
  CurrentRejectedItemCount + CurrentFilteredLineCount` ([#18]).

### Changed

- `CurrentSkippedItemCount` now counts **only** records skipped by the
  `SkipItemCount` budget. Records discarded by `MalformedLineHandling.Skip`
  now increment the new `CurrentRejectedItemCount` instead — a behavior change
  from 0.4.0, where they counted toward `CurrentSkippedItemCount` ([#18]).
- Numeric fields are now parsed with the target type's natural `NumberStyles`
  (`Integer` / `Number`) by default, consistently across every target framework,
  configurable via `[FixedWidthField(NumberStyles = …)]`. Previously net8.0+
  parsed with `NumberStyles.Any` and .NET Framework / netstandard used
  `TypeConverter.ConvertFromInvariantString`. As a result, currency symbols,
  scientific notation, and parenthesized negatives no longer parse by default —
  opt in per field with an explicit `NumberStyles` ([#9]).

## [0.4.0] - 2026-07-14

### Added

- `FixedWidthLoader<TRecord>` now implements `ISupportDryRun`. Set `IsDryRun`
  to `true` to run the full pipeline — enumerate the source, evaluate
  `SkipItemCount` / `MaximumItemCount`, increment progress counters, fire the
  progress-timer callback, and validate field widths — without writing anything
  to the output fixed-width stream ([#197]).

## [0.3.0] - 2026-07-13

### Added

- `CompressedStreams` example demonstrating GZip and Brotli round trips
  (load to and extract from a compressed stream), plus documentation in the
  README and the DocFX examples guide ([#32]).

### Changed

- Reuse the cached `TypeConverter` across the nullable-unwrap recursion in the
  value parser, avoiding a redundant per-value `TypeDescriptor.GetConverter`
  lookup for nullable `TypeConverter`-backed fields. Behavior is unchanged
  ([#208]).
- Internal maintenance (no public-API or runtime-behavior change): corrected
  stale and empty XML-doc comments, and applied small source simplifications —
  shared buffered reader/writer construction helpers on the `Stream`
  constructors and reuse of the precomputed header label ([#207], [#209]).

## [0.2.3] - 2026-07-06

### Changed

- Dependabot bump: dotnet-dependencies group (7 packages).

## [0.2.2] - 2026-06-27

### Changed

- Upgraded to `Wolfgang.Etl.Abstractions` 0.14.1. The base extractor/loader now
  implement `IDisposable`/`IAsyncDisposable`; `FixedWidthExtractor` and
  `FixedWidthLoader` drop their hand-rolled dispose interface and chain
  `base.Dispose(disposing)`. Public surface is unchanged apart from the
  inherited dispose members.

### Added

- Canonical maintenance round (no public-API or runtime-behavior change):
  CodeQL `security-extended` query pack, `PublicApiAnalyzers` with
  `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`, SourceLink + deterministic
  CI builds + `.snupkg` symbol packages + complete NuGet metadata, a Stryker
  mutation-testing workflow, and a release-time docs-build verification job.

### Fixed

- Pinned `AssemblyVersion` to `1.0.0.0` as a binding-stability baseline so
  .NET Framework consumers do not need binding redirects on every patch bump;
  `FileVersion`/`InformationalVersion` carry the real release version.
- Documentation corrections: package is now linked as published on NuGet.org,
  and the target-framework references match the shipped TFMs.

### Removed

- Internal cleanup (no public-API or behavior change): dropped the unused
  segment-formatting path from the line parser — superseded by the
  allocation-free direct-write path — and removed template-scaffolding leftovers.

## [0.2.1] - 2026-05-09

### Changed

- Span-based field writes and `DateTime` parsing on the hot path, cutting
  writer allocations by roughly 42–49% and `DateTime` load allocations by ~26%
  with no change to output bytes ([#84]).

### Added

- BenchmarkDotNet → gh-pages benchmark-chart workflow ([#83]).
- Tests covering the new span-based write paths ([#86]).

## [0.2.0] - 2026-04-28

### Changed

- **Breaking:** dropped the `TProgress` generic parameter; progress is now
  reported through the fixed `FixedWidthReport` type ([#60]).
- **Breaking:** removed the two-parameter `(stream/reader/writer, logger)`
  constructors in favor of a consistent constructor set ([#62]).

### Added

- `SECURITY.md` and security-hardened CI workflows.

### Fixed

- Analyzer errors surfaced by updated Roslynator, SonarAnalyzer, and Meziantou
  ([#49]).

## [0.1.0] - 2026-03-24

### Added

- Initial release: `FixedWidthExtractor<TRecord>` and `FixedWidthLoader<TRecord>`
  for streaming extraction and loading of fixed-width text via
  `IAsyncEnumerable<T>`, built on `Wolfgang.Etl.Abstractions`.
- Attribute-based field mapping (`[FixedWidthField]`, `[FixedWidthSkip]`) with
  configurable alignment and padding, header handling, and custom value
  parsers/converters.
- `ILogger` support and `Stream` constructor overloads with a 64 KB buffer.
- Compiled-delegate field mapping (replacing reflection) and span-based numeric
  parsing for reduced allocations.
- Nine runnable example console apps covering the major features.

[#139]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/139
[#141]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/141
[#146]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/146
[#148]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/148
[#149]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/149
[#150]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/150
[#151]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/151
[#155]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/155
[#156]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/156
[#157]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/157
[#158]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/158
[#159]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/159
[#160]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/160
[#161]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/161
[#162]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/162
[#163]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/163
[#14]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/14
[#19]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/19
[#22]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/22
[#24]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/24
[#23]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/23
[#21]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/21
[#30]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/30
[#140]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/140
[#147]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/147
[#152]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/152
[#153]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/153
[#165]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/165
[#253]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/253
[#26]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/26
[#275]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/275
[Unreleased]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.5.1...v0.6.0
[0.5.1]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.3...v0.3.0
[0.2.3]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v.0.1.0...v0.2.0
[0.1.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/releases/tag/v.0.1.0
[#32]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/32
[#49]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/49
[#60]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/60
[#62]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/62
[#83]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/83
[#84]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/84
[#9]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/9
[#16]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/16
[#18]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/18
[#86]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/86
[#197]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/197
[#207]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/207
[#208]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/208
[#209]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/209
