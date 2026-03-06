using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wolfgang.Etl.Abstractions;
using Wolfgang.Etl.FixedWidth;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit
{
    [ExcludeFromCodeCoverage]
    public class HeaderRecord
    {
        [FixedWidthField(0, 10, Header = "FIRST_NM")]
        public string FirstName { get; set; }



        [FixedWidthField(1, 10, Header = "LAST_NM")]
        public string LastName { get; set; }
    }



    public class FixedWidthLoaderTests
    {
        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static FixedWidthLoader<PersonRecord, Report> CreateLoader(out StringWriter writer)
        {
            writer = new StringWriter();
            return new FixedWidthLoader<PersonRecord, Report>(writer);
        }



        private static string[] GetLines(StringWriter writer)
        {
            var parts = writer.ToString().Split
            (
                new[] { "\r\n", "\n" },
                StringSplitOptions.None
            );
            if (parts.Length > 0 && parts[parts.Length - 1] == "")
            {
                var trimmed = new string[parts.Length - 1];
                Array.Copy
                (
                    parts,
                    trimmed,
                    trimmed.Length
                );
                return trimmed;
            }
            return parts;
        }


        private static async IAsyncEnumerable<PersonRecord> ToAsyncEnumerable( IEnumerable<PersonRecord> records)
        {
            foreach (var r in records)
            {
                yield return r;
                await Task.Yield();
            }
        }



        private static async IAsyncEnumerable<HeaderRecord> ToAsyncEnumerable2( IEnumerable<HeaderRecord> records)
        {
            foreach (var r in records)
            {
                yield return r;
                await Task.Yield();
            }
        }



        // ------------------------------------------------------------------
        // Happy path
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoadAsync_with_valid_records_writes_correct_lines()
        {
            var loader = CreateLoader(out var writer);

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
            }));

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

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord() // all properties at default — null/0
            }));

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



        [Fact]
        public async Task LoadAsync_increments_CurrentItemCount_for_each_record_written()
        {
            var loader = CreateLoader(out _);

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
            }));

            Assert.Equal
            (
                2,
                loader.CurrentItemCount
            );
        }



        [Fact]
        public async Task LoadAsync_writes_to_any_TextWriter()
        {
            using var writer = new StringWriter();
            var loader = new FixedWidthLoader<PersonRecord, Report>(writer);

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

            Assert.Contains
            (
                "John",
                writer.ToString()
            );
        }



        // ------------------------------------------------------------------
        // Header
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoadAsync_when_WriteHeader_is_true_writes_the_header_as_the_first_line()
        {
            var loader = CreateLoader(out var writer);
            loader.WriteHeader = true;

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

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
            var loader = new FixedWidthLoader<HeaderRecord, Report>(writer);
            loader.WriteHeader = true;

            await loader.LoadAsync(ToAsyncEnumerable2(new[]
            {
                new HeaderRecord { FirstName = "John", LastName = "Smith" },
            }));

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
        // MaximumItemCount / SkipItemCount
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoadAsync_when_MaximumItemCount_is_reached_stops_writing()
        {
            var loader = CreateLoader(out var writer);
            loader.MaximumItemCount = 2;

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
                new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 55 },
            }));

            Assert.Equal
            (
                2,
                GetLines(writer).Length
            );
        }



        [Fact]
        public async Task LoadAsync_when_SkipItemCount_is_set_skips_the_first_N_records()
        {
            var loader = CreateLoader(out var writer);
            loader.SkipItemCount = 1;

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
                new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 55 },
            }));

            var lines = GetLines(writer);

            Assert.Equal
            (
                2,
                lines.Length
            );
            Assert.StartsWith
            (
                "Jane",
                lines[0]
            );
        }



        // ------------------------------------------------------------------
        // Null record
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoadAsync_when_a_null_record_is_encountered_throws_InvalidOperationException()
        {
            var loader = CreateLoader(out _);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await loader.LoadAsync(ToAsyncEnumerable(new PersonRecord[] { null })));
        }



        // ------------------------------------------------------------------
        // Delimiter
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoadAsync_when_FieldDelimiter_is_set_inserts_delimiter_between_fields()
        {
            var loader = CreateLoader(out var writer);
            loader.FieldDelimiter = " | ";

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

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

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

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
            public string FirstName { get; set; }



            [FixedWidthField(1, 10)]
            public string LastName { get; set; }



            [FixedWidthField(2, 3, Alignment = FieldAlignment.Right)]
            public int Age { get; set; }
        }



        [Fact]
        public async Task LoadAsync_when_WriteHeader_and_FieldDelimiter_are_set_header_line_is_also_delimited_space_padded()
        {
            var writer = new StringWriter();
            var loader = new FixedWidthLoader<SpacePaddedRecord, Report>(writer)
            {
                WriteHeader = true, FieldDelimiter = " | "
            };

            await loader.LoadAsync
            (

                    new[]
                    {
                        new SpacePaddedRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                    }
                    .ToAsyncEnumerable()
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

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

            var lines = GetLines(writer);

            Assert.Equal
            (
                3,
                lines.Length
            );
            Assert.True(lines[1].Replace("-", "").Trim().Length == 0 || lines[1].All(c => c == '-'));
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

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

            var lines = GetLines(writer);

            Assert.Contains
            (
                "-|-",
                lines[1]
            );
        }



        [Fact]
        public async Task LoadAsync_when_FieldSeparator_is_set_but_WriteHeader_is_false_does_not_write_a_separator_line()
        {
            var loader = CreateLoader(out var writer);
            loader.WriteHeader = false;
            loader.FieldSeparator = '-';

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

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
            var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);
            loader.WriteHeader = true;
            loader.FieldSeparator = '-';

            await loader.LoadAsync( ToAsyncEnumerable(new[]
                {
                    new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                    new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
                }));

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
            var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);

            await loader.LoadAsync( ToAsyncEnumerable(new[]
                {
                    new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                    new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
                    new PersonRecord { FirstName = "Bob", LastName = "Jones", Age = 55 },
                }));

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
                ctx.PropertyName == nameof(PersonRecord.FirstName)
                    ? ((string)value).ToUpperInvariant()
                    : FixedWidthConverter.Strict(value, ctx);

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

            var lines = GetLines(writer);
            Assert.StartsWith
            (
                "JOHN      ",
                lines[0]
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
                ctx
            ) => label.ToUpperInvariant();

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
            }));

            var lines = GetLines(writer);
            Assert.StartsWith
            (
                "FIRSTNAME ",
                lines[0]
            );
        }



        // ------------------------------------------------------------------
        // CreateProgressReport
        // ------------------------------------------------------------------

        [Fact]
        public async Task GetProgressReport_returns_FixedWidthReport_with_current_counts()
        {
            var writer = new StringWriter();
            var loader = new FixedWidthLoader<PersonRecord, FixedWidthReport>(writer);

            await loader.LoadAsync(ToAsyncEnumerable(new[]
            {
                new PersonRecord { FirstName = "John", LastName = "Smith", Age = 42 },
                new PersonRecord { FirstName = "Jane", LastName = "Doe", Age = 30 },
            }));

            var report = loader.GetProgressReport();

            Assert.Equal
            (
                2,
                report.CurrentCount
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
        public void GetProgressReport_when_TProgress_is_not_FixedWidthReport_throws_NotImplementedException()
        {
            var loader = new FixedWidthLoader<PersonRecord, System.Exception>(new StringWriter());

            Assert.Throws<NotImplementedException>(() => loader.GetProgressReport());
        }
    }
}
