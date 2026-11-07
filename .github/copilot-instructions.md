# Copilot Coding Agent Instructions

## Repository summary

`Wolfgang.Etl.FixedWidth` is a .NET library (NuGet package) for reading and writing
fixed-width text and binary/mainframe records. It provides extractors and loaders built
on `Wolfgang.Etl.Abstractions` (`ExtractorBase` / `LoaderBase` / `TransformerBase`),
plus a Roslyn analyzer package (`Wolfgang.Etl.FixedWidth.Analyzers`, rules FW00x).

- **Source:** `src/Wolfgang.Etl.FixedWidth/` (targets `net462;net481;netstandard2.0;net5.0;net6.0;net7.0;net8.0;net10.0`)
  and `src/Wolfgang.Etl.FixedWidth.Analyzers/`.
- **Tests:** `tests/Wolfgang.Etl.FixedWidth.Tests.Unit/` (multi-TFM, contract tests from
  `Wolfgang.Etl.TestKit.Xunit`), plus `Tests.Concurrency`, `Tests.Fuzz`, `Tests.Snapshot`,
  `Tests.SourceLink` and the `tests/AotSmoke` native-AOT smoke app.
- **Also:** `benchmarks/`, `examples/`, `samples/`, `tools/`, `docfx_project/` (API docs),
  `docs/` (ADRs, baselines, guides).

## Build and validate

Prerequisite: the .NET 10.0 SDK (older runtimes are needed only to run the matching test TFMs).

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

`pwsh ./scripts/build-pr.ps1` runs the same checks as the PR workflow's Windows stage:
build, all test TFMs, the coverage gates, DevSkim and gitleaks. The local tools it uses
(ReportGenerator, DevSkim, DocFX, ...) are pinned in `.config/dotnet-tools.json` and
restored with `dotnet tool restore`.

## Gates a change must pass

- **Warnings are errors.** Fix the warning; do not suppress it unless there is no other way.
- **Coverage** (line coverage, enforced per assembly by `pr.yaml` and `release.yaml`):
  production assemblies at `CODECOV_MINIMUM`, test assemblies at `CODECOV_TEST_MINIMUM`
  (both set in the workflows' `env:`; `build-pr.ps1` mirrors them).
- **Async-first:** `BannedSymbols.txt` bans blocking calls (`Task.Wait`, `.Result`,
  `Thread.Sleep`) and synchronous file / `Stream` I/O. New I/O must be async.
- **Public API:** `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` record the public
  surface (RS0016 / RS0017), and package validation compares each pack with the last
  published version.
- **Protected files** (`.editorconfig`, `Directory.Build.*`, `BannedSymbols.txt`,
  `*.globalconfig`, `*.ruleset`, `.github/workflows/*`) are checked by the Protected Files
  Guard; change them in a PR of their own.

## Conventions

The code style (Allman braces, three blank lines between members, multi-line argument
lists, file-scoped namespaces, `Method_when_condition_expected_result` test names) and the
contribution workflow are documented in [`CONTRIBUTING.md`](../CONTRIBUTING.md). Record
user-visible changes as a fragment in `changelog/unreleased/` (format in `scripts/changelog.ps1`;
required whenever `src/` changes, unless the PR is labelled `no-changelog`).
