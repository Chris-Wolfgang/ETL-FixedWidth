using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Tests for <see cref="FixedWidthLineParser"/> direct-write methods
/// (<see cref="FixedWidthLineParser.WriteRecord{T}"/>,
///  <see cref="FixedWidthLineParser.WriteHeader"/>,
///  <see cref="FixedWidthLineParser.WriteSeparator"/>)
/// that write directly to a <see cref="TextWriter"/> instead of returning segments.
/// These cover the delimiter gap-fill, trailing-delimiter, and trailing-padding paths.
/// </summary>
public class DirectWriteTests
{
    // ------------------------------------------------------------------
    // Test POCOs
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    private class SimpleRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;



        [FixedWidthField(2, 5, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class SkipMiddleRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthSkip(1, 8, Message = "DOB")]
        [FixedWidthField(2, 10)]
        public string LastName { get; set; } = string.Empty;
    }



    [ExcludeFromCodeCoverage]
    private class SkipTrailingRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthSkip(1, 3, Message = "Filler")]
        public string Unused { get; set; } = string.Empty;
    }



    [ExcludeFromCodeCoverage]
    private class SkipLeadingRecord
    {
        [FixedWidthSkip(0, 5, Message = "RecordType")]
        [FixedWidthField(1, 10)]
        public string FirstName { get; set; } = string.Empty;



        [FixedWidthField(2, 10)]
        public string LastName { get; set; } = string.Empty;
    }



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string WriteRecordToString<T>
    (
        T record,
        FieldMapResult fieldMap,
        string? delimiter = null
    )
    {
        using var sw = new StringWriter();
        FixedWidthLineParser.WriteRecord
        (
            sw,
            record,
            fieldMap,
            FixedWidthConverter.ConvertToString,
            delimiter
        );
        return sw.ToString();
    }



    private static string WriteHeaderToString
    (
        FieldMapResult fieldMap,
        string? delimiter = null
    )
    {
        using var sw = new StringWriter();
        FixedWidthLineParser.WriteHeader
        (
            sw,
            fieldMap,
            FixedWidthConverter.TruncateHeader,
            delimiter
        );
        return sw.ToString();
    }



    private static string WriteSeparatorToString
    (
        FieldMapResult fieldMap,
        char separatorChar = '-',
        string? delimiter = null
    )
    {
        using var sw = new StringWriter();
        FixedWidthLineParser.WriteSeparator
        (
            sw,
            fieldMap,
            separatorChar,
            delimiter
        );
        return sw.ToString();
    }



    // ------------------------------------------------------------------
    // WriteRecord — no delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_no_delimiter_writes_fields_without_separators()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord
        {
            FirstName = "John",
            LastName = "Smith",
            Age = 42
        };

        var result = WriteRecordToString(record, fieldMap);

        Assert.Equal
        (
            "John      Smith     00042",
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteRecord — with delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_delimiter_is_set_inserts_delimiter_between_fields()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord
        {
            FirstName = "John",
            LastName = "Smith",
            Age = 42
        };

        var result = WriteRecordToString(record, fieldMap, "|");

        Assert.Equal
        (
            "John      |Smith     |00042",
            result
        );
    }



    [Fact]
    public void WriteRecord_when_delimiter_has_multiple_characters_inserts_full_delimiter()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord
        {
            FirstName = "John",
            LastName = "Smith",
            Age = 42
        };

        var result = WriteRecordToString(record, fieldMap, " | ");

        Assert.Equal
        (
            "John       | Smith      | 00042",
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteRecord — with skip columns and delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_skip_middle_and_delimiter_emits_gap_delimiters()
    {
        var fieldMap = FieldMap.GetResult<SkipMiddleRecord>();
        var record = new SkipMiddleRecord
        {
            FirstName = "John",
            LastName = "Smith"
        };

        var result = WriteRecordToString(record, fieldMap, "|");

        // Field 0 (10) || skip 1 (8 spaces) Field 2 (10)
        // The gap delimiter is emitted before the skip gap, then the skip
        // content and field content are written contiguously.
        Assert.Equal
        (
            "John      ||        Smith     ",
            result
        );
    }



    [Fact]
    public void WriteRecord_when_skip_trailing_and_delimiter_emits_trailing_delimiter_and_padding()
    {
        var fieldMap = FieldMap.GetResult<SkipTrailingRecord>();
        var record = new SkipTrailingRecord
        {
            FirstName = "Jane"
        };

        var result = WriteRecordToString(record, fieldMap, "|");

        // Field 0 (10) | trailing skip (3 spaces)
        Assert.Equal
        (
            "Jane      |   ",
            result
        );
    }



    [Fact]
    public void WriteRecord_when_skip_leading_and_delimiter_emits_leading_gap()
    {
        var fieldMap = FieldMap.GetResult<SkipLeadingRecord>();
        var record = new SkipLeadingRecord
        {
            FirstName = "John",
            LastName = "Smith"
        };

        var result = WriteRecordToString(record, fieldMap, "|");

        // | skip 0 (5 spaces) Field 1 (10) | Field 2 (10)
        // The leading skip gap is preceded by its delimiter, then the
        // field content follows contiguously.
        Assert.Equal
        (
            "|     John      |Smith     ",
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteHeader — no delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteHeader_when_no_delimiter_writes_header_labels_without_separators()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var result = WriteHeaderToString(fieldMap);

        Assert.Equal
        (
            "FirstName LastName  Age  ",
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteHeader — with delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteHeader_when_delimiter_is_set_inserts_delimiter_between_headers()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var result = WriteHeaderToString(fieldMap, "|");

        Assert.Equal
        (
            "FirstName |LastName  |Age  ",
            result
        );
    }



    [Fact]
    public void WriteHeader_when_skip_middle_and_delimiter_emits_gap_delimiters()
    {
        var fieldMap = FieldMap.GetResult<SkipMiddleRecord>();

        var result = WriteHeaderToString(fieldMap, "|");

        // FirstName(10) || skip(8 spaces) LastName(10)
        Assert.Equal
        (
            "FirstName ||        LastName  ",
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteSeparator — no delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteSeparator_when_no_delimiter_writes_separator_chars_across_full_width()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var result = WriteSeparatorToString(fieldMap, '-');

        // 10 + 10 + 5 = 25 dashes
        Assert.Equal
        (
            new string('-', 25),
            result
        );
    }



    // ------------------------------------------------------------------
    // WriteSeparator — with delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void WriteSeparator_when_delimiter_is_set_inserts_delimiter_between_separator_segments()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var result = WriteSeparatorToString(fieldMap, '-', "|");

        Assert.Equal
        (
            "----------|----------|-----",
            result
        );
    }



    [Fact]
    public void WriteSeparator_when_skip_trailing_and_delimiter_emits_trailing_separator()
    {
        var fieldMap = FieldMap.GetResult<SkipTrailingRecord>();

        var result = WriteSeparatorToString(fieldMap, '=', "|");

        // Field 0 (10) | trailing skip (3)
        Assert.Equal
        (
            "==========|===",
            result
        );
    }



    [Fact]
    public void WriteSeparator_when_skip_middle_and_delimiter_emits_gap_separator()
    {
        var fieldMap = FieldMap.GetResult<SkipMiddleRecord>();

        var result = WriteSeparatorToString(fieldMap, '-', "|");

        // Field 0 (10) || skip+Field 2 (8+10 = 18)
        Assert.Equal
        (
            "----------||------------------",
            result
        );
    }



    // ------------------------------------------------------------------
    // ParseLine — delimiter error message includes delimiter width
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_line_too_short_with_delimiter_error_message_includes_delimiter_width_breakdown()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        // SimpleRecord: 10 + 10 + 5 = 25 field width + 2 delimiters * 3 = 31 total
        var shortLine = "John       | Smith";

        var ex = Assert.Throws<LineTooShortException>
        (
            () => FixedWidthLineParser.ParseLine<SimpleRecord>
            (
                shortLine,
                1,
                fieldMap,
                " | "
            )
        );

        Assert.Contains
        (
            "field/skip width",
            ex.Message
        );
        Assert.Contains
        (
            "delimiter width",
            ex.Message
        );
    }
}
