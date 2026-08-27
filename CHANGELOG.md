# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed

- **Breaking. The members deprecated in 0.11.0 are gone**, completing that deprecation cycle:

  | removed | use instead |
  |---|---|
  | `Encoding` property on `FixedWidthBinaryExtractor<T>`, `FixedWidthBinaryLoader<T>`, `FixedWidthDataReader<T>`, `FixedWidthMultiRecordExtractor` | the `Encoding` property on each type's options record |
  | `X(Stream, ILogger<X>)` on those same four types | `X(Stream, XOptions?, ILogger<X>?)` |
  | `FixedWidthExtractor<T>(Stream, Encoding)` and `(Stream, ILogger<T>, Encoding)` | `(Stream, FixedWidthExtractorOptions?, ILogger<T>?)` |
  | `FixedWidthLoader<T>(Stream, Encoding)` and `(Stream, ILogger<T>, Encoding)` | `(Stream, FixedWidthLoaderOptions?, ILogger<T>?)` |
  | `FixedWidthReport(int, int, long)` and `(int, int, int, int, long)` | `FixedWidthReport(FixedWidthReportOptions)` |

  Fourteen members, eighteen API entries — each `Encoding` property contributes a `get` and an
  `init`. Recorded as intentional breaks; PackageValidation suppressions rise from 0 to 90
  (18 x 5 target frameworks).

  Every one of these carried an `[Obsolete]` message naming its replacement throughout 0.11.0, so
  callers have had a release in which their code compiled with a warning pointing at the fix.

  The supporting machinery goes with them: `ResolvedEncoding` — which existed only to let options
  win over the obsolete property — and the `ToOptions` helpers that fed the `Encoding` shims. With
  the properties gone the options are fully known at construction time, so each type captures its
  encoding in the constructor again.

  The three `[EditorBrowsable(EditorBrowsableState.Never)]` overloads are **not** affected. They are
  not deprecated; each preserves a 0.10.1 binary signature and serves a call that is still correct
  code. Their fate is tracked separately.

### Added

### Changed

### Deprecated

- **Binary-compatibility overloads without deprecation.**
  `FixedWidthExtractor<T>(TextReader)`, `FixedWidthLoader<T>(TextWriter)` and
  `FixedWidthTransformer<TSource, TDestination>(Func<TSource, TDestination>)` are restored, but
  deliberately **not** `[Obsolete]`. Unlike the other compatibility overloads here, the call each
  one serves — `new X(reader)` — is still correct, idiomatic code with nothing to migrate to, so a
  deprecation warning would be noise. They are marked
  `[EditorBrowsable(EditorBrowsableState.Never)]` instead, the usual treatment for an overload that
  exists only to keep already-compiled assemblies loading.

- **The `Encoding` properties on `FixedWidthBinaryExtractor<T>`, `FixedWidthBinaryLoader<T>`,
  `FixedWidthMultiRecordExtractor` and `FixedWidthDataReader<T>`** are `[Obsolete]` rather than
  removed, so 0.10.x source keeps compiling.

  Resolution rule: **options win when supplied; the property is the fallback when they are not.**
  The two cannot conflict in existing code, because the options constructor did not exist before
  0.11.0 — a caller was necessarily using the property.

  ```csharp
  new X(stream) { Encoding = latin1 }                 // property honoured
  new X(stream, new XOptions { Encoding = latin1 })   // supported route
  new X(stream, new XOptions { Encoding = latin1 }) { Encoding = Encoding.ASCII }   // options win
  ```

  The value is resolved at the point of use, not captured in the constructor. That is load-bearing:
  an `init` property is assigned *after* the constructor body runs, so capturing it there would read
  the default and silently ignore whatever the caller set — the exact inert-property failure this
  release set out to remove. Two of these properties were already inert on part of their own
  surface: `FixedWidthDataReader<T>` said so in its XML doc, and `FixedWidthMultiRecordExtractor`
  had the same silent gap. They are now honoured only where they mean something.

  Three tests pin the rule, including the case that fails if the value is captured early.

