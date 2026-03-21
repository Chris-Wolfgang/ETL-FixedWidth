// =============================================================================
// Example: Custom Parsers and Converters
// =============================================================================
//
// This example demonstrates how to customize the way field values are parsed
// (extractor) and converted (loader) using the ValueParser and ValueConverter
// delegate properties.
//
// Scenario: Our fixed-width file stores booleans as "Y"/"N" instead of the
// default "True"/"False". We also have a DateTime field that uses the
// "MM/dd/yyyy" format, which requires a Format on the attribute.
//
// Key concepts:
//   - ValueParser (extractor): FixedWidthValueParser delegate
//       Signature: object FixedWidthValueParser(ReadOnlyMemory<char> text, FieldContext context)
//       The "text" parameter is a zero-copy slice of the source line. Use
//       text.Span for zero-allocation comparisons, or text.ToString() when
//       you need a string.
//
//   - ValueConverter (loader): Func<object, FieldContext, string>
//       Receives the boxed property value and a FieldContext, returns a string
//       that must fit within the field width.
//
//   - FieldContext provides: PropertyName, PropertyType, FieldLength, Pad,
//     Alignment, Format, and HeaderLabel.
//
//   - DateTime/DateTimeOffset/TimeSpan fields always require an explicit
//     Format on the [FixedWidthField] attribute — the library throws
//     InvalidOperationException if one is missing.
//
//   - The default parser (FixedWidthConverter.DefaultParser) and default
//     converter (FixedWidthConverter.Strict) handle all standard types.
//     Custom delegates should fall back to these defaults for types they
//     do not need to customize.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace CustomParsersConverters;

// ---------------------------------------------------------------------------
// Record definition
// ---------------------------------------------------------------------------
// Each property is decorated with [FixedWidthField(index, length)].
// The index determines the column order; the length is the character width.
//
// IsActive uses a single character ("Y" or "N") — the default parser would
// try bool.Parse("Y") and fail, so we need a custom ValueParser.
//
// HireDate uses the "MM/dd/yyyy" format. The Format property on the attribute
// tells both the default parser and the default converter how to handle it.
// Without Format, the library throws InvalidOperationException at runtime.
// ---------------------------------------------------------------------------

public class EmployeeRecord
{
    [FixedWidthField(0, 15)]
    public string Name { get; set; } = string.Empty;



    [FixedWidthField(1, 1)]
    public bool IsActive { get; set; }



    [FixedWidthField(2, 10, Format = "MM/dd/yyyy")]
    public DateTime HireDate { get; set; }



    [FixedWidthField(3, 10)]
    public string Department { get; set; } = string.Empty;
}

public static class Program
{
    public static async Task Main()
    {
        // -----------------------------------------------------------------
        // Build sample data
        // -----------------------------------------------------------------
        // Each line is exactly 36 characters wide:
        //   Name       (15) + IsActive (1) + HireDate (10) + Department (10)
        //
        // Booleans are represented as "Y" or "N" — a common convention in
        // mainframe and legacy fixed-width files.
        // -----------------------------------------------------------------

        var data =
            "Alice Johnson  Y03/15/2020Engr" + Environment.NewLine +
            "Bob Smith      N11/01/2018Sales     " + Environment.NewLine +
            "Carol Davis    Y07/22/2022Marketing ";

        Console.WriteLine("=== Source Data ===");
        Console.WriteLine(data);
        Console.WriteLine();


        // -----------------------------------------------------------------
        // EXTRACT with a custom ValueParser
        // -----------------------------------------------------------------
        // The ValueParser delegate is called for every field on every line.
        // We intercept bool fields and compare the raw text to "Y". For all
        // other types, we delegate to FixedWidthConverter.DefaultParser,
        // which handles strings, ints, DateTimes (with Format), etc.
        //
        // Note: text.Span.SequenceEqual("Y".AsSpan()) is a zero-allocation
        // comparison — it avoids allocating a string from the ReadOnlyMemory.
        // -----------------------------------------------------------------

        var reader = new StringReader(data);
        var extractor = new FixedWidthExtractor<EmployeeRecord, FixedWidthReport>(reader);

        extractor.ValueParser = (text, ctx) =>
            ctx.PropertyType == typeof(bool)
                ? (object)(text.Span.SequenceEqual("Y".AsSpan()))
                : FixedWidthConverter.DefaultParser(text, ctx);

        Console.WriteLine("=== Extracted Records ===");

        var records = new List<EmployeeRecord>();

        await foreach (var record in extractor.ExtractAsync())
        {
            records.Add(record);
            Console.WriteLine
            (
                $"  Name={record.Name}, IsActive={record.IsActive}, " +
                $"HireDate={record.HireDate:MM/dd/yyyy}, Department={record.Department}"
            );
        }

        Console.WriteLine();


        // -----------------------------------------------------------------
        // LOAD with a custom ValueConverter
        // -----------------------------------------------------------------
        // The ValueConverter delegate is called for every field on every
        // record being written. We intercept bool fields and write "Y"/"N"
        // instead of the default "True"/"False". For everything else, we
        // delegate to FixedWidthConverter.Strict, which formats values using
        // InvariantCulture and throws FieldOverflowException if the result
        // exceeds the field width.
        //
        // The converter must return a string that fits within the field
        // width (FieldContext.FieldLength). The framework handles padding
        // after the converter returns.
        // -----------------------------------------------------------------

        var output = new StringWriter();
        var loader = new FixedWidthLoader<EmployeeRecord, FixedWidthReport>(output);

        loader.ValueConverter = (value, ctx) =>
            ctx.PropertyType == typeof(bool)
                ? ((bool)value ? "Y" : "N")
                : FixedWidthConverter.Strict(value, ctx);

        // Feed the extracted records back into the loader.
        // ToAsyncEnumerable() converts the List<T> to IAsyncEnumerable<T>.
        await loader.LoadAsync(ToAsyncEnumerable(records));

        Console.WriteLine("=== Loaded Output ===");
        Console.WriteLine(output.ToString());
        Console.WriteLine();


        // -----------------------------------------------------------------
        // Verify round-trip
        // -----------------------------------------------------------------
        // The output should match the original data exactly, confirming
        // that the custom parser and converter are symmetric.
        // -----------------------------------------------------------------

        Console.WriteLine("=== Round-Trip Verification ===");

        var originalLines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var outputLines = output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < outputLines.Length; i++)
        {
            var match = originalLines[i] == outputLines[i] ? "MATCH" : "DIFF";
            Console.WriteLine($"  Line {i + 1}: [{outputLines[i]}] -> {match}");
        }
    }



    /// <summary>
    /// Helper to convert a synchronous list into an async enumerable,
    /// since LoadAsync requires IAsyncEnumerable&lt;T&gt;.
    /// </summary>
    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
