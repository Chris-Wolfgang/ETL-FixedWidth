// =============================================================================
// Example: Error Handling
// =============================================================================
//
// This example demonstrates the three MalformedLineHandling modes and the
// exception hierarchy used by the FixedWidth extractor.
//
// Exception hierarchy:
//   MalformedLineException (abstract base)
//     |-- LineTooShortException    — line is shorter than the expected width
//     |-- FieldConversionException — a field's raw value cannot be parsed
//
// All MalformedLineException subtypes carry:
//   - LineNumber  (long)  — 1-based physical line in the file
//   - LineContent (string) — the raw text of the offending line
//
// LineTooShortException adds:
//   - ExpectedWidth (int) — minimum characters required for all fields
//   - ActualWidth   (int) — actual character count of the line
//
// FieldConversionException adds:
//   - FieldName    (string) — the property that failed to parse
//   - ExpectedType (Type)   — the CLR type the value was being converted to
//   - RawValue     (string) — the raw string that could not be converted
//
// MalformedLineHandling modes:
//   - ThrowException (default) — throws immediately on the first bad line
//   - Skip           — silently skips bad lines, increments CurrentSkippedItemCount
//   - ReturnDefault  — yields a default-constructed record for bad lines
//
// BlankLineHandling controls what happens with completely empty lines:
//   - ThrowException (default) — throws LineTooShortException
//   - Skip           — silently skips blank lines (invisible to all counting)
//   - ReturnDefault  — yields a default record for blank lines
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;

namespace ErrorHandling;

// ---------------------------------------------------------------------------
// Record definition
// ---------------------------------------------------------------------------
// Fields:
//   Code  — 5 characters, left-aligned (string)
//   Price — 8 characters, right-aligned (decimal) — numeric parsing
//   Name  — 15 characters, left-aligned (string)
//
// Total expected line width: 5 + 8 + 15 = 28 characters.
// ---------------------------------------------------------------------------

public class ProductRecord
{
    [FixedWidthField(0, 5)]
    public string Code { get; set; } = string.Empty;



    [FixedWidthField(1, 8, Alignment = FieldAlignment.Right)]
    public decimal Price { get; set; }



    [FixedWidthField(2, 15)]
    public string Name { get; set; } = string.Empty;
}

public static class Program
{
    // -----------------------------------------------------------------
    // Sample data with intentional errors
    // -----------------------------------------------------------------
    // Line 1: Good      — "WDG01   19.99Widget Alpha   "
    // Line 2: Too short — "SHORT" (only 5 chars, expected 28)
    // Line 3: Good      — "WDG02    9.50Widget Beta    "
    // Line 4: Blank     — "" (empty line)
    // Line 5: Bad price — "WDG03 NOT_NUMBWidget Gamma   " (decimal parse fails)
    // Line 6: Good      — "WDG04   49.00Widget Delta   "
    // -----------------------------------------------------------------

    private static string BuildSampleData()
    {
        return
            "WDG01   19.99Widget Alpha   " + Environment.NewLine +
            "SHORT" + Environment.NewLine +
            "WDG02    9.50Widget Beta    " + Environment.NewLine +
            "" + Environment.NewLine +
            "WDG03NOT_NUMBWidget Gamma  " + Environment.NewLine +
            "WDG04   49.00Widget Delta  ";
    }



    public static async Task Main()
    {
        Console.WriteLine("=== Sample Data ===");
        var data = BuildSampleData();
        var lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            Console.WriteLine($"  Line {i + 1}: [{lines[i]}]");
        }
        Console.WriteLine();

        // Run each scenario independently with its own copy of the data.
        await ThrowOnError();
        Console.WriteLine();

        await SkipErrors();
        Console.WriteLine();

