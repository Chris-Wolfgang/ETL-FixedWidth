using System;
using System.Diagnostics.CodeAnalysis;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Wolfgang.Etl.FixedWidth.Parsing;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthLineParserTests
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
    private class DateRecord
    {
        [FixedWidthField(0, 8, Format = "yyyyMMdd")]
        public DateTime BirthDate { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class NullableRecord
    {
        [FixedWidthField(0, 10)]
        public string Name { get; set; } = string.Empty;



        [FixedWidthField(1, 5)]
        public int? OptionalValue { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class TrimRecord
    {
        [FixedWidthField(0, 10, TrimValue = false)]
        public string? RawValue { get; set; }



        [FixedWidthField(1, 10, TrimValue = true)]
        public string? TrimmedValue { get; set; }
    }



    // ------------------------------------------------------------------
    // ParseLine — happy path
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_with_a_valid_line_populates_all_fields()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var line = "John      Smith     00042";

        var result = FixedWidthLineParser.ParseLine<SimpleRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            "John",
            result.FirstName
        );
        Assert.Equal
        (
            "Smith",
            result.LastName
        );
        Assert.Equal
        (
            42,
            result.Age
        );
    }



    [Fact]
    public void ParseLine_when_TrimValue_is_true_trims_field_values()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var line = "John      Smith     00042";

        var result = FixedWidthLineParser.ParseLine<SimpleRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            "John",
            result.FirstName
        ); // not "John      "
        Assert.Equal
        (
            "Smith",
            result.LastName
        ); // not "Smith     "
    }



    [Fact]
    public void ParseLine_when_TrimValue_is_false_preserves_whitespace_in_field_values()
    {
        var fieldMap = FieldMap.GetResult<TrimRecord>();
        var line = "  spaces    trimmed   ";

        var result = FixedWidthLineParser.ParseLine<TrimRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            "  spaces  ",
            result.RawValue
        );
        Assert.Equal
        (
            "trimmed",
            result.TrimmedValue
        );
    }



    [Fact]
    public void ParseLine_when_field_has_a_Format_string_parses_date_correctly()
    {
        var fieldMap = FieldMap.GetResult<DateRecord>();
        var line = "19900115";

        var result = FixedWidthLineParser.ParseLine<DateRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            new DateTime
            (
                1990,
                1,
                15
            ),
            result.BirthDate
        );
    }



    [Fact]
    public void ParseLine_when_nullable_field_is_empty_returns_null()
    {
        var fieldMap = FieldMap.GetResult<NullableRecord>();
        var line = "Jane           ";

        var result = FixedWidthLineParser.ParseLine<NullableRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Null(result.OptionalValue);
    }



    [Fact]
    public void ParseLine_when_nullable_field_has_a_value_parses_correctly()
    {
        var fieldMap = FieldMap.GetResult<NullableRecord>();
        var line = "Jane      00099";

        var result = FixedWidthLineParser.ParseLine<NullableRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            99,
            result.OptionalValue
        );
    }



    // ------------------------------------------------------------------
    // ParseLine — error cases
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_line_is_too_short_throws_LineTooShortException()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var line = "Jo"; // Way too short

        var ex = Assert.Throws<LineTooShortException>( () => FixedWidthLineParser.ParseLine<SimpleRecord>(line, 5, fieldMap));

        Assert.Equal
        (
            5,
            ex.LineNumber
        );
        Assert.Equal
        (
            line,
            ex.LineContent
        );
        Assert.Equal
        (
            25,
            ex.ExpectedWidth
        ); // 10 + 10 + 5
        Assert.Equal
        (
            line.Length,
            ex.ActualWidth
        );
    }



    [Fact]
    public void ParseLine_when_a_field_value_cannot_be_converted_throws_FieldConversionException()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        const string line = "John      Smith     NOTANUMBER"; // Age field is not a number

        var ex = Assert.Throws<FieldConversionException>( () => FixedWidthLineParser.ParseLine<SimpleRecord>(line, 3, fieldMap));

        Assert.Equal
        (
            3,
            ex.LineNumber
        );
        Assert.Equal
        (
            line,
            ex.LineContent
        );
        Assert.Equal
        (
            "Age",
            ex.FieldName
        );
        Assert.Equal
        (
            typeof(int),
            ex.ExpectedType
        );
        Assert.Equal
        (
            "NOTAN",
            ex.RawValue
        ); // Age field is length 5, starts at 20 → "NOTAN"
        Assert.NotNull(ex.InnerException);
    }



    // ------------------------------------------------------------------
    // FormatSegments — happy path
    // ------------------------------------------------------------------

    [Fact]
    public void FormatSegments_when_field_is_left_aligned_pads_with_spaces()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "John", LastName = "Smith", Age = 42 };

        var line = string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict));

        Assert.Equal
        (
            "John      Smith     00042",
            line
        );
    }



    [Fact]
    public void FormatSegments_when_using_Strict_and_value_is_too_long_throws_FieldOverflowException()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "Christopher", LastName = "Smith", Age = 1 };

        Assert.Throws<FieldOverflowException>( () => string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict)));
    }



    [Fact]
    public void FormatSegments_when_using_Strict_and_value_is_too_long_FieldOverflowException_carries_diagnostics()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "Christopher", LastName = "Smith", Age = 1 };

        var ex = Assert.Throws<FieldOverflowException>( () => string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict)));

        Assert.Equal
        (
            "FirstName",
            ex.PropertyName
        );
        Assert.Equal
        (
            10,
            ex.FieldLength
        ); // defined as FixedWidthField(0, 10)
        Assert.Equal
        (
            11,
            ex.ActualLength
        ); // "Christopher".Length
    }



    [Fact]
    public void FormatSegments_when_using_Truncate_and_value_is_too_long_truncates_to_field_width()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "Christopher", LastName = "Smith", Age = 1 };

        var line = string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Truncate));

        Assert.Equal
        (
            "Christophe",
            line.Substring
            (
                0,
                10
            )
        ); // truncated to 10 chars
    }



    [Fact]
    public void FormatSegments_when_field_value_is_null_writes_spaces()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = null!, LastName = "Smith", Age = 0 };

        var line = string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict));

        Assert.Equal
        (
            "          ",
            line.Substring
            (
                0,
                10
            )
        );
    }



    [Fact]
    public void FormatSegments_when_field_has_a_Format_string_formats_date_correctly()
    {
        var fieldMap = FieldMap.GetResult<DateRecord>();
        var record = new DateRecord { BirthDate = new DateTime
        (
            1990,
            1,
            15
        ) };

        var line = string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict));

        Assert.Equal
        (
            "19900115",
            line
        );
    }



    // ------------------------------------------------------------------
    // Round-trip
    // ------------------------------------------------------------------

    [Fact]
    public void FormatSegments_when_custom_converter_returns_string_longer_than_field_width_throws_FieldOverflowException()
    {
        // Verifies the safety-net throw inside FormatSegment — fires when a custom
        // converter bypasses length validation and returns an overlong string.
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "John", LastName = "Smith", Age = 1 };

        // Custom converter that ignores field-length constraints.
        Func<object, FieldContext, string> badConverter = (_, _) => "This string is far too long";

        var ex = Assert.Throws<FieldOverflowException>
        (
            () => string.Concat(FixedWidthLineParser.FormatSegments(record, fieldMap, badConverter))
        );

        Assert.Equal
        (
            "FirstName",
            ex.PropertyName
        );
        Assert.Equal
        (
            10,
            ex.FieldLength
        );
        Assert.True(ex.ActualLength > 10);
    }



    [Fact]
    public void FormatHeaderSegments_when_custom_header_converter_returns_string_longer_than_field_width_throws_FieldOverflowException()
    {
        // Verifies the overflow throw inside FormatHeaderSegment — fires when a custom
        // header converter returns a label that exceeds the field width.
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        // Custom header converter that returns an overlong label.
        Func<string, FieldContext, string> badConverter = (_, _) => "This header is way too long for the field";

        var ex = Assert.Throws<FieldOverflowException>
        (
            () => string.Concat(FixedWidthLineParser.FormatHeaderSegments(fieldMap, badConverter))
        );

        Assert.Equal
        (
            "FirstName",
            ex.PropertyName
        );
        Assert.Equal
        (
            10,
            ex.FieldLength
        );
        Assert.True(ex.ActualLength > 10);
    }



    [Fact]
    public void ParseLine_and_FormatSegments_round_trip_is_stable()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var original = new SimpleRecord { FirstName = "Jane", LastName = "Doe", Age = 30 };

        var line = string.Concat(FixedWidthLineParser.FormatSegments(original, fieldMap, FixedWidthConverter.Strict));
        var parsed = FixedWidthLineParser.ParseLine<SimpleRecord>
        (
            line,
            1,
            fieldMap
        );

        Assert.Equal
        (
            original.FirstName,
            parsed.FirstName
        );
        Assert.Equal
        (
            original.LastName,
            parsed.LastName
        );
        Assert.Equal
        (
            original.Age,
            parsed.Age
        );
    }



    // ------------------------------------------------------------------
    // FormatSeparatorSegments
    // ------------------------------------------------------------------

    [Fact]
    public void FormatSeparatorSegments_when_called_returns_separator_chars_matching_field_widths()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var segments = FixedWidthLineParser.FormatSeparatorSegments(fieldMap, '-');
        var line = string.Concat(segments);

        Assert.Equal
        (
            "----------" + "----------" + "-----",
            line
        );
    }



    // ------------------------------------------------------------------
    // WriteRecord — direct TextWriter output
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_called_writes_formatted_record_to_writer()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "Alice", LastName = "Jones", Age = 30 };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: null);

        Assert.Equal
        (
            "Alice     Jones     00030",
            writer.ToString()
        );
    }



    [Fact]
    public void WriteRecord_when_field_delimiter_is_set_inserts_delimiter_between_fields()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "Alice", LastName = "Jones", Age = 30 };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: "|");

        Assert.Contains("|", writer.ToString());
    }



    // ------------------------------------------------------------------
    // WriteHeader — direct TextWriter output
    // ------------------------------------------------------------------

    [Fact]
    public void WriteHeader_when_called_writes_header_labels_to_writer()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteHeader(writer, fieldMap, FixedWidthConverter.StrictHeader, fieldDelimiter: null);

        var header = writer.ToString();
        Assert.Contains("FirstName", header);
        Assert.Contains("LastName", header);
        Assert.Contains("Age", header);
    }



    // ------------------------------------------------------------------
    // WriteSeparator — direct TextWriter output
    // ------------------------------------------------------------------

    [Fact]
    public void WriteSeparator_when_called_writes_separator_chars_to_writer()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteSeparator(writer, fieldMap, '-', fieldDelimiter: null);

        Assert.Equal
        (
            "-------------------------",
            writer.ToString()
        );
    }



    [Fact]
    public void WriteRecord_when_custom_converter_returns_string_longer_than_field_width_throws_FieldOverflowException()
    {
        // Covers the safety-net throw inside WriteFieldSegment — fires when a
        // custom value converter bypasses length validation and returns an
        // overlong string on the direct-write path.
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var record = new SimpleRecord { FirstName = "John", LastName = "Smith", Age = 1 };
        var writer = new System.IO.StringWriter();

        Func<object, FieldContext, string> badConverter = (_, _) => "This string is far too long";

        var ex = Assert.Throws<FieldOverflowException>
        (
            () => FixedWidthLineParser.WriteRecord(writer, record, fieldMap, badConverter, fieldDelimiter: null)
        );

        Assert.Equal
        (
            "FirstName",
            ex.PropertyName
        );
        Assert.Equal
        (
            10,
            ex.FieldLength
        );
        Assert.True(ex.ActualLength > 10);
    }



    [Fact]
    public void WriteHeader_when_custom_header_converter_returns_string_longer_than_field_width_throws_FieldOverflowException()
    {
        // Covers the overflow throw inside WriteHeaderSegmentTo — fires when a
        // custom header converter returns an overlong label on the direct-write path.
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var writer = new System.IO.StringWriter();

        Func<string, FieldContext, string> badConverter = (_, _) => "This header is way too long for the field";

        var ex = Assert.Throws<FieldOverflowException>
        (
            () => FixedWidthLineParser.WriteHeader(writer, fieldMap, badConverter, fieldDelimiter: null)
        );

        Assert.Equal
        (
            "FirstName",
            ex.PropertyName
        );
        Assert.Equal
        (
            10,
            ex.FieldLength
        );
        Assert.True(ex.ActualLength > 10);
    }



    [ExcludeFromCodeCoverage]
    private class WideFieldRecord
    {
        // Exceeds the 256-char stackalloc threshold in WriteFieldSegment so the
        // pooled-buffer / per-WritePadding fallback path is exercised.
        [FixedWidthField(0, 300)]
        public string Wide { get; set; } = string.Empty;
    }



    [Fact]
    public void WriteRecord_when_field_width_exceeds_stackalloc_threshold_uses_writepadding_fallback_left_aligned()
    {
        // Covers the non-stackalloc branch of WriteFieldSegment: when attr.Length
        // is greater than the 256-char stackalloc cap, the writer falls back to
        // direct value + WritePadding writes. This test verifies the left-aligned
        // (default) path; pad is space, value is on the left.
        var fieldMap = FieldMap.GetResult<WideFieldRecord>();
        var record = new WideFieldRecord { Wide = "value" };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: null);

        var output = writer.ToString();
        Assert.Equal(300, output.Length);
        Assert.StartsWith("value", output);
        Assert.Equal(new string(' ', 295), output.Substring(5));
    }



    [ExcludeFromCodeCoverage]
    private class WideRightAlignedRecord
    {
        [FixedWidthField(0, 300, Alignment = FieldAlignment.Right, Pad = '0')]
        public string Wide { get; set; } = string.Empty;
    }



    [Fact]
    public void WriteRecord_when_field_width_exceeds_stackalloc_threshold_uses_writepadding_fallback_right_aligned()
    {
        // Companion to the left-aligned variant — verifies the right-aligned
        // branch of the non-stackalloc fallback (WritePadding fires before the
        // value is written).
        var fieldMap = FieldMap.GetResult<WideRightAlignedRecord>();
        var record = new WideRightAlignedRecord { Wide = "42" };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: null);

        var output = writer.ToString();
        Assert.Equal(300, output.Length);
        Assert.EndsWith("42", output);
        Assert.Equal(new string('0', 298), output.Substring(0, 298));
    }



    [Fact]
    public void WriteSeparator_when_field_delimiter_is_set_inserts_delimiter_between_separators()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteSeparator(writer, fieldMap, '-', fieldDelimiter: "|");

        Assert.Contains("|", writer.ToString());
    }



    // ------------------------------------------------------------------
    // WriteRecord with skip columns — gap and trailing delimiters
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_skip_columns_present_pads_gaps_with_spaces()
    {
        var fieldMap = FieldMap.GetResult<WriteSkipRecord>();
        var record = new WriteSkipRecord { Name = "Test", Value = 1 };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: null);

        Assert.Equal
        (
            fieldMap.ExpectedLineWidth,
            writer.ToString().Length
        );
    }



    [Fact]
    public void WriteRecord_when_skip_columns_and_delimiter_present_emits_gap_delimiters()
    {
        var fieldMap = FieldMap.GetResult<WriteSkipRecord>();
        var record = new WriteSkipRecord { Name = "Test", Value = 1 };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: "|");

        var output = writer.ToString();
        var delimiterCount = output.Split('|').Length - 1;
        var expectedDelimiters = Math.Max(0, fieldMap.TotalColumnCount - 1);
        Assert.Equal(expectedDelimiters, delimiterCount);
    }



    // ------------------------------------------------------------------
    // FormatSegments with skip columns — trailing padding
    // ------------------------------------------------------------------

    [Fact]
    public void FormatSegments_when_skip_columns_are_trailing_pads_remaining_width()
    {
        var fieldMap = FieldMap.GetResult<TrailingSkipRecord>();
        var record = new TrailingSkipRecord { Name = "Test" };

        var segments = FixedWidthLineParser.FormatSegments(record, fieldMap, FixedWidthConverter.Strict);
        var line = string.Concat(segments);

        Assert.Equal
        (
            fieldMap.ExpectedLineWidth,
            line.Length
        );
    }



    // ------------------------------------------------------------------
    // FormatHeaderSegments — with skip columns
    // ------------------------------------------------------------------

    [Fact]
    public void FormatHeaderSegments_when_called_returns_header_labels_padded_to_field_widths()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();

        var segments = FixedWidthLineParser.FormatHeaderSegments(fieldMap, FixedWidthConverter.StrictHeader);
        var line = string.Concat(segments);

        Assert.Equal(25, line.Length);
        Assert.Contains("FirstName", line);
        Assert.Contains("LastName", line);
        Assert.Contains("Age", line);
    }



    [Fact]
    public void FormatHeaderSegments_when_skip_columns_present_fills_gaps_with_spaces()
    {
        var fieldMap = FieldMap.GetResult<WriteSkipRecord>();

        var segments = FixedWidthLineParser.FormatHeaderSegments(fieldMap, FixedWidthConverter.StrictHeader);
        var line = string.Concat(segments);

        Assert.Equal
        (
            fieldMap.ExpectedLineWidth,
            line.Length
        );
    }



    // ------------------------------------------------------------------
    // WriteHeader with skip columns and delimiters
    // ------------------------------------------------------------------

    [Fact]
    public void WriteHeader_when_skip_columns_and_delimiter_present_emits_gap_delimiters()
    {
        var fieldMap = FieldMap.GetResult<WriteSkipRecord>();
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteHeader(writer, fieldMap, FixedWidthConverter.StrictHeader, fieldDelimiter: "|");

        var output = writer.ToString();
        var delimiterCount = output.Split('|').Length - 1;
        var expectedDelimiters = Math.Max(0, fieldMap.TotalColumnCount - 1);
        Assert.Equal(expectedDelimiters, delimiterCount);
    }



    // ------------------------------------------------------------------
    // WriteSeparator with skip columns and delimiters
    // ------------------------------------------------------------------

    [Fact]
    public void WriteSeparator_when_skip_columns_and_delimiter_present_emits_gap_delimiters()
    {
        var fieldMap = FieldMap.GetResult<WriteSkipRecord>();
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteSeparator(writer, fieldMap, '-', fieldDelimiter: "|");

        var output = writer.ToString();
        var delimiterCount = output.Split('|').Length - 1;
        var expectedDelimiters = Math.Max(0, fieldMap.TotalColumnCount - 1);
        Assert.Equal(expectedDelimiters, delimiterCount);
    }



    // ------------------------------------------------------------------
    // WriteTrailingDelimiters — trailing skip columns
    // ------------------------------------------------------------------

    [Fact]
    public void WriteRecord_when_trailing_skip_columns_and_delimiter_emits_trailing_delimiters()
    {
        var fieldMap = FieldMap.GetResult<TrailingSkipRecord>();
        var record = new TrailingSkipRecord { Name = "Test" };
        var writer = new System.IO.StringWriter();

        FixedWidthLineParser.WriteRecord(writer, record, fieldMap, FixedWidthConverter.Strict, fieldDelimiter: "|");

        var output = writer.ToString();
        var delimiterCount = output.Split('|').Length - 1;
        var expectedDelimiters = Math.Max(0, fieldMap.TotalColumnCount - 1);
        Assert.Equal(expectedDelimiters, delimiterCount);
    }



    // ------------------------------------------------------------------
    // ParseLine with custom ValueParser
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_custom_valueParser_is_provided_uses_custom_parser()
    {
        var fieldMap = FieldMap.GetResult<SimpleRecord>();
        var line = "John      Smith     00042";

        var record = FixedWidthLineParser.ParseLine<SimpleRecord>
        (
            line,
            lineNumber: 1,
            fieldMap,
            fieldDelimiter: null,
            valueParser: (value, context) => FixedWidthConverter.ParseValue
            (
                value,
                context.PropertyType,
                context.Format,
                null
            )
        );

        Assert.Equal("John", record.FirstName);
        Assert.Equal(42, record.Age);
    }



    // ------------------------------------------------------------------
    // FormatSeparatorSegments with trailing skip columns
    // ------------------------------------------------------------------

    [Fact]
    public void FormatSeparatorSegments_when_trailing_skip_columns_present_fills_trailing_width()
    {
        var fieldMap = FieldMap.GetResult<TrailingSkipRecord>();

        var segments = FixedWidthLineParser.FormatSeparatorSegments(fieldMap, '=');
        var line = string.Concat(segments);

        Assert.Equal
        (
            fieldMap.ExpectedLineWidth,
            line.Length
        );
    }



    // ------------------------------------------------------------------
    // Test POCOs
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    private class WriteSkipRecord
    {
        [FixedWidthField(0, 10)]
        public string Name { get; set; } = string.Empty;



        [FixedWidthSkip(1, 5)]
        [FixedWidthField(2, 5, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Value { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private class TrailingSkipRecord
    {
        [FixedWidthField(0, 10)]
        public string Name { get; set; } = string.Empty;



        [FixedWidthSkip(1, 5)]
        public string Unused { get; set; } = string.Empty;
    }
}



public class FixedWidthConverterTests
{
    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static FieldContext MakeContext
    (
        string propertyName,
        int fieldLength,
        string? format = null
    )
        => new FieldContext
        (
            propertyName,
            typeof(object),
            fieldLength,
            ' ',
            FieldAlignment.Left,
            format,
            propertyName
        );


    // ------------------------------------------------------------------
    // ConvertToString — format required for date/time types
    // ------------------------------------------------------------------

    [Fact]
    public void ConvertToString_when_value_is_DateTime_and_Format_is_null_throws_InvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ConvertToString(new DateTime(1990, 1, 15), MakeContext("BirthDate", 8)));

        Assert.Contains
        (
            "Format",
            ex.Message
        );
    }



    [Fact]
    public void ConvertToString_when_value_is_DateTimeOffset_and_Format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ConvertToString(DateTimeOffset.UtcNow, MakeContext("Timestamp", 20)));
    }



    [Fact]
    public void ConvertToString_when_value_is_TimeSpan_and_Format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ConvertToString(TimeSpan.FromHours(1.5), MakeContext("Duration", 6)));
    }



    [Fact]
    public void ConvertToString_when_value_is_DateTime_and_Format_is_set_uses_InvariantCulture()
    {
        var dt = new DateTime
        (
            1990,
            1,
            15
        );
        var result = FixedWidthConverter.ConvertToString
        (
            dt,
            MakeContext
            (
                "BirthDate",
                8,
                "yyyyMMdd"
            )
        );

        Assert.Equal
        (
            "19900115",
            result
        );
    }



    [Fact]
    public void ConvertToString_when_value_is_decimal_and_Format_is_null_uses_InvariantCulture()
    {
        var result = FixedWidthConverter.ConvertToString
        (
            1234.56m,
            MakeContext
            (
                "Amount",
                10
            )
        );

        Assert.Equal
        (
            "1234.56",
            result
        );
    }



    [Fact]
    public void ConvertToString_when_value_is_decimal_and_Format_is_set_applies_the_Format()
    {
        var result = FixedWidthConverter.ConvertToString
        (
            1234.5m,
            MakeContext
            (
                "Amount",
                10,
                "F2"
            )
        );

        Assert.Equal
        (
            "1234.50",
            result
        );
    }



    [Fact]
    public void ConvertToString_when_value_is_int_converts_to_string()
    {
        var result = FixedWidthConverter.ConvertToString
        (
            42,
            MakeContext
            (
                "Age",
                3
            )
        );

        Assert.Equal
        (
            "42",
            result
        );
    }



    [Fact]
    public void ConvertToString_when_value_is_null_returns_empty_string()
    {
        var result = FixedWidthConverter.ConvertToString
        (
            value: null,
            MakeContext
            (
                "Name",
                10
            )
        );

        Assert.Equal
        (
            string.Empty,
            result
        );
    }



    // ------------------------------------------------------------------
    // ParseValue — format required for date/time types (symmetric with ConvertToString)
    // ------------------------------------------------------------------

    [Fact]
    public void ParseValue_when_targetType_is_DateTime_and_format_is_null_throws_InvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("19900115".AsMemory(), typeof(DateTime), format: null));

        Assert.Contains
        (
            "Format",
            ex.Message
        );
    }



    [Fact]
    public void ParseValue_when_targetType_is_DateTimeOffset_and_format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("20260101T120000".AsMemory(), typeof(DateTimeOffset), format: null));
    }



    [Fact]
    public void ParseValue_when_targetType_is_TimeSpan_and_format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("01:30:00".AsMemory(), typeof(TimeSpan), format: null));
    }



    [Fact]
    public void ParseValue_when_targetType_is_DateTime_and_format_is_set_parses_correctly()
    {
        var result = FixedWidthConverter.ParseValue
        (
            "19900115".AsMemory(),
            typeof(DateTime),
            "yyyyMMdd"
        );

        Assert.Equal
        (
            new DateTime
            (
                1990,
                1,
                15
            ),
            result
        );
    }



    [Fact]
    public void ParseValue_when_targetType_is_DateTimeOffset_and_format_is_set_parses_correctly()
    {
        var result = (DateTimeOffset)FixedWidthConverter.ParseValue
        (
            "20260101T1200".AsMemory(),
            typeof(DateTimeOffset),
            "yyyyMMddTHHmm"
        );

        Assert.Equal
        (
            2026,
            result.Year
        );
        Assert.Equal
        (
            1,
            result.Month
        );
        Assert.Equal
        (
            1,
            result.Day
        );
        Assert.Equal
        (
            12,
            result.Hour
        );
        Assert.Equal
        (
            0,
            result.Minute
        );
    }



    [Fact]
    public void ParseValue_when_targetType_is_TimeSpan_and_format_is_set_parses_correctly()
    {
        var result = FixedWidthConverter.ParseValue
        (
            "013000".AsMemory(),
            typeof(TimeSpan),
            "hhmmss"
        );

        Assert.Equal
        (
            new TimeSpan
            (
                1,
                30,
                0
            ),
            result
        );
    }



    [Fact]
    public void Truncate_when_value_fits_within_field_width_returns_value_unchanged()
    {
        var ctx = new FieldContext
        (
            "Name",
            typeof(string),
            10,
            ' ',
            FieldAlignment.Left,
            format: null,
            "Name"
        );
        var result = FixedWidthConverter.Truncate
        (
            "John",
            ctx
        );

        Assert.Equal
        (
            "John",
            result
        );
    }



    [Fact]
    public void TruncateHeader_when_label_fits_within_field_width_returns_label_unchanged()
    {
        var ctx = new FieldContext
        (
            "Name",
            typeof(string),
            10,
            ' ',
            FieldAlignment.Left,
            format: null,
            "Name"
        );
        var result = FixedWidthConverter.TruncateHeader
        (
            "Name",
            ctx
        );

        Assert.Equal
        (
            "Name",
            result
        );
    }



    [Fact]
    public void TruncateHeader_when_label_exceeds_field_width_truncates_to_field_width()
    {
        var ctx = new FieldContext
        (
            "Name",
            typeof(string),
            4,
            ' ',
            FieldAlignment.Left,
            format: null,
            "Name"
        );
        var result = FixedWidthConverter.TruncateHeader
        (
            "FirstName",
            ctx
        );

        Assert.Equal
        (
            "Firs",
            result
        );
    }



    [Fact]
    public void ParseValue_when_targetType_is_not_a_known_type_uses_TypeConverter()
    {
        // char is not handled by the date/time or string branches —
        // falls through to TypeDescriptor.GetConverter path.
        var result = FixedWidthConverter.ParseValue
        (
            "A".AsMemory(),
            typeof(char),
            format: null
        );

        Assert.Equal
        (
            'A',
            result
        );
    }
}



