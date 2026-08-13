using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Wolfgang.Etl.FixedWidth.Exceptions;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthDataReader{TRecord}"/> (#26) — the forward-only <see cref="IDataReader"/>
/// that serves field values directly from the parsed line without allocating a record per row.
/// </summary>
public sealed class FixedWidthDataReaderTests
{
    [ExcludeFromCodeCoverage]
    private sealed record Person
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthField(1, 10)]
        public string LastName { get; set; } = string.Empty;

        [FixedWidthField(2, 3, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Age { get; set; }

        [FixedWidthField(3, 5)]
        public int? Score { get; set; }
    }


    // 10 + 10 + 3 + 5 = 28 chars/line.
    private static string Line(string first, string last, int age, string score)
        => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}{3,-5}", first, last, age, score);

    private static FixedWidthDataReader<Person> Reader(params string[] lines)
        => new(new StringReader(string.Join("\n", lines)));

    // Source reader for tests that also set init-only config in an object initializer
    // (init props can't be assigned on the Reader(...) factory result).
    private static StringReader Src(params string[] lines)
        => new(string.Join("\n", lines));


    [Fact]
    public void Read_serves_fields_by_ordinal_name_and_typed_accessor()
    {
        using var reader = Reader(Line("Alice", "Smith", 30, "42"));

        Assert.True(reader.Read());
        Assert.Equal("Alice", reader.GetString(0));
        Assert.Equal("Smith", reader["LastName"]);
        Assert.Equal(30, reader.GetInt32(2));
        Assert.Equal(30, reader.GetValue(reader.GetOrdinal("Age")));
        Assert.Equal(42, reader.GetValue(3));
        Assert.False(reader.Read());
    }


    [Fact]
    public void Field_metadata_matches_the_layout()
    {
        using var reader = Reader(Line("Alice", "Smith", 30, "42"));

        Assert.Equal(4, reader.FieldCount);
        Assert.Equal("FirstName", reader.GetName(0));
        Assert.Equal(2, reader.GetOrdinal("Age"));
        Assert.Equal(2, reader.GetOrdinal("age"));   // case-insensitive fallback
        Assert.Equal(typeof(string), reader.GetFieldType(0));
        Assert.Equal(typeof(int), reader.GetFieldType(2));
    }


    [Fact]
    public void IsDBNull_is_true_for_an_empty_nullable_field()
    {
        using var reader = Reader(Line("Bob", "Jones", 25, ""));   // Score blank

        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(3));
        Assert.Equal(DBNull.Value, reader.GetValue(3));
        Assert.False(reader.IsDBNull(0));
    }


    [Fact]
    public void HeaderLineCount_skips_leading_lines()
    {
        using var reader = new FixedWidthDataReader<Person>(Src("HEADER ROW IGNORED         00xxxxx", Line("Alice", "Smith", 30, "1")))
        {
            HeaderLineCount = 1,
        };

        Assert.True(reader.Read());
        Assert.Equal("Alice", reader.GetString(0));
        Assert.False(reader.Read());
    }


    [Fact]
    public void Skip_and_Maximum_item_counts_are_honored()
    {
        using var reader = new FixedWidthDataReader<Person>(Src(
            Line("A", "1", 1, "1"),
            Line("B", "2", 2, "2"),
            Line("C", "3", 3, "3"),
            Line("D", "4", 4, "4")))
        {
            SkipItemCount = 1,
            MaximumItemCount = 2,
        };

        Assert.True(reader.Read());
        Assert.Equal("B", reader.GetString(0));
        Assert.True(reader.Read());
        Assert.Equal("C", reader.GetString(0));
        Assert.False(reader.Read());   // D is beyond the max
    }


    [Fact]
    public void BlankLineHandling_Skip_ignores_blank_lines()
    {
        using var reader = new FixedWidthDataReader<Person>(Src(Line("A", "1", 1, "1"), string.Empty, Line("B", "2", 2, "2")))
        {
            BlankLineHandling = BlankLineHandling.Skip,
        };

        Assert.True(reader.Read());
        Assert.Equal("A", reader.GetString(0));
        Assert.True(reader.Read());
        Assert.Equal("B", reader.GetString(0));
        Assert.False(reader.Read());
    }


    [Fact]
    public void BlankLineHandling_ThrowException_throws()
    {
        // A zero-length line in the middle (a trailing empty line is swallowed by ReadLine).
        using var reader = new FixedWidthDataReader<Person>(Src(Line("A", "1", 1, "1"), string.Empty, Line("B", "2", 2, "2")))
        {
            BlankLineHandling = BlankLineHandling.ThrowException,
        };

        Assert.True(reader.Read());   // A
        Assert.Throws<LineTooShortException>(() => reader.Read());   // the blank line
    }


    [Fact]
    public void BlankLineHandling_ReturnDefault_yields_a_default_row()
    {
        using var reader = new FixedWidthDataReader<Person>(Src(Line("A", "1", 1, "1"), string.Empty, Line("B", "2", 2, "2")))
        {
            BlankLineHandling = BlankLineHandling.ReturnDefault,
        };

        Assert.True(reader.Read());   // A
        Assert.True(reader.Read());   // default row from the blank line
        Assert.True(reader.IsDBNull(0));   // reference-type (string) default -> null -> DBNull
        Assert.Equal(0, reader.GetInt32(2));   // value-type default
        Assert.True(reader.IsDBNull(3));        // nullable default -> null
        Assert.True(reader.Read());   // B
        Assert.Equal("B", reader.GetString(0));
    }


    [Fact]
    public void MalformedLineHandling_Skip_skips_short_lines()
    {
        using var reader = new FixedWidthDataReader<Person>(Src(Line("A", "1", 1, "1"), "TOO SHORT", Line("B", "2", 2, "2")))
        {
            MalformedLineHandling = MalformedLineHandling.Skip,
        };

        Assert.True(reader.Read());
        Assert.Equal("A", reader.GetString(0));
        Assert.True(reader.Read());
        Assert.Equal("B", reader.GetString(0));
        Assert.False(reader.Read());
    }


    [Fact]
    public void MalformedLineHandling_ThrowException_throws_on_a_short_line()
    {
        using var reader = Reader("TOO SHORT");

        Assert.Throws<LineTooShortException>(() => reader.Read());
    }


    [Fact]
    public void GetSchemaTable_reflects_the_layout()
    {
        using var reader = Reader(Line("A", "1", 1, "1"));

        var schema = reader.GetSchemaTable();

        Assert.Equal(4, schema.Rows.Count);
        Assert.Equal("FirstName", schema.Rows[0]["ColumnName"]);
        Assert.Equal(typeof(int), schema.Rows[2]["DataType"]);
        Assert.Equal(3, schema.Rows[2]["ColumnSize"]);
        Assert.False((bool)schema.Rows[2]["AllowDBNull"]);   // int (non-nullable)
        Assert.True((bool)schema.Rows[3]["AllowDBNull"]);    // int? (nullable)
    }


    [Fact]
    public void DataTable_Load_consumes_the_reader_end_to_end()
    {
        using var reader = Reader(Line("Alice", "Smith", 30, "42"), Line("Bob", "Jones", 25, ""));

        var table = new DataTable();
        table.Load(reader);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0]["FirstName"]);
        Assert.Equal(30, table.Rows[0]["Age"]);
        Assert.Equal(DBNull.Value, table.Rows[1]["Score"]);
    }


    [Fact]
    public void Stream_constructor_reads_records()
    {
        var bytes = Encoding.UTF8.GetBytes(Line("Alice", "Smith", 30, "7"));
        using var stream = new MemoryStream(bytes);
        using var reader = new FixedWidthDataReader<Person>(stream);

        Assert.True(reader.Read());
        Assert.Equal("Alice", reader.GetString(0));
    }


    [Fact]
    public void GetValues_fills_the_buffer_and_GetChars_reports_length()
    {
        using var reader = Reader(Line("Alice", "Smith", 30, "42"));
        Assert.True(reader.Read());

        var values = new object[4];
        Assert.Equal(4, reader.GetValues(values));
        Assert.Equal("Alice", values[0]);

        Assert.Equal(5, reader.GetChars(0, 0, null, 0, 0));   // "Alice".Length
    }


    [Fact]
    public void Reading_before_first_Read_or_after_Close_is_rejected()
    {
        var reader = Reader(Line("A", "1", 1, "1"));

        Assert.Throws<InvalidOperationException>(() => reader.GetValue(0));   // no current row yet

        reader.Dispose();
        Assert.True(reader.IsClosed);
        Assert.Throws<InvalidOperationException>(() => reader.Read());
    }


    [ExcludeFromCodeCoverage]
    private sealed record Wide
    {
        [FixedWidthField(0, 5)]
        public bool Flag { get; set; }

        [FixedWidthField(1, 4)]
        public byte B { get; set; }

        [FixedWidthField(2, 1)]
        public char C { get; set; }

        [FixedWidthField(3, 6)]
        public short S { get; set; }

        [FixedWidthField(4, 12)]
        public long L { get; set; }

        [FixedWidthField(5, 8)]
        public decimal D { get; set; }

        [FixedWidthField(6, 6)]
        public double Db { get; set; }

        [FixedWidthField(7, 6)]
        public float F { get; set; }

        [FixedWidthField(8, 8, Format = "yyyyMMdd")]
        public DateTime Dt { get; set; }

        [FixedWidthField(9, 36)]
        public Guid G { get; set; }
    }


    [Fact]
    public void Typed_accessors_return_the_field_clr_values()
    {
        var line =
            "True".PadRight(5) + "12".PadRight(4) + "X" + "123".PadRight(6) +
            "1234567890".PadRight(12) + "12.34".PadRight(8) + "1.5".PadRight(6) +
            "2.5".PadRight(6) + "20260101" + "00000000-0000-0000-0000-000000000000";
        using var reader = new FixedWidthDataReader<Wide>(new StringReader(line));

        Assert.True(reader.Read());
        Assert.True(reader.GetBoolean(0));
        Assert.Equal((byte)12, reader.GetByte(1));
        Assert.Equal('X', reader.GetChar(2));
        Assert.Equal((short)123, reader.GetInt16(3));
        Assert.Equal(1234567890L, reader.GetInt64(4));
        Assert.Equal(12.34m, reader.GetDecimal(5));
        Assert.Equal(1.5d, reader.GetDouble(6));
        Assert.Equal(2.5f, reader.GetFloat(7));
        Assert.Equal(new DateTime(2026, 1, 1), reader.GetDateTime(8));
        Assert.Equal(Guid.Empty, reader.GetGuid(9));
        Assert.Equal("Boolean", reader.GetDataTypeName(0));
        Assert.Equal(typeof(short), reader.GetFieldType(3));
    }


    [Fact]
    public void Non_forward_and_unsupported_members_behave_as_documented()
    {
        using var reader = Reader(Line("Alice", "Smith", 30, "1"));

        Assert.False(reader.NextResult());
        Assert.Equal(0, reader.Depth);
        Assert.Equal(-1, reader.RecordsAffected);

        Assert.True(reader.Read());
        Assert.Throws<NotSupportedException>(() => reader.GetBytes(0, 0, null, 0, 0));
        Assert.Throws<NotSupportedException>(() => reader.GetData(0));

        var buffer = new char[5];
        Assert.Equal(5, reader.GetChars(0, 0, buffer, 0, 5));
        Assert.Equal("Alice", new string(buffer));
    }


    [Fact]
    public void Argument_validation_and_unknown_field_names()
    {
        using var reader = Reader(Line("A", "1", 1, "1"));

        Assert.Throws<ArgumentNullException>(() => reader.GetOrdinal(null!));
        Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("Nope"));
        Assert.Throws<ArgumentNullException>(() => reader.GetValues(null!));
    }


    [Fact]
    public void MalformedLineHandling_ReturnDefault_yields_a_default_row()
    {
        using var reader = new FixedWidthDataReader<Person>(Src("SHORT", Line("A", "1", 1, "1")))
        {
            MalformedLineHandling = MalformedLineHandling.ReturnDefault,
        };

        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(0));   // default row from the short line
        Assert.True(reader.Read());
        Assert.Equal("A", reader.GetString(0));
    }


    [Fact]
    public void BlankLine_ReturnDefault_within_skip_budget_is_skipped()
    {
        using var reader = new FixedWidthDataReader<Person>(Src(string.Empty, Line("A", "1", 1, "1")))
        {
            BlankLineHandling = BlankLineHandling.ReturnDefault,
            SkipItemCount = 1,   // the blank consumes the skip budget, then A is served
        };

        Assert.True(reader.Read());
        Assert.Equal("A", reader.GetString(0));
        Assert.False(reader.Read());
    }


    [Fact]
    public void A_whitespace_only_line_is_a_data_line_not_a_blank_line()
    {
        // With BlankLineHandling.Skip a *blank* (zero-length) line would be skipped. A whitespace-only
        // line is NOT blank (matches the extractor) — it is a data line, so a short one is malformed
        // and hits MalformedLineHandling (default ThrowException) rather than being silently skipped.
        using var reader = new FixedWidthDataReader<Person>(Src("     "))
        {
            BlankLineHandling = BlankLineHandling.Skip,
        };

        Assert.Throws<LineTooShortException>(() => reader.Read());
    }


    [Fact]
    public void Accessing_values_after_Close_reports_the_closed_reader()
    {
        var reader = Reader(Line("A", "1", 1, "1"));
        Assert.True(reader.Read());

        reader.Close();

        var ex = Assert.Throws<InvalidOperationException>(() => reader.GetValue(0));
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
        reader.Close();   // idempotent
    }


    [Fact]
    public void Encoding_property_decodes_a_stream_with_a_non_default_encoding()
    {
        // 0xE9 is 'é' in Latin-1 but an invalid lead byte under the default UTF-8.
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        var data = latin1.GetBytes(Line("é", "X", 1, "1"));
        using var reader = new FixedWidthDataReader<Person>(new MemoryStream(data)) { Encoding = latin1 };

        Assert.True(reader.Read());
        Assert.Equal("é", reader.GetString(0));   // UTF-8 (the default) would have mis-decoded 0xE9
    }


    [Fact]
    public void Read_emits_a_start_log_when_a_logger_is_supplied()
    {
        var logger = new CapturingLogger<FixedWidthDataReader<Person>>();
        using var reader = new FixedWidthDataReader<Person>(new StringReader(Line("A", "1", 1, "1")), logger);

        reader.Read();

        Assert.Contains(logger.Messages, m => m.Contains("started", StringComparison.OrdinalIgnoreCase));
    }


    [ExcludeFromCodeCoverage]
    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public System.Collections.Generic.List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>
        (
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, exception));

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
