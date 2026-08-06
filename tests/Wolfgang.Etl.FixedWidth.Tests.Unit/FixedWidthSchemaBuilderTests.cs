using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Enums;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Covers <see cref="FixedWidthSchemaBuilder{T}"/> and the extractor/loader <c>Schema</c> override (#23):
/// building a layout in code, using it on a type that carries no attributes, equivalence with the
/// attribute-resolved layout, and the builder's validation.
/// </summary>
public sealed class FixedWidthSchemaBuilderTests
{
    // An undecorated record — no [FixedWidthField] attributes — to prove the builder maps a type the
    // caller does not own / cannot decorate.
    [ExcludeFromCodeCoverage]
    public sealed class PlainPerson
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int Age { get; set; }
    }


    [ExcludeFromCodeCoverage]
    private sealed class ReadOnlyProperty
    {
        public string Name => "fixed";
    }


    private static FixedWidthSchemaBuilder<PlainPerson> PlainPersonBuilder()
        => new FixedWidthSchemaBuilder<PlainPerson>()
            .Field(r => r.FirstName, index: 0, length: 10)
            .Field(r => r.LastName, index: 1, length: 10)
            .Field(r => r.Age, index: 2, length: 3, alignment: FieldAlignment.Right, pad: '0');


    [Fact]
    public void Built_schema_is_equivalent_to_the_attribute_schema()
    {
        // A builder schema for PersonRecord's exact attribute layout must match FixedWidthSchema.For<PersonRecord>().
        var built = new FixedWidthSchemaBuilder<PersonRecord>()
            .Field(r => r.FirstName, 0, 10)
            .Field(r => r.LastName, 1, 10)
            .Field(r => r.Age, 2, 3, alignment: FieldAlignment.Right, pad: '0')
            .Build();

        var fromAttributes = FixedWidthSchema.For<PersonRecord>();

        Assert.Equal(fromAttributes.ExpectedLineWidth, built.ExpectedLineWidth);
        Assert.Equal(fromAttributes.TotalColumnCount, built.TotalColumnCount);
        Assert.Equal(fromAttributes.Fields.Count, built.Fields.Count);

        for (var i = 0; i < fromAttributes.Fields.Count; i++)
        {
            var expected = fromAttributes.Fields[i];
            var actual = built.Fields[i];
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.StartPosition, actual.StartPosition);
            Assert.Equal(expected.EndPosition, actual.EndPosition);
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected.Alignment, actual.Alignment);
            Assert.Equal(expected.Pad, actual.Pad);
            Assert.Equal(expected.IsSkip, actual.IsSkip);
        }
    }


    [Fact]
    public async Task Extractor_reads_an_undecorated_type_via_builder_schema()
    {
        var schema = PlainPersonBuilder().Build();

        var extractor = new FixedWidthExtractor<PlainPerson>
        (
            new StringReader(Content(("Alice", "Smith", 30), ("Bob", "Jones", 25)))
        )
        {
            Schema = schema,
        };

        var people = await DrainAsync(extractor);

        Assert.Collection
        (
            people,
            p => Assert.Equal(("Alice", "Smith", 30), (p.FirstName, p.LastName, p.Age)),
            p => Assert.Equal(("Bob", "Jones", 25), (p.FirstName, p.LastName, p.Age))
        );
    }


    [Fact]
    public async Task Loader_writes_an_undecorated_type_via_builder_schema()
    {
        var schema = PlainPersonBuilder().Build();
        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PlainPerson>(writer) { Schema = schema };

        await loader.LoadAsync(ToAsync(new PlainPerson { FirstName = "Alice", LastName = "Smith", Age = 30 }), CancellationToken.None);

        Assert.Equal("Alice     Smith     030", Normalize(writer.ToString()).TrimEnd('\n'));
    }


    [Fact]
    public async Task Round_trips_an_undecorated_type_through_the_builder_schema()
    {
        var schema = PlainPersonBuilder().Build();
        var source = Content(("Alice", "Smith", 30), ("Bob", "Jones", 25));

        var extractor = new FixedWidthExtractor<PlainPerson>(new StringReader(source)) { Schema = schema };
        var people = await DrainAsync(extractor);

        var writer = new StringWriter();
        var loader = new FixedWidthLoader<PlainPerson>(writer) { Schema = schema };
        await loader.LoadAsync(ToAsync(people.ToArray()), CancellationToken.None);

        Assert.Equal(source.TrimEnd('\n'), Normalize(writer.ToString()).TrimEnd('\n'));
    }


    [Fact]
    public async Task Skip_column_defined_by_the_builder_is_honored()
    {
        // FirstName(0,10) [skip 8] EmployeeNumber(18,6)
        var schema = new FixedWidthSchemaBuilder<PlainRecordWithSkip>()
            .Field(r => r.FirstName, 0, 10)
            .Skip(1, 8, message: "DOB")
            .Field(r => r.EmployeeNumber, 2, 6)
            .Build();

        Assert.Equal(24, schema.ExpectedLineWidth);
        Assert.Equal(1, schema.SkipCount);

        var extractor = new FixedWidthExtractor<PlainRecordWithSkip>
        (
            new StringReader("Alice     19900101E12345\n")
        )
        {
            Schema = schema,
        };

        var records = await DrainAsync(extractor);

        Assert.Single(records);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal("E12345", records[0].EmployeeNumber);
    }


    [Fact]
    public async Task Schema_overrides_attributes_when_set()
    {
        // PersonRecord's attributes map all three fields; this schema maps only FirstName, so Age stays default.
        var schema = new FixedWidthSchemaBuilder<PersonRecord>()
            .Field(r => r.FirstName, 0, 10)
            .Skip(1, 13)
            .Build();

        var extractor = new FixedWidthExtractor<PersonRecord>
        (
            new StringReader("Alice     Smith     030\n")
        )
        {
            Schema = schema,
        };

        var people = await DrainAsync(extractor);

        Assert.Equal("Alice", people[0].FirstName);
        Assert.Equal(0, people[0].Age);      // not mapped by the override schema
        Assert.Null(people[0].LastName);     // not mapped
    }


    [Fact]
    public void Build_with_no_fields_throws()
    {
        var builder = new FixedWidthSchemaBuilder<PlainPerson>().Skip(0, 5);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }


    [Fact]
    public void Build_with_duplicate_index_throws()
    {
        var builder = new FixedWidthSchemaBuilder<PlainPerson>()
            .Field(r => r.FirstName, 0, 10)
            .Field(r => r.LastName, 0, 10);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }


    [Fact]
    public void Field_without_a_public_setter_throws()
    {
        var builder = new FixedWidthSchemaBuilder<ReadOnlyProperty>();

        Assert.Throws<InvalidOperationException>(() => builder.Field(r => r.Name, 0, 5));
    }


    [Fact]
    public void Field_unwraps_a_boxing_conversion_in_the_selector()
    {
        // r => (object)r.Age inserts a Convert node the resolver must unwrap to reach the property.
        var schema = new FixedWidthSchemaBuilder<PlainPerson>()
            .Field(r => (object)r.Age, 0, 3)
            .Build();

        Assert.Equal("Age", schema.Fields[0].Name);
    }


    [Fact]
    public void Field_with_a_non_property_selector_throws()
    {
        var builder = new FixedWidthSchemaBuilder<PlainPerson>();

        Assert.Throws<ArgumentException>(() => builder.Field(r => r.Age + 1, 0, 5));
    }


    [Fact]
    public void Field_rejects_null_selector_and_invalid_index_or_length()
    {
        var builder = new FixedWidthSchemaBuilder<PlainPerson>();

        Assert.Throws<ArgumentNullException>(() => builder.Field<string>(null!, 0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Field(r => r.FirstName, -1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Field(r => r.FirstName, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Skip(0, -3));
    }


    [Fact]
    public async Task Extractor_with_a_mismatched_schema_record_type_throws()
    {
        var plainSchema = PlainPersonBuilder().Build();
        var extractor = new FixedWidthExtractor<PersonRecord>(new StringReader("Alice     Smith     030\n"))
        {
            Schema = plainSchema,   // schema is for PlainPerson, not PersonRecord
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
            {
            }
        });
    }


    [Fact]
    public async Task Loader_with_a_mismatched_schema_record_type_throws()
    {
        var plainSchema = PlainPersonBuilder().Build();
        var loader = new FixedWidthLoader<PersonRecord>(new StringWriter()) { Schema = plainSchema };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(ToAsync(new PersonRecord { FirstName = "Alice", LastName = "Smith", Age = 30 }), CancellationToken.None));
    }


    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    [ExcludeFromCodeCoverage]
    public sealed class PlainRecordWithSkip
    {
        public string FirstName { get; set; } = string.Empty;

        public string EmployeeNumber { get; set; } = string.Empty;
    }


    private static string Content(params (string First, string Last, int Age)[] people)
        => string.Concat(people.Select(p => string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,-10}{2:000}\n", p.First, p.Last, p.Age)));


    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");


    private static async Task<IReadOnlyList<T>> DrainAsync<T>(FixedWidthExtractor<T> extractor)
        where T : notnull, new()
    {
        var list = new List<T>();
        await foreach (var item in extractor.ExtractAsync(CancellationToken.None).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }


#pragma warning disable CS1998 // synchronous sample sequence
    private static async IAsyncEnumerable<T> ToAsync<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
#pragma warning restore CS1998
}
