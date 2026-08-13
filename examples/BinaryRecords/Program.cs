using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;

// Binary / mainframe fixed-length records mix text with COBOL COMP (binary integer) and COMP-3
// (packed decimal) fields, and are NOT newline-delimited — every record is a fixed number of bytes.
// This example writes a few records with FixedWidthBinaryLoader, then reads them back with
// FixedWidthBinaryExtractor (a symmetric round trip).

var accounts = new[]
{
    new Account { AccountId = "ACCT0001", TransactionCount = 42, Balance = 1234.56m },
    new Account { AccountId = "ACCT0002", TransactionCount = 7, Balance = -0.05m },
    new Account { AccountId = "ACCT0003", TransactionCount = 128, Balance = 9876.25m },
};

using var buffer = new MemoryStream();
int recordBytes;
using (var loader = new FixedWidthBinaryLoader<Account>(buffer))
{
    recordBytes = loader.RecordByteLength;
    await loader.LoadAsync(ToAsync(accounts), CancellationToken.None);
}

Console.WriteLine($"Wrote {accounts.Length} records = {buffer.Length} bytes ({recordBytes} bytes each, no delimiters).");
Console.WriteLine();

buffer.Position = 0;
using var extractor = new FixedWidthBinaryExtractor<Account>(buffer);

Console.WriteLine($"  {"AccountId",-10} {"Txns",5} {"Balance",12}");
Console.WriteLine($"  {new string('-', 10)} {new string('-', 5)} {new string('-', 12)}");
await foreach (var account in extractor.ExtractAsync(CancellationToken.None))
{
    Console.WriteLine($"  {account.AccountId,-10} {account.TransactionCount,5} {account.Balance.ToString("0.00", CultureInfo.InvariantCulture),12}");
}

// Real mainframe data is usually EBCDIC — register the code-page provider and pass the encoding:
//
//     System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);   // System.Text.Encoding.CodePages
//     var extractor = new FixedWidthBinaryExtractor<Account>(stream, System.Text.Encoding.GetEncoding("IBM037"));

#pragma warning disable CS1998 // synchronous sample sequence
static async IAsyncEnumerable<Account> ToAsync(IEnumerable<Account> items)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
#pragma warning restore CS1998


public sealed class Account
{
    [FixedWidthBinaryField(0, 8, BinaryFieldType.Text)]
    public string AccountId { get; set; } = string.Empty;

    [FixedWidthBinaryField(1, 4, BinaryFieldType.Binary)]
    public int TransactionCount { get; set; }

    [FixedWidthBinaryField(2, 5, BinaryFieldType.PackedDecimal, Scale = 2)]   // PIC S9(7)V99 COMP-3
    public decimal Balance { get; set; }
}
