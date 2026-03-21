// ---------------------------------------------------------------------------
// BasicExtraction Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the simplest use case for the FixedWidthExtractor:
// reading fixed-width text data and yielding strongly typed records.
//
// Key concepts covered:
//   - Defining a record class with [FixedWidthField] attributes
//   - Column indexing (zero-based) and field lengths
//   - Right-alignment and custom pad characters for numeric fields
//   - TrimValue behavior (enabled by default — leading/trailing whitespace
//     and pad characters are trimmed from extracted values)
//   - Using StringReader as a stand-in for FileStream
//   - Iterating records with "await foreach"
//   - Inspecting the extraction report afterward
//
// In production, you would typically wrap a FileStream or StreamReader
// instead of a StringReader. The extractor accepts any TextReader.
// ---------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

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

// StringReader implements TextReader, so the extractor can consume it directly.
// When using a TextReader constructor, the caller owns the reader's lifetime —
// the extractor does not dispose it.
var reader = new StringReader(inputData);

// ---------------------------------------------------------------------------
// Step 2: Create the extractor.
//
// The two generic type parameters are:
//   - TRecord   : the POCO type whose properties map to fixed-width columns.
//   - TProgress : the progress report type. Use FixedWidthReport for the
//                 built-in report that includes CurrentItemCount,
//                 CurrentSkippedItemCount, and CurrentLineNumber.
// ---------------------------------------------------------------------------

var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(reader);

// ---------------------------------------------------------------------------
// Step 3: Extract records using "await foreach".
//
// ExtractAsync() returns an IAsyncEnumerable<PersonRecord>. Each iteration
// reads one line, parses it according to the [FixedWidthField] attributes,
// and yields a populated PersonRecord instance.
//
// A CancellationToken can be passed to ExtractAsync() to support cooperative
// cancellation in long-running extractions.
// ---------------------------------------------------------------------------

Console.WriteLine("Extracted records:");
Console.WriteLine(new string('-', 40));

await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
{
    Console.WriteLine
    (
        $"  {person.FirstName,-10} {person.LastName,-10} Age: {person.Age}"
    );
}

// ---------------------------------------------------------------------------
// Step 4: Inspect the final report.
//
// After extraction completes, the extractor's CurrentItemCount property
// reflects how many records were yielded. You can also call
// GetProgressReport() (internal, used by tests) or simply read the
// public properties on the extractor.
// ---------------------------------------------------------------------------

Console.WriteLine(new string('-', 40));
Console.WriteLine($"Total records extracted: {extractor.CurrentItemCount}");

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
