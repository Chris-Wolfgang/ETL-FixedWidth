using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Wolfgang.Etl.FixedWidth.Attributes;
using Wolfgang.Etl.FixedWidth.Generated;
using Wolfgang.Etl.FixedWidth.Tests.Unit.TestModels;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Tests for the <c>Wolfgang.Etl.FixedWidth.Analyzers</c> source generator (#13). The
/// generator runs over this test assembly's <see cref="GeneratedAccessorFixture"/> at
/// compile time and registers factory/getter/setter delegates from a module initializer.
/// The module-initializer path only compiles on net5.0+, so the registration and
/// conformance assertions are scoped to <c>NET5_0_OR_GREATER</c>; the end-to-end
/// extraction test runs on every framework because the runtime seam falls back to
/// reflection where no generated delegate exists.
/// </summary>
public sealed class FixedWidthAccessorGeneratorTests
{
    private static PropertyInfo[] FieldProperties() =>
        typeof(GeneratedAccessorFixture)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<FixedWidthFieldAttribute>() != null)
            .ToArray();



    private static GeneratedAccessorFixture SampleRecord() => new()
    {
        Name = "Bob",
        Age = 42,
        HireDate = new DateTime(2024, 1, 15),
        Salary = 1234.5m
    };



#if NET5_0_OR_GREATER

    [Fact]
    public void Generator_registers_a_factory_that_creates_the_record()
    {
        var found = GeneratedAccessorRegistry.TryGetFactory(typeof(GeneratedAccessorFixture), out var factory);

        Assert.True(found, "The generator should register a factory for the decorated type.");
        Assert.IsType<GeneratedAccessorFixture>(factory());
    }



    [Fact]
    public void Generator_registers_a_getter_for_every_field_property()
    {
        var sample = SampleRecord();

        foreach (var property in FieldProperties())
        {
            var found = GeneratedAccessorRegistry.TryGetGetter(typeof(GeneratedAccessorFixture), property.Name, out var getter);

            Assert.True(found, $"Expected a generated getter for '{property.Name}'.");
            Assert.Equal(property.GetValue(sample), getter(sample));
        }
    }



    [Fact]
    public void Generator_registers_a_setter_for_every_field_property()
    {
        var sample = SampleRecord();

        foreach (var property in FieldProperties())
        {
            var found = GeneratedAccessorRegistry.TryGetSetter(typeof(GeneratedAccessorFixture), property.Name, out var setter);
            var target = new GeneratedAccessorFixture();
            setter(target, property.GetValue(sample));

            Assert.True(found, $"Expected a generated setter for '{property.Name}'.");
            Assert.Equal(property.GetValue(sample), property.GetValue(target));
        }
    }



    [Fact]
    public void Generated_accessors_conform_to_the_reflection_fallback()
    {
        // For each field, the generated getter/setter must behave identically to plain
        // reflection (PropertyInfo) — this pins the fast path to the fallback so the two
        // can never silently diverge.
        var sample = SampleRecord();

        foreach (var property in FieldProperties())
        {
            Assert.True(GeneratedAccessorRegistry.TryGetGetter(typeof(GeneratedAccessorFixture), property.Name, out var getter));
            Assert.True(GeneratedAccessorRegistry.TryGetSetter(typeof(GeneratedAccessorFixture), property.Name, out var setter));

            var value = property.GetValue(sample);

            var viaGenerated = new GeneratedAccessorFixture();
            setter(viaGenerated, value);

            var viaReflection = new GeneratedAccessorFixture();
            property.SetValue(viaReflection, value);

            Assert.Equal(property.GetValue(viaReflection), getter(viaGenerated));
        }
    }

#endif



    [Fact]
    public async Task Extraction_populates_the_record_through_the_field_mapping_seam()
    {
        // Name(10) + Age(5,right,'0') + HireDate(8,yyyyMMdd) + Salary(6) = 29 chars.
        const string line = "Bob       " + "00042" + "20240115" + "1234.5";
        var extractor = new FixedWidthExtractor<GeneratedAccessorFixture>(new StringReader(line));

        var results = await extractor.ExtractAsync().ToListAsync();

        var record = Assert.Single(results);
        Assert.Equal("Bob", record.Name);
        Assert.Equal(42, record.Age);
        Assert.Equal(new DateTime(2024, 1, 15), record.HireDate);
        Assert.Equal(1234.5m, record.Salary);
        Assert.Equal("42", record.Age.ToString(CultureInfo.InvariantCulture));
    }
}
