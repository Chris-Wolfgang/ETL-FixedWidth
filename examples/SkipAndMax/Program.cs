// ==========================================================================
// Example 8 — SkipAndMax (Pagination with SkipItemCount and MaximumItemCount)
// ==========================================================================
//
// Demonstrates how to use SkipItemCount and MaximumItemCount on the extractor
// to implement simple pagination over a fixed-width data source.
//
// KEY CONCEPTS:
//
//   SkipItemCount — the number of DATA items to discard before yielding.
//       Skipped items are counted in the report as CurrentSkippedItemCount.
//       This is independent of HeaderLineCount — header/separator lines are
//       structural and do not count toward the skip budget.
//
//   MaximumItemCount — the maximum number of items to yield. Once this many
//       records have been yielded, extraction stops immediately. Defaults to
//       int.MaxValue (effectively unlimited).
//
// IMPORTANT: Each "page" requires a fresh TextReader (or a reset Stream).
//   The extractor reads forward-only through the TextReader. Once it reaches
//   MaximumItemCount and stops, the reader's internal position is somewhere
//   in the middle of the data — there is no way to rewind a TextReader. For
//   in-memory data (StringReader), simply create a new StringReader from the
//   same string. For file-based streams, seek the Stream back to position 0
//   and create a new StreamReader (or a new FixedWidthExtractor with the
//   reset stream).
//
// This example:
//   1. Generates 20 rows of inline data
//   2. Reads 3 "pages" of 5 records each using SkipItemCount and MaximumItemCount
//   3. Prints each page's records and the report counts
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

namespace SkipAndMaxExample;

// --------------------------------------------------------------------------
// Record type — Id is right-aligned and zero-padded, Description is left-aligned.
// --------------------------------------------------------------------------
public class ItemRecord
{
    // Column 0: Id, 5 characters wide, right-aligned, padded with '0'.
    // Right alignment + '0' pad produces output like "00001", "00002", etc.
    [FixedWidthField(0, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Id { get; set; }



    // Column 1: Description, 20 characters wide, left-aligned (default).
    [FixedWidthField(1, 20)]
    public string Description { get; set; } = string.Empty;
}



public static class Program
{
    public static async Task Main()
    {
        // ======================================================================
        // Step 1: Generate 20 rows of fixed-width data into a string
        // ======================================================================

        // Build the source data as a list of ItemRecord objects.
        var items = new List<ItemRecord>();
        for (var i = 1; i <= 20; i++)
        {
            items.Add
            (
                new ItemRecord
                {
                    Id = i,
                    Description = $"Item number {i}"
                }
            );
        }

        // Write all 20 records to a string using the loader.
        // No header — just raw data lines (one per record).
        var dataWriter = new StringWriter();
        var loader = new FixedWidthLoader<ItemRecord, FixedWidthReport>(dataWriter);

        await loader.LoadAsync
        (
            ToAsyncEnumerable(items),
            CancellationToken.None
        );

        var allData = dataWriter.ToString();

        Console.WriteLine("=== All 20 rows of raw fixed-width data ===");
        Console.WriteLine();
        Console.WriteLine(allData);

        // ======================================================================
        // Step 2: Read 3 pages of 5 records each
        // ======================================================================

        var pageSize = 5;

        for (var page = 1; page <= 3; page++)
        {
            var skip = (page - 1) * pageSize;

            Console.WriteLine($"=== Page {page} (SkipItemCount={skip}, MaximumItemCount={pageSize}) ===");
            Console.WriteLine();

            // IMPORTANT: Create a fresh StringReader for each page.
            // The extractor reads forward-only. After extracting page 1 (5 items),
            // the reader's position is past line 5. We cannot rewind a StringReader,
            // so we must create a new one from the original string each time.
            var pageReader = new StringReader(allData);
            var extractor = new FixedWidthExtractor<ItemRecord, FixedWidthReport>(pageReader);

            // SkipItemCount: skip the first N data items. For page 1, skip 0.
            // For page 2, skip 5 (the first page's worth). For page 3, skip 10.
            // Skipped items are not yielded but ARE counted in the report's
            // CurrentSkippedItemCount.
            extractor.SkipItemCount = skip;

            // MaximumItemCount: stop after yielding this many items. Once 5
            // records have been yielded, the extractor stops reading even if
            // more data remains in the reader.
            extractor.MaximumItemCount = pageSize;

            // Extract and print the page's records.
            await foreach (var item in extractor.ExtractAsync(CancellationToken.None))
            {
                Console.WriteLine($"  Id={item.Id:D5}, Description={item.Description}");
            }

            Console.WriteLine();

            // Print the report counts to show how skip/max interact.
            // CurrentItemCount = number of items yielded (should be pageSize).
            // CurrentSkippedItemCount = number of items discarded by SkipItemCount.
            Console.WriteLine($"  Report: CurrentItemCount={extractor.CurrentItemCount}, " +
                              $"CurrentSkippedItemCount={extractor.CurrentSkippedItemCount}");
            Console.WriteLine();
        }

        // Expected output for each page:
        //
        //   Page 1: Items 1-5   (skip=0,  yielded=5, skipped=0)
        //   Page 2: Items 6-10  (skip=5,  yielded=5, skipped=5)
        //   Page 3: Items 11-15 (skip=10, yielded=5, skipped=10)
        //
        // Note: Items 16-20 are never read — they would appear on page 4.
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
