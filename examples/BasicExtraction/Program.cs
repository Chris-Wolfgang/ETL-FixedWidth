// ---------------------------------------------------------------------------
// BasicExtraction Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the FixedWidthExtractor as part of an ETL pipeline.
// The extractor reads fixed-width text data and yields strongly typed records,
// which then flow through a TestTransformer and into a TestLoader from the
// Wolfgang.Etl.TestKit package.
//
// Key concepts covered:
//   - Defining a record class with [FixedWidthField] attributes
//   - Column indexing (zero-based) and field lengths
//   - Right-alignment and custom pad characters for numeric fields
//   - Composing an ETL pipeline: Extractor -> Transformer -> Loader
//   - Using TestTransformer and TestLoader as stand-ins for real components
//   - Streaming architecture: records flow one at a time through the pipeline
//
// In production, you would replace the TestTransformer with a real transformer
// that applies business logic, and the TestLoader with a loader that writes to
// a database, API, or file. The composition pattern remains the same.
// ---------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.TestKit;

// ---------------------------------------------------------------------------
// Step 1: Prepare the fixed-width input data.
//
// Each line must be exactly as wide as the sum of all field lengths (23 chars).
// In production you would open a file:
//
//   await using var stream = File.OpenRead("people.dat");
//   var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(stream);
//
// Here we use a StringReader for a self-contained example.
// ---------------------------------------------------------------------------

var inputData =
    "Alice     Anderson  025\n" +
    "Bob       Baker     042\n" +
    "Charlie   Clark     033";

var reader = new StringReader(inputData);

// ---------------------------------------------------------------------------
// Step 2: Create the pipeline components.
//
// The pipeline uses three components:
//   - FixedWidthExtractor: parses fixed-width text into PersonRecord objects
//   - TestTransformer:     a pass-through transformer from Wolfgang.Etl.TestKit
//                          (in production, this would apply business logic)
//   - TestLoader:          an in-memory loader from Wolfgang.Etl.TestKit that
//                          collects records for inspection (in production, this
//                          would write to a database, file, or API)
// ---------------------------------------------------------------------------

var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(reader);
var transformer = new TestTransformer<PersonRecord>();
var loader = new TestLoader<PersonRecord>(collectItems: true);

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
// Step 4: Inspect the results.
//
// The TestLoader's GetCollectedItems() returns a snapshot of all records that
// flowed through the pipeline. Each component also tracks its own item count.
// ---------------------------------------------------------------------------

Console.WriteLine("Records that flowed through the pipeline:");
Console.WriteLine(new string('-', 40));

foreach (var person in loader.GetCollectedItems()!)
{
    Console.WriteLine
    (
        $"  {person.FirstName,-10} {person.LastName,-10} Age: {person.Age}"
    );
}

Console.WriteLine(new string('-', 40));
Console.WriteLine($"Records extracted:   {extractor.CurrentItemCount}");
Console.WriteLine($"Records transformed: {transformer.CurrentItemCount}");
Console.WriteLine($"Records loaded:      {loader.CurrentItemCount}");

// ---------------------------------------------------------------------------
// PersonRecord — the POCO class with [FixedWidthField] attributes.
//
// Each property decorated with [FixedWidthField] maps to a contiguous slice
// of characters in a fixed-width line. The two required parameters are:
//   - Index  (int): zero-based column order. Columns are laid out left to
//                    right in ascending index order.
//   - Length (int): the number of characters this field occupies.
//
// Optional named parameters control formatting during writes and parsing
// during reads:
//   - Alignment: Left (default) or Right. Affects padding direction.
//   - Pad:       The pad character (default ' '). Used during writes;
//                trimmed during reads when TrimValue is true.
//   - TrimValue: When true (default), leading and trailing whitespace is
//                trimmed from the extracted string before type conversion.
//   - Header:    Custom label for the column header (used by the loader).
//   - Format:    Format string for IFormattable types (e.g. "yyyyMMdd").
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a single person record in a fixed-width file.
/// The total line width is 10 + 10 + 3 = 23 characters.
/// </summary>
public class PersonRecord
{
    // Column 0: 10-character first name, left-aligned, space-padded.
    // Example: "Alice     " (the extractor trims trailing spaces automatically).
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    // Column 1: 10-character last name, left-aligned, space-padded.
    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    // Column 2: 3-character age, right-aligned, zero-padded.
    // The raw text "025" is trimmed of leading zeros and parsed to int 25.
    // Right alignment and '0' padding are purely cosmetic for the writer;
    // during extraction, TrimValue trims whitespace and the default parser
    // handles numeric conversion via TypeConverter.
    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
