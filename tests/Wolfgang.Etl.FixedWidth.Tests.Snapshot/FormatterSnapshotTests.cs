using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using VerifyTests;
using VerifyXunit;
using Xunit;
using static VerifyXunit.Verifier;

namespace Wolfgang.Etl.FixedWidth.Tests.Snapshot;

/// <summary>
/// Approval/snapshot tests (#150) that pin the loader's exact text output. Unlike
/// targeted assertions, these catch accidental format drift — a padding, header,
/// separator, or alignment change shows up as a snapshot diff. The
/// <c>.verified.txt</c> files under <c>Snapshots/</c> are the approved baseline;
/// CI fails if output diverges from them.
/// </summary>
public class FormatterSnapshotTests
{
    [ExcludeFromCodeCoverage]
    private record PersonRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(2, 5, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private record SkipLayoutRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthSkip(1, 8, Message = "DOB")]
        [FixedWidthField(2, 6)]
        public string EmployeeNumber { get; set; } = string.Empty;
    }



    private static readonly IReadOnlyList<PersonRecord> People = new[]
    {
        new PersonRecord { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new PersonRecord { FirstName = "Bob", LastName = "Brown", Age = 7 },
    };



    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }



    private static async Task<string> Write<T>(IEnumerable<T> items, System.Action<FixedWidthLoader<T>>? configure = null)
        where T : notnull
    {
        var writer = new StringWriter();
        using var loader = new FixedWidthLoader<T>(writer);
        configure?.Invoke(loader);
        await loader.LoadAsync(ToAsync(items), CancellationToken.None);
        return writer.ToString();
    }



    private static SettingsTask Snapshot(string output) =>
        Verify(output).UseDirectory("Snapshots");



    [Fact]
    public Task Default_fixed_width_output() =>
        Snapshot(Write(People).GetAwaiter().GetResult());



    [Fact]
    public Task With_header_row() =>
        Snapshot(Write(People, l => l.WriteHeader = true).GetAwaiter().GetResult());



    [Fact]
    public Task With_field_delimiter() =>
        Snapshot(Write(People, l => l.FieldDelimiter = " | ").GetAwaiter().GetResult());



    [Fact]
    public Task With_header_and_separator_line() =>
        Snapshot(Write(People, l =>
        {
            l.WriteHeader = true;
            l.FieldSeparator = '-';   // draws a separator line beneath the header
        }).GetAwaiter().GetResult());



    [Fact]
    public Task Skip_column_layout() =>
        Snapshot(Write(new[] { new SkipLayoutRecord { FirstName = "Carol", EmployeeNumber = "E1234" } }).GetAwaiter().GetResult());
}
