using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers the public schema-introspection API <see cref="FixedWidthSchema"/> (#22).
/// </summary>
public class FixedWidthSchemaTests
{
    [ExcludeFromCodeCoverage]
    private record SkipLayoutRecord
    {
        [FixedWidthField(0, 10)]
        public string FirstName { get; set; } = string.Empty;

        [FixedWidthSkip(1, 8, Message = "DOB")]
        [FixedWidthField(2, 6)]
        public string EmployeeNumber { get; set; } = string.Empty;
    }



    [ExcludeFromCodeCoverage]
    private record TypedRecord
    {
        [FixedWidthField(0, 8, Alignment = FieldAlignment.Right, Pad = '0')]
        public int Id { get; set; }

        [FixedWidthField(1, 12, NumberStyles = NumberStyles.Any, Format = "0.00")]
        public decimal Amount { get; set; }

        [FixedWidthField(2, 8, Format = "yyyyMMdd", Header = "TxnDate")]
        public DateTime Date { get; set; }
    }



    [Fact]
    public void For_resolves_positions_widths_and_totals()
    {
        // PersonRecord: FirstName(0,10) LastName(1,10) Age(2,3 right '0')
        var schema = FixedWidthSchema.For<PersonRecord>();

        Assert.Equal(typeof(PersonRecord), schema.RecordType);
        Assert.Equal(23, schema.ExpectedLineWidth);
        Assert.Equal(3, schema.TotalColumnCount);
        Assert.Equal(3, schema.FieldCount);
        Assert.Equal(0, schema.SkipCount);

        var age = schema.Fields[2];
        Assert.Equal("Age", age.Name);
        Assert.Equal(20, age.StartPosition);
        Assert.Equal(22, age.EndPosition);
        Assert.Equal(3, age.Length);
        Assert.Equal(typeof(int), age.PropertyType);
        Assert.Equal(FieldAlignment.Right, age.Alignment);
        Assert.Equal('0', age.Pad);
        Assert.False(age.IsSkip);
    }



    [Fact]
    public void Fields_include_skip_columns_in_position_order()
    {
        // FirstName(0,10) [skip DOB](1,8) EmployeeNumber(2,6)
        var schema = FixedWidthSchema.For<SkipLayoutRecord>();

        Assert.Equal(3, schema.TotalColumnCount);
        Assert.Equal(2, schema.FieldCount);
        Assert.Equal(1, schema.SkipCount);
        Assert.Equal(24, schema.ExpectedLineWidth);

        var skip = schema.Fields[1];
        Assert.True(skip.IsSkip);
        Assert.Equal(1, skip.ColumnIndex);
        Assert.Null(skip.Name);
        Assert.Null(skip.PropertyType);
        Assert.Equal("DOB", skip.SkipMessage);
        Assert.Equal(10, skip.StartPosition);
        Assert.Equal(17, skip.EndPosition);
        Assert.Equal(8, skip.Length);

        var employeeNumber = schema.Fields[2];
        Assert.Equal("EmployeeNumber", employeeNumber.Name);
        Assert.Equal(18, employeeNumber.StartPosition);
    }



    [Fact]
    public void Field_metadata_reflects_format_header_and_numberstyles()
    {
        var schema = FixedWidthSchema.For<TypedRecord>();

        var amount = schema.Fields.Single(f => string.Equals(f.Name, "Amount", StringComparison.Ordinal));
        Assert.Equal("0.00", amount.Format);
        Assert.Equal(NumberStyles.Any, amount.NumberStyles);

        var date = schema.Fields.Single(f => string.Equals(f.Name, "Date", StringComparison.Ordinal));
        Assert.Equal("yyyyMMdd", date.Format);
        Assert.Equal("TxnDate", date.Header);

        var id = schema.Fields.Single(f => string.Equals(f.Name, "Id", StringComparison.Ordinal));
        // No explicit NumberStyles -> null (uses the type's natural style).
        Assert.Null(id.NumberStyles);
        // Header defaults to the property name when not specified.
        Assert.Equal("Id", id.Header);
    }



    [Fact]
    public void For_Type_overload_matches_generic()
    {
        var generic = FixedWidthSchema.For<PersonRecord>();
        var byType = FixedWidthSchema.For(typeof(PersonRecord));

        Assert.Equal(generic.ExpectedLineWidth, byType.ExpectedLineWidth);
        Assert.Equal(generic.TotalColumnCount, byType.TotalColumnCount);
    }



    [Fact]
    public void For_null_type_throws()
    {
        Assert.Throws<ArgumentNullException>(() => FixedWidthSchema.For(null!));
    }



    [Fact]
    public void ToDiagram_renders_layout_table_with_skips_and_footer()
    {
        var diagram = FixedWidthSchema.For<SkipLayoutRecord>().ToDiagram();

        Assert.StartsWith("Position  Field", diagram, StringComparison.Ordinal);
        Assert.Contains("[skip]", diagram, StringComparison.Ordinal);
        Assert.Contains("EmployeeNumber", diagram, StringComparison.Ordinal);
        Assert.Contains("Total width: 24  |  Columns: 3 (2 fields + 1 skip)  |  Delimiter: none", diagram, StringComparison.Ordinal);
        Assert.DoesNotContain(" \n", diagram, StringComparison.Ordinal);   // no trailing whitespace
    }



    [Fact]
    public void ToDiagram_shows_alignment_pad_and_format_for_fields()
    {
        var diagram = FixedWidthSchema.For<TypedRecord>().ToDiagram();

        Assert.Contains("Id", diagram, StringComparison.Ordinal);
        Assert.Contains("Int32", diagram, StringComparison.Ordinal);
        Assert.Contains("Right", diagram, StringComparison.Ordinal);
        Assert.Contains("'0'", diagram, StringComparison.Ordinal);
        Assert.Contains("yyyyMMdd", diagram, StringComparison.Ordinal);   // Date format
        Assert.Contains("3 fields + 0 skips", diagram, StringComparison.Ordinal);
    }
}
