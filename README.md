# Wolfgang.Etl.FixedWidth

Extractor and Loader for reading and writing fixed width files and text streams

[![NuGet](https://img.shields.io/nuget/v/Wolfgang.Etl.FixedWidth.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Wolfgang.Etl.FixedWidth/)
[![Downloads](https://img.shields.io/nuget/dt/Wolfgang.Etl.FixedWidth.svg?logo=nuget&label=downloads)](https://www.nuget.org/packages/Wolfgang.Etl.FixedWidth/)
[![PR build](https://img.shields.io/github/actions/workflow/status/Chris-Wolfgang/ETL-FixedWidth/pr.yaml?event=pull_request_target&label=PR%20build&logo=github)](https://github.com/Chris-Wolfgang/ETL-FixedWidth/actions/workflows/pr.yaml)
[![release](https://img.shields.io/github/actions/workflow/status/Chris-Wolfgang/ETL-FixedWidth/release.yaml?event=release&label=release&logo=github)](https://github.com/Chris-Wolfgang/ETL-FixedWidth/actions/workflows/release.yaml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Multi--Targeted-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/ETL-FixedWidth)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/Chris-Wolfgang/ETL-FixedWidth/badge)](https://scorecard.dev/viewer/?uri=github.com/Chris-Wolfgang/ETL-FixedWidth)

---

## 📦 Installation

```bash
dotnet add package Wolfgang.Etl.FixedWidth
```

**NuGet Package:** [Wolfgang.Etl.FixedWidth on NuGet.org](https://www.nuget.org/packages/Wolfgang.Etl.FixedWidth/)

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

## 📚 Documentation

- **GitHub Repository:** [https://github.com/Chris-Wolfgang/ETL-FixedWidth](https://github.com/Chris-Wolfgang/ETL-FixedWidth)
- **API Documentation:** https://Chris-Wolfgang.github.io/ETL-FixedWidth/
- **Formatting Guide:** [README-FORMATTING.md](docs/README-FORMATTING.md)
- **Contributing Guide:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🚀 Quick Start

### Extraction — Reading a Fixed-Width File

Define a POCO with `[FixedWidthField]` attributes, then read records with `await foreach`:

```csharp
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// 1. Define the record class.
public class PersonRecord
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}

// 2. Create the extractor (accepts any TextReader or Stream).
var reader = new StringReader
(
    "Alice     Anderson  025\n" +
    "Bob       Baker     042\n" +
    "Charlie   Clark     033"
);

var extractor = new FixedWidthExtractor<PersonRecord>(reader);

// 3. Iterate records asynchronously.
await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
{
    Console.WriteLine($"{person.FirstName} {person.LastName}, Age {person.Age}");
}

// Output:
//   Alice Anderson, Age 25
//   Bob Baker, Age 42
//   Charlie Clark, Age 33
```

### Loading — Writing a Fixed-Width File

```csharp
var writer = new StringWriter();
var loader = new FixedWidthLoader<PersonRecord>(writer);

// LoadAsync accepts an IAsyncEnumerable<PersonRecord>; `sourceItems` is any
// async sequence of records (for example, the output of a FixedWidthExtractor).
await loader.LoadAsync(sourceItems, CancellationToken.None);

Console.WriteLine(writer.ToString());
// Output:
//   Alice     Anderson  025
//   Bob       Baker     042
//   Charlie   Clark     033
```

For file-based I/O, use the `Stream` constructor which creates a 64 KB buffered reader/writer for improved throughput:

```csharp
// Extraction from a file
await using var readStream = File.OpenRead("people.dat");
using var extractor = new FixedWidthExtractor<PersonRecord>(readStream);

// Loading to a file
await using var writeStream = File.OpenWrite("output.dat");
using var loader = new FixedWidthLoader<PersonRecord>(writeStream);
```

Because the `Stream` constructors accept any `Stream`, compression works out of the box — wrap the file stream in a `GZipStream` or `BrotliStream` to read or write compressed fixed-width data (common for mainframe `.gz` exports) without a decompressed copy on disk:

```csharp
// Extraction from a GZip-compressed file
await using var readStream = File.OpenRead("people.dat.gz");
await using var readGzip = new GZipStream(readStream, CompressionMode.Decompress);
using var extractor = new FixedWidthExtractor<PersonRecord>(readGzip);

// Loading to a GZip-compressed file
await using var writeStream = File.Create("output.dat.gz");
await using var writeGzip = new GZipStream(writeStream, CompressionLevel.Optimal);
using var loader = new FixedWidthLoader<PersonRecord>(writeGzip);
```

See the [CompressedStreams](examples/CompressedStreams) example for a complete GZip and Brotli round trip.

### Controlling line endings

`FixedWidthExtractor` reads any line ending automatically — `\n`, `\r`, or `\r\n` — so no configuration is needed for input.

For **output**, the loader writes each record with its `TextWriter`'s newline. To force a specific ending regardless of the platform you run on — for example, a downstream mainframe or FTP consumer that requires Unix `\n` — pass a `TextWriter` with the `NewLine` you want:

```csharp
// Force Unix (LF) line endings, even on Windows
await using var stream = File.Create("output.dat");
await using var writer = new StreamWriter(stream) { NewLine = "\n" };
using var loader = new FixedWidthLoader<PersonRecord>(writer);

await loader.LoadAsync(records, CancellationToken.None);
```

`NewLine` accepts any string (`"\n"`, `"\r\n"`, or a custom terminator). The default is `Environment.NewLine`.

### Inspecting the layout

`FixedWidthSchema.For<T>()` exposes the resolved field layout as a read-only view — useful for generating documentation, building validation tooling, or debugging a mapping. It applies the same validation as extraction, so an invalid layout (duplicate column index, a mapped field with no public setter) throws here too.

```csharp
var schema = FixedWidthSchema.For<PersonRecord>();

foreach (var field in schema.Fields)   // includes skip columns (field.IsSkip)
{
    Console.WriteLine($"{field.StartPosition}-{field.EndPosition}  {field.Name}  ({field.Length})");
}

schema.ExpectedLineWidth;   // total line width, including skipped columns
schema.TotalColumnCount;    // columns including skips
schema.FieldCount;          // mapped fields only
schema.SkipCount;           // skipped columns
```

Each `FixedWidthFieldInfo` carries `Name`, `StartPosition`/`EndPosition`, `Length`, `ColumnIndex`, `PropertyType`, `Alignment`, `Pad`, `Format`, `Header`, and `NumberStyles`. Skipped columns have `IsSkip == true` and expose a `SkipMessage` instead of a name.

`ToDiagram()` renders the layout as a text table — drop it into a log line at startup or paste it into a ticket:

```csharp
Console.WriteLine(FixedWidthSchema.For<EmployeeRecord>().ToDiagram());
```

```text
Position  Field           Type    Length  Align  Pad  Format
--------  --------------  ------  ------  -----  ---  ------
0-9       FirstName       String  10      Left   ' '
10-17     [skip]                  8
18-23     EmployeeNumber  String  6       Left   ' '

Total width: 24  |  Columns: 3 (2 fields + 1 skip)  |  Delimiter: none
```

### Defining the layout in code

When you can't decorate the record type — a third-party POCO, or a layout chosen at runtime — build the schema with `FixedWidthSchemaBuilder<T>` instead of attributes, then hand it to the extractor or loader via its `Schema` property:

```csharp
var schema = new FixedWidthSchemaBuilder<CustomerRecord>()
    .Field(r => r.CustomerId, index: 0, length: 8)
    .Field(r => r.Name, index: 1, length: 30)
    .Skip(index: 2, length: 5)
    .Field(r => r.Balance, index: 3, length: 9, alignment: FieldAlignment.Right, format: "0000000.00")
    .Build();

using var extractor = new FixedWidthExtractor<CustomerRecord>(reader) { Schema = schema };
```

The builder uses lambda expressions for type-safe, refactor-proof property references — no magic strings. `index` is the zero-based column ordinal (the same value as `[FixedWidthField(index, length)]`); start positions are computed from the column lengths. A schema built this way is **equivalent** to one resolved from attributes: it validates the same way (duplicate index, no public setter) and is fully introspectable via `Fields` / `ToDiagram()`. Setting `Schema` overrides any attributes on the type.

See the [SchemaBuilder](examples/SchemaBuilder) example for a runnable walk-through.

### Transforming between layouts

To reformat a fixed-width file from one layout to another — reordering, adding/removing, or format-converting fields (a common mainframe-migration task) — `FixedWidthTransformer<TSource, TDestination>` is the projection stage between an extractor and a loader:

```csharp
using var extractor   = new FixedWidthExtractor<LegacyRecord>(sourceReader);
using var transformer = new FixedWidthTransformer<LegacyRecord, ModernRecord>(
    legacy => new ModernRecord
    {
        Id   = legacy.OldId,
        Name = legacy.FullName.Trim(),
    });
using var loader      = new FixedWidthLoader<ModernRecord>(destinationWriter);

// Extract → transform → load in a single streaming pass.
var modern = transformer.TransformAsync(extractor.ExtractAsync(token), token);
await loader.LoadAsync(modern, token);
```

The projection delegate handles every reformatting case. When source and destination differ only in layout — the same property names and compatible types — use the auto-mapping factory instead of writing the copy by hand:

```csharp
using var transformer = FixedWidthTransformer<LegacyRecord, ModernRecord>.ByMatchingProperties();
```

`ByMatchingProperties()` copies every source property to the destination property of the same name and an assignable type, and requires a public parameterless constructor on the destination.

### Reading files with multiple record types

Mainframe and EDI batch files often interleave several record layouts on different lines — a header, many detail rows, and a trailer — distinguished by a discriminator character. `FixedWidthMultiExtractor` routes each line to the right POCO: register one rule per type, and the first matching predicate wins.

```csharp
using var extractor = new FixedWidthMultiExtractor(reader)
    .When(line => line[0] == 'H', typeof(HeaderRecord))
    .When(line => line[0] == 'D', typeof(DetailRecord))
    .When(line => line[0] == 'T', typeof(TrailerRecord));

await foreach (var record in extractor.ExtractAsync(token))
{
    switch (record)
    {
        case HeaderRecord h: /* ... */ break;
        case DetailRecord d: /* ... */ break;
        case TrailerRecord t: /* ... */ break;
    }
}
```

Each record type keeps its own independent `[FixedWidthField]` layout. A line that matches no rule throws by default; set `UnmatchedLineHandling = UnmatchedLineHandling.Skip` to drop it, or register a catch-all type with `.Otherwise(typeof(UnknownRecord))`. Blank lines are skipped before predicates run (so a discriminator can index the line safely), and the extractor shares the family's `HeaderLineCount`, `FieldDelimiter`, `ValueParser`, `SkipItemCount`/`MaximumItemCount`, dead-letter `OnError`, and progress reporting.

**Trailer record-count validation** falls out of this naturally: capture the trailer as it streams past, count the details, and compare — no extra API needed.

```csharp
if (trailer.RecordCount != detailCount)
    throw new InvalidDataException($"Trailer says {trailer.RecordCount}, file has {detailCount}.");
```

See the [MultiRecordTrailer](examples/MultiRecordTrailer) example for a runnable header/detail/trailer walk-through that checks both the record count and a control total.

### Composing an ETL pipeline

Rather than wiring an extractor, transformer, and loader together by hand, the whole extract → transform → load flow can be expressed as one fluent chain on the generic `EtlPipeline` (from `Wolfgang.Etl.Abstractions` 0.16.0). `FixedWidthExtractor<T>` source factories hang off `EtlPipeline.Create()` and `FixedWidthLoader<T>` sink terminators hang off the pipeline, with the extractor/loader configuration exposed as inline setters:

```csharp
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;

// Fixed-width in, human-readable table out — path factories own the files they open.
await EtlPipeline
    .Create()
    .FixedWidthExtractor<PersonRecord>("people.dat")
    .FixedWidthLoader<PersonRecord>("people.txt")
    .WriteHeader(true)
    .FieldSeparator('-')
    .FieldDelimiter(" | ")
    .RunAsync();
```

Insert transform stages with `Through` — an inline `Func<IAsyncEnumerable<T>, IAsyncEnumerable<TOut>>` stage needs no reference to the operators package:

```csharp
await EtlPipeline
    .Create()
    .FixedWidthExtractor<PersonRecord>(sourceReader)
    .Through(KeepAdults)                 // a stream-to-stream transform delegate
    .FixedWidthLoader<PersonRecord>(destinationWriter)
    .RunAsync();
```

Every source and sink has **path**, `Stream`, and `TextReader`/`TextWriter` overloads (plus an existing-`FixedWidthExtractor<T>` overload). **Path** factories own the file stream they open and dispose it when the run finishes, on success or failure; caller-supplied streams, readers, and writers are always left open. The builder methods (`HeaderLineCount`, `MalformedLineHandling`, `FieldDelimiter`, `Encoding`, `WriteHeader`, `ValueConverter`, `IsDryRun`, …) map 1:1 to the `FixedWidthExtractor<T>` / `FixedWidthLoader<T>` properties.

See the [PipelineExtensions](examples/PipelineExtensions) example for a complete, runnable walk-through.

### Metrics and observability

The extractor and loader can emit standard [`System.Diagnostics.Metrics`](https://learn.microsoft.com/dotnet/core/diagnostics/metrics) instruments from the meter **`Wolfgang.Etl.FixedWidth`**, so throughput and error rates flow to OpenTelemetry, Prometheus, Grafana, Application Insights, or any `MeterListener`. Metrics are **zero-config**: they activate automatically when a listener subscribes to the `Wolfgang.Etl.FixedWidth` meter — there's no flag to set. When nothing is listening, the extract/load loop (sampling once per operation) runs **no** metric code at all, so telemetry adds zero overhead for callers that don't use it.

| Instrument | Type | Description |
|---|---|---|
| `wolfgang.etl.fixedwidth.items.extracted` | Counter | Items successfully extracted |
| `wolfgang.etl.fixedwidth.items.loaded` | Counter | Items successfully loaded |
| `wolfgang.etl.fixedwidth.items.skipped` | Counter | Items skipped via the skip budget |
| `wolfgang.etl.fixedwidth.lines.read` | Counter | Physical lines read (including blank/skipped) |
| `wolfgang.etl.fixedwidth.operation.duration` | Histogram (ms) | Duration of an extract/load operation |

Every measurement is tagged `etl.operation` (`extract` or `load`) and `etl.record_type` (`typeof(TRecord).Name`).

```csharp
// Subscribe once at startup — OpenTelemetry, zero per-call code; metrics turn on automatically:
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("Wolfgang.Etl.FixedWidth"));
```

See the [Metrics](examples/Metrics) example for a runnable `MeterListener` walk-through.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| **Attribute-based field mapping** | `[FixedWidthField(index, length)]` maps properties to columns by index and width |
| **Skip columns** | `[FixedWidthSkip(index, length)]` declares columns in the file that are not mapped to any property |
| **Alignment and padding** | `Alignment = FieldAlignment.Left\|Right` with configurable `Pad` character (default space) |
| **Custom parsing** | `ValueParser` delegate on the extractor for custom extraction logic per field |
| **Custom conversion** | `ValueConverter` delegate on the loader for custom write formatting per field |
| **Header rows** | `HasHeader` / `HeaderLineCount` (extractor) and `WriteHeader` (loader) |
| **Separator lines** | `FieldSeparator` character for visual separator lines between headers and data |
| **Field delimiters** | `FieldDelimiter` string (e.g. `" \| "`) inserted between fields for human-readable output |
| **Pagination** | `SkipItemCount` and `MaximumItemCount` for skipping and limiting records |
| **Blank line handling** | `BlankLineHandling` — `ThrowException`, `Skip`, or `ReturnDefault` |
| **Malformed line handling** | `MalformedLineHandling` — `ThrowException`, `Skip`, or `ReturnDefault` |
| **Line filtering** | `LineFilter` delegate for custom line-level control (`Process`, `Skip`, `Stop`) |
| **Progress reporting** | Timer-based `IProgress<T>` reporting via `FixedWidthReport` (includes `CurrentLineNumber`) |
| **Zero-copy parsing** | `ReadOnlyMemory<char>` slicing avoids string allocations during field extraction |
| **Span-based numerics** | `Span<char>`-based numeric parsing on net8.0+ for reduced allocation |
| **Compiled delegates** | Field accessors use compiled delegates instead of reflection for fast property get/set |
| **Schema introspection** | `FixedWidthSchema.For<T>()` exposes the resolved layout (positions, widths, types, skips); `ToDiagram()` renders it as a text table |
| **Code-defined layout** | `FixedWidthSchemaBuilder<T>` defines a layout in fluent, type-safe code (no attributes required); assign it to the extractor/loader `Schema` property |
| **Format transformation** | `FixedWidthTransformer<TSource, TDestination>` projects one layout to another in a single streaming pass, with optional `ByMatchingProperties()` auto-mapping |
| **Multi-record-type files** | `FixedWidthMultiExtractor` routes each line to a different POCO by a discriminator predicate (`.When(…)` / `.Otherwise(…)`), for header/detail/trailer batch files |
| **Pipeline composition** | `EtlPipeline.Create().FixedWidthExtractor<T>(…).FixedWidthLoader<T>(…).RunAsync()` — fluent source factories and sink terminators over the generic `EtlPipeline` (requires `Wolfgang.Etl.Abstractions` 0.16.0) |
| **Metrics** | Zero-config `System.Diagnostics.Metrics` instruments (throughput, skips, duration) from the `Wolfgang.Etl.FixedWidth` meter — OpenTelemetry / Prometheus / any `MeterListener` |
| **Multi-TFM support** | net462, net481, netstandard2.0, net8.0, net10.0 |

**Examples:**

The [examples/](examples/) folder contains 15 runnable console projects demonstrating each feature:

| Example | Description |
|---------|-------------|
| [BasicExtraction](examples/BasicExtraction) | Read fixed-width data into strongly typed records |
| [BasicLoading](examples/BasicLoading) | Write records to fixed-width output |
| [CompressedStreams](examples/CompressedStreams) | Read and write GZip / Brotli compressed fixed-width data |
| [RoundTrip](examples/RoundTrip) | Extract, transform, and reload records end-to-end |
| [CustomParsersConverters](examples/CustomParsersConverters) | Custom `ValueParser` and `ValueConverter` delegates |
| [ProgressReporting](examples/ProgressReporting) | Timer-based `IProgress<FixedWidthReport>` callbacks |
| [ErrorHandling](examples/ErrorHandling) | `BlankLineHandling`, `MalformedLineHandling`, and `LineFilter` |
| [FieldDelimiter](examples/FieldDelimiter) | Delimited output (e.g. `" \| "`) for human-readable tables |
| [SkipAndMax](examples/SkipAndMax) | `SkipItemCount` and `MaximumItemCount` for pagination |
| [HeadersAndSeparators](examples/HeadersAndSeparators) | `WriteHeader`, `HasHeader`, and `FieldSeparator` |
| [PipelineExtensions](examples/PipelineExtensions) | Compose extract → transform → load as one `EtlPipeline` fluent chain |
| [Metrics](examples/Metrics) | Subscribe to the `Wolfgang.Etl.FixedWidth` meter and read throughput/duration metrics |
| [SchemaBuilder](examples/SchemaBuilder) | Define a layout in code with `FixedWidthSchemaBuilder<T>` instead of attributes |
| [DataReader](examples/DataReader) | Expose a fixed-width source as an `IDataReader` for `SqlBulkCopy` / `DataTable` (no POCO per row) |
| [MultiRecordTrailer](examples/MultiRecordTrailer) | Route header/detail/trailer records with `FixedWidthMultiExtractor` and validate the trailer's record count and control total |

---

## 🎯 Supported Frameworks

This library targets:

- **.NET Framework:** 4.6.2, 4.8.1
- **.NET Standard:** 2.0
- **.NET:** 8.0, 10.0

> The CI test matrix additionally exercises the library on .NET Framework 4.7.x/4.8 and .NET 5.0–9.0 via the `netstandard2.0` facade; those are tested-against runtimes, not package target frameworks.

See the [NuGet package page](https://www.nuget.org/packages/Wolfgang.Etl.FixedWidth/) for the authoritative per-TFM compatibility matrix.

## 🔍 Code Quality & Static Analysis

This project enforces **strict code quality standards** through **7 specialized analyzers** and custom async-first rules:

### Analyzers in Use

1. **Microsoft.CodeAnalysis.NetAnalyzers** - Built-in .NET analyzers for correctness and performance
2. **Roslynator.Analyzers** - Advanced refactoring and code quality rules
3. **AsyncFixer** - Async/await best practices and anti-pattern detection
4. **Microsoft.VisualStudio.Threading.Analyzers** - Thread safety and async patterns
5. **Microsoft.CodeAnalysis.BannedApiAnalyzers** - Prevents usage of banned synchronous APIs
6. **Meziantou.Analyzer** - Comprehensive code quality rules
7. **SonarAnalyzer.CSharp** - Industry-standard code analysis

### Async-First Enforcement

This library uses **`BannedSymbols.txt`** to prohibit synchronous APIs and enforce async-first patterns:

**Blocked APIs Include:**
- ❌ `Task.Wait()`, `Task.Result` - Use `await` instead
- ❌ `Thread.Sleep()` - Use `await Task.Delay()` instead
- ❌ Synchronous file I/O (`File.ReadAllText`) - Use async versions
- ❌ Synchronous stream operations - Use `ReadAsync()`, `WriteAsync()`
- ❌ `Parallel.For/ForEach` - Use `Task.WhenAll()` or `Parallel.ForEachAsync()`
- ❌ Obsolete APIs (`WebClient`, `BinaryFormatter`)

**Why?** To ensure all code is **truly async** and **non-blocking** for optimal performance in async contexts.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later (required for the `net10.0` target framework)
- Optional: [PowerShell Core](https://github.com/PowerShell/PowerShell) for formatting scripts

### Build Steps

```bash
# Clone the repository
git clone https://github.com/Chris-Wolfgang/ETL-FixedWidth.git
cd ETL-FixedWidth

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Run code formatting (PowerShell Core)
pwsh ./scripts/format.ps1
```

### Code Formatting

This project uses `.editorconfig` and `dotnet format`:

```bash
# Format code
dotnet format

# Verify formatting (as CI does)
dotnet format --verify-no-changes
```

See [README-FORMATTING.md](docs/README-FORMATTING.md) for detailed formatting guidelines.

### Building Documentation

This project uses [DocFX](https://dotnet.github.io/docfx/) to generate API documentation:

```bash
# Install DocFX (one-time setup)
dotnet tool install -g docfx

# Generate API metadata and build documentation
cd docfx_project
docfx metadata  # Extract API metadata from source code
docfx build     # Build HTML documentation

# Documentation is generated in the docs/ folder at the repository root
```

The documentation is automatically built and deployed to GitHub Pages when changes are pushed to the `main` branch.

**Local Preview:**
```bash
# Serve documentation locally (with live reload)
cd docfx_project
docfx build --serve

# Open http://localhost:8080 in your browser
```

**Documentation Structure:**
- `docfx_project/` - DocFX configuration and source files
- `docs/` - Generated HTML documentation (published to GitHub Pages)
- `docfx_project/index.md` - Main landing page content
- `docfx_project/docs/` - Additional documentation articles
- `docfx_project/api/` - Auto-generated API reference YAML files

---

## 🔐 Verify the build

The library is built **deterministically**, so you can rebuild the exact
assemblies from the tagged source and confirm a NuGet release was built from that
source and nothing else. Every GitHub release attaches a
`reproducible-build-manifest.json` with the expected per-framework assembly
hashes and the toolchain that produced them.

```bash
# Download the manifest for a release, rebuild at the tag, and compare hashes.
gh release download v0.8.0 --repo Chris-Wolfgang/ETL-FixedWidth --pattern reproducible-build-manifest.json
git clone --depth 1 --branch v0.8.0 https://github.com/Chris-Wolfgang/ETL-FixedWidth
dotnet build ETL-FixedWidth/src/Wolfgang.Etl.FixedWidth/Wolfgang.Etl.FixedWidth.csproj -c Release -p:ContinuousIntegrationBuild=true
find ETL-FixedWidth/src/Wolfgang.Etl.FixedWidth/bin/Release -name 'Wolfgang.Etl.FixedWidth.dll' -exec sha256sum {} \;
```

See **[docs/REPRODUCIBLE-BUILD.md](docs/REPRODUCIBLE-BUILD.md)** for the full
procedure — which SDK version to use, how to file a discrepancy, and how to
publish a third-party verification attestation.

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Code quality standards
- Build and test instructions
- Pull request guidelines
- Analyzer configuration details

---


## 🙏 Acknowledgments

- **[Wolfgang.Etl.Abstractions](https://github.com/Chris-Wolfgang/ETL-Abstractions)** — provides the `ExtractorBase`, `LoaderBase`, and `TransformerBase` base classes, progress reporting infrastructure, and the `IProgressTimer` contract that this library builds on.
- **[Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)** — provides the `ILogger` interface used for optional structured diagnostic logging throughout the extractor and loader.
