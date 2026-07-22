# Getting Started

This guide will help you quickly get up and running with Wolfgang.Etl.FixedWidth.

## Prerequisites

- .NET 8.0 SDK or later to follow this guide (the package also targets net462, net481, and netstandard2.0 for older runtimes)

## Installation

### Via NuGet Package Manager

```bash
dotnet add package Wolfgang.Etl.FixedWidth
```

### Via Package Manager Console

```powershell
Install-Package Wolfgang.Etl.FixedWidth
```

## Quick Start

### Define a record class

Map each property to a fixed-width column using `[FixedWidthField]` attributes:

```csharp
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

public class PersonRecord
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
```

### Extract records from fixed-width text

```csharp
using System.IO;
using System.Threading;
using Wolfgang.Etl.FixedWidth;

var inputData =
    "Alice     Anderson  025\n" +
    "Bob       Baker     042\n" +
    "Charlie   Clark     033";

var reader = new StringReader(inputData);

var extractor = new FixedWidthExtractor<PersonRecord>(reader);

await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
{
    Console.WriteLine($"{person.FirstName} {person.LastName}, Age {person.Age}");
}

Console.WriteLine($"Total extracted: {extractor.CurrentItemCount}");
```

### Load records to fixed-width text

```csharp
using System.IO;
using System.Threading;
using Wolfgang.Etl.FixedWidth;

var writer = new StringWriter();

var loader = new FixedWidthLoader<PersonRecord>(writer);

await loader.LoadAsync(recordsAsyncEnumerable, CancellationToken.None);

Console.WriteLine(writer.ToString());
Console.WriteLine($"Total loaded: {loader.CurrentItemCount}");
```

In production, replace `StringReader` / `StringWriter` with a `FileStream` or `StreamReader` — the extractor and loader accept any `TextReader` / `TextWriter`, or a raw `Stream` directly.

### Controlling line endings

`FixedWidthExtractor` reads `\n`, `\r`, and `\r\n` automatically, so input needs no configuration.

For output, the loader writes each record with its `TextWriter`'s newline. To force a specific ending regardless of platform — e.g. Unix `\n` for a downstream mainframe or FTP consumer — pass a `TextWriter` with the `NewLine` you want:

```csharp
using System.IO;

// Force Unix (LF) line endings, even on Windows
await using var stream = File.Create("output.dat");
await using var writer = new StreamWriter(stream) { NewLine = "\n" };
using var loader = new FixedWidthLoader<PersonRecord>(writer);

await loader.LoadAsync(recordsAsyncEnumerable, CancellationToken.None);
```

`NewLine` accepts any string; the default is `Environment.NewLine`.

### Inspecting the layout

`FixedWidthSchema.For<T>()` exposes the resolved field layout as a read-only view — handy for generating documentation, building validation tooling, or debugging a mapping. It runs the same validation as extraction, so an invalid layout throws here too.

```csharp
var schema = FixedWidthSchema.For<PersonRecord>();

foreach (var field in schema.Fields)   // includes skip columns (field.IsSkip)
{
    Console.WriteLine($"{field.StartPosition}-{field.EndPosition}  {field.Name}  ({field.Length})");
}

Console.WriteLine($"Line width: {schema.ExpectedLineWidth}, fields: {schema.FieldCount}, skips: {schema.SkipCount}");
```

Each `FixedWidthFieldInfo` carries `Name`, `StartPosition`/`EndPosition`, `Length`, `ColumnIndex`, `PropertyType`, `Alignment`, `Pad`, `Format`, `Header`, and `NumberStyles`. Skipped columns have `IsSkip == true` and a `SkipMessage`.

`ToDiagram()` renders the layout as a text table for logs, tickets, or docs:

