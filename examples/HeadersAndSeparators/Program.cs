// ==========================================================================
// Example 9 — HeadersAndSeparators
// ==========================================================================
//
// Demonstrates headers, separators, custom HeaderConverter, and LineFilter.
//
// KEY CONCEPTS:
//
//   WriteHeader (loader) / HasHeader (extractor):
//       When true, the loader writes a header row before data rows using
//       property names (or the Header attribute value if set). The extractor's
//       HasHeader=true tells it to skip 1 header line. For multi-line headers,
//       use HeaderLineCount directly (e.g. HeaderLineCount = 2).
//
//   FieldSeparator:
//       When set (and WriteHeader/HasHeader is true), the loader writes a
//       separator line after the header, filling each field width with the
//       separator character. The extractor skips this line automatically when
//       FieldSeparator is set. The separator character itself does not need to
//       match between loader and extractor — only its presence matters. The
//       extractor simply skips the line at position HeaderLineCount + 1.
//
//   Header property on [FixedWidthField]:
//       Overrides the default header label (the property name) with a custom
//       string. Useful when property names differ from desired column headers.
//
//   HeaderConverter:
//       A delegate that transforms header labels before writing. The default
//       (StrictHeader) validates that labels fit within the field width. You
//       can wrap it to apply transformations like upper-casing.
//
//   LineFilter:
//       A delegate invoked for every data line before parsing. Returns a
//       LineAction: Process (parse normally), Skip (ignore the line), or
//       Stop (end extraction immediately). Useful for footer lines, comment
//       lines, or sentinel markers. LineFilter is NOT invoked for header or
//       separator lines — those are handled structurally.
//
// This example:
//   1. Loads ReportRecord data with header and separator
//   2. Extracts it back with matching HasHeader and FieldSeparator
//   3. Shows a custom HeaderConverter that upper-cases headers
//   4. Shows LineFilter to stop extraction at a footer line
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace HeadersAndSeparatorsExample;

// --------------------------------------------------------------------------
// Record type — uses the Header property to customize column header labels.
//
// Without Header, the loader would use the property name ("Region", "Sales",
// "Units"). With Header set, the specified label is used instead. This is
// useful when the desired header text contains spaces or special characters
// that are not valid in C# property names.
// --------------------------------------------------------------------------
public class ReportRecord
{
    // Column 0: Region, 10 characters wide.
    // Header = "Region" — same as the property name, shown for explicitness.
    [FixedWidthField(0, 10, Header = "Region")]
    public string Region { get; set; } = string.Empty;



    // Column 1: Sales amount, 10 characters wide, right-aligned.
    // Header = "Sales ($)" — note the space and special characters. This
    // would not be a valid C# property name, so the Header attribute is needed.
    [FixedWidthField(1, 10, Alignment = FieldAlignment.Right, Header = "Sales ($)")]
    public decimal Sales { get; set; }



    // Column 2: Units sold, 8 characters wide, right-aligned.
    // Header = "Units" — shorter than the property name, but matches the
    // report's column header style.
    [FixedWidthField(2, 8, Alignment = FieldAlignment.Right, Header = "Units")]
    public int Units { get; set; }
}