        await DefaultOnError();
    }



    // =====================================================================
    // Scenario 1: ThrowException (default)
    // =====================================================================
    // The extractor throws immediately when it encounters the first
    // malformed line. This is the safest mode — it ensures you never
    // silently lose data. Use it when data quality is critical and you
    // want to fix the source file rather than skip bad records.
    // =====================================================================

    private static async Task ThrowOnError()
    {
        Console.WriteLine("=== Scenario 1: MalformedLineHandling.ThrowException ===");

        var reader = new StringReader(BuildSampleData());
        var extractor = new FixedWidthExtractor<ProductRecord>(reader);

        // ThrowException is the default, but we set it explicitly for clarity.
        extractor.MalformedLineHandling = MalformedLineHandling.ThrowException;

        // BlankLineHandling defaults to ThrowException as well. We set Skip
        // here so we can demonstrate the MalformedLineHandling path without
        // the blank line stopping us first.
        extractor.BlankLineHandling = BlankLineHandling.Skip;

        try
        {
            await foreach (var record in extractor.ExtractAsync())
            {
                Console.WriteLine($"  Extracted: Code={record.Code}, Price={record.Price}, Name={record.Name}");
            }
        }
        catch (LineTooShortException ex)
        {
            // LineTooShortException is thrown when a line has fewer
            // characters than the sum of all field widths.
            Console.WriteLine($"  CAUGHT LineTooShortException:");
            Console.WriteLine($"    Message:       {ex.Message}");
            Console.WriteLine($"    LineNumber:     {ex.LineNumber}");
            Console.WriteLine($"    LineContent:    [{ex.LineContent}]");
            Console.WriteLine($"    ExpectedWidth:  {ex.ExpectedWidth}");
            Console.WriteLine($"    ActualWidth:    {ex.ActualWidth}");
        }
        catch (FieldConversionException ex)
        {
            // FieldConversionException is thrown when the line is long
            // enough but a field's value cannot be parsed to the target type.
            Console.WriteLine($"  CAUGHT FieldConversionException:");
            Console.WriteLine($"    Message:       {ex.Message}");
            Console.WriteLine($"    LineNumber:     {ex.LineNumber}");
            Console.WriteLine($"    LineContent:    [{ex.LineContent}]");
            Console.WriteLine($"    FieldName:      {ex.FieldName}");
            Console.WriteLine($"    ExpectedType:   {ex.ExpectedType.Name}");
            Console.WriteLine($"    RawValue:       [{ex.RawValue}]");
        }
        catch (MalformedLineException ex)
        {
            // You can also catch the base type to handle both uniformly.
            Console.WriteLine($"  CAUGHT MalformedLineException:");
            Console.WriteLine($"    Message:    {ex.Message}");
            Console.WriteLine($"    LineNumber: {ex.LineNumber}");
        }
    }



    // =====================================================================
    // Scenario 2: Skip
    // =====================================================================
    // Bad lines are silently skipped. The extractor increments
    // CurrentSkippedItemCount for each skipped line and continues to the
    // next. Use this mode when you want to process as many good records
    // as possible and handle errors after the fact (e.g., by checking
    // the skipped count).
    // =====================================================================

    private static async Task SkipErrors()
    {
        Console.WriteLine("=== Scenario 2: MalformedLineHandling.Skip ===");

        var reader = new StringReader(BuildSampleData());
        var extractor = new FixedWidthExtractor<ProductRecord>(reader);

        // Skip malformed lines — the extractor will continue past bad data.
        extractor.MalformedLineHandling = MalformedLineHandling.Skip;

        // Also skip blank lines so they do not cause errors.
        // When BlankLineHandling is Skip, blank lines are invisible to all
        // counting logic — they do not affect SkipItemCount, MaximumItemCount,
        // or CurrentSkippedItemCount.
        extractor.BlankLineHandling = BlankLineHandling.Skip;

        var records = new List<ProductRecord>();

        await foreach (var record in extractor.ExtractAsync())
        {
            records.Add(record);
            Console.WriteLine($"  Extracted: Code={record.Code}, Price={record.Price}, Name={record.Name}");
        }

        Console.WriteLine();
        Console.WriteLine($"  Total extracted:  {extractor.CurrentItemCount}");
        Console.WriteLine($"  Total skipped:    {extractor.CurrentSkippedItemCount}");
        Console.WriteLine($"  Total lines read: {extractor.CurrentLineNumber}");

        // The skipped count tells you how many lines had problems.
        // In this example, 2 lines are skipped (the too-short line and
        // the line with an unparseable decimal). The blank line is
        // invisible when BlankLineHandling is Skip, so it does not
        // appear in any count.
    }



    // =====================================================================
    // Scenario 3: ReturnDefault
    // =====================================================================
    // Bad lines yield a default-constructed record instead of throwing or
    // skipping. This means every line in the file produces a record —
    // you get a 1:1 mapping from lines to records. The default record
    // has all properties at their CLR default values (0 for decimal,
    // "" for string initialized via = string.Empty, etc.).
    //
    // Use this mode when you need positional correspondence between
    // input lines and output records, and plan to filter or flag the
    // defaults downstream.
    // =====================================================================

    private static async Task DefaultOnError()
    {
        Console.WriteLine("=== Scenario 3: MalformedLineHandling.ReturnDefault ===");

        var reader = new StringReader(BuildSampleData());
        var extractor = new FixedWidthExtractor<ProductRecord>(reader);

        // Return a default record for malformed lines.
        extractor.MalformedLineHandling = MalformedLineHandling.ReturnDefault;

        // Also return defaults for blank lines, so every source line
        // maps to exactly one output record.
        extractor.BlankLineHandling = BlankLineHandling.ReturnDefault;

        var lineIndex = 0;

        await foreach (var record in extractor.ExtractAsync())
        {
            lineIndex++;

            // A default record will have Code="" and Price=0.
            // You can detect these downstream to flag or filter them.
            var isDefault = record.Code == string.Empty && record.Price == 0m;
            var marker = isDefault ? " <-- DEFAULT" : "";

            Console.WriteLine
            (
                $"  Line {lineIndex}: Code={record.Code}, " +
                $"Price={record.Price}, Name={record.Name}{marker}"
            );
        }

        Console.WriteLine();
        Console.WriteLine($"  Total extracted:  {extractor.CurrentItemCount}");
        Console.WriteLine($"  Total lines read: {extractor.CurrentLineNumber}");
    }
}