public class NullableParsingTests
{
    // ------------------------------------------------------------------
    // Test POCO
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    private class NullableRecord
    {
        [FixedWidthField(0, 5)]
        public int? NullableInt { get; set; }



        [FixedWidthField(1, 10)]
        public string NullableString { get; set; } = string.Empty;



        [FixedWidthField(2, 8, Format = "yyyyMMdd")]
        public DateTime? NullableDateTime { get; set; }



        [FixedWidthField(3, 20, Format = "yyyy-MM-ddTHH:mm:ss")]
        public DateTimeOffset? NullableDateTimeOffset { get; set; }



        [FixedWidthField(4, 8, Format = @"hh\:mm\:ss")]
        public TimeSpan? NullableTimeSpan { get; set; }



        [FixedWidthField(5, 10)]
        public decimal? NullableDecimal { get; set; }



        [FixedWidthField(6, 5)]
        public bool? NullableBool { get; set; }
    }



    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static FieldMapResult Descriptors
        => FieldMap.GetResult<NullableRecord>();


    private static NullableRecord Parse(string line)
        => FixedWidthLineParser.ParseLine<NullableRecord>
        (
            line,
            1,
            Descriptors
        );


    // ------------------------------------------------------------------
    // Empty fields → null
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_nullable_int_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableInt);
    }



    [Fact]
    public void ParseLine_when_nullable_string_field_is_empty_returns_empty_string()
    {
        // string with TrimValue=true trims to empty string (not null).
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Equal
        (
            string.Empty,
            record.NullableString
        );
    }



    [Fact]
    public void ParseLine_when_nullable_DateTime_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableDateTime);
    }



    [Fact]
    public void ParseLine_when_nullable_DateTimeOffset_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableDateTimeOffset);
    }



    [Fact]
    public void ParseLine_when_nullable_TimeSpan_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableTimeSpan);
    }



    [Fact]
    public void ParseLine_when_nullable_decimal_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableDecimal);
    }



    [Fact]
    public void ParseLine_when_nullable_bool_field_is_empty_returns_null()
    {
        var record = Parse("     " + new string(' ', 10 + 8 + 20 + 8 + 10 + 5));

        Assert.Null(record.NullableBool);
    }



    // ------------------------------------------------------------------
    // Non-empty fields → correct value
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_nullable_int_field_has_a_value_parses_correctly()
    {
        var line = "42   " + new string
        (
            ' ',
            10 + 8 + 20 + 8 + 10 + 5
        );
        var record = Parse(line);

        Assert.Equal
        (
            42,
            record.NullableInt
        );
    }



    [Fact]
    public void ParseLine_when_nullable_DateTime_field_has_a_value_parses_correctly()
    {
        var line =
            "     " + // NullableInt
            "          " + // NullableString
            "19900115" + // NullableDateTime
            new string
            (
                ' ',
                20 + 8 + 10 + 5
            );
        var record = Parse(line);

        Assert.Equal
        (
            new DateTime
            (
                1990,
                1,
                15
            ),
            record.NullableDateTime
        );
    }



    [Fact]
    public void ParseLine_when_nullable_TimeSpan_field_has_a_value_parses_correctly()
    {
        var line =
            "     " + // NullableInt
            "          " + // NullableString
            "        " + // NullableDateTime (empty)
            "                    " + // NullableDateTimeOffset (empty)
            "13:45:00" + // NullableTimeSpan
            new string
            (
                ' ',
                10 + 5
            );
        var record = Parse(line);

        Assert.Equal
        (
            new TimeSpan
            (
                13,
                45,
                0
            ),
            record.NullableTimeSpan
        );
    }



    [Fact]
    public void ParseLine_when_nullable_decimal_field_has_a_value_parses_correctly()
    {
        var line =
            "     " + // NullableInt
            "          " + // NullableString
            "        " + // NullableDateTime (empty)
            "                    " + // NullableDateTimeOffset (empty)
            "        " + // NullableTimeSpan (empty)
            "1234.56   " + // NullableDecimal
            "     "; // NullableBool
        var record = Parse(line);

        Assert.Equal
        (
            1234.56m,
            record.NullableDecimal
        );
    }



    [Fact]
    public void ParseLine_when_nullable_bool_field_has_a_value_parses_correctly()
    {
        var line =
            "     " + // NullableInt
            "          " + // NullableString
            "        " + // NullableDateTime (empty)
            "                    " + // NullableDateTimeOffset (empty)
            "        " + // NullableTimeSpan (empty)
            "          " + // NullableDecimal (empty)
            "True "; // NullableBool
        var record = Parse(line);

        Assert.Equal
        (
            true,
            record.NullableBool
        );
    }
}



