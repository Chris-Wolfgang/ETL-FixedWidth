// ---------------------------------------------------------------------------
// PipelineExtensions Example
// ---------------------------------------------------------------------------
//
// This example demonstrates the class-named fixed-width source factories and
// sink terminators that hang off the generic EtlPipeline fluent chain
// (issue #253, requires Wolfgang.Etl.Abstractions 0.16.0):
//
//   EtlPipeline.Create()
//       .FixedWidthExtractor<T>(path | stream | reader | extractor)
//       .Through(...)                       // optional transform stages
//       .FixedWidthLoader<T>(path | stream | writer)
//       .RunAsync();
//
// Instead of wiring an extractor, transformer, and loader together by hand
// (see the RoundTrip example), the whole extract -> transform -> load flow is
// expressed as one readable statement, with the extractor/loader configuration
// exposed as inline fluent setters (HasHeader, FieldDelimiter, WriteHeader, ...).
//
// Key concepts covered:
//   - EtlPipeline.Create() as the entry point
//   - FixedWidthExtractor<T> source factories and FixedWidthLoader<T> sink
//     terminators as extension methods on the pipeline
//   - Inline Through stages (a stream-to-stream transform supplied as a
//     delegate — no dependency on the Wolfgang.Etl.Transformers operators)
//   - Fluent configuration that maps 1:1 to the extractor/loader properties
//   - Resource ownership: path-based factories own and dispose the files they
//     open; caller-supplied readers/writers are left open
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// ===========================================================================
// Scenario 1: reader -> inline transform -> writer, as a single chain.
//
// The extractor reads from a StringReader (stand-in for a file), an inline
// Through stage keeps only the adults and upper-cases their surnames, and the
// loader writes to a StringWriter. The caller owns both the reader and the
// writer, so the pipeline leaves them open.
// ===========================================================================

var inputReader = new StringReader
(
    "Alice     Smith     030\n" +
    "Bob       Jones     025\n" +
    "Carol     White     035\n"
);
var outputWriter = new StringWriter();

await EtlPipeline
    .Create()
    .FixedWidthExtractor<Person>(inputReader)
    .Through(KeepAdults)
    .FixedWidthLoader<Person>(outputWriter)
    .RunAsync();

Console.WriteLine("Scenario 1 — filter (age >= 30) + upper-case surnames:");
Console.WriteLine(new string('-', 40));
Console.Write(outputWriter.ToString());
Console.WriteLine(new string('-', 40));
Console.WriteLine();

// ===========================================================================
// Scenario 2: fluent configuration on both ends.
//
// Read a headerless, pure fixed-width source and write a human-readable table:
// a header row, a '-' separator line under it, and " | " between every field.
// Each setter maps 1:1 to a property on FixedWidthExtractor<T> /
// FixedWidthLoader<T>; the first pipeline operator materializes the extractor.
// ===========================================================================

var plainReader = new StringReader
(
    "Alice     Smith     030\n" +
    "Bob       Jones     025\n"
);
var tableWriter = new StringWriter();

await EtlPipeline
    .Create()
    .FixedWidthExtractor<Person>(plainReader)
    .FixedWidthLoader<Person>(tableWriter)
    .WriteHeader(true)
    .FieldSeparator('-')
    .FieldDelimiter(" | ")
    .RunAsync();

Console.WriteLine("Scenario 2 — header + separator + delimited output:");
Console.WriteLine(new string('-', 40));
Console.Write(tableWriter.ToString());
Console.WriteLine(new string('-', 40));
Console.WriteLine();

// ===========================================================================
// Scenario 3: path-based factories own the files they open.
//
// The path source and path sink open their own file streams and dispose them
// when RunAsync completes (on success or failure) — no `using` needed at the
// call site. The files below are deleted afterward to prove the handles were
// released.
// ===========================================================================

var sourcePath = Path.Combine(Path.GetTempPath(), $"fw-pipeline-{Guid.NewGuid():N}.txt");
var targetPath = Path.Combine(Path.GetTempPath(), $"fw-pipeline-{Guid.NewGuid():N}.out.txt");
await File.WriteAllTextAsync(sourcePath, "Alice     Smith     030\nBob       Jones     025\n");

await EtlPipeline
    .Create()
    .FixedWidthExtractor<Person>(sourcePath)
    .FixedWidthLoader<Person>(targetPath)
    .RunAsync();

Console.WriteLine("Scenario 3 — path in, path out (files owned + disposed):");
Console.WriteLine(new string('-', 40));
Console.Write(await File.ReadAllTextAsync(targetPath));
Console.WriteLine(new string('-', 40));

// A locked handle would throw here; the successful delete proves both files
// were released.
File.Delete(sourcePath);
File.Delete(targetPath);
Console.WriteLine("Both files deleted — handles were released.");

// ---------------------------------------------------------------------------
// Inline Through stage.
//
// A stream-to-stream transform supplied as a delegate: it keeps records whose
// Age is at least 30 and upper-cases the surname. Because it is expressed as a
// Func<IAsyncEnumerable<Person>, IAsyncEnumerable<Person>>, no reference to the
// Wolfgang.Etl.Transformers operator package is required.
//
// The #pragma suppresses CS1998 because this particular stage is synchronous;
// a stage that awaited a database or service would not need it.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // Async method lacks 'await' operators
static async IAsyncEnumerable<Person> KeepAdults(IAsyncEnumerable<Person> source)
{
    await foreach (var person in source)
    {
        if (person.Age >= 30)
        {
            yield return new Person
            {
                FirstName = person.FirstName,
                LastName = person.LastName.ToUpperInvariant(),
                Age = person.Age,
            };
        }
    }
}
#pragma warning restore CS1998

// ---------------------------------------------------------------------------
// Person — the record type. FirstName(10) + LastName(10) + Age(3), so each
// data line is exactly 23 characters wide.
// ---------------------------------------------------------------------------

/// <summary>
/// A person record: first name and last name (10 chars each) plus a
/// right-aligned, zero-padded age (3 chars).
/// </summary>
public class Person
{
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10)]
    public string LastName { get; set; } = string.Empty;



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}