- **The pre-0.11.0 `Stream` constructors are `[Obsolete]` rather than removed.** On
  `FixedWidthBinaryExtractor<T>`, `FixedWidthBinaryLoader<T>`, `FixedWidthMultiRecordExtractor` and
  `FixedWidthDataReader<T>`:

  ```csharp
  X(Stream stream, ILogger<X> logger)                                    // obsolete
  X(Stream stream, XOptions? options = null, ILogger<X>? logger = null)  // use this
  ```

  and on `FixedWidthExtractor<T>` / `FixedWidthLoader<T>`, which additionally took a loose encoding:

  ```csharp
  X(Stream stream, Encoding encoding)                                    // obsolete
  X(Stream stream, ILogger<X> logger, Encoding encoding)                 // obsolete
  X(Stream stream, XOptions? options = null, ILogger<X>? logger = null)  // use this
  ```

  Passing a `null` encoding to the obsolete overloads still means "use the default", as it did when
  the parameter was declared `Encoding? encoding = null`.

  These carry the **pre-0.11.0 binary signatures**, so already-compiled consumers keep working and
  get a deprecation warning instead of a `MissingMethodException`. PackageValidation suppressions
  drop from **95 to 55**.

  Note there is deliberately no one-argument `X(Stream)` overload: 0.10.1 never emitted such a
  signature. `new X(stream)` compiled to `.ctor(Stream, ILogger)` with `null` baked in at the
  caller's compile time, so the two-argument shim is what restores compatibility. Adding a
  one-argument overload would carry no compatibility value and would make `new X(stream)` — still
  the correct call — emit a deprecation warning.

  For the same reason `(TextReader)` / `(TextWriter)` are **not** restored: reinstating them would
  make `new X(reader)` warn. Those two signatures, the replaced `Encoding` properties, and
  `FixedWidthTransformer<TSource, TDestination>(Func<TSource, TDestination>)` remain recorded as
  intentional breaks.

  The obsolete overloads deliberately declare **no default arguments**. That is what keeps
  `new X(stream)` unambiguous: an exact-arity candidate beats one that needs default-argument
  substitution, so the call binds the obsolete overload with a warning. Giving them defaults
  instead produces `CS0121` and breaks `new X(stream)` outright — worse than removing them.

  One consequence while they exist: `new X(stream, logger: log)` binds the obsolete overload. Pass
  `options: null` explicitly to reach the new constructor. Both go away when these are removed.

### Removed

### Fixed

### Security

## [0.11.0] - 2026-08-27

Constructor configuration is now uniform across the package: every `Stream`-based constructor takes
an options record, and the logger is always the last, optional parameter.

**This release is binary-compatible with 0.10.1.** It began as a breaking change — nineteen removed
members — and every one of them was subsequently restored as a compatibility overload or a
deprecated property. `dotnet pack` records **zero** `PackageValidation` suppressions against the
0.10.1 baseline, and `CompatibilitySuppressions.xml` has been deleted because there is nothing left
to suppress. Existing compiled assemblies continue to load; existing source continues to compile,
with deprecation warnings where an old style has a supported replacement.

### Added

- **Options records for every `Stream`-based constructor** — `FixedWidthExtractorOptions`,
  `FixedWidthLoaderOptions`, `FixedWidthBinaryExtractorOptions`, `FixedWidthBinaryLoaderOptions`,
  `FixedWidthMultiRecordExtractorOptions` and `FixedWidthDataReaderOptions`, each carrying an
  `Encoding` property. Defaults are declared on the property initializers — `Encoding.UTF8`
  everywhere except the binary types, which keep `Encoding.ASCII` — so no constructor body can
  diverge from them. Omitting the options object gives the same result: the constructors resolve
  `options ?? new FixedWidthXxxOptions()`.

  The `TextReader`/`TextWriter` constructors deliberately take **no** options. A caller-supplied
  reader or writer already carries its own encoding, so the setting would be inert there.

- **`FixedWidthReportOptions` record and a `FixedWidthReport(FixedWidthReportOptions)`
  constructor.** The two positional constructors differed only by whether the two extractor-only
  counts appeared *in the middle* of the argument list, and four of the five parameters are `int`.
  Naming each count at the call site removes the standing risk of transposing them. Every member
  defaults to zero, so a loader states three counts instead of passing two explicit zeroes
  positionally.

- **A trailing optional logger on `FixedWidthTransformer<TSource, TDestination>`**, both on the
  public constructor and on the internal timer-injecting one. It was the only type in the package
  that accepted no logger at all. The transformer now emits Information-level started/completed
  records mirroring `FixedWidthExtractor<T>`, so a pipeline's middle stage is no longer silent in
  logs that show its extract and load stages.

  Adding the parameter changes the emitted signature, so this is a binary — not source — break,
  recorded as intentional alongside the others in this release.

