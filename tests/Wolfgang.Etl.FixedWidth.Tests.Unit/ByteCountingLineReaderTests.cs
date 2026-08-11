using System.Collections.Generic;
using System.IO;
using System.Text;
using Wolfgang.Etl.FixedWidth.Parsing;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="ByteCountingLineReader"/> (#31): it must reproduce
/// <see cref="TextReader.ReadLine"/> exactly while reporting the byte offset of the next unread line.
/// </summary>
public sealed class ByteCountingLineReaderTests
{
    private static (List<string> Lines, List<long> Offsets) ReadAll(string input, Encoding encoding, int bufferSize = 8192)
    {
        using var reader = new ByteCountingLineReader(new StringReader(input), encoding, 0, bufferSize);
        var lines = new List<string>();
        var offsets = new List<long>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
            offsets.Add(reader.BytesConsumed);
        }

        return (lines, offsets);
    }


    [Fact]
    public void Splits_lines_and_reports_ascii_byte_offsets_for_lf()
    {
        var (lines, offsets) = ReadAll("AAA\nBB\nC", Encoding.ASCII);

        Assert.Equal(new[] { "AAA", "BB", "C" }, lines);
        Assert.Equal(new long[] { 4, 7, 8 }, offsets);   // "AAA\n"=4, +"BB\n"=3 ->7, +"C"(no term)=1 ->8
    }


    [Fact]
    public void Handles_crlf_and_lone_cr_terminators()
    {
        var (crlf, crlfOffsets) = ReadAll("A\r\nBB\r\n", Encoding.ASCII);
        Assert.Equal(new[] { "A", "BB" }, crlf);
        Assert.Equal(new long[] { 3, 7 }, crlfOffsets);   // "A\r\n"=3, +"BB\r\n"=4 ->7

        var (cr, crOffsets) = ReadAll("A\rBB\r", Encoding.ASCII);
        Assert.Equal(new[] { "A", "BB" }, cr);
        Assert.Equal(new long[] { 2, 5 }, crOffsets);     // "A\r"=2, +"BB\r"=3 ->5
    }


    [Fact]
    public void Emits_empty_lines()
    {
        var (lines, offsets) = ReadAll("\n\nX\n", Encoding.ASCII);

        Assert.Equal(new[] { "", "", "X" }, lines);
        Assert.Equal(new long[] { 1, 2, 4 }, offsets);
    }


    [Fact]
    public void Empty_input_yields_no_lines()
    {
        var (lines, _) = ReadAll(string.Empty, Encoding.ASCII);

        Assert.Empty(lines);
    }


    [Fact]
    public void Counts_multibyte_utf8_content_in_bytes_not_chars()
    {
        // 'é' is one char but two UTF-8 bytes; the euro sign is three.
        var (lines, offsets) = ReadAll("é\n€\n", Encoding.UTF8);

        Assert.Equal(new[] { "é", "€" }, lines);
        Assert.Equal(new long[] { 3, 7 }, offsets);   // "é\n"=2+1=3, "€\n"=3+1=4 ->7
    }


    [Fact]
    public void Detects_crlf_split_across_a_buffer_boundary()
    {
        // A tiny buffer forces the '\r' and '\n' of a CRLF into separate reads.
        var (lines, offsets) = ReadAll("AB\r\nCD\r\n", Encoding.ASCII, bufferSize: 3);

        Assert.Equal(new[] { "AB", "CD" }, lines);
        Assert.Equal(new long[] { 4, 8 }, offsets);
    }


    [Theory]
    [InlineData("one\ntwo\nthree")]
    [InlineData("a\r\nb\r\nc\r\n")]
    [InlineData("\n\n\n")]
    [InlineData("no terminator")]
    [InlineData("mixed\r\nendings\nhere\r")]
    [InlineData("")]
    public void Line_splitting_matches_TextReader_ReadLine(string input)
    {
        var expected = new List<string>();
        using (var baseline = new StringReader(input))
        {
            string? l;
            while ((l = baseline.ReadLine()) != null)
            {
                expected.Add(l);
            }
        }

        var (actual, _) = ReadAll(input, Encoding.ASCII, bufferSize: 4);

        Assert.Equal(expected, actual);
    }


    [Fact]
    public void Peek_and_Read_advance_and_count_bytes()
    {
        using var reader = new ByteCountingLineReader(new StringReader("AB"), Encoding.ASCII);

        Assert.Equal('A', reader.Peek());   // does not advance
        Assert.Equal('A', reader.Read());
        Assert.Equal(1, reader.BytesConsumed);
        Assert.Equal('B', reader.Read());
        Assert.Equal(2, reader.BytesConsumed);
        Assert.Equal(-1, reader.Peek());    // end of input
        Assert.Equal(-1, reader.Read());
    }


    [Fact]
    public void Initial_offset_is_added_to_every_reported_offset()
    {
        using var reader = new ByteCountingLineReader(new StringReader("AB\nCD\n"), Encoding.ASCII, initialByteOffset: 100);

        reader.ReadLine();
        Assert.Equal(103, reader.BytesConsumed);   // 100 + "AB\n"
        reader.ReadLine();
        Assert.Equal(106, reader.BytesConsumed);
    }
}
