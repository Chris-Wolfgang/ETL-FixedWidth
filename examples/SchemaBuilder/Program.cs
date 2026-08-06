// ---------------------------------------------------------------------------
// SchemaBuilder Example
// ---------------------------------------------------------------------------
//
// This example demonstrates defining a fixed-width layout in code with
// FixedWidthSchemaBuilder<T> (issue #23) instead of [FixedWidthField]
// attributes. This is the approach to reach for when you do not own the record
// type (a third-party POCO you cannot decorate) or when the layout is decided
// at runtime.
//
// The Customer type below carries NO attributes. The schema maps it entirely
// through fluent, type-safe lambda expressions, and is then handed to the
// extractor and loader via their Schema property.
//
// Key concepts covered:
//   - Building a layout with .Field(...) / .Skip(...) / .Build()
//   - Type-safe property references (c => c.Name) — no magic strings
//   - A built schema is introspectable, exactly like an attribute-resolved one
//     (ToDiagram / Fields)
//   - Using the schema for both loading and extraction via the Schema property
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Enums;

// ---------------------------------------------------------------------------
// Step 1: Build the layout in code for an undecorated type.
//
//   Id    columns 0    width 5   right-aligned, zero-padded
//   Name  column  1    width 15
//   [skip]column  2    width 3   (a reserved range in the file)
//   City  column  3    width 10
// ---------------------------------------------------------------------------

var schema = new FixedWidthSchemaBuilder<Customer>()
    .Field(c => c.Id, index: 0, length: 5, alignment: FieldAlignment.Right, pad: '0')
    .Field(c => c.Name, index: 1, length: 15)
    .Skip(index: 2, length: 3, message: "reserved")
    .Field(c => c.City, index: 3, length: 10)
    .Build();

// ---------------------------------------------------------------------------
// Step 2: A built schema is introspectable, just like an attribute-resolved one.
// ---------------------------------------------------------------------------

Console.WriteLine("Layout defined in code:");
Console.WriteLine(schema.ToDiagram());
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 3: Load records to fixed-width text using the schema.
// ---------------------------------------------------------------------------

var customers = new[]
{
    new Customer { Id = 1, Name = "Alice", City = "London" },
    new Customer { Id = 42, Name = "Bob", City = "Paris" },
};

var writer = new StringWriter();
var loader = new FixedWidthLoader<Customer>(writer) { Schema = schema };
await loader.LoadAsync(ToAsyncEnumerable(customers), CancellationToken.None);

var text = writer.ToString();
Console.WriteLine("Written fixed-width output (columns shown with a ruler):");
Console.WriteLine("0    5              20 23        33");
Console.Write(text);
Console.WriteLine();

// ---------------------------------------------------------------------------
// Step 4: Extract the same text back through the same schema.
// ---------------------------------------------------------------------------

var extractor = new FixedWidthExtractor<Customer>(new StringReader(text)) { Schema = schema };

Console.WriteLine("Extracted back into records:");
await foreach (var customer in extractor.ExtractAsync(CancellationToken.None))
{
    Console.WriteLine($"  Id={customer.Id}, Name={customer.Name}, City={customer.City}");
}

// ---------------------------------------------------------------------------
// Adapts a synchronous array to the IAsyncEnumerable the loader consumes.
// ---------------------------------------------------------------------------

#pragma warning disable CS1998 // synchronous sequence — no await needed
static async IAsyncEnumerable<Customer> ToAsyncEnumerable(IEnumerable<Customer> items)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
#pragma warning restore CS1998

// ---------------------------------------------------------------------------
// Customer — an undecorated POCO. No [FixedWidthField] attributes; the layout
// lives entirely in the schema built above.
// ---------------------------------------------------------------------------

/// <summary>A customer record whose fixed-width layout is defined in code, not by attributes.</summary>
public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
}
