// ==========================================================================
// Example 7 — FieldDelimiter
// ==========================================================================
//
// Demonstrates the FieldDelimiter property on both the loader and extractor.
//
// FieldDelimiter inserts a string (e.g. " | ") between every adjacent pair
// of fields. This transforms the output from a pure fixed-width format into
// a human-readable table layout while preserving exact field widths.
//
// KEY RULE: The FieldDelimiter value on the extractor MUST match the value
// used on the loader. The delimiter width is accounted for when calculating
// field start positions, so a mismatch will cause fields to be read from
// the wrong offset.
//
// This example:
//   1. Loads ContactRecord data WITH a delimiter (" | ") — table-style output
//   2. Extracts the same data back to verify the round-trip
//   3. Loads the same data WITHOUT a delimiter for comparison
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;

namespace FieldDelimiterExample;

// --------------------------------------------------------------------------
// Record type — each property maps to a fixed-width column.
//
// Index determines column order. Length sets the character width.
// The delimiter is inserted BETWEEN columns — it does not affect the
// individual field widths defined here.
// --------------------------------------------------------------------------
public class ContactRecord
{
    // Column 0: Name, 15 characters wide, left-aligned (default)
    [FixedWidthField(0, 15)]
    public string Name { get; set; } = string.Empty;



    // Column 1: Email, 25 characters wide, left-aligned (default)
    [FixedWidthField(1, 25)]
    public string Email { get; set; } = string.Empty;



    // Column 2: Phone, 12 characters wide, left-aligned (default)
    [FixedWidthField(2, 12)]
    public string Phone { get; set; } = string.Empty;
}



public static class Program
{
    public static async Task Main()
    {
        // ----- Sample data -----
        var contacts = new List<ContactRecord>
        {
            new ContactRecord { Name = "Alice Johnson", Email = "alice@example.com", Phone = "555-100-2000" },
            new ContactRecord { Name = "Bob Smith", Email = "bob.smith@corp.net", Phone = "555-200-3000" },
            new ContactRecord { Name = "Carol Lee", Email = "carol.lee@mail.org", Phone = "555-300-4000" },
        };

        // ======================================================================
        // PART 1: Load WITH delimiter — produces a human-readable table
        // ======================================================================
        Console.WriteLine("=== WITH FieldDelimiter (\" | \") ===");
        Console.WriteLine();

        var withDelimiter = new StringWriter();
        var loaderWithDelim = new FixedWidthLoader<ContactRecord>(withDelimiter);

        // FieldDelimiter: the string placed between every pair of adjacent fields.
        // " | " adds 3 characters of visual separation between each column.
        loaderWithDelim.FieldDelimiter = " | ";

        // WriteHeader: emit a header row using property names (or the Header
        // attribute if set) before the data rows.
        loaderWithDelim.WriteHeader = true;

        // FieldSeparator: when WriteHeader is true, a separator line of this
        // character is written after the header. The separator respects the
        // delimiter — it fills field widths with the separator char and places
        // the delimiter string between them.
        loaderWithDelim.FieldSeparator = '-';

        // Load the records into the StringWriter.
        await loaderWithDelim.LoadAsync
        (
            ToAsyncEnumerable(contacts),
            CancellationToken.None
        );

        var outputWithDelimiter = withDelimiter.ToString();
        Console.WriteLine(outputWithDelimiter);

        // Expected output (approximate):
        //   Name            | Email                     | Phone
        //   --------------- | ------------------------- | ------------
        //   Alice Johnson   | alice@example.com         | 555-100-2000
        //   Bob Smith       | bob.smith@corp.net        | 555-200-3000
        //   Carol Lee       | carol.lee@mail.org        | 555-300-4000

        // ======================================================================
        // PART 2: Extract the delimited data back into ContactRecord objects
        // ======================================================================
        Console.WriteLine("=== Round-trip: extracting records back ===");
        Console.WriteLine();

        // The extractor's FieldDelimiter, HasHeader, and FieldSeparator must
        // match the loader's settings exactly. If FieldDelimiter is wrong, the
        // extractor will calculate incorrect field start positions and produce
        // garbled data.
        var readerWithDelim = new StringReader(outputWithDelimiter);
        var extractorWithDelim = new FixedWidthExtractor<ContactRecord>(readerWithDelim);
        extractorWithDelim.FieldDelimiter = " | ";
        extractorWithDelim.HasHeader = true;
        extractorWithDelim.FieldSeparator = '-';

        await foreach (var contact in extractorWithDelim.ExtractAsync(CancellationToken.None))
        {
            Console.WriteLine($"  Name={contact.Name}, Email={contact.Email}, Phone={contact.Phone}");
        }

        Console.WriteLine();

        // ======================================================================
        // PART 3: Load WITHOUT delimiter — pure fixed-width output for comparison
        // ======================================================================
        Console.WriteLine("=== WITHOUT FieldDelimiter (pure fixed-width) ===");
        Console.WriteLine();

        var withoutDelimiter = new StringWriter();
        var loaderNoDelim = new FixedWidthLoader<ContactRecord>(withoutDelimiter);

        // No delimiter — fields are concatenated directly with no visual separator.
        // Each field occupies exactly its declared width (15 + 25 + 12 = 52 chars).
        loaderNoDelim.WriteHeader = true;
        loaderNoDelim.FieldSeparator = '-';

        await loaderNoDelim.LoadAsync
        (
            ToAsyncEnumerable(contacts),
            CancellationToken.None
        );

        var outputWithoutDelimiter = withoutDelimiter.ToString();
        Console.WriteLine(outputWithoutDelimiter);

        // Expected output (approximate):
        //   Name           Email                    Phone
        //   ----------------------------------------------------
        //   Alice Johnson  alice@example.com        555-100-2000
        //   Bob Smith      bob.smith@corp.net       555-200-3000
        //   Carol Lee      carol.lee@mail.org       555-300-4000

        // Notice how the delimited version is much easier to read — the " | "
        // visually separates fields. The pure fixed-width version packs fields
        // edge-to-edge, which is compact but harder for humans to scan.
    }



    /// <summary>
    /// Converts a list to an IAsyncEnumerable for use with the loader.
    /// </summary>
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>
    (
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        foreach (var item in items)
        {
            token.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }
}
