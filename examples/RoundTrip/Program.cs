// ---------------------------------------------------------------------------
// RoundTrip Example
// ---------------------------------------------------------------------------
//
// This example demonstrates a complete ETL (Extract-Transform-Load) pipeline
// using the Wolfgang.Etl.FixedWidth library.
//
// The pipeline:
//   1. EXTRACT: Read fixed-width input data containing names and birth years
//   2. TRANSFORM: Calculate each person's age from their birth year
//   3. LOAD: Write the transformed data to a new fixed-width format
//
// Key concepts covered:
//   - The ETL pipeline pattern: Extract -> Transform -> Load
//   - How IAsyncEnumerable<T> connects the three stages
//   - Using different record types for input and output
//   - Composing stages in a single expression:
//       await loader.LoadAsync(Transform(extractor.ExtractAsync(token)), token)
//   - Integer field parsing and formatting
//   - Streaming architecture: records flow one at a time through the pipeline
//     without buffering the entire dataset in memory
//
// In production, the extractor would read from a file, the transformer might
// call a database or web service, and the loader would write to a file or
// network stream. The composition pattern remains the same.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// ---------------------------------------------------------------------------
// Step 1: Prepare the fixed-width input data.
//
// Four people with known birth years. Each line is exactly 24 characters.
// In production this would be a file:
//
//   await using var stream = File.OpenRead("births.dat");
//   var extractor = new FixedWidthExtractor<InputRecord>(stream);
// ---------------------------------------------------------------------------

var inputData =
    "Alice Anderson      1990\n" +
    "Bob Baker           1984\n" +
    "Charlie Clark       1993\n" +
    "Diana Davis         2001";

// ---------------------------------------------------------------------------
// Step 2: Create the extractor and loader.
//
// The extractor reads from a StringReader (stand-in for a file).
// The loader writes to a StringWriter (stand-in for an output file).
//
// Both report progress via FixedWidthReport — the built-in report
// that tracks CurrentItemCount, CurrentSkippedItemCount, and CurrentLineNumber.
// ---------------------------------------------------------------------------

var inputReader = new StringReader(inputData);
var outputWriter = new StringWriter();

var extractor = new FixedWidthExtractor<InputRecord>(inputReader);
var loader = new FixedWidthLoader<OutputRecord>(outputWriter);

// ---------------------------------------------------------------------------
// Step 3: Run the pipeline.
//
// The three stages are composed using IAsyncEnumerable<T>:
//
//   extractor.ExtractAsync(token)   -> IAsyncEnumerable<InputRecord>
//   TransformAsync(source, token)   -> IAsyncEnumerable<OutputRecord>
//   loader.LoadAsync(source, token) -> Task (consumes the entire stream)
//
// This is the core ETL pattern. Records flow one at a time from extractor
// through the transformer to the loader. No intermediate list or buffer is
// needed — the pipeline processes records in a streaming fashion.
//
// The composition reads inside-out:
//   loader.LoadAsync(TransformAsync(extractor.ExtractAsync(token), token), token)
// ---------------------------------------------------------------------------

var token = CancellationToken.None;

await loader.LoadAsync
(
    TransformAsync(extractor.ExtractAsync(token), token),
    token
);

// ---------------------------------------------------------------------------
// Step 4: Display the results.
// ---------------------------------------------------------------------------

Console.WriteLine("INPUT (name + birth year):");
Console.WriteLine(new string('-', 40));
Console.WriteLine(inputData);
Console.WriteLine();
Console.WriteLine("OUTPUT (name + calculated age):");
Console.WriteLine(new string('-', 40));
Console.WriteLine(outputWriter.ToString());
Console.WriteLine(new string('-', 40));
Console.WriteLine($"Records extracted: {extractor.CurrentItemCount}");
Console.WriteLine($"Records loaded:    {loader.CurrentItemCount}");

// ---------------------------------------------------------------------------
// Transform function.
//
// This async iterator method is the "T" in ETL. It consumes an
// IAsyncEnumerable<InputRecord> and yields IAsyncEnumerable<OutputRecord>.
//
// The transform is simple here (calculate age = 2026 - BirthYear), but in
// a real pipeline this could involve:
//   - Database lookups
//   - Web service calls
//   - Complex business logic
//   - Filtering or splitting records
//
// Because it uses "yield return", records flow through one at a time.
// The method does not buffer the entire input — it processes each record
// as soon as the extractor yields it, and the loader consumes the output
// as soon as the transform yields it.
//
// The #pragma suppresses CS1998 ("async method lacks await") because this
// particular transform is synchronous. An async transform (e.g., one that
// calls a database) would naturally use await and not need the pragma.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // Async method lacks 'await' operators
static async IAsyncEnumerable<OutputRecord> TransformAsync
(
    IAsyncEnumerable<InputRecord> source,
    [EnumeratorCancellation] CancellationToken token
)
{
    // The current year for age calculation. In production you would
    // use DateTime.Now.Year or inject the reference date.
    const int currentYear = 2026;

    await foreach (var input in source.WithCancellation(token))
    {
        token.ThrowIfCancellationRequested();

        // Map the input record to an output record, calculating the age.
        yield return new OutputRecord
        {
            FullName = input.FullName,
            Age = currentYear - input.BirthYear,
        };
    }
}
#pragma warning restore CS1998

// ---------------------------------------------------------------------------
// InputRecord — the source record type.
//
// Represents the schema of the input file. Each person has a full name
// (20 chars) and a 4-digit birth year.
// ---------------------------------------------------------------------------

/// <summary>
/// Source record read from the input file. Contains a name and birth year.
/// Total line width: 20 + 4 = 24 characters.
/// </summary>
public class InputRecord
{
    // Column 0: Full name, 20 characters, left-aligned, space-padded.
    [FixedWidthField(0, 20)]
    public string FullName { get; set; } = string.Empty;



    // Column 1: Birth year, 4 characters. The default parser uses
    // TypeConverter to parse "1990" into int 1990.
    [FixedWidthField(1, 4)]
    public int BirthYear { get; set; }
}

// ---------------------------------------------------------------------------
// OutputRecord — the destination record type.
//
// The transform step calculates age from birth year and maps FullName
// through unchanged. The output uses a 3-character right-aligned field
// for the age.
// ---------------------------------------------------------------------------

/// <summary>
/// Destination record written to the output file. Contains a name and
/// calculated age. Total line width: 20 + 3 = 23 characters.
/// </summary>
public class OutputRecord
{
    // Column 0: Full name, 20 characters — same width as the input.
    [FixedWidthField(0, 20)]
    public string FullName { get; set; } = string.Empty;



    // Column 1: Calculated age, 3 characters, right-aligned, space-padded.
    // A 30-year-old is written as " 30" (one leading space).
    [FixedWidthField(1, 3, Alignment = FieldAlignment.Right)]
    public int Age { get; set; }
}
