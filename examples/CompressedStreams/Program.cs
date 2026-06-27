// ---------------------------------------------------------------------------
// CompressedStreams Example
// ---------------------------------------------------------------------------
//
// This example demonstrates that Wolfgang.Etl.FixedWidth works transparently
// over compressed streams. Because the extractor and loader accept any
// System.IO.Stream, you can wrap a GZipStream / BrotliStream around a file or
// network stream and the library neither knows nor cares — compression is
// purely a property of the underlying stream.
//
// Compressed fixed-width exports are common: EBCDIC / ASCII mainframe extracts
// frequently arrive as `.dat.gz` archives.
//
// Key concepts covered:
//   - Loading records THROUGH a compression stream into a destination stream
//   - Extracting records BACK from the decompressed stream (a full round-trip)
//   - The dispose ordering that matters for compression:
//       * the loader's Stream constructor wraps the stream with leaveOpen:true,
//         so disposing the loader flushes its 64 KB buffer but does NOT close
//         the compression stream;
//       * the compression stream must therefore be disposed AFTER the loader so
//         it writes its trailer (footer) and produces a valid archive.
//     The nested `using` blocks below get this ordering right automatically
//     (inner `using` disposes first).
//   - GZip and Brotli are interchangeable — only the wrapper type changes.
//
// In production the inner MemoryStream would be a FileStream:
//
//   await using var file = File.Create("output.dat.gz");
//   using var gzip = new GZipStream(file, CompressionMode.Compress);
//   using var loader = new FixedWidthLoader<Person>(gzip);
//   await loader.LoadAsync(records, token);
//
// ...and on the read side:
//
//   await using var file = File.OpenRead("output.dat.gz");
//   using var gzip = new GZipStream(file, CompressionMode.Decompress);
//   using var extractor = new FixedWidthExtractor<Person>(gzip);
//   await foreach (var record in extractor.ExtractAsync(token)) { ... }
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

var people = new List<Person>
{
    new() { FirstName = "Alice", LastName = "Anderson", Age = 30 },
    new() { FirstName = "Bob", LastName = "Baker", Age = 42 },
    new() { FirstName = "Charlie", LastName = "Clark", Age = 25 },
};

// ---------------------------------------------------------------------------
// GZip round-trip
// ---------------------------------------------------------------------------
Console.WriteLine("=== GZip ===");
var gzip = await CompressAsync(people, raw => new GZipStream(raw, CompressionMode.Compress, leaveOpen: true));
Console.WriteLine($"Loaded {people.Count} records into {gzip.Length} GZip-compressed bytes.");
Console.WriteLine("Extracted back from the compressed bytes:");
await ExtractAndPrintAsync(gzip, raw => new GZipStream(raw, CompressionMode.Decompress, leaveOpen: true));

Console.WriteLine();

// ---------------------------------------------------------------------------
// Brotli round-trip — identical code, only the wrapper type differs
// ---------------------------------------------------------------------------
Console.WriteLine("=== Brotli ===");
var brotli = await CompressAsync(people, raw => new BrotliStream(raw, CompressionMode.Compress, leaveOpen: true));
Console.WriteLine($"Loaded {people.Count} records into {brotli.Length} Brotli-compressed bytes.");
Console.WriteLine("Extracted back from the compressed bytes:");
await ExtractAndPrintAsync(brotli, raw => new BrotliStream(raw, CompressionMode.Decompress, leaveOpen: true));

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

// Loads the records through the compression stream and returns the compressed
// bytes. The nested using blocks dispose the loader first (flushing its buffer
// into the compression stream), then the compression stream (writing the
// trailer) — both leave the underlying MemoryStream open so we can read it.
static async Task<byte[]> CompressAsync(IEnumerable<Person> source, Func<Stream, Stream> wrapForCompression)
{
    using var raw = new MemoryStream();

    using (var compression = wrapForCompression(raw))
    using (var loader = new FixedWidthLoader<Person>(compression))
    {
        await loader.LoadAsync(ToAsyncEnumerable(source), CancellationToken.None);
    }

    return raw.ToArray();
}

// Decompresses the bytes and extracts the records back out.
static async Task ExtractAndPrintAsync(byte[] compressed, Func<Stream, Stream> wrapForDecompression)
{
    using var raw = new MemoryStream(compressed);
    using var decompression = wrapForDecompression(raw);
    using var extractor = new FixedWidthExtractor<Person>(decompression);

    await foreach (var person in extractor.ExtractAsync(CancellationToken.None))
    {
        Console.WriteLine($"  {person.FirstName.Trim()} {person.LastName.Trim()}, age {person.Age}");
    }
}

// Wraps an in-memory sequence as an IAsyncEnumerable<T> for LoadAsync. In
// production the source would already be async (a database reader, another
// extractor, etc.).
static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
{
    foreach (var item in source)
    {
        yield return item;
    }

    await Task.CompletedTask;
}

public class Person
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
