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
| **Format transformation** | `FixedWidthTransformer<TSource, TDestination>` projects one layout to another in a single streaming pass, with optional `ByMatchingProperties()` auto-mapping |
| **Multi-TFM support** | net462, net481, netstandard2.0, net8.0, net10.0 |

**Examples:**

The [examples/](examples/) folder contains 10 runnable console projects demonstrating each feature:

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

---

## 🎯 Target Frameworks

The package targets the following frameworks (see the project file for the authoritative list):

| Framework | Versions |
|-----------|----------|
| .NET Framework | .NET 4.6.2, .NET 4.8.1 |
| .NET Standard | .NET Standard 2.0 |
| .NET | .NET 8.0, .NET 10.0 |

> The CI test matrix additionally exercises the library on .NET Framework 4.7.x/4.8 and .NET 5.0–9.0 via the netstandard2.0 facade; those are tested-against runtimes, not package target frameworks.

---

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
