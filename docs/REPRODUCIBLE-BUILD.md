# Verifying a release of Wolfgang.Etl.FixedWidth

Anyone can check that a published release of Wolfgang.Etl.FixedWidth was built from the tagged source, without
trusting this repository's CI. This page is for a consumer who has never seen the repository.

## What "reproducible" means here

The same tag, built with the SDK recorded in the release's `BUILD-MANIFEST.txt`, **on the operating system its
`runner-os` line records**, gives:

- byte-identical `.dll` and `.pdb` files for every target framework, and
- `.nupkg` / `.snupkg` packages whose **entries** match the published packages entry by entry.

What is expected to differ, and is never compared:

- **the package file itself** (`.nupkg` / `.snupkg` as a zip): NuGet stamps every zip entry with the pack time, so
  two packs of the same commit never give the same zip bytes, even on one machine. Compare the entries.
- **a build on a different operating system than the release's `runner-os`**: this repository's release workflow
  currently packs on Windows (`pack-and-validate` runs on `windows-latest`), so a Linux rebuild of a release is an
  informational cross-OS check, not an expected match. Cross-OS identity is reported by the
  [Reproducible Build workflow](https://github.com/Chris-Wolfgang/ETL-FixedWidth/actions/workflows/reproducible-build.yaml), not promised.

Every pull request is checked the same way: the workflow packs the commit twice on Ubuntu and fails unless both
packs are identical by the rules above. That gate compares two Ubuntu builds with each other; it does not compare an
Ubuntu build with the Windows-built release.

## What a release publishes

Each GitHub Release carries, next to the packages:

| Asset | Contents |
|---|---|
| `SHA256SUMS.txt` | `<sha256>  <tfm>/<file>` for every `.dll`/`.pdb`, and `<sha256>  <package-file>` for every `.nupkg`/`.snupkg` |
| `BUILD-MANIFEST.txt` | exact SDK version (`dotnet --version`), runner image and version, commit SHA, tag, and the `CI` / `ContinuousIntegrationBuild` / `Deterministic` values |

Both are subjects of the release's build-provenance attestation, so step 5 below covers them as well as the packages.

## Verify a release

You need git, the GitHub CLI (`gh`), Python 3, PowerShell (`pwsh`, for the third-party notices step) and a machine of
the OS that `BUILD-MANIFEST.txt` records as `runner-os` (a Linux machine or container gives an informational
cross-OS comparison when the release was built on Windows).

1. **Clone the tag.**

   ```bash
   TAG=v1.2.3   # the release you are checking
   git clone --branch "$TAG" --depth 1 https://github.com/Chris-Wolfgang/ETL-FixedWidth.git
   cd ETL-FixedWidth
   gh release download "$TAG" --repo Chris-Wolfgang/ETL-FixedWidth --dir ../release
   cat ../release/BUILD-MANIFEST.txt
   ```

2. **Install the recorded SDK.** `global.json` pins the major version; install the exact version that
   `BUILD-MANIFEST.txt` names (for example with `dotnet-install.sh --version <sdk>`), and check that `dotnet --version`
   prints it from inside the clone.

3. **Restore, generate the notices and pack exactly as the workflow does**, once per packable project under `src/`.
   The release workflow restores, then runs `scripts/third-party-notices.ps1` before packing;
   `Directory.Build.props` packs the generated `obj/THIRD-PARTY-NOTICES.md` only when it exists, so skipping that
   step makes the rebuilt packages lack an entry the published ones carry.

   ```bash
   export CI=true
   dotnet restore
   pwsh ./scripts/third-party-notices.ps1
   for proj in $(git ls-files 'src/*.csproj' 'src/**/*.csproj'); do
     [ "$(dotnet msbuild "$proj" -getProperty:IsPackable | tr -d '[:space:]')" = "true" ] || continue
     dotnet pack "$proj" -c Release -p:ContinuousIntegrationBuild=true -p:Deterministic=true -o ../rebuilt
   done
   ```

4. **Compare.**

   ```bash
   # Assemblies and symbols, keyed <tfm>/<file> exactly as in SHA256SUMS.txt.
   for d in $(git ls-files 'src/*.csproj' 'src/**/*.csproj' | xargs -n1 dirname); do
     [ -d "$d/bin/Release" ] || continue
     (cd "$d/bin/Release" && find . -mindepth 2 -maxdepth 2 \( -name '*.dll' -o -name '*.pdb' \) \
       | sed 's|^\./||' | xargs sha256sum)
   done | sort -u -k2 > ../rebuilt-assemblies.txt
   grep -vE '\.s?nupkg$' ../release/SHA256SUMS.txt | sort -k2 | diff - ../rebuilt-assemblies.txt \
     && echo "Assemblies and symbols: identical"

   # Packages, entry by entry.
   python3 - ../release ../rebuilt <<'PY'
   import hashlib, pathlib, sys, zipfile
   def entries(p):
       with zipfile.ZipFile(p) as z:
           return {e.filename: hashlib.sha256(z.read(e)).hexdigest() for e in z.infolist() if not e.is_dir()}
   ok = True
   for pub in sorted(pathlib.Path(sys.argv[1]).glob("*.*nupkg")):
       mine = pathlib.Path(sys.argv[2]) / pub.name
       same = mine.exists() and entries(pub) == entries(mine)
       ok &= same
       print(("identical entries: " if same else "DIFFERS: ") + pub.name)
   sys.exit(0 if ok else 1)
   PY
   ```

5. **Check the provenance** of the attested assets against this repository. The release attests only the `.nupkg`
   files, `SHA256SUMS.txt` and `BUILD-MANIFEST.txt`; the other assets it uploads (`.snupkg`, SBOMs, the JSON manifest,
   the coverage ZIP) carry no attestation and are not verified here:

   ```bash
   for f in ../release/*.nupkg ../release/SHA256SUMS.txt ../release/BUILD-MANIFEST.txt; do
     gh attestation verify "$f" --repo Chris-Wolfgang/ETL-FixedWidth || { echo "ATTESTATION FAILED: $f"; break; }
   done
   ```

## Releases cut before the standard

A tag cut before this repository took the reproducible-builds pieces (literal copyright year, LF-normalized generated
sources, the reference-assemblies pin, `global.json`) has no `SHA256SUMS.txt` or `BUILD-MANIFEST.txt`, and its copyright
attribute may carry the year it was built in. To rebuild such a tag, pass the original year
(`-p:Copyright="Copyright (c) <year> <holder>"`) and expect a match only on the operating system that built the
release (for these tags, the one the workflow of that time used); a build on another OS is informational.

In this repository, releases up to and including v0.13.x attach `reproducible-build-manifest.json` instead: the
per-framework `Wolfgang.Etl.FixedWidth.dll` SHA-256 hashes and the SDK that built them. Compare those hashes with
`find src/Wolfgang.Etl.FixedWidth/bin/Release -name Wolfgang.Etl.FixedWidth.dll -exec sha256sum {} \;` after step 3 (on the same operating system that built the release).
Later releases attach that file as well as `SHA256SUMS.txt` and `BUILD-MANIFEST.txt`.
