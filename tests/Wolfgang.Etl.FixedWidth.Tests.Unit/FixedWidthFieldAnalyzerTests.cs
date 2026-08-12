#if NET8_0_OR_GREATER
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Wolfgang.Etl.FixedWidth.Analyzers;
using Wolfgang.Etl.FixedWidth.Attributes;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Drives <see cref="FixedWidthFieldAnalyzer"/> (#27) directly over in-memory
/// compilations and asserts the FW0NN diagnostics it reports. net8.0+ only — the harness
/// builds reference metadata from the runtime's trusted-platform-assemblies list.
/// </summary>
public sealed class FixedWidthFieldAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(FixedWidthFieldAttribute).Assembly.Location))
            .ToImmutableArray();

        var compilation = CSharpCompilation.Create
        (
            "FixedWidthAnalyzerTestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var withAnalyzers = compilation.WithAnalyzers
        (
            ImmutableArray.Create<DiagnosticAnalyzer>(new FixedWidthFieldAnalyzer())
        );

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }



    private static string Wrap(string body) =>
        "using System;\n" +
        "using Wolfgang.Etl.FixedWidth.Attributes;\n" +
        "public class Record\n{\n" + body + "\n}\n";



    [Fact]
    public async Task Reports_FW003_for_a_duplicate_index()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 10)] public string First { get; set; } = \"\";\n" +
            "    [FixedWidthField(0, 5)] public string Second { get; set; } = \"\";"));

        Assert.Equal(2, diagnostics.Count(d => string.Equals(d.Id, "FW003", StringComparison.Ordinal)));
    }



    [Fact]
    public async Task Reports_FW004_for_a_DateTime_field_without_a_format()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 8)] public DateTime When { get; set; }"));

        Assert.Contains(diagnostics, d => string.Equals(d.Id, "FW004", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Does_not_report_FW004_when_a_format_is_present()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 8, Format = \"yyyyMMdd\")] public DateTime When { get; set; }"));

        Assert.DoesNotContain(diagnostics, d => string.Equals(d.Id, "FW004", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Reports_FW005_when_the_format_is_wider_than_the_field()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 8, Format = \"yyyy-MM-dd HH:mm:ss\")] public DateTime When { get; set; }"));

        Assert.Contains(diagnostics, d => string.Equals(d.Id, "FW005", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Reports_FW007_for_a_property_without_a_public_setter()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 5)] public string Code { get; private set; } = \"\";"));

        Assert.Contains(diagnostics, d => string.Equals(d.Id, "FW007", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Reports_FW008_for_a_property_without_a_public_getter()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    private string _code = \"\";\n" +
            "    [FixedWidthField(0, 5)] public string Code { set { _code = value; } }"));

        Assert.Contains(diagnostics, d => string.Equals(d.Id, "FW008", StringComparison.Ordinal));
    }



    [Fact]
    public async Task Reports_nothing_for_a_well_formed_record()
    {
        var diagnostics = await AnalyzeAsync(Wrap(
            "    [FixedWidthField(0, 10)] public string Name { get; set; } = \"\";\n" +
            "    [FixedWidthField(1, 5)] public int Age { get; set; }\n" +
            "    [FixedWidthField(2, 8, Format = \"yyyyMMdd\")] public DateTime HireDate { get; set; }"));

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("FW", StringComparison.Ordinal)));
    }
}
#endif
