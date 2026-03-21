// =============================================================================
// Example: Progress Reporting
// =============================================================================
//
// This example demonstrates how to receive periodic progress updates during
// a long-running extraction using the timer-based progress model.
//
// Key concepts:
//   - ReportingInterval (TimeSpan): controls how often the internal timer
//     fires. The timer starts automatically when ExtractAsync is called with
//     a non-null IProgress<TProgress> parameter. Set this BEFORE calling
//     ExtractAsync.
//
//   - Progress<FixedWidthReport>: the standard .NET progress reporting
//     mechanism. The callback receives a FixedWidthReport snapshot each time
//     the timer fires. The FixedWidthReport contains:
//       * CurrentItemCount   — number of records extracted so far
//       * CurrentSkippedItemCount — number of records skipped
//       * CurrentLineNumber  — the 1-based physical line being processed
//
//   - The timer fires on a thread pool thread. The Progress<T> callback is
//     marshalled to the synchronization context (if one exists). In a
//     console app there is no SynchronizationContext, so the callback runs
//     on the thread pool as well.
//
//   - IMPORTANT: If extraction completes faster than the ReportingInterval,
//     the progress callback may never fire. This is expected — the timer is
//     a "best effort" periodic snapshot, not a per-item notification.
//
//   - After extraction completes, you can inspect the extractor's final
//     state via CurrentItemCount, CurrentSkippedItemCount, and
//     CurrentLineNumber directly on the extractor instance.
// =============================================================================

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

namespace ProgressReporting;

// ---------------------------------------------------------------------------
// Record definition
// ---------------------------------------------------------------------------
// A simple record with two fields:
//   - Id: 5 characters, right-aligned, zero-padded (e.g. "00042")
//   - Value: 20 characters, left-aligned, space-padded (default)
// ---------------------------------------------------------------------------

public class DataRecord
{
    [FixedWidthField(0, 5, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Id { get; set; }



    [FixedWidthField(1, 20)]
    public string Value { get; set; } = string.Empty;
}

public static class Program
{
    public static async Task Main()
    {
        // -----------------------------------------------------------------
        // Generate sample data with 100 rows
        // -----------------------------------------------------------------
        // Each line is 25 characters: Id (5) + Value (20).
        // We build the data in a StringBuilder for efficiency.
        // -----------------------------------------------------------------

        const int rowCount = 100;
        var sb = new StringBuilder();

        for (var i = 1; i <= rowCount; i++)
        {
            // Right-align the Id and pad with zeros to 5 characters.
            var id = i.ToString().PadLeft(5, '0');

            // Left-align the Value and pad with spaces to 20 characters.
            var value = $"Item-{i}".PadRight(20);

            sb.AppendLine($"{id}{value}");
        }

        var data = sb.ToString();

        Console.WriteLine($"=== Generated {rowCount} rows of data ===");
        Console.WriteLine($"First line: [{data.Substring(0, 25)}]");
        Console.WriteLine();


        // -----------------------------------------------------------------
        // Set up the extractor with progress reporting
        // -----------------------------------------------------------------
        // 1. Create the extractor from a StringReader.
        // 2. Set ReportingInterval to 50ms — this means the internal timer
        //    will attempt to fire every 50 milliseconds and invoke the
        //    progress callback with a snapshot of the current state.
        // 3. Create a Progress<FixedWidthReport> with a callback that
        //    prints the current progress to the console.
        // -----------------------------------------------------------------

        var reader = new StringReader(data);
        var extractor = new FixedWidthExtractor<DataRecord, FixedWidthReport>(reader);

        // Set the reporting interval BEFORE calling ExtractAsync.
        // The timer is created internally when ExtractAsync starts.
        extractor.ReportingInterval = 50;

        // The Progress<T> callback fires each time the timer elapses.
        // In a real application, you might update a progress bar or log
        // a status message. Here we just print to the console.
        var progressCallbackCount = 0;
        var progress = new Progress<FixedWidthReport>(report =>
        {
            progressCallbackCount++;
            Console.WriteLine
            (
                $"  [Progress #{progressCallbackCount}] " +
                $"Items: {report.CurrentItemCount}, " +
                $"Line: {report.CurrentLineNumber}"
            );
        });


        // -----------------------------------------------------------------
        // Run the extraction
        // -----------------------------------------------------------------
        // ExtractAsync accepts an optional IProgress<TProgress> parameter.
        // When provided, the extractor starts an internal timer that fires
        // at the configured ReportingInterval and reports snapshots via
        // the IProgress callback.
        //
        // Note: We add a small delay inside the loop to give the timer a
        // chance to fire. In a real-world scenario with file I/O, the
        // natural latency of disk reads provides this opportunity.
        // -----------------------------------------------------------------

        Console.WriteLine("=== Extraction with Progress Reporting ===");

        var itemCount = 0;

        await foreach (var record in extractor.ExtractAsync(progress))
        {
            itemCount++;

            // Add a small delay every 10 records so the timer has a chance
            // to fire. Without this, extraction of 100 in-memory rows
            // completes in microseconds — faster than the 50ms interval.
            if (itemCount % 10 == 0)
            {
                await Task.Delay(25);
            }
        }

        // Give any remaining timer callbacks a moment to execute.
        // The timer may have one last event queued on the thread pool.
        await Task.Delay(100);

        Console.WriteLine();


        // -----------------------------------------------------------------
        // Print the final report
        // -----------------------------------------------------------------
        // After extraction completes, the extractor's properties reflect
        // the final state. These are always accurate — unlike the timer
        // callbacks, which are periodic snapshots that may miss the final
        // few items.
        // -----------------------------------------------------------------

        Console.WriteLine("=== Final Report ===");
        Console.WriteLine($"  Total items extracted:  {extractor.CurrentItemCount}");
        Console.WriteLine($"  Total items skipped:    {extractor.CurrentSkippedItemCount}");
        Console.WriteLine($"  Total lines read:       {extractor.CurrentLineNumber}");
        Console.WriteLine($"  Progress callbacks:     {progressCallbackCount}");
        Console.WriteLine();

        // -----------------------------------------------------------------
        // Note on timing
        // -----------------------------------------------------------------
        // If you see "Progress callbacks: 0", that means extraction
        // completed before the first 50ms timer tick. This is normal for
        // small datasets processed entirely in memory. In production,
        // reading from disk or a network stream introduces enough latency
        // for the timer to fire multiple times.
        // -----------------------------------------------------------------

        if (progressCallbackCount == 0)
        {
            Console.WriteLine
            (
                "  (No progress callbacks fired — extraction completed " +
                "faster than the 50ms reporting interval.)"
            );
        }
    }
}
