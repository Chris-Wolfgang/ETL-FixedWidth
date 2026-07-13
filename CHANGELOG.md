# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `CompressedStreams` example demonstrating GZip and Brotli round trips
  (load to and extract from a compressed stream), plus documentation in the
  README and the DocFX examples guide. Documentation only — no public-API or
  runtime-behavior change ([#32](https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/32)).

### Changed

### Deprecated

### Removed

### Fixed

### Security

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

[Unreleased]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.3...HEAD
[0.2.3]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v.0.1.0...v0.2.0
[0.1.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/releases/tag/v.0.1.0
[#49]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/49
[#60]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/60
[#62]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/62
[#83]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/83
[#84]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/84
[#86]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/pull/86
