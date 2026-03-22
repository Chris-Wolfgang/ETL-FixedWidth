// ---------------------------------------------------------------------------
// BasicLoading Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the FixedWidthLoader as part of an ETL pipeline.
// A TestExtractor from Wolfgang.Etl.TestKit provides the source data, which
// flows through a TestTransformer and into the FixedWidthLoader for output.
//
// Key concepts covered:
//   - Defining a record class with [FixedWidthField] attributes
//   - Creating a FixedWidthLoader with a TextWriter (StringWriter here)
//   - Composing an ETL pipeline: Extractor -> Transformer -> Loader
//   - Using TestExtractor and TestTransformer as stand-ins for real components
//   - Padding, alignment, and pad character behavior during writes
//   - Ownership semantics: the loader does NOT dispose a caller-owned writer
//   - Streaming architecture: records flow one at a time through the pipeline
//
// In production, you would replace the TestExtractor with a real extractor
// (e.g., a database reader or API client) and the TestTransformer with a
// transformer that applies business logic. The composition pattern remains
// the same.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.TestKit;

// ---------------------------------------------------------------------------
// Step 1: Create the source data using a TestExtractor.
//
// The TestExtractor is an in-memory extractor from Wolfgang.Etl.TestKit. It
// wraps an IEnumerable<T> and yields items as an IAsyncEnumerable<T>, just
// like a real extractor would. In production, this would be replaced by a
// real extractor that reads from a database, API, or another file format.
// ---------------------------------------------------------------------------

var people = new List<PersonRecord>
{
    new PersonRecord { FirstName = "Alice",   LastName = "Anderson", Age = 25 },
    new PersonRecord { FirstName = "Bob",     LastName = "Baker",    Age = 42 },
    new PersonRecord { FirstName = "Charlie", LastName = "Clark",    Age = 33 },
};

var extractor = new TestExtractor<PersonRecord>(people);

// ---------------------------------------------------------------------------
// Step 2: Create the transformer and loader.
//
// The pipeline uses three components:
//   - TestExtractor:       provides in-memory source records (see Step 1)
//   - TestTransformer:     a pass-through transformer from Wolfgang.Etl.TestKit
//                          (in production, this would apply business logic)
//   - FixedWidthLoader:    writes records to fixed-width text output
//
// When using the TextWriter constructor, ownership semantics are:
//   - The caller owns the writer's lifetime (the loader does NOT dispose it).
//   - Calling loader.Dispose() is optional and has no effect.
//
// When using the Stream constructor instead, the loader creates an internal
// StreamWriter with a 64 KB buffer and Dispose() must be called to release it.
// ---------------------------------------------------------------------------

var transformer = new TestTransformer<PersonRecord>();

var writer = new StringWriter();
var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);

// ---------------------------------------------------------------------------
// Step 3: Run the pipeline.
//
// The three stages are composed using IAsyncEnumerable<T>:
//
//   extractor.ExtractAsync(token)          -> IAsyncEnumerable<PersonRecord>
//   transformer.TransformAsync(source)     -> IAsyncEnumerable<PersonRecord>
//   loader.LoadAsync(source, token)        -> Task (consumes the entire stream)
//
// Records flow one at a time from extractor through the transformer to the
// loader. No intermediate list or buffer is needed.
// ---------------------------------------------------------------------------

var token = CancellationToken.None;

await loader.LoadAsync
(
    transformer.TransformAsync(extractor.ExtractAsync(token), token),
    token
);

// ---------------------------------------------------------------------------
// Step 4: Print the output.
//
// Each line is exactly 23 characters wide:
//   "Alice     Anderson  025"
//   "Bob       Baker     042"
//   "Charlie   Clark     033"
// ---------------------------------------------------------------------------

Console.WriteLine("Fixed-width output:");
Console.WriteLine(new string('-', 40));
Console.WriteLine(writer.ToString());
Console.WriteLine(new string('-', 40));
Console.WriteLine($"Records extracted:   {extractor.CurrentItemCount}");
Console.WriteLine($"Records transformed: {transformer.CurrentItemCount}");
Console.WriteLine($"Records loaded:      {loader.CurrentItemCount}");

// ---------------------------------------------------------------------------
// PersonRecord — the POCO class with [FixedWidthField] attributes.
//
// The loader reads these attributes to determine how to format each property
// value into the fixed-width output. The key attribute properties are:
//
//   - Index:     Zero-based column order. Fields are written left to right.
//   - Length:    The exact number of characters this field occupies.
//   - Alignment: Left (default) pads on the right; Right pads on the left.
//   - Pad:       The padding character (default is space ' ').
//
// During writes, if a value's string representation is shorter than Length,
// it is padded according to Alignment and Pad. If it is longer, the default
// Strict converter throws a FieldOverflowException. You can switch to
// FixedWidthConverter.Truncate to silently truncate instead.
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a single person record in a fixed-width file.
/// Total line width: 10 + 10 + 3 = 23 characters per line.
/// </summary>
public class PersonRecord
{
    // Column 0: First name, 10 characters, left-aligned, space-padded.
    // "Alice" becomes "Alice     " (5 characters + 5 spaces).
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    // Column 1: Last name, 10 characters, left-aligned, space-padded.
    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    // Column 2: Age, 3 characters, right-aligned, zero-padded.
    // The integer 25 is converted to "25", then right-aligned and padded
    // with '0' to produce "025".
    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