public static class Program
{
    public static async Task Main()
    {
        // ----- Sample data -----
        var records = new List<ReportRecord>
        {
            new ReportRecord { Region = "North", Sales = 12500.50m, Units = 340 },
            new ReportRecord { Region = "South", Sales = 9800.75m, Units = 275 },
            new ReportRecord { Region = "East", Sales = 15200.00m, Units = 410 },
            new ReportRecord { Region = "West", Sales = 11000.25m, Units = 305 },
        };

        // ======================================================================
        // PART 1: Load with header and separator
        // ======================================================================
        Console.WriteLine("=== Load with WriteHeader + FieldSeparator ===");
        Console.WriteLine();

        var writer1 = new StringWriter();
        var loader1 = new FixedWidthLoader<ReportRecord, FixedWidthReport>(writer1);

        // WriteHeader: emit a header row. The header labels come from the
        // Header property on [FixedWidthField] — "Region", "Sales ($)", "Units".
        loader1.WriteHeader = true;

        // FieldSeparator: after the header, write a separator line where each
        // field width is filled with this character. The separator respects
        // field widths: 10 dashes, 10 dashes, 8 dashes = 28 total characters.
        loader1.FieldSeparator = '-';

        await loader1.LoadAsync
        (
            ToAsyncEnumerable(records),
            CancellationToken.None
        );

        var output1 = writer1.ToString();
        Console.WriteLine(output1);

        // Expected output:
        //   Region       Sales ($)   Units
        //   ----------------------------
        //   North         12500.50     340
        //   South          9800.75     275
        //   East          15200.00     410
        //   West          11000.25     305

        // ======================================================================
        // PART 2: Extract with matching header and separator settings
        // ======================================================================
        Console.WriteLine("=== Extract with HasHeader + FieldSeparator ===");
        Console.WriteLine();

        var reader2 = new StringReader(output1);
        var extractor2 = new FixedWidthExtractor<ReportRecord, FixedWidthReport>(reader2);

        // HasHeader: tells the extractor that line 1 is a header — skip it.
        // This is a convenience for HeaderLineCount = 1. If your file has
        // multiple header lines (e.g. a title row and a column names row),
        // set HeaderLineCount = 2 instead.
        extractor2.HasHeader = true;

        // FieldSeparator: tells the extractor that the line immediately after
        // the header(s) is a separator — skip it too. The character value ('-')
        // is not used for parsing; it only signals that the separator line exists.
        // The actual content of the separator line is ignored.
        extractor2.FieldSeparator = '-';

        await foreach (var record in extractor2.ExtractAsync(CancellationToken.None))
        {
            Console.WriteLine($"  Region={record.Region}, Sales={record.Sales}, Units={record.Units}");
        }

        Console.WriteLine();

        // ======================================================================
        // PART 3: Custom HeaderConverter — upper-case all headers
        // ======================================================================
        Console.WriteLine("=== Custom HeaderConverter (upper-case) ===");
        Console.WriteLine();

        var writer3 = new StringWriter();
        var loader3 = new FixedWidthLoader<ReportRecord, FixedWidthReport>(writer3);
        loader3.WriteHeader = true;
        loader3.FieldSeparator = '=';

        // HeaderConverter: a delegate that transforms the header label string
        // before it is written. The default (StrictHeader) just validates that
        // the label fits within the field width. Here we wrap it to upper-case
        // the label first, then pass it through StrictHeader for validation.
        //
        // The FieldContext parameter provides metadata about the field (width,
        // alignment, etc.) so the converter can make informed decisions.
        loader3.HeaderConverter = (label, ctx) =>
            FixedWidthConverter.StrictHeader(label.ToUpperInvariant(), ctx);

        await loader3.LoadAsync
        (
            ToAsyncEnumerable(records),
            CancellationToken.None
        );

        Console.WriteLine(writer3.ToString());

        // Expected output:
        //   REGION      SALES ($)   UNITS
        //   ============================
        //   North         12500.50     340
        //   ...

        // ======================================================================
        // PART 4: LineFilter — stop extraction at a footer line
        // ======================================================================
        Console.WriteLine("=== LineFilter — stop at footer ===");
        Console.WriteLine();

        // Build a fixed-width string that has data rows followed by a footer.
        // The footer starts with "TOTAL" — a common pattern in report files.
        var writer4 = new StringWriter();
        var loader4 = new FixedWidthLoader<ReportRecord, FixedWidthReport>(writer4);
        loader4.WriteHeader = true;
        loader4.FieldSeparator = '-';

        await loader4.LoadAsync
        (
            ToAsyncEnumerable(records),
            CancellationToken.None
        );

        // Append a footer line manually. In real-world files, this might be
        // a summary row written by a legacy mainframe system.
        await writer4.WriteLineAsync("TOTAL      48501.50     1330");

        var dataWithFooter = writer4.ToString();
        Console.WriteLine("Raw data with footer:");
        Console.WriteLine(dataWithFooter);

        // Extract with a LineFilter that stops when it sees "TOTAL".
        var reader4 = new StringReader(dataWithFooter);
        var extractor4 = new FixedWidthExtractor<ReportRecord, FixedWidthReport>(reader4);
        extractor4.HasHeader = true;
        extractor4.FieldSeparator = '-';

        // LineFilter: invoked for every DATA line (after structural header/separator
        // lines are skipped). The delegate receives the raw line string and returns
        // a LineAction:
        //   - Process: parse the line normally and yield the record
        //   - Skip: ignore this line entirely (invisible to counting)
        //   - Stop: end extraction immediately (the current line is NOT parsed)
        //
        // Here we stop when a line starts with "TOTAL" — the footer is never
        // parsed, so we avoid a parse error on the non-standard footer format.
        extractor4.LineFilter = (line) =>
            line.StartsWith("TOTAL") ? LineAction.Stop : LineAction.Process;

        Console.WriteLine("Extracted records (stopped before TOTAL footer):");

        await foreach (var record in extractor4.ExtractAsync(CancellationToken.None))
        {
            Console.WriteLine($"  Region={record.Region}, Sales={record.Sales}, Units={record.Units}");
        }

        Console.WriteLine();
        Console.WriteLine($"  Report: CurrentItemCount={extractor4.CurrentItemCount}");
        Console.WriteLine();

        // The LineFilter stopped extraction cleanly — 4 data records were
        // yielded and the "TOTAL" footer was never parsed.
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
