# Getting Started

This guide will help you quickly get up and running with Wolfgang.Etl.FixedWidth.

## Prerequisites

- .NET 8.0 SDK or later (netstandard2.0 and netstandard2.1 packages are also available for older runtimes)

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

var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(reader);

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

var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);

await loader.LoadAsync(recordsAsyncEnumerable, CancellationToken.None);

Console.WriteLine(writer.ToString());
Console.WriteLine($"Total loaded: {loader.CurrentItemCount}");
```

In production, replace `StringReader` / `StringWriter` with a `FileStream` or `StreamReader` — the extractor and loader accept any `TextReader` / `TextWriter`, or a raw `Stream` directly.

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
