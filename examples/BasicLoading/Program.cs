// ---------------------------------------------------------------------------
// BasicLoading Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the simplest use case for the FixedWidthLoader:
// writing strongly typed records to a fixed-width text output.
//
// Key concepts covered:
//   - Defining a record class with [FixedWidthField] attributes
//   - Creating a FixedWidthLoader with a TextWriter (StringWriter here)
//   - Converting an IEnumerable<T> to IAsyncEnumerable<T> for the loader
//   - Padding, alignment, and pad character behavior during writes
//   - Ownership semantics: the loader does NOT dispose a caller-owned writer
//   - Strict vs Truncate converter (the default is Strict — values that
//     exceed the field length throw a FieldOverflowException)
//   - Using StringWriter as a stand-in for FileStream
//
// In production, you would typically write to a FileStream:
//
//   await using var stream = File.OpenWrite("people.dat");
//   using var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(stream);
//
// The Stream constructor creates an internal StreamWriter with a 64 KB buffer
// for improved throughput. The caller retains ownership of the Stream.
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
// Step 1: Create the source data.
//
// The loader consumes an IAsyncEnumerable<TRecord>. Since we have an
// in-memory list, we need a small helper to convert it. In a real pipeline,
// the IAsyncEnumerable would typically come from an extractor or transformer.
// ---------------------------------------------------------------------------

var people = new List<PersonRecord>
{
    new PersonRecord { FirstName = "Alice",   LastName = "Anderson", Age = 25 },
    new PersonRecord { FirstName = "Bob",     LastName = "Baker",    Age = 42 },
    new PersonRecord { FirstName = "Charlie", LastName = "Clark",    Age = 33 },
};

// ---------------------------------------------------------------------------
// Step 2: Create the loader with a StringWriter.
//
// StringWriter implements TextWriter, so the loader can write to it directly.
// When using the TextWriter constructor, ownership semantics are:
//   - The caller owns the writer's lifetime (the loader does NOT dispose it).
//   - The caller is responsible for flushing the writer if needed.
//   - Calling loader.Dispose() is optional and has no effect.
//
// When using the Stream constructor instead, the loader creates an internal
// StreamWriter and Dispose() must be called to release it.
// ---------------------------------------------------------------------------

var writer = new StringWriter();

var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);

// ---------------------------------------------------------------------------
// Step 3: Load the records.
//
// LoadAsync consumes the entire async enumerable and writes each record as
// a fixed-width line to the underlying TextWriter. The method completes
// when the enumerable is exhausted or MaximumItemCount is reached.
//
// A CancellationToken can be passed to support cooperative cancellation.
// ---------------------------------------------------------------------------

await loader.LoadAsync
(
    ToAsyncEnumerable(people),
    CancellationToken.None
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
Console.WriteLine($"Total records loaded: {loader.CurrentItemCount}");

// ---------------------------------------------------------------------------
// Helper: Convert IEnumerable<T> to IAsyncEnumerable<T>.
//
// The #pragma suppresses CS1998 ("async method lacks await") because this
// method is intentionally synchronous — it wraps a synchronous enumerable
// in an async iterator so the loader can consume it.
//
// In a real ETL pipeline, the IAsyncEnumerable would typically come from
// an extractor's ExtractAsync() method, making this helper unnecessary.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // Async method lacks 'await' operators
static async IAsyncEnumerable<T> ToAsyncEnumerable<T>
(
    IEnumerable<T> source,
    [EnumeratorCancellation] CancellationToken token = default
)
{
    foreach (var item in source)
    {
        token.ThrowIfCancellationRequested();
        yield return item;
    }
}
#pragma warning restore CS1998

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
