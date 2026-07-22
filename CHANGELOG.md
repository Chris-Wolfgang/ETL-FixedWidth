# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

### Changed

### Deprecated

### Removed

### Fixed

### Security

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
[#22]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/22
[#24]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/24
[#30]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/30
[#253]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/253
[Unreleased]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.6.0...HEAD
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
