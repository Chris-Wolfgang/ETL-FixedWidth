# Examples

The [examples directory](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/) contains runnable console applications that demonstrate key features of Wolfgang.Etl.FixedWidth. Each example is a standalone project you can build and run with `dotnet run`.

## BasicExtraction

Demonstrates the simplest extraction workflow: defining a record class with `[FixedWidthField]` attributes, reading fixed-width text from a `StringReader`, and iterating records with `await foreach`.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/BasicExtraction)

## BasicLoading

Demonstrates the simplest loading workflow: writing strongly typed records to fixed-width text output using a `StringWriter`, including padding, alignment, and pad character behavior.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/BasicLoading)

## RoundTrip

Shows a full round-trip pipeline: extracting records from fixed-width text, then loading them back out to produce identical output. Validates that extraction and loading are symmetric.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/RoundTrip)

## CustomParsersConverters

Demonstrates how to plug in custom `ValueParser` delegates for extraction and custom `ValueConverter` delegates for loading, enabling non-standard type conversions and formatting.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/CustomParsersConverters)

## ProgressReporting

Shows how to use timer-based progress reporting to receive periodic `FixedWidthReport` snapshots during long-running extraction or loading operations.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/ProgressReporting)

## ErrorHandling

Demonstrates `BlankLineHandling` and `MalformedLineHandling` modes, showing how to skip, error on, or pass through problematic lines during extraction.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/ErrorHandling)

## FieldDelimiter

Shows how to configure inter-field delimiters (e.g., `|`) between columns in the fixed-width output, and how the extractor handles delimited input.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/FieldDelimiter)

## SkipAndMax

Demonstrates `SkipItemCount` and `MaximumItemCount` for pagination — skipping the first N records and capping the total number of records extracted or loaded.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/SkipAndMax)

## HeadersAndSeparators

Shows how to enable header row output and separator lines when loading, including custom header text via the `Header` property on `[FixedWidthField]`.

[View source](https://github.com/Chris-Wolfgang/ETL-FixedWidth/tree/main/examples/HeadersAndSeparators)
