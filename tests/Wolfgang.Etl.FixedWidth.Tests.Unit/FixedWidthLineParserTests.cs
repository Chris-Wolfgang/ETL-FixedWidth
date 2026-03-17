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
        var ex = Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("19900115", typeof(DateTime), format: null));

        Assert.Contains
        (
            "Format",
            ex.Message
        );
    }



    [Fact]
    public void ParseValue_when_targetType_is_DateTimeOffset_and_format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("20260101T120000", typeof(DateTimeOffset), format: null));
    }



    [Fact]
    public void ParseValue_when_targetType_is_TimeSpan_and_format_is_null_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>( () => FixedWidthConverter.ParseValue("01:30:00", typeof(TimeSpan), format: null));
    }



    [Fact]
    public void ParseValue_when_targetType_is_DateTime_and_format_is_set_parses_correctly()
    {
        var result = FixedWidthConverter.ParseValue
        (
            "19900115",
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
            "20260101T1200",
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
            "013000",
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
            "A",
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