- **A trailing optional logger on the internal timer-injecting constructors** of
  `FixedWidthBinaryExtractor<T>` and `FixedWidthBinaryLoader<T>`. They previously took a timer but
  no logger, so a test could inject one or the other but not both.

- **`FixedWidthMultiRecordExtractor(Stream, IProgressTimer, ILogger<...>? = null)`** (internal).
  The `Stream` shape previously had no timer-injecting constructor, so only the `TextReader` shape
  could be tested with a deterministic timer.

### Changed

- **`logger` is now optional on the `(TextReader, ILogger<T>)` / `(TextWriter, ILogger<T>)`
  constructors**, defaulting to `NullLogger.Instance` rather than throwing `ArgumentNullException`.
  The parameter list is unchanged, so the emitted signature is identical and this is not a binary
  breaking change.

- **Every type now has a single initialization path.** `FixedWidthExtractor<T>`,
  `FixedWidthLoader<T>`, `FixedWidthMultiRecordExtractor` and `FixedWidthDataReader<T>` previously
  assigned their shared fields independently in each constructor. They now chain into one private
  constructor that assigns them in exactly one place. No API or behavior change — this closes the
  gap that produced two shipped defects elsewhere in the fleet, including this package's own
  internal constructor that hard-coded UTF-8 while its public counterpart honored the caller's
  encoding.

### Deprecated

- **The two positional `FixedWidthReport` constructors** — `(int, int, long)` and
  `(int, int, int, int, long)` — are `[Obsolete]` in favour of the `FixedWidthReportOptions`
  overload. They still work and remain under test; they are scheduled for removal in a future
  release. All six in-package call sites have moved to the new constructor.

### Removed

