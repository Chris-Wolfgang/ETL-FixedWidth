using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
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
// ------------------------------------------------------------------
// Shared test POCOs
// ------------------------------------------------------------------

[ExcludeFromCodeCoverage]
public record PersonRecord
{
#pragma warning disable CS8618 // null by design — tests verify default(PersonRecord) has null strings
    [FixedWidthField(0, 10)]
    public string FirstName { get; set; }



    [FixedWidthField(1, 10)]
    public string LastName { get; set; }
#pragma warning restore CS8618



    [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
    public int Age { get; set; }
}



public class FixedWidthExtractorTests
    : ExtractorBaseContractTests<
        FixedWidthExtractor<PersonRecord, FixedWidthReport>,
        PersonRecord,
        FixedWidthReport>
{
    // ------------------------------------------------------------------
    // Contract test factory methods
    // ------------------------------------------------------------------

    private static readonly IReadOnlyList<PersonRecord> ExpectedItems = new[]
    {
        new PersonRecord { FirstName = "Alice", LastName = "Anderson", Age = 25 },
        new PersonRecord { FirstName = "Bob", LastName = "Brown", Age = 30 },
        new PersonRecord { FirstName = "Carol", LastName = "Clark", Age = 35 },
        new PersonRecord { FirstName = "Dan", LastName = "Davis", Age = 40 },
        new PersonRecord { FirstName = "Eve", LastName = "Evans", Age = 45 },
    };



    /// <summary>
    /// Builds fixed-width content for the first <paramref name="count"/> expected items.
    /// Each line is 23 chars: FirstName(10) + LastName(10) + Age(3, right-aligned, zero-padded).
    /// </summary>
    private static string BuildPersonContent(int count) =>
        string.Join
        (
            "\n",
            ExpectedItems.Take(count).Select
            (
                p => $"{(p.FirstName ?? "").PadRight(10)}{(p.LastName ?? "").PadRight(10)}{p.Age.ToString().PadLeft(3, '0')}"
            )
        );



    /// <inheritdoc/>
    protected override FixedWidthExtractor<PersonRecord, FixedWidthReport> CreateSut(int itemCount) =>
        new(new StringReader(BuildPersonContent(itemCount)));



    /// <inheritdoc/>
    protected override IReadOnlyList<PersonRecord> CreateExpectedItems() => ExpectedItems;



    /// <inheritdoc/>
    protected override FixedWidthExtractor<PersonRecord, FixedWidthReport> CreateSutWithTimer(
        IProgressTimer timer) =>
        new(new StringReader(BuildPersonContent(5)), timer);



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static FixedWidthExtractor<PersonRecord, Report> CreateExtractor(string content) =>
        new(new StringReader(content));



    // ------------------------------------------------------------------
    // Header / separator skipping
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_HeaderLineCount_is_set_skips_that_many_header_lines()
    {
        var extractor = CreateExtractor( "FirstName LastName  Age\n" + "John      Smith     042\n" + "Jane      Doe       030");
        extractor.HeaderLineCount = 1;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_HasHeader_is_true_skips_one_header_line()
    {
        var extractor = CreateExtractor( "FirstName LastName  Age\n" + "John      Smith     042\n" + "Jane      Doe       030");
        extractor.HasHeader = true;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public void HasHeader_get_returns_true_when_HeaderLineCount_is_greater_than_zero()
    {
        var extractor = CreateExtractor(string.Empty);
        extractor.HeaderLineCount = 2;

        Assert.True(extractor.HasHeader);
    }



    [Fact]
    public void HasHeader_set_to_false_sets_HeaderLineCount_to_zero()
    {
        var extractor = CreateExtractor(string.Empty);
        extractor.HeaderLineCount = 2;
        extractor.HasHeader = false;

        Assert.Equal
        (
            0,
            extractor.HeaderLineCount
        );
    }



    [Fact]
    public void HasHeader_set_to_true_then_direct_HeaderLineCount_assignment_overrides_it()
    {
        // HasHeader = true sets HeaderLineCount to 1, but a subsequent direct
        // assignment to HeaderLineCount takes full effect.
        var extractor = CreateExtractor(string.Empty);
        extractor.HasHeader = true;
        extractor.HeaderLineCount = 2;

        Assert.Equal
        (
            2,
            extractor.HeaderLineCount
        );
    }



    [Fact]
    public async Task ExtractAsync_when_FieldSeparator_is_set_skips_the_separator_line()
    {
        var extractor = CreateExtractor( "FirstName LastName  Age\n" + "-----------------------\n" + "John      Smith     042");
        extractor.HeaderLineCount = 1;
        extractor.FieldSeparator = '-';

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_FieldSeparator_is_set_but_HeaderLineCount_is_zero_does_not_skip_any_lines()
    {
        // FieldSeparator with HeaderLineCount = 0 should not skip any lines.
        var extractor = CreateExtractor( "John      Smith     042\n" + "Jane      Doe       030");
        extractor.FieldSeparator = '-';

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
    }



    // ------------------------------------------------------------------
    // Blank line handling
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_blank_line_is_encountered_throws_by_default()
    {
        var extractor = CreateExtractor("John      Smith     042\n\nJane      Doe       030");

        await Assert.ThrowsAsync<LineTooShortException>( async () => await extractor.ExtractAsync().ToListAsync());
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_Skip_skips_blank_lines()
    {
        var extractor = CreateExtractor("John      Smith     042\n\nJane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.Skip;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ReturnDefault_yields_a_default_record_for_blank_lines()
    {
        var extractor = CreateExtractor("John      Smith     042\n\nJane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.ReturnDefault;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            3,
            results.Count
        );
        Assert.Null(results[1].FirstName);
    }



    // ------------------------------------------------------------------
    // Short line / malformed line handling
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_line_is_too_short_throws_by_default()
    {
        // Line too short — only 15 chars, need 23.
        var extractor = CreateExtractor("John      Smith ");

        await Assert.ThrowsAsync<LineTooShortException>( async () => await extractor.ExtractAsync().ToListAsync());
    }



    [Fact]
    public async Task ExtractAsync_when_MalformedLineHandling_is_Skip_skips_short_lines()
    {
        var extractor = CreateExtractor( "John      Smith \n" + // too short — 16 chars
                                         "Jane      Doe       030");
        extractor.MalformedLineHandling = MalformedLineHandling.Skip;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "Jane",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_MalformedLineHandling_is_ReturnDefault_yields_a_default_record_for_short_lines()
    {
        var extractor = CreateExtractor( "John      Smith \n" + // too short — 16 chars
                                         "Jane      Doe       030");
        extractor.MalformedLineHandling = MalformedLineHandling.ReturnDefault;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Null(results[0].FirstName);
        Assert.Equal
        (
            "Jane",
            results[1].FirstName
        );
    }



    // ------------------------------------------------------------------
    // LineFilter
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Process_parses_the_line_normally()
    {
        var extractor = CreateExtractor("John      Smith     042");
        extractor.LineFilter = _ => LineAction.Process;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Skip_skips_the_line()
    {
        var extractor = CreateExtractor( "# comment  \n" + "John      Smith     042");
        extractor.LineFilter = line => line.StartsWith("#")
            ? LineAction.Skip
            : LineAction.Process;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Stop_ends_the_stream()
    {
        var extractor = CreateExtractor( "John      Smith     042\n" + "END\n" + "Jane      Doe       030");
        extractor.LineFilter = line => "END".Equals(line, StringComparison.InvariantCultureIgnoreCase)
            ? LineAction.Stop
            : LineAction.Process;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Stop_on_a_trailing_separator_line_ends_the_stream()
    {
        var extractor = CreateExtractor( "John      Smith     042\n" + "Jane      Doe       030\n" + "-----------------------");
        extractor.LineFilter = line => line.Length > 0 && line.Trim('-').Length == 0
            ? LineAction.Stop
            : LineAction.Process;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
    }



    [Fact]
    public async Task ExtractAsync_LineFilter_is_not_invoked_for_blank_lines()
    {
        // BlankLineHandling is evaluated before LineFilter — blank lines never reach it.
        var extractor = CreateExtractor( "John      Smith     042\n" + "\n" + "Jane      Doe       030");

        extractor.BlankLineHandling = BlankLineHandling.Skip;
        var filterInvokedForBlank = false;
        extractor.LineFilter = line =>
        {
            if (string.IsNullOrEmpty(line))
            {
                filterInvokedForBlank = true;
            }

            return LineAction.Process;
        };

        await extractor.ExtractAsync().ToListAsync();

        Assert.False(filterInvokedForBlank);
    }



    // ------------------------------------------------------------------
    // All-spaces row / blank line + skip/max interaction
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_row_is_all_spaces_parses_as_a_default_record_and_counts_toward_MaximumItemCount()
    {
        // A row of all spaces is a valid data line — produces a record with all
        // trimmed-empty/default fields and counts toward MaximumItemCount.
        var allSpaces = new string
        (
            ' ',
            23
        ); // PersonRecord is 10+10+3 = 23
        var extractor = CreateExtractor( "John      Smith     042\n" + allSpaces + "\n" + "Jane      Doe       030");

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            3,
            results.Count
        );
        Assert.Equal
        (
            string.Empty,
            results[1].FirstName
        ); // trimmed spaces → empty string, not null
        Assert.Equal
        (
            0,
            results[1].Age
        ); // default int
    }



    [Fact]
    public async Task ExtractAsync_when_all_spaces_row_is_within_the_skip_budget_counts_toward_SkipItemCount()
    {
        var allSpaces = new string
        (
            ' ',
            23
        );
        var extractor = CreateExtractor( allSpaces + "\n" + "John      Smith     042");
        extractor.SkipItemCount = 1;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
        Assert.Equal
        (
            1,
            extractor.CurrentSkippedItemCount
        );
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_Skip_blank_line_does_not_count_toward_SkipItemCount()
    {
        // BlankLineHandling.Skip — blank line is invisible to counting logic.
        // With SkipItemCount = 1, the blank line should not consume the skip budget.
        var extractor = CreateExtractor( "\n" + // blank — invisible
                                         "John      Smith     042\n" +  // should be skipped (skip budget = 1)
                                         "Jane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.Skip;
        extractor.SkipItemCount = 1;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "Jane",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ReturnDefault_and_blank_line_is_within_the_skip_budget_counts_toward_SkipItemCount()
    {
        // BlankLineHandling.ReturnDefault — blank line within skip budget counts as a skip.
        var extractor = CreateExtractor( "\n" + // blank — counts as skip #1
                                         "John      Smith     042\n" + // counts as skip #2
                                         "Jane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.ReturnDefault;
        extractor.SkipItemCount = 2;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "Jane",
            results[0].FirstName
        );
        Assert.Equal
        (
            2,
            extractor.CurrentSkippedItemCount
        );
    }



    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ReturnDefault_and_blank_line_is_past_the_skip_budget_counts_toward_MaximumItemCount()
    {
        // BlankLineHandling.ReturnDefault — blank line past skip budget yields a default
        // record and counts toward MaximumItemCount.
        var extractor = CreateExtractor( "John      Smith     042\n" + "\n" + "Jane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.ReturnDefault;
        extractor.MaximumItemCount = 2;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
        Assert.Null(results[1].FirstName); // the blank line's default record
    }



    [Fact]
    public async Task ExtractAsync_when_LineFilter_returns_Skip_the_line_is_invisible_to_all_counting()
    {
        // LineAction.Skip lines are invisible — they do not affect any counter.
        var extractor = CreateExtractor( "# comment  \n" + "John      Smith     042");
        extractor.LineFilter = line => line.StartsWith("#")
            ? LineAction.Skip
            : LineAction.Process;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            0,
            extractor.CurrentSkippedItemCount
        );
        Assert.Equal
        (
            1,
            extractor.CurrentItemCount
        );
    }



    // ------------------------------------------------------------------
    // Delimiter round-trip
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_FieldDelimiter_is_set_parses_fields_correctly()
    {
        const string content = "John       | Smith      | 042";
        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
        };

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
        Assert.Equal
        (
            "Smith",
            results[0].LastName
        );
        Assert.Equal
        (
            42,
            results[0].Age
        );
    }



    [Fact]
    public async Task ExtractAsync_when_HasHeader_and_FieldDelimiter_are_set_skips_header_and_parses_data()
    {
        var content =
            "FirstName  | LastName   | Age\n" +
            "John       | Smith      | 042\n" +
            "Jane       | Doe        | 030";

        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
            HeaderLineCount = 1,
        };

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
        Assert.Equal
        (
            "Jane",
            results[1].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_HasHeader_FieldSeparator_and_FieldDelimiter_are_all_set_skips_header_and_separator()
    {
        const string content = "FirstName  | LastName   | Age\n" +
                               "-----------| -----------| ---\n" +
                               "John       | Smith      | 042";

        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
            HeaderLineCount = 1,
            FieldSeparator = '-',
        };

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_FieldDelimiter_is_set_and_line_is_too_short_throws_MalformedLineException()
    {
        // With " | " delimiter PersonRecord expects 29 chars — line below is too short.
        const string content = "John       | Smith     ";
        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
        };

        await Assert.ThrowsAsync<LineTooShortException>( async () => await extractor.ExtractAsync().ToListAsync());
    }



    [Fact]
    public async Task ExtractAsync_when_FieldDelimiter_is_set_and_MalformedLineHandling_is_Skip_skips_short_lines()
    {
        const string content =
            "John       | Smith     \n" + // too short
            "Jane       | Doe        | 030";

        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
            MalformedLineHandling = MalformedLineHandling.Skip
        };

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "Jane",
            results[0].FirstName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_FieldDelimiter_is_set_and_MalformedLineHandling_is_ReturnDefault_yields_a_default_record_for_short_lines()
    {
        const string content = "John       | Smith     \n" + // too short
                               "Jane       | Doe        | 030";

        var extractor = new FixedWidthExtractor<PersonRecord, Report>(new StringReader(content))
        {
            FieldDelimiter = " | ",
            MalformedLineHandling = MalformedLineHandling.ReturnDefault
        };

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            results.Count
        );
        Assert.Null(results[0].FirstName);
        Assert.Equal
        (
            "Jane",
            results[1].FirstName
        );
    }



    // ------------------------------------------------------------------
    // ValueParser
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_custom_ValueParser_is_set_uses_it_for_all_fields()
    {
        // Upper-case all string values to verify the parser is being called.
        var extractor = CreateExtractor("john      smith     042");
        extractor.ValueParser =
            (
                    text,
                    ctx
                ) =>
                ctx.PropertyType == typeof(string)
                    ? text.ToString().Trim().ToUpperInvariant()
                    : FixedWidthConverter.DefaultParser(text, ctx);

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "JOHN",
            results[0].FirstName
        );
        Assert.Equal
        (
            "SMITH",
            results[0].LastName
        );
        Assert.Equal
        (
            42,
            results[0].Age
        ); // non-string falls through to DefaultParser
    }



    [Fact]
    public Task ExtractAsync_default_ValueParser_is_FixedWidthConverter_DefaultParser()
    {
        var extractor = CreateExtractor(string.Empty);

        Assert.Equal
        (
            FixedWidthConverter.DefaultParser,
            extractor.ValueParser
        );
        return Task.CompletedTask;
    }



    // ------------------------------------------------------------------
    // FixedWidthReport progress
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_using_FixedWidthReport_CurrentLineNumber_reflects_the_physical_line_number()
    {
        // 1 header + 1 separator + 3 data rows = lines 1-5.
        // After extracting all records, CurrentLineNumber should be 5.
        const string content = "FirstName LastName  Age\n" + // line 1 — header
                               "-----------------------\n" + // line 2 — separator
                               "John      Smith     042\n" + // line 3
                               "Jane      Doe       030\n" + // line 4
                               "Bob       Jones     055"; // line 5

        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(new StringReader(content))
        {
            HasHeader = true,
            FieldSeparator = '-',
        };

        await foreach (var _ in extractor.ExtractAsync()) {}

        Assert.Equal
        (
            5L,
            extractor.CurrentLineNumber
        );
        Assert.Equal
        (
            3,
            extractor.CurrentItemCount
        );
    }



    [Fact]
    public async Task ExtractAsync_when_using_FixedWidthReport_and_an_exception_is_thrown_CurrentLineNumber_points_to_the_offending_line()
    {
        // Line 3 is too short — CurrentLineNumber should be 3 when exception fires.
        const string content = "John      Smith     042\n" + // line 1 — ok
                               "Jane      Doe       030\n" + // line 2 — ok
                               "Bad       Line"; // line 3 — too short

        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(new StringReader(content));

        var ex = await Assert.ThrowsAsync<LineTooShortException>(async () =>
        {
            await foreach (var _ in extractor.ExtractAsync()) { }
        });

        Assert.Equal
        (
            3L,
            ex.LineNumber
        );
    }



    [Fact]
    public async Task ExtractAsync_when_using_FixedWidthReport_and_MalformedLineHandling_is_Skip_CurrentSkippedItemCount_reflects_skipped_malformed_lines()
    {
        const string content = "John      Smith     042\n" +
                               "Bad       Line     \n" + // too short — skipped
                               "Jane      Doe       030";

        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(new StringReader(content))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        _ = await extractor.ExtractAsync().ToListAsync();

        Assert.Equal
        (
            2,
            extractor.CurrentItemCount
        );
        Assert.Equal
        (
            1,
            extractor.CurrentSkippedItemCount
        );
    }



    // ------------------------------------------------------------------
    // FixedWidthSkipAttribute — integration
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    private class EmployeeRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthSkip(1, 8, Message = "DOB")]
        [FixedWidthSkip(2, 8, Message = "HireDate")]
        [FixedWidthField(3, 5)]
        public string EmployeeNumber { get; set; } = string.Empty;



        [FixedWidthField(4, 10)]
        public string LastName { get; set; } = string.Empty;
    }



    private static FixedWidthExtractor<EmployeeRecord, Report> CreateEmployeeExtractor(string content) =>
        new(new StringReader(content));




    [Fact]
    public async Task ExtractAsync_when_FixedWidthSkip_attributes_are_present_parses_the_correct_fields()
    {
        // FirstName(10) + DOB(8) + HireDate(8) + EmployeeNumber(5) + LastName(10) = 41
        const string line = "John      19900101202301012345 Smith     ";
        var extractor = CreateEmployeeExtractor(line);

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
        Assert.Equal
        (
            "2345",
            results[0].EmployeeNumber
        ); // trimmed (TrimValue defaults to true)
        Assert.Equal
        (
            "Smith",
            results[0].LastName
        );
    }



    [Fact]
    public async Task ExtractAsync_when_FixedWidthSkip_attributes_are_present_expected_line_width_includes_skip_column_widths()
    {
        // Line that is too short — missing the skip columns
        const string line = "John      2345 Smith     "; // only 25 chars, need 41
        var extractor = CreateEmployeeExtractor(line);

        await Assert.ThrowsAsync<LineTooShortException>(async () => await extractor.ExtractAsync().ToListAsync());
    }



    // ------------------------------------------------------------------
    // CreateProgressReport
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetProgressReport_returns_FixedWidthReport_with_current_counts()
    {
        const string content = "John      Smith     042\nJane      Doe       030";
        var extractor = new FixedWidthExtractor<PersonRecord, FixedWidthReport>(new StringReader(content));

        await foreach (var _ in extractor.ExtractAsync()) { }

        var report = extractor.GetProgressReport();

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



    [Fact]
    public void GetProgressReport_when_TProgress_is_not_FixedWidthReport_throws_NotSupportedException()
    {
        var extractor = new FixedWidthExtractor<PersonRecord, Exception>(new StringReader(string.Empty));

        Assert.Throws<NotSupportedException>(extractor.GetProgressReport);
    }



    // ------------------------------------------------------------------
    // BlankLineHandling.ReturnDefault + MaximumItemCount already reached
    // ------------------------------------------------------------------

    [Fact]
    public async Task ExtractAsync_when_BlankLineHandling_is_ReturnDefault_and_MaximumItemCount_is_already_reached_stops_before_yielding_blank_default()
    {
        // MaximumItemCount = 1, first line is a normal record, second line is blank.
        // The blank line should NOT be yielded because the budget is already full.
        var extractor = CreateExtractor("John      Smith     042\n\nJane      Doe       030");
        extractor.BlankLineHandling = BlankLineHandling.ReturnDefault;
        extractor.MaximumItemCount = 1;

        var results = await extractor.ExtractAsync().ToListAsync();

        Assert.Single(results);
        Assert.Equal
        (
            "John",
            results[0].FirstName
        );
    }

}