public class LineLengthValidationTests
{
    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    // PersonRecord: FirstName(10) + LastName(10) + Age(3) = 23 chars expected
    private static FieldMapResult FieldMap => Parsing.FieldMap.GetResult<PersonRecord>();



    // ------------------------------------------------------------------
    // No delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_line_is_the_exact_expected_length_succeeds()
    {
        var line = "John      Smith     042";
        var record = FixedWidthLineParser.ParseLine<PersonRecord>
        (
            line,
            1,
            FieldMap
        );

        Assert.Equal
        (
            "John",
            record.FirstName
        );
        Assert.Equal
        (
            "Smith",
            record.LastName
        );
        Assert.Equal
        (
            42,
            record.Age
        );
    }



    [Fact]
    public void ParseLine_when_line_is_longer_than_expected_ignores_trailing_characters()
    {
        // Extra trailing characters should be silently ignored.
        const string line = "John      Smith     042EXTRAGARBAGE";
        var record = FixedWidthLineParser.ParseLine<PersonRecord>
        (
            line,
            1,
            FieldMap
        );

        Assert.Equal
        (
            "John",
            record.FirstName
        );
        Assert.Equal
        (
            42,
            record.Age
        );
    }



    [Fact]
    public void ParseLine_when_line_is_too_short_throws_LineTooShortException()
    {
        var line = "John      Smith"; // only 15 chars, need 23

        var ex = Assert.Throws<LineTooShortException>( () => FixedWidthLineParser.ParseLine<PersonRecord>(line, 7, FieldMap));

        Assert.Equal
        (
            7,
            ex.LineNumber
        );
        Assert.Equal
        (
            line,
            ex.LineContent
        );
        Assert.Equal
        (
            23,
            ex.ExpectedWidth
        );
        Assert.Equal
        (
            15,
            ex.ActualWidth
        );
    }



