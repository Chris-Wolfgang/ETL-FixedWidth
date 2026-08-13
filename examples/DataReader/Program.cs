using System;
using System.Data;
using System.Globalization;
using System.IO;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// FixedWidthDataReader<T> exposes a fixed-width source as a forward-only IDataReader, with the
// column layout taken from T's [FixedWidthField] attributes. It serves each field value directly
// from the parsed line — NO T instance is allocated per row — which is exactly what ADO.NET bulk
// consumers want. This example loads a DataTable (no database required); the headline consumer in
// production is SqlBulkCopy (shown at the bottom).

// Sample fixed-width customer data: Id [0..6) | Name [6..26) | Balance [26..36).
var data = string.Join
(
    "\n",
    Row(1, "Alice Johnson", 1234.50m),
    Row(2, "Bob Smith", 42.00m),
    Row(3, "Carol White", 9876.25m)
);

using var reader = new FixedWidthDataReader<Customer>(new StringReader(data));

// Any IDataReader consumer can drink from it. DataTable.Load pulls the schema and every row:
var table = new DataTable();
table.Load(reader);

Console.WriteLine($"Loaded {table.Rows.Count} rows into a DataTable through IDataReader — no POCO per row:");
Console.WriteLine();
Console.WriteLine($"  {"Id",-4} {"Name",-16} {"Balance",10}");
Console.WriteLine($"  {new string('-', 4)} {new string('-', 16)} {new string('-', 10)}");
foreach (DataRow row in table.Rows)
{
    Console.WriteLine($"  {row["Id"],-4} {row["Name"],-16} {((decimal)row["Balance"]).ToString("0.00", CultureInfo.InvariantCulture),10}");
}

// In production the reader flows straight into SqlBulkCopy — same object, zero POCO allocation:
//
//     using var reader = new FixedWidthDataReader<Customer>(File.OpenRead("customers.dat"));
//     using var bulkCopy = new SqlBulkCopy(connectionString) { DestinationTableName = "Customers" };
//     await bulkCopy.WriteToServerAsync(reader);

static string Row(int id, string name, decimal balance)
    => id.ToString("000000", CultureInfo.InvariantCulture)
       + name.PadRight(20)
       + balance.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(10);


public sealed class Customer
{
    [FixedWidthField(0, 6, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Id { get; set; }

    [FixedWidthField(1, 20)]
    public string Name { get; set; } = string.Empty;

    [FixedWidthField(2, 10, Alignment = FieldAlignment.Right, Format = "0.00")]
    public decimal Balance { get; set; }
}
