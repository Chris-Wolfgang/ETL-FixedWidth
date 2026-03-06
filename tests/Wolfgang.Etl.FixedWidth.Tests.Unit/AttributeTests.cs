using System;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

public class FixedWidthFieldAttributeTests
{
    [Fact]
    public void Constructor_when_index_is_negative_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthFieldAttribute(-1, 10));
    }



    [Fact]
    public void Constructor_when_length_is_zero_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthFieldAttribute(0, 0));
    }



    [Fact]
    public void Constructor_when_length_is_negative_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthFieldAttribute(0, -1));
    }



    [Fact]
    public void Constructor_sets_Index_and_Length()
    {
        var attr = new FixedWidthFieldAttribute
        (
            2,
            10
        );

        Assert.Equal
        (
            2,
            attr.Index
        );
        Assert.Equal
        (
            10,
            attr.Length
        );
    }



    [Fact]
    public void Header_Alignment_Pad_Format_TrimValue_have_expected_defaults()
    {
        var attr = new FixedWidthFieldAttribute
        (
            0,
            5
        );

        Assert.Null(attr.Header);
        Assert.Equal
        (
            FieldAlignment.Left,
            attr.Alignment
        );
        Assert.Equal
        (
            ' ',
            attr.Pad
        );
        Assert.Null(attr.Format);
        Assert.True(attr.TrimValue);
    }



    [Fact]
    public void Header_Alignment_Pad_Format_TrimValue_can_be_set()
    {
        var attr = new FixedWidthFieldAttribute
        (
            0,
            5
        )
        {
            Header = "MyHeader",
            Alignment = FieldAlignment.Right,
            Pad = '0',
            Format = "D5",
            TrimValue = false,
        };

        Assert.Equal
        (
            "MyHeader",
            attr.Header
        );
        Assert.Equal
        (
            FieldAlignment.Right,
            attr.Alignment
        );
        Assert.Equal
        (
            '0',
            attr.Pad
        );
        Assert.Equal
        (
            "D5",
            attr.Format
        );
        Assert.False(attr.TrimValue);
    }
}



public class FixedWidthSkipAttributeTests
{
    [Fact]
    public void Constructor_when_index_is_negative_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthSkipAttribute(-1, 5));
    }



    [Fact]
    public void Constructor_when_length_is_zero_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthSkipAttribute(0, 0));
    }



    [Fact]
    public void Constructor_when_length_is_negative_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedWidthSkipAttribute(0, -1));
    }



    [Fact]
    public void Constructor_sets_Index_and_Length()
    {
        var attr = new FixedWidthSkipAttribute
        (
            1,
            8
        );

        Assert.Equal
        (
            1,
            attr.Index
        );
        Assert.Equal
        (
            8,
            attr.Length
        );
    }



    [Fact]
    public void Message_can_be_set_and_read()
    {
        var attr = new FixedWidthSkipAttribute
        (
            0,
            5
        ) { Message = "DOB" };

        Assert.Equal
        (
            "DOB",
            attr.Message
        );
    }
}
