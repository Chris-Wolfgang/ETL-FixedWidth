using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wolfgang.Etl.TestKit.Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Verifies <see cref="FixedWidthLoader{TRecord}"/> honors the
/// <c>ISupportDryRun</c> contract: a dry run enumerates the source but writes
/// nothing to the output stream, while a normal run does write.
/// </summary>
public class FixedWidthLoaderDryRunContractTests
    : SupportsDryRunContractTests<FixedWidthLoader<PersonRecord>>
{
    private static readonly IReadOnlyList<PersonRecord> SourceItems = new List<PersonRecord>
    {
        new() { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new() { FirstName = "Bob", LastName = "Brown", Age = 30 },
    };



    protected override FixedWidthLoader<PersonRecord> CreateSut() =>
        new(new MemoryStream());



    protected override async Task<bool> RunAndReportSideEffectAsync(bool isDryRun)
    {
        var stream = new MemoryStream();
        var sut = new FixedWidthLoader<PersonRecord>(stream) { IsDryRun = isDryRun };

        await sut.LoadAsync(SourceItems.ToAsyncEnumerable());

        // The Stream constructor owns (and flushes) the writer, so any bytes
        // written show up here. A dry run must leave the stream empty.
        return stream.Length > 0;
    }
}
