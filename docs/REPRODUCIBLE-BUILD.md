# Reproducible builds — verify it yourself

`Wolfgang.Etl.FixedWidth` is built deterministically, so anyone can rebuild the
exact same compiled assemblies from the tagged source and confirm the release on
NuGet was built from that source and nothing else. This page is the **consumer
side** of that guarantee: how *you* independently verify a release.

> Related: [`reproducible-build.yaml`](../.github/workflows/reproducible-build.yaml)
> proves the build is reproducible on every PR (it builds the same commit twice in
> different paths and fails if the assemblies differ). This document is about a
> third party reproducing it out-of-band.

## What is reproducible

The unit of verification is the **compiled assembly** — one
`Wolfgang.Etl.FixedWidth.dll` per target framework
(`net462`, `net481`, `netstandard2.0`, `net8.0`, `net10.0`). These are
byte-for-byte reproducible because the build sets:

- `Deterministic` (default) — no timestamps or random GUIDs baked into the IL;
- `ContinuousIntegrationBuild=true` on CI — normalizes source paths to `/_/` so
  the checkout directory doesn't affect output;
- `EmbedUntrackedSources` + SourceLink — provenance is embedded deterministically.

> **NuGet packages (`.nupkg`) are *not* byte-for-byte reproducible.** A `.nupkg`
> is a ZIP and records per-entry timestamps, so its hash varies between builds.
> The manifest lists the package hashes for reference, but **verify the
> assemblies**, not the package.

## The per-release manifest

Every GitHub release attaches **`reproducible-build-manifest.json`**, produced by
[`release.yaml`](../.github/workflows/release.yaml) from the exact build that was
published. It records the expected assembly hashes plus the toolchain that
produced them:

```jsonc
{
  "tag": "v0.8.0",
  "commit": "…",
  "dotnetSdk": "10.0.100",          // the SDK you must use to reproduce
  "runnerOs": "Windows",
  "buildConfiguration": "Release",
  "assemblies": [
    { "tfm": "net10.0", "file": "Wolfgang.Etl.FixedWidth.dll", "sha256": "…" },
    …
  ],
  "packages": [                      // reference only — not byte-reproducible
    { "file": "Wolfgang.Etl.FixedWidth.0.8.0.nupkg", "sha256": "…" }
  ]
}
```

Download it from the release page, or with the CLI:

```bash
gh release download v0.8.0 --repo Chris-Wolfgang/ETL-FixedWidth --pattern reproducible-build-manifest.json
```

## Reproduce it

You need the **same .NET SDK version** the manifest records in `dotnetSdk`
(a different SDK ships a different Roslyn and may emit different IL). Install it
from <https://dotnet.microsoft.com/download/dotnet>.

```bash
# 1. Clone the source at the exact published tag.
git clone --depth 1 --branch v0.8.0 https://github.com/Chris-Wolfgang/ETL-FixedWidth
cd ETL-FixedWidth

# 2. Build Release with the CI determinism flag (matches the release build).
dotnet build src/Wolfgang.Etl.FixedWidth/Wolfgang.Etl.FixedWidth.csproj \
  -c Release -p:ContinuousIntegrationBuild=true

# 3. Hash each per-TFM assembly and compare against the manifest.
find src/Wolfgang.Etl.FixedWidth/bin/Release -name 'Wolfgang.Etl.FixedWidth.dll' \
  -exec sha256sum {} \;
```

If your `sha256` values match the manifest's `assemblies[].sha256`, the published
release is reproducible from source on your machine.

## If a hash does not match

A mismatch is worth reporting — it may be a determinism regression, a
toolchain difference, or something more serious.

1. Double-check you used the **exact** `dotnetSdk` from the manifest
   (`dotnet --version` must match) and passed `-p:ContinuousIntegrationBuild=true`.
2. [Open an issue](https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues/new)
   titled `reproducible-build discrepancy: <tag>` and include: the tag, your
   `dotnet --version`, your OS, and the assembly hashes you got versus the
   manifest's. Label it `maintenance - security`.

## Publish a third-party verification attestation

Independent verifications make the guarantee stronger than a single publisher's
claim. If you reproduced a release, you can publish an attestation others can
find:

- **Reproducible Builds conventions** — follow
  <https://reproducible-builds.org/docs/> to record your environment and result.
- **[vouchsafe.io](https://vouchsafe.io/)** (or a similar attestation service) —
  publish a signed statement that tag `vX.Y.Z` reproduced to the manifest hashes
  in your environment, and link it back on the release discussion.

Link your attestation in a comment on the release so future consumers can find
corroborating verifications.