    [Fact]
    public void ParseLine_when_line_is_empty_throws_LineTooShortException()
    {
        var ex = Assert.Throws<LineTooShortException>( () => FixedWidthLineParser.ParseLine<PersonRecord>(string.Empty, 1, FieldMap));

        Assert.Equal
        (
            23,
            ex.ExpectedWidth
        );
        Assert.Equal
        (
            0,
            ex.ActualWidth
        );
    }



    // ------------------------------------------------------------------
    // With delimiter
    // ------------------------------------------------------------------

    [Fact]
    public void ParseLine_when_FieldDelimiter_is_set_and_line_is_the_exact_expected_length_succeeds()
    {
        // PersonRecord with " | " delimiter:
        // 10 + 3 + 10 + 3 + 3 = 29 chars  (2 delimiters of 3 chars each)
        var line = "John       | Smith      | 042";
        var record = FixedWidthLineParser.ParseLine<PersonRecord>
        (
            line,
            1,
            FieldMap,
            " | "
        );

        Assert.Equal
        (
            "John",
            record.FirstName
        );
        Assert.Equal
        (
            "Smith",
            record.LastName
        );
        Assert.Equal
        (
            42,
            record.Age
        );
    }



    [Fact]
    public void ParseLine_when_FieldDelimiter_is_set_and_line_is_too_short_throws_LineTooShortException()
    {
        // Line missing the Age field entirely
        var line = "John       | Smith     ";

        var ex = Assert.Throws<LineTooShortException>( () => FixedWidthLineParser.ParseLine<PersonRecord>(line, 2, FieldMap, " | "));

        Assert.Equal
        (
            29,
            ex.ExpectedWidth
        ); // 23 + 6 delimiter
        Assert.Equal
        (
            line.Length,
            ex.ActualWidth
        );
    }



    [Fact]
    public void ParseLine_when_FieldDelimiter_is_set_and_line_is_longer_than_expected_ignores_trailing_characters()
    {
        var line = "John       | Smith      | 042EXTRA";
        var record = FixedWidthLineParser.ParseLine<PersonRecord>
        (
            line,
            1,
            FieldMap,
            " | "
        );

        Assert.Equal
        (
            "John",
            record.FirstName
        );
        Assert.Equal
        (
            42,
            record.Age
        );
    }
}
