using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;

// A mainframe-style batch file interleaves three record layouts on different lines:
//   H = header  (batch date + description)
//   D = detail  (one per transaction)
//   T = trailer (a record count + control total for integrity checking)
//
// FixedWidthMultiRecordExtractor (#19) routes each line to the right POCO by its leading discriminator
// character. Trailer-count validation (#25) is then ordinary application logic on top: capture the
// trailer, count the details as they stream past, and compare. No extra library API is needed.

var file = string.Join
(
    "\n",
    "H20260320DAILY BATCH         ",   // H + date(8) + description(20)
    Detail(1, "Alice Johnson", 123450),
    Detail(2, "Bob Smith", 4200),
    Detail(3, "Carol White", 987625),
    Trailer(count: 3, total: 1115275) // sum of the three amounts
);

using var extractor = new FixedWidthMultiRecordExtractor(new StringReader(file))
    .When(line => line[0] == 'H', typeof(HeaderRecord))
    .When(line => line[0] == 'D', typeof(DetailRecord))
    .When(line => line[0] == 'T', typeof(TrailerRecord));

var detailCount = 0L;
var runningTotal = 0L;
var sawTrailer = false;
var trailerRecordCount = 0L;
var trailerTotal = 0L;

await foreach (var record in extractor.ExtractAsync(CancellationToken.None))
{
    switch (record)
    {
        case HeaderRecord h:
            Console.WriteLine($"Batch {h.BatchDate} — {h.Description.Trim()}");
            break;

        case DetailRecord d:
            detailCount++;
            runningTotal += d.Amount;
            Console.WriteLine($"  #{d.Id,-3} {d.Name.Trim(),-16} {Money(d.Amount),10}");
            break;

        case TrailerRecord t:
            sawTrailer = true;
            trailerRecordCount = t.RecordCount;
            trailerTotal = t.Total;
            break;
    }
}

Console.WriteLine();

// Integrity check: the trailer's declared counts must match what we actually read.
if (!sawTrailer)
{
    throw new InvalidDataException("File ended without a trailer record.");
}

ValidateCount("record count", trailerRecordCount, detailCount);
ValidateCount("control total", trailerTotal, runningTotal);

Console.WriteLine($"Trailer OK: {detailCount} detail records totalling {Money(runningTotal)} match the trailer.");

static void ValidateCount(string label, long expected, long actual)
{
    if (expected != actual)
    {
        throw new InvalidDataException($"Trailer {label} mismatch: trailer says {expected}, file has {actual}.");
    }
}

static string Detail(int id, string name, long amountCents)
    => "D" + id.ToString("00000000", CultureInfo.InvariantCulture)
           + name.PadRight(20)
           + amountCents.ToString("0000000000", CultureInfo.InvariantCulture);

static string Trailer(long count, long total)
    => "T" + count.ToString("00000000", CultureInfo.InvariantCulture)
           + total.ToString("0000000000", CultureInfo.InvariantCulture);

static string Money(long cents)
    => (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

internal sealed class HeaderRecord
{
    [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
    [FixedWidthField(1, 8)] public string BatchDate { get; set; } = string.Empty;
    [FixedWidthField(2, 20)] public string Description { get; set; } = string.Empty;
}

internal sealed class DetailRecord
{
    [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
    [FixedWidthField(1, 8)] public int Id { get; set; }
    [FixedWidthField(2, 20)] public string Name { get; set; } = string.Empty;
    [FixedWidthField(3, 10)] public long Amount { get; set; }
}

internal sealed class TrailerRecord
{
    [FixedWidthField(0, 1)] public string Type { get; set; } = string.Empty;
    [FixedWidthField(1, 8)] public int RecordCount { get; set; }
    [FixedWidthField(2, 10)] public long Total { get; set; }
}