- **Breaking.** Six superseded constructors on `FixedWidthExtractor<T>` and `FixedWidthLoader<T>`
  (#332):

  - `(Stream, ILogger<T>, Encoding? = null)` — logger in the middle
  - `(Stream, Encoding? = null)` — loose encoding parameter
  - `(TextReader)` / `(TextWriter)` — subsumed by the optional-logger overload

  Deleting them is **source-compatible**: existing calls rebind to the surviving constructors.
  It is a **binary** break, because optional-argument defaults are baked in at the caller's compile
  time, so already-compiled consumers must be recompiled.

  They were briefly marked `[Obsolete]` instead. That was reverted: the superseded constructor won
  overload resolution for the simplest call, so obsoleting it warned every caller writing perfectly
  correct new code, and — with `TreatWarningsAsErrors` — broke them outright.

### Fixed

- **`netcoreapp3.1` and `net5.0` were running zero tests.** Both slots reported
  *"No test is available"* and contributed nothing, while `dotnet test` exited non-zero with **no
  reported failures** — so a green-looking local run said nothing about those two frameworks.

  `xunit.runner.visualstudio` **2.8.2 ships `build`/`lib` assets for `net462` and `net6.0` only**, so
  neither slot resolved a test adapter. The runner is now pinned per slot: **2.4.5** (the newest 2.x
  that still ships a `netcoreapp3.1` asset, which `net5.0` also consumes) for those two frameworks,
  2.8.2 everywhere else, both capped below `3.0.0` since runner 3.x drops these frameworks outright.

  Restores **695** tests on `netcoreapp3.1` and **700** on `net5.0`. Test-infrastructure only — no
  product code, no API change.

- **A null `Stream` passed to `FixedWidthExtractor<T>` or `FixedWidthLoader<T>` reported the wrong
  parameter name.** Both types route their two input shapes through one private constructor, and a
  null stream fell through to the reader/writer branch — so the `ArgumentNullException` named
  `reader` or `writer`, parameters the caller never passed, contradicting the documented contract.

  Each constructor now null-checks its own source before delegating. Caught in review of the
  single-initialization-path change earlier in this release, which introduced it;
  `FixedWidthDataReader<T>` already did this correctly.

- **`FixedWidthMultiRecordExtractor` had the same defect by a different route.** Its private core
  checked for both sources being null and reported `reader` unconditionally, so a null `Stream`
  was misnamed there too. Fixed the same way.

  Each private core also keeps an explicit both-sources-null guard, throwing
  `InvalidOperationException` rather than `ArgumentNullException` — at that point neither parameter
  name is the one the caller passed, and naming one arbitrarily is the defect being guarded against.
  It cannot fire through the public or internal surface; it exists so a constructor added later that
  forgets its own null check fails loudly at construction. Reached by reflection in tests so the
  guard is verified rather than merely asserted.

### Security

## [0.10.1] - 2026-08-18

Maintenance release: dependency refresh, build cleanup, and CI-noise hardening. **No
public-API change** — `PublicAPI.Shipped.txt` picks up nullability annotations that were
missing on symbols shipped in 0.10.0, but the compiled surface itself is unchanged; this
is a drop-in bump.

### Changed

- Bumped the `Wolfgang.Etl.Abstractions` family to **0.23.1**
  (`Wolfgang.Etl.Abstractions` for `src/`, and the test-only
  `Wolfgang.Etl.TestKit` / `Wolfgang.Etl.TestKit.Xunit` — including the `examples/`
  projects). Security + test-code-hardening release; no shipped-API change.
  Pinned at 0.23.1 rather than 0.23.2 pending the perf-regression investigation
  tracked at Chris-Wolfgang/ETL-Abstractions#427 — downstream `LoaderBenchmarks`
  file-write benchmarks regressed 2-4x on 0.23.2 (a bisect cleared at 0.23.1);
  will bump back to 0.23.2 or later once the upstream fix lands.
- Refreshed the remaining NuGet references to their latest patch/minor
  (`Meziantou.Analyzer`, `Roslynator.Analyzers`, `SonarAnalyzer.CSharp`,
  `Microsoft.SourceLink.GitHub`, `Microsoft.NET.Test.Sdk` net8.0+ pin,
  `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.Logging.Abstractions`,
  `System.Diagnostics.DiagnosticSource`).
- Gated the `Microsoft.CodeAnalysis.PublicApiAnalyzers` PackageReference on
  `Exists('PublicAPI.*.txt')` so the analyzer runs only in projects that opt in
  (fleet parity with `repo-template`); test / example / benchmark projects no
  longer pay for RS0016 / RS0037 they can't satisfy.
- Filled in the missing nullability annotations on the ~108 shipped members in
  `src/Wolfgang.Etl.FixedWidth/PublicAPI.Shipped.txt`. Metadata-only correction
  — the compiled surface already had those annotations from 0.10.0; only the
  tracking file was behind.

### Fixed

- MSB3277 `System.Collections.Immutable` conflict from the
  `Wolfgang.Etl.FixedWidth.Analyzers` `ProjectReference` on `net6.0` /
  `netcoreapp3.1` in the test project ([#311]). `ReferenceOutputAssembly` is
  now conditional on the TFM: `true` only on `net8.0+` (where
  `FixedWidthFieldAnalyzerTests` / `FixedWidthAccessorGeneratorOutputTests`
  instantiate the analyzer / generator types directly), `false` on older TFMs
  where those tests are `#if`'d out and the runtime closure would clash with
  the framework's own 6.x reference. `OutputItemType="Analyzer"` still runs
  the generator + FW0NN diagnostics at compile time on every TFM.

### Security

- Reduced the Scorecard code-scanning alert count from 95 → 0 ([#307]):
  SHA-pinned every previously-tag-only GitHub Action across all 19 workflow
  files (fleet convention `action@<sha> # vX`), version-pinned
  `pip install semgrep`, and added a `jq`-based SARIF-filter step in
  `scorecard.yaml` that drops `DangerousWorkflowID` /
  `BranchProtectionID` / `CodeReviewID` / `CIIBestPracticesID` /
  `FuzzingID` — plus the `PinnedDependenciesID` sub-checks for
  `nugetCommand not pinned` and `pipCommand not pinned` — before the
  code-scanning upload. Full rationale documented inline in `scorecard.yaml`.
  The scorecard.dev score published to the transparency log (README badge)
  still counts every check; only the code-scanning-tab noise is trimmed. A
  regression to an unpinned `@vN` action ref (`gitHubAction not pinned by
  hash`) still fires.
- Reduced the InspectCode code-scanning alert count from 1,232 → 0 ([#307])
  via scope-local `.editorconfig` files under `tests/`, `benchmarks/`,
  `examples/`, `src/Wolfgang.Etl.FixedWidth/`, and
  `src/Wolfgang.Etl.FixedWidth.Analyzers/` (each rule silenced with an
  inline rationale, matching the Try-Pattern peer's noise-floor profile),
  plus per-call-site `#pragma warning disable X` / `// ReSharper disable
  once X` annotations at the ~30 legitimate src false-positives with the
  rationale on the same line. No solution-wide `src/` silences.

## [0.10.0] - 2026-08-14

### Changed

- Adopted **ETL core 0.22.0** — `Wolfgang.Etl.Abstractions` 0.21.0 -> 0.22.0, along with the test-only
  `Wolfgang.Etl.TestKit` / `Wolfgang.Etl.TestKit.Xunit` references (including the `examples/`
  projects, which were still pinned to the pre-fold TestKit 0.14.0). 0.22.0 is the release in which
  the TestKit packages were folded into the ETL-Abstractions repository and now build and ship from
  there. The public API of all four core packages is unchanged.
- Inherited from Abstractions 0.22.0: the `await foreach` sites in `ExtractorBase` and
  `TransformerBase` now use `ConfigureAwait(false)`, removing a sync-over-async deadlock risk for
  consumers on the `net462` and `netstandard2.0` targets that drive the pipeline from a
  synchronization context. No behavioural change on the modern targets.

## [0.9.0] - 2026-08-12

Compile-time tooling (a source generator for Native-AOT-friendly field accessors and a
Roslyn analyzer for layout mistakes), multi-record-type routing, an `IDataReader`,
byte-offset checkpoint/resume, and COBOL binary field support. Additive public API — no
breaking changes.

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
  `FieldDelimiter` — all `init`-only, set in the object initializer), typed accessors, and
  `GetSchemaTable()` ([#26]).
- Byte-offset checkpoint / resume on `FixedWidthExtractor<T>` ([#31]): opt in with
  `TrackByteOffset = true` and read `CurrentByteOffset` after each record to persist a
  checkpoint; on restart, set `StartByteOffset` to that value to seek straight to the next
  unread line and skip the millions of records already processed. Terminators (`\n`, `\r`,
  `\r\n`), multi-byte UTF-8, and a leading byte-order mark are all counted exactly, so a
  saved offset is a precise byte position. Tracking is opt-in (it wraps the reader in a
  byte-counting decoder) and requires the `Stream` constructor — a seekable stream for
  resume; the default read path is unchanged. On resume, header lines are not re-skipped
  and `SkipItemCount` applies from the resumed position. `CurrentLineNumber` is exposed for
  diagnostics independent of checkpointing.
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
- Trailer record-count validation guidance and a runnable `MultiRecordTrailer` example —
  once multiple record types can be routed ([#19]), verifying a trailer's declared record
  count and control total against what was read is ordinary application logic, needing no
  new API ([#25]).
- Compile-time field-mapping source generator ([#13]): a new analyzer package,
  `Wolfgang.Etl.FixedWidth.Analyzers`, ships inside this NuGet and — for every type with
  `[FixedWidthField]` properties — emits a factory plus direct-access getter/setter
  delegates and registers them from a module initializer. The runtime prefers these over
  the previous `Expression.Compile`d delegates, removing the last `RequiresDynamicCode`
  code path so extraction and loading are **Native AOT and trimming compatible**, with no
  per-type startup JIT cost. It requires no code changes — keep decorating POCOs with
  `[FixedWidthField]`. Types the generator cannot handle (generic, inaccessible, or on
  `net462`/`netstandard2.0` where module initializers do not exist) transparently fall
  back to the reflection path, so behaviour is identical either way. Groundwork for the
  Native AOT support tracked by [#12].
- Compile-time layout diagnostics ([#27]): the `Wolfgang.Etl.FixedWidth.Analyzers` package
  now also ships a Roslyn analyzer that flags `[FixedWidthField]` mistakes in the IDE and
  the build, before the code runs — **FW003** (error) duplicate column index, **FW004**
  (warning) a `DateTime`/`DateTimeOffset`/`TimeSpan` field with no `Format` (which throws at
  runtime), **FW005** (warning) a `Format` pattern wider than the field, **FW007** (warning)
  a mapped property with no public setter, and **FW008** (info) one with no public getter.
  (The issue's FW001/FW002 — overlapping/gapped byte positions — do not apply to this
  library's index-based model, where positions are derived and contiguous by construction
  and gaps are declared explicitly with `[FixedWidthSkip]`; FW006 is deferred as too
  heuristic to flag without false positives.)

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
[#25]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/25
[#23]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/23
[#21]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/21
[#30]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/30
[#31]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/31
[#12]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/12
[#13]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/13
[#27]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/27
[#140]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/140
[#147]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/147
[#152]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/152
[#153]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/153
[#165]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/165
[#253]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/253
[#26]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/26
[#275]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/275
[Unreleased]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.10.1...HEAD
[0.10.1]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.10.0...v0.10.1
[0.10.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.9.0...v0.10.0
[0.9.0]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/compare/v0.8.0...v0.9.0
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
[#307]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/307
[#311]: https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/311
