// ---------------------------------------------------------------------------
// CompressedStreams Example
// ---------------------------------------------------------------------------
//
// This example demonstrates that FixedWidthLoader and FixedWidthExtractor work
// with *any* Stream — including the compression streams in
// System.IO.Compression. Because the Stream-based constructors accept an
// arbitrary Stream, GZip and Brotli compression work out of the box: you simply
// wrap the underlying stream in a GZipStream / BrotliStream before handing it to
// the loader or extractor.
//
// Compressed mainframe exports are common — fixed-width files often arrive as
// .gz archives — so reading and writing them without a temporary decompressed
// copy on disk is a frequent requirement.
//
// Key concepts covered:
//   - Loading records into a GZip-compressed stream
//   - Extracting records back out of a GZip-compressed stream
//   - The same pattern with Brotli
//   - Why the compression stream must be disposed (flushed) before the
//     compressed bytes can be read back
//   - Using leaveOpen so the loader/compressor does not close the backing store
//
// This example is self-contained: it compresses into an in-memory MemoryStream
// so it can be run with `dotnet run` without touching the file system. In
// production you would swap the MemoryStream for a FileStream, e.g.:
//
//   await using var file = File.Create("people.dat.gz");
//   await using var gzip = new GZipStream(file, CompressionLevel.Optimal);
//   using var loader = new FixedWidthLoader<PersonRecord>(gzip);
//
//   await using var file = File.OpenRead("people.dat.gz");
//   await using var gzip = new GZipStream(file, CompressionMode.Decompress);
//   using var extractor = new FixedWidthExtractor<PersonRecord>(gzip);
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// ---------------------------------------------------------------------------
// Step 1: The records we want to persist in compressed fixed-width form.
// ---------------------------------------------------------------------------

var people = new[]
{
    new PersonRecord { FirstName = "Alice",   LastName = "Anderson", Age = 25 },
    new PersonRecord { FirstName = "Bob",     LastName = "Baker",    Age = 42 },
    new PersonRecord { FirstName = "Charlie", LastName = "Clark",    Age = 33 },
};

var token = CancellationToken.None;

// ---------------------------------------------------------------------------
// Step 2: GZip round trip.
//
// Write (load) the records into a GZip-compressed stream, then read (extract)
// them straight back out — no decompressed copy is ever materialized on disk.
// ---------------------------------------------------------------------------

Console.WriteLine("=== GZip ===");

var gzipBytes = await CompressAsync
(
    people,
    backing => new GZipStream(backing, CompressionLevel.Optimal, leaveOpen: true),
    token
);

Console.WriteLine($"Compressed {people.Length} records into {gzipBytes.Length} bytes.");

await ExtractAndPrintAsync
(
    gzipBytes,
    backing => new GZipStream(backing, CompressionMode.Decompress),
    token
);

// ---------------------------------------------------------------------------
// Step 3: Brotli round trip.
//
// Identical pattern — only the compression stream type changes. This is the
// whole point: the loader and extractor neither know nor care which Stream they
// are wrapped around.
// ---------------------------------------------------------------------------

Console.WriteLine();
Console.WriteLine("=== Brotli ===");

var brotliBytes = await CompressAsync
(
    people,
    backing => new BrotliStream(backing, CompressionLevel.Optimal, leaveOpen: true),
    token
);

Console.WriteLine($"Compressed {people.Length} records into {brotliBytes.Length} bytes.");

await ExtractAndPrintAsync
(
    brotliBytes,
    backing => new BrotliStream(backing, CompressionMode.Decompress),
    token
);

// ---------------------------------------------------------------------------
// CompressAsync — load records through a compression stream and return the
// compressed bytes.
//
// The compression stream MUST be disposed before the compressed bytes are
// complete: GZip/Brotli buffer internally and write a trailing footer on
// dispose. The nested `using` blocks guarantee that ordering — the loader is
// disposed first (flushing its buffered writer), then the compression stream is
// disposed (flushing the compressor and writing the footer) — all before we
// read `backing.ToArray()`.
//
// `leaveOpen: true` on the compression stream keeps the backing MemoryStream
// open after the compressor is disposed, so we can still read its bytes.
// ---------------------------------------------------------------------------

static async Task<byte[]> CompressAsync
(
    IReadOnlyCollection<PersonRecord> records,
    Func<Stream, Stream> wrap,
    CancellationToken token
)
{
    using var backing = new MemoryStream();

    // Scope the compressor + loader so both are disposed (and thus flushed)
    // before we snapshot the backing stream's bytes below.
    await using (var compressor = wrap(backing))
    {
        using var loader = new FixedWidthLoader<PersonRecord>(compressor);
        await loader.LoadAsync(ToAsyncEnumerable(records, token), token);
    }

    return backing.ToArray();
}

// ---------------------------------------------------------------------------
// ExtractAndPrintAsync — decompress the bytes and stream the records back out
// through a FixedWidthExtractor, printing each one.
// ---------------------------------------------------------------------------

static async Task ExtractAndPrintAsync
(
    byte[] compressed,
    Func<Stream, Stream> wrap,
    CancellationToken token
)
{
    using var backing = new MemoryStream(compressed);
    await using var decompressor = wrap(backing);
    using var extractor = new FixedWidthExtractor<PersonRecord>(decompressor);

    Console.WriteLine("Extracted records:");

    await foreach (var person in extractor.ExtractAsync(token))
    {
        Console.WriteLine
        (
            $"  {person.FirstName,-10} {person.LastName,-10} Age: {person.Age}"
        );
    }
}

// ---------------------------------------------------------------------------
// ToAsyncEnumerable — adapt an in-memory collection to the
// IAsyncEnumerable<T> that LoadAsync consumes. In a real pipeline this would be
// the output of a FixedWidthExtractor or a transformer, which is already async.
//
// The #pragma suppresses CS1998 ("async method lacks await") because this
// adapter has no genuinely asynchronous work to await.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // Async method lacks 'await' operators
static async IAsyncEnumerable<PersonRecord> ToAsyncEnumerable
(
    IEnumerable<PersonRecord> records,
    [EnumeratorCancellation] CancellationToken token
)
{
    foreach (var record in records)
    {
        token.ThrowIfCancellationRequested();
        yield return record;
    }
}
#pragma warning restore CS1998

// ---------------------------------------------------------------------------
// PersonRecord — the POCO mapped to a 23-character fixed-width line
// (10 + 10 + 3). See the BasicExtraction example for a fuller walk-through of
// the [FixedWidthField] attribute.
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a single person record in a fixed-width file.
/// The total line width is 10 + 10 + 3 = 23 characters.
/// </summary>
public class PersonRecord
{
    // Column 0: 10-character first name, left-aligned, space-padded.
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    // Column 1: 10-character last name, left-aligned, space-padded.
    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    // Column 2: 3-character age, right-aligned, zero-padded.
    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
