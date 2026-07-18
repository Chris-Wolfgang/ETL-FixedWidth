using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Wolfgang.Etl.FixedWidth;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.Unit;

/// <summary>
/// Guards XML-doc <c>&lt;example&gt;</c> blocks against API rot (#151). Every
/// member referenced through a known public type or a known receiver variable
/// (<c>loader</c>, <c>extractor</c>, <c>ctx</c>) in an example must still exist on
/// the current public surface — so an example that outlives a renamed or removed
/// member fails this test instead of misleading readers.
/// </summary>
/// <remarks>
/// This is a symbol-existence check, not a full compile. The example blocks are
/// deliberately terse fragments with free variables (a full <c>csc</c> gate would
/// need per-snippet harnesses and a heavy analyzer dependency across every target
/// framework). The high-value drift — a doc referencing an API member that no
/// longer exists — is caught here by reflection, on every TFM, with no extra
/// dependency. Member access through an unmapped receiver (e.g. a local whose type
/// the check cannot infer) is intentionally out of scope.
/// </remarks>
public class DocExampleApiReferenceTests
{
    // Receiver variable name -> the type whose members it exposes in the examples.
    // Open generics are fine: member names are identical across type arguments.
    private static readonly IReadOnlyDictionary<string, Type> ReceiverTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["loader"] = typeof(FixedWidthLoader<>),
        ["extractor"] = typeof(FixedWidthExtractor<>),
        ["ctx"] = typeof(FieldContext),
        ["context"] = typeof(FieldContext),
    };



    // <TypeOrReceiver>.<Member> — Member is a C# identifier; a following '(' or not
    // does not matter (we only assert the name exists in any member kind).
    private static readonly Regex MemberAccess = new
    (
        @"(?<recv>[A-Za-z_][A-Za-z0-9_]*)\.(?<member>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled
    );



    private static readonly HashSet<string> IgnoredMembers = new(StringComparer.Ordinal)
    {
        // BCL members that appear in examples on non-library receivers/values.
        "ToUpperInvariant", "All", "StartsWith", "IsNullOrWhiteSpace", "OpenRead", "CompletedTask",
    };



    private static IEnumerable<(string File, Type Owner, string Member)> ExampleReferences()
    {
        var publicTypes = typeof(FixedWidthLoader<>).Assembly
            .GetExportedTypes()
            .GroupBy(t => StripArity(t.Name), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var file in EnumerateSourceFiles())
        {
            var source = File.ReadAllText(file);
            foreach (var code in ExtractExampleCode(source))
            {
                foreach (var m in MemberAccess.Matches(code).Cast<Match>())
                {
                    var recv = m.Groups["recv"].Value;
                    var member = m.Groups["member"].Value;
                    if (IgnoredMembers.Contains(member))
                    {
                        continue;
                    }

                    if (publicTypes.TryGetValue(recv, out var typeOwner))
                    {
                        yield return (Path.GetFileName(file), typeOwner, member);
                    }
                    else if (ReceiverTypes.TryGetValue(recv, out var recvOwner))
                    {
                        yield return (Path.GetFileName(file), recvOwner, member);
                    }
                }
            }
        }
    }



    [Fact]
    public void Every_example_member_reference_exists_on_current_api()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        var references = ExampleReferences().ToList();

        // Guards against extraction silently matching nothing (which would make the
        // assertion vacuously pass and hide real rot).
        Assert.NotEmpty(references);

        var rotted = references
            .Where(r => r.Owner.GetMember(r.Member, flags).Length == 0)
            .Select(r => $"{r.File}: '{r.Owner.Name}.{r.Member}'")
            .Distinct()
            .ToList();

        Assert.True
        (
            rotted.Count == 0,
            "Doc <example> blocks reference API members that no longer exist:" +
            Environment.NewLine + string.Join(Environment.NewLine, rotted)
        );
    }



    private static string StripArity(string typeName)
    {
        var tick = typeName.IndexOf('`');
        return tick < 0 ? typeName : typeName.Substring(0, tick);
    }



    private static IEnumerable<string> ExtractExampleCode(string source)
    {
        // Match each <example>...</example>, then the <code>...</code> inside it,
        // strip the leading '///' doc-comment markers, and XML-decode entities.
        foreach (var ex in Regex.Matches(source, "<example>(.*?)</example>", RegexOptions.Singleline).Cast<Match>())
        {
            foreach (var code in Regex.Matches(ex.Groups[1].Value, "<code>(.*?)</code>", RegexOptions.Singleline).Cast<Match>())
            {
                var lines = code.Groups[1].Value
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Select(l => Regex.Replace(l, @"^\s*///\s?", string.Empty));

                var text = string.Join("\n", lines)
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&amp;", "&")
                    .Replace("&quot;", "\"");

                yield return text;
            }
        }
    }



    private static IEnumerable<string> EnumerateSourceFiles()
    {
        // [CallerFilePath] is unreliable in CI (deterministic /_/ paths), so walk up
        // from the test's base directory to the repo root and read the source tree.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var srcDir = Path.Combine(dir.FullName, "src", "Wolfgang.Etl.FixedWidth");
            if (Directory.Exists(srcDir))
            {
                return Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                    .ToList();
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Wolfgang.Etl.FixedWidth from the test base directory.");
    }
}
