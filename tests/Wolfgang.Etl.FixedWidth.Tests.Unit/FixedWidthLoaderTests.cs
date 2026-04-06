using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.TestKit.Xunit;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

[ExcludeFromCodeCoverage]
public class HeaderRecord
{
    [FixedWidthField(0, 10, Header = "FIRST_NM")]
    public string FirstName { get; set; } = string.Empty;



    [FixedWidthField(1, 10, Header = "LAST_NM")]
    public string LastName { get; set; } = string.Empty;
}



public class FixedWidthLoaderTests
    : LoaderBaseContractTests
    <
        FixedWidthLoader<PersonRecord>,
        PersonRecord,
        FixedWidthReport
    >
{
    // ------------------------------------------------------------------
    // Contract test factory methods
    // ------------------------------------------------------------------

    /// <inheritdoc/>
    protected override FixedWidthLoader<PersonRecord> CreateSut(int itemCount) =>
        new(new StringWriter());



    /// <inheritdoc/>
    protected override IReadOnlyList<PersonRecord> CreateSourceItems() => new[]
    {
        new PersonRecord { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new PersonRecord { FirstName = "Bob", LastName = "Brown", Age = 30 },
        new PersonRecord { FirstName = "Carol", LastName = "Clark", Age = 35 },
        new PersonRecord { FirstName = "Dan", LastName = "Davis", Age = 40 },
        new PersonRecord { FirstName = "Eve", LastName = "Evans", Age = 45 },
    };



    /// <inheritdoc/>
    protected override FixedWidthLoader<PersonRecord> CreateSutWithTimer(
        IProgressTimer timer) =>
        new(new StringWriter(), timer);



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static FixedWidthLoader<PersonRecord> CreateLoader(out StringWriter writer)
    {
        writer = new StringWriter();
        return new FixedWidthLoader<PersonRecord>(writer);
    }



    private static string[] GetLines(StringWriter writer)
    {
        var parts = writer.ToString().Split
        (
            ["\r\n", "\n"],
            StringSplitOptions.None
        );
        if (parts.Length == 0 || parts[^1] != "")
        {
            return parts;
        }

        var trimmed = new string[parts.Length - 1];
        Array.Copy
        (
            parts,
            trimmed,
            trimmed.Length
        );
        return trimmed;
    }




    // ------------------------------------------------------------------
    // Happy path
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_with_valid_records_writes_correct_lines()
    {
        var loader = CreateLoader(out var writer);

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            2,
            lines.Length
        );
        Assert.Equal
        (
            "John      Smith     042",
            lines[0]
        );
        Assert.Equal
        (
            "Jane      Doe       030",
            lines[1]
        );
    }



    [Fact]
    public async Task LoadAsync_when_record_has_all_default_values_writes_a_correctly_sized_line()
    {
        var loader = CreateLoader(out var writer);

        await loader.LoadAsync(new[]
        {
            new PersonRecord(), // all properties at default — null/0
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Single(lines);
        Assert.Equal
        (
            23,
            lines[0].Length
        ); // 10 + 10 + 3
        Assert.Equal
        (
            "                    000",
            lines[0]
        ); // null strings → spaces, Age=0 → "000" (right-aligned, pad='0')
    }



    // ------------------------------------------------------------------
    // Header
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_WriteHeader_is_true_writes_the_header_as_the_first_line()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = true;

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            "FirstName LastName  Age",
            lines[0]
        );
        Assert.Equal
        (
            "John      Smith     042",
            lines[1]
        );
    }



    [Fact]
    public async Task LoadAsync_when_WriteHeader_is_true_and_Header_attribute_is_set_uses_the_attribute_value()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<HeaderRecord>(writer) { WriteHeader = true };

        await loader.LoadAsync(new[]
        {
            new HeaderRecord { FirstName = "John", LastName = "Smith" },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            "FIRST_NM  LAST_NM   ",
            lines[0]
        );
        Assert.Equal
        (
            "John      Smith     ",
            lines[1]
        );
    }



    // ------------------------------------------------------------------
    // SkipItemCount (verifies output content, not just counts)
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_SkipItemCount_is_set_skips_the_first_N_records()
    {
        var loader = CreateLoader(out var writer);
        loader.SkipItemCount = 1;

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
            new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 55 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            2,
            lines.Length
        );
        Assert.StartsWith
        (
            "Jane",
            lines[0],
            StringComparison.Ordinal
        );
    }



    [Fact]
    public async Task LoadAsync_when_SkipItemCount_and_MaximumItemCount_are_both_set_skips_then_loads()
    {
        var loader = CreateLoader(out var writer);
        loader.SkipItemCount = 2;
        loader.MaximumItemCount = 2;

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "Alice", LastName = "Aaa", Age = 10 },
            new PersonRecord { FirstName = "Bob", LastName = "Bbb", Age = 20 },
            new PersonRecord { FirstName = "Carol", LastName = "Ccc", Age = 30 },
            new PersonRecord { FirstName = "Dave", LastName = "Ddd", Age = 40 },
            new PersonRecord { FirstName = "Eve", LastName = "Eee", Age = 50 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        // Should skip Alice and Bob, then load Carol and Dave, stop before Eve
        Assert.Equal
        (
            2,
            lines.Length
        );
        Assert.StartsWith
        (
            "Carol",
            lines[0],
            StringComparison.Ordinal
        );
        Assert.StartsWith
        (
            "Dave",
            lines[1],
            StringComparison.Ordinal
        );
        Assert.Equal(2, loader.CurrentItemCount);
        Assert.Equal(2, loader.CurrentSkippedItemCount);
    }



    // ------------------------------------------------------------------
    // Null record
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_a_null_record_is_encountered_throws_InvalidOperationException()
    {
        var loader = CreateLoader(out _);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await loader.LoadAsync(new PersonRecord[] { null! }.ToAsyncEnumerable()));
    }



    // ------------------------------------------------------------------
    // Delimiter
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_FieldDelimiter_is_set_inserts_delimiter_between_fields()
    {
        var loader = CreateLoader(out var writer);
        loader.FieldDelimiter = " | ";

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            "John       | Smith      | 042",
            lines[0]
        );
    }



    [Fact]
    public async Task LoadAsync_when_WriteHeader_and_FieldDelimiter_are_set_header_line_is_also_delimited_zero_padded()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = true;
        loader.FieldDelimiter = " | ";

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            "FirstName  | LastName   | Age",
            lines[0]
        );
        Assert.Equal
        (
            "John       | Smith      | 042",
            lines[1]
        );
    }



    [ExcludeFromCodeCoverage]
    private class SpacePaddedRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;



        [FixedWidthField(2, 3, Alignment = FieldAlignment.Right)]
        public int Age { get; set; }
    }



    [Fact]
    public async Task LoadAsync_when_WriteHeader_and_FieldDelimiter_are_set_header_line_is_also_delimited_space_padded()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<SpacePaddedRecord>(writer)
        {
            WriteHeader = true,
            FieldDelimiter = " | ",
        };

        await loader.LoadAsync
        (
            new[]
            {
                new SpacePaddedRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }.ToAsyncEnumerable()
        );

        var lines = GetLines(writer);

        Assert.Equal
        (
            "FirstName  | LastName   | Age",
            lines[0]
        );
        Assert.Equal
        (
            "John       | Smith      |  42",
            lines[1]
        );
    }



    // ------------------------------------------------------------------
    // Separator
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_FieldSeparator_is_set_writes_separator_line_after_the_header()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = true;
        loader.FieldSeparator = '-';

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Equal
        (
            3,
            lines.Length
        );
        Assert.True(lines[1].Replace('-', ' ').Trim().Length == 0 || lines[1].All(c => c == '-'));
        Assert.Equal
        (
            "John      Smith     042",
            lines[2]
        );
    }



    [Fact]
    public async Task LoadAsync_when_FieldSeparator_and_FieldDelimiter_are_set_separator_line_is_also_delimited()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = true;
        loader.FieldSeparator = '-';
        loader.FieldDelimiter = "-|-";

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);

        Assert.Contains
        (
            "-|-",
            lines[1],
            StringComparison.Ordinal
        );
    }



    [Fact]
    public async Task LoadAsync_when_FieldSeparator_is_set_but_WriteHeader_is_false_does_not_write_a_separator_line()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = false;
        loader.FieldSeparator = '-';

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        Assert.Single(GetLines(writer));
    }



    // ------------------------------------------------------------------
    // FixedWidthReport progress
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_using_FixedWidthReport_CurrentLineNumber_reflects_the_physical_line_number()
    {
        // 1 header + 1 separator + 2 data rows = 4 physical lines written.
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer)
        {
            WriteHeader = true,
            FieldSeparator = '-',
        };

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        }.ToAsyncEnumerable());

        Assert.Equal
        (
            4L,
            loader.CurrentLineNumber
        );
        Assert.Equal
        (
            2,
            loader.CurrentItemCount
        );
    }



    [Fact]
    public async Task LoadAsync_when_using_FixedWidthReport_and_WriteHeader_is_false_CurrentLineNumber_counts_data_rows_only()
    {
        // No header or separator — 3 data rows = lines 1-3.
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer);

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
            new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 55 },
        }.ToAsyncEnumerable());

        Assert.Equal
        (
            3L,
            loader.CurrentLineNumber
        );
    }



    // ------------------------------------------------------------------
    // ValueConverter / HeaderConverter
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_ValueConverter_is_set_uses_custom_converter()
    {
        var loader = CreateLoader(out var writer);
        loader.ValueConverter =
            (
                    value,
                    ctx
                ) =>
                string.Equals(ctx.PropertyName, nameof(PersonRecord.FirstName), StringComparison.Ordinal)
                    ? ((string)value).ToUpperInvariant()
                    : FixedWidthConverter.Strict(value, ctx);

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);
        Assert.StartsWith
        (
            "JOHN      ",
            lines[0],
            StringComparison.Ordinal
        );
    }



    [Fact]
    public async Task LoadAsync_when_HeaderConverter_is_set_uses_custom_converter()
    {
        var loader = CreateLoader(out var writer);
        loader.WriteHeader = true;
        loader.HeaderConverter =
        (
            label,
            _
        ) => label.ToUpperInvariant();

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
        }.ToAsyncEnumerable());

        var lines = GetLines(writer);
        Assert.StartsWith
        (
            "FIRSTNAME ",
            lines[0],
            StringComparison.Ordinal
        );
    }



    // ------------------------------------------------------------------
    // CreateProgressReport
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetProgressReport_returns_FixedWidthReport_with_current_counts()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PersonRecord>(writer);

        await loader.LoadAsync(new[]
        {
            new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
        }.ToAsyncEnumerable());

        var report = loader.GetProgressReport();

        Assert.Equal
        (
            2,
            report.CurrentItemCount
        );
        Assert.Equal
        (
            0,
            report.CurrentSkippedItemCount
        );
        Assert.Equal
        (
            2L,
            report.CurrentLineNumber
        );
    }



    // ------------------------------------------------------------------
    // StrictHeader overflow
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    private class OverflowHeaderRecord
    {
        [FixedWidthField(0, 3, Header = "VERY_LONG_HEADER")]
        public int Id { get; set; }
    }



    [Fact]
    public async Task LoadAsync_when_WriteHeader_is_true_and_header_exceeds_field_width_throws_FieldOverflowException()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<OverflowHeaderRecord>(writer)
        {
            WriteHeader = true,
        };

        await Assert.ThrowsAsync<FieldOverflowException>
        (
            async () => await loader.LoadAsync
            (
                new[] { new OverflowHeaderRecord { Id = 1 } }.ToAsyncEnumerable()
            )
        );
    }



    // ------------------------------------------------------------------
    // TruncateHeader converter
    // ------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_when_HeaderConverter_is_TruncateHeader_truncates_long_headers()
    {
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<OverflowHeaderRecord>(writer)
        {
            WriteHeader = true,
            HeaderConverter = FixedWidthConverter.TruncateHeader,
        };

        await loader.LoadAsync
        (
            new[] { new OverflowHeaderRecord { Id = 1 } }.ToAsyncEnumerable()
        );

        var lines = GetLines(writer);

        Assert.Equal
        (
            2,
            lines.Length
        );
        Assert.Equal
        (
            "VER",
            lines[0]
        ); // "VERY_LONG_HEADER" truncated to 3 chars
    }
}
