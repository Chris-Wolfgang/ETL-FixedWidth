#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Wolfgang.Etl.FixedWidth.Analyzers;
using Wolfgang.Etl.FixedWidth.Attributes;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Drives <see cref="FixedWidthAccessorGenerator"/> (#13) at test runtime with a
/// <see cref="CSharpGeneratorDriver"/> and asserts the emitted source, so the generator's
/// own code paths (model building, emission, mangling) are exercised and measured. net8.0+
/// only — references are built from the runtime's trusted-platform-assemblies list.
/// </summary>
public sealed class FixedWidthAccessorGeneratorOutputTests
{
    private static string RunGenerator(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(FixedWidthFieldAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create
        (
            "FixedWidthGeneratorTestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FixedWidthAccessorGenerator().AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        return string.Join
        (
            "\n",
            driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString())
        );
    }



    private static string Wrap(string body) =>
        "using System;\n" +
        "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
        "namespace Consumer;\n" +
        "public class Person\n{\n" + body + "\n}\n";



    [Fact]
    public void Emits_factory_getter_and_setter_for_a_decorated_type()
    {
        var output = RunGenerator(Wrap(
            "    [FixedWidthField(0, 10)] public string Name { get; set; } = \"\";\n" +
            "    [FixedWidthField(1, 5)] public int Age { get; set; }"));

        Assert.Contains("RegisterFactory(typeof(global::Consumer.Person)", output, StringComparison.Ordinal);
        Assert.Contains("Get_Name", output, StringComparison.Ordinal);
        Assert.Contains("Set_Name", output, StringComparison.Ordinal);
        Assert.Contains("Get_Age", output, StringComparison.Ordinal);
        Assert.Contains("Set_Age", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Emits_exactly_one_accessor_class_per_type()
    {
        var output = RunGenerator(Wrap(
            "    [FixedWidthField(0, 10)] public string Name { get; set; } = \"\";\n" +
            "    [FixedWidthField(1, 5)] public int Age { get; set; }"));

        var classCount = output.Split(new[] { "internal static class FixedWidthAccessors_" }, StringSplitOptions.None).Length - 1;
        Assert.Equal(1, classCount);
    }



    [Fact]
    public void Skips_the_setter_for_an_init_only_property()
    {
        var output = RunGenerator(Wrap(
            "    [FixedWidthField(0, 5)] public string Code { get; init; } = \"\";"));

        Assert.Contains("Get_Code", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Set_Code", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Skips_the_getter_for_a_set_only_property()
    {
        var output = RunGenerator(Wrap(
            "    private string _v = \"\";\n" +
            "    [FixedWidthField(0, 5)] public string Code { set { _v = value; } }"));

        Assert.Contains("Set_Code", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Get_Code", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Omits_the_factory_for_a_type_without_a_parameterless_constructor()
    {
        var output = RunGenerator(
            "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
            "namespace Consumer;\n" +
            "public class NoCtor\n{\n" +
            "    public NoCtor(int x) { Name = x.ToString(); }\n" +
            "    [FixedWidthField(0, 5)] public string Name { get; set; }\n}\n");

        Assert.Contains("Set_Name", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterFactory", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Emits_a_factory_for_a_struct()
    {
        var output = RunGenerator(
            "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
            "namespace Consumer;\n" +
            "public struct Rec\n{\n" +
            "    [FixedWidthField(0, 5)] public string Name { get; set; }\n}\n");

        Assert.Contains("RegisterFactory(typeof(global::Consumer.Rec)", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Generates_nothing_for_a_generic_type()
    {
        var output = RunGenerator(
            "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
            "namespace Consumer;\n" +
            "public class Gen<T>\n{\n" +
            "    [FixedWidthField(0, 5)] public string Name { get; set; } = \"\";\n}\n");

        Assert.DoesNotContain("FixedWidthAccessors_", output, StringComparison.Ordinal);
    }



    [Fact]
    public void Generates_nothing_for_an_inaccessible_nested_type()
    {
        var output = RunGenerator(
            "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
            "namespace Consumer;\n" +
            "public class Outer\n{\n" +
            "    private class Hidden\n    {\n" +
            "        [FixedWidthField(0, 5)] public string Name { get; set; } = \"\";\n    }\n}\n");

        Assert.DoesNotContain("FixedWidthAccessors_", output, StringComparison.Ordinal);
    }
}
#endif