```csharp
Console.WriteLine(FixedWidthSchema.For<EmployeeRecord>().ToDiagram());
// Position  Field           Type    Length  Align  Pad  Format
// --------  --------------  ------  ------  -----  ---  ------
// 0-9       FirstName       String  10      Left   ' '
// 10-17     [skip]                  8
// 18-23     EmployeeNumber  String  6       Left   ' '
//
// Total width: 24  |  Columns: 3 (2 fields + 1 skip)  |  Delimiter: none
```

### Transforming between layouts

`FixedWidthTransformer<TSource, TDestination>` reformats records from one layout to another (reorder, add/remove, or format-convert fields) as the projection stage between an extractor and a loader:

```csharp
using var extractor   = new FixedWidthExtractor<LegacyRecord>(sourceReader);
using var transformer = new FixedWidthTransformer<LegacyRecord, ModernRecord>(
    legacy => new ModernRecord { Id = legacy.OldId, Name = legacy.FullName.Trim() });
using var loader      = new FixedWidthLoader<ModernRecord>(destinationWriter);

var modern = transformer.TransformAsync(extractor.ExtractAsync(token), token);
await loader.LoadAsync(modern, token);
```

When source and destination share property names and compatible types, `FixedWidthTransformer<LegacyRecord, ModernRecord>.ByMatchingProperties()` builds the copy automatically (the destination needs a public parameterless constructor).

### Composing an ETL pipeline

The whole extract → transform → load flow can be written as one fluent chain on the generic `EtlPipeline` (from `Wolfgang.Etl.Abstractions` 0.16.0). `FixedWidthExtractor<T>` source factories hang off `EtlPipeline.Create()` and `FixedWidthLoader<T>` sink terminators hang off the pipeline, with the extractor/loader configuration exposed as inline setters:

```csharp
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;

await EtlPipeline
    .Create()
    .FixedWidthExtractor<PersonRecord>("people.dat")
    .Through(KeepAdults)                 // optional stream-to-stream transform delegate
    .FixedWidthLoader<PersonRecord>("people.txt")
    .WriteHeader(true)
    .FieldDelimiter(" | ")
    .RunAsync();
```

Every source and sink has **path**, `Stream`, and `TextReader`/`TextWriter` overloads (plus an existing-`FixedWidthExtractor<T>` overload). Path factories own the file stream they open and dispose it when the run finishes, on success or failure; caller-supplied streams, readers, and writers are left open. See the [PipelineExtensions example](examples.md#pipelineextensions) for a runnable walk-through.

### Metrics and observability

The extractor and loader emit `System.Diagnostics.Metrics` instruments from the meter `Wolfgang.Etl.FixedWidth` — counters (`items.extracted`, `items.loaded`, `items.skipped`, `lines.read`) and a duration histogram (`operation.duration`), each tagged with `etl.operation` and `etl.record_type`. Subscribe with OpenTelemetry and the telemetry flows to Prometheus, Grafana, Application Insights, and so on, with no changes to your extraction/loading code:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("Wolfgang.Etl.FixedWidth"));
```

Metrics are a no-op when nothing is listening. See the [Metrics example](examples.md#metrics) for a raw `MeterListener` walk-through.

## Next Steps

- Browse the [Examples](examples.md) for more detailed scenarios
- Explore the [API Reference](../api/index.md) for detailed documentation
- Read the [Introduction](introduction.md) to learn more about Wolfgang.Etl.FixedWidth

## Common Issues

- **FieldOverflowException during loading** — a value exceeds its declared field length. Either increase the `Length` in `[FixedWidthField]` or switch to `FixedWidthConverter.Truncate`.
- **MalformedLineException during extraction** — a line is shorter or longer than expected. Set `MalformedLineHandling` to `Skip` to ignore such lines, or correct the input data.

## Additional Resources

- [GitHub Repository](https://github.com/Chris-Wolfgang/ETL-FixedWidth)
- [Contributing Guidelines](https://github.com/Chris-Wolfgang/ETL-FixedWidth/blob/main/CONTRIBUTING.md)
- [Report an Issue](https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues)
