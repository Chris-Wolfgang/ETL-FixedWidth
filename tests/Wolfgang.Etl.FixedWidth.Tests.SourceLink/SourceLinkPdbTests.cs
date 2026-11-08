// SourceLink PDB gates — the mechanical preconditions for F11-into-source.
//
// Interactive-debugger step-into is not automatable in an ordinary CI job, but
// the things that make it work ARE: the assembly's PDB must be portable (not
// full-format), it must carry a SourceLink CustomDebugInformation record, that
// record must map this repo's source paths to GitHub raw URLs, and those URLs
// must actually resolve. If all of those hold, F11-into-source works by
// construction; if any breaks, a consumer's debugger silently falls back to
// decompiled placeholders.
//
// Refs #91.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Wolfgang.Etl.FixedWidth.Tests.SourceLink;

public class SourceLinkPdbTests
{
    private const string RepoSlug = "Chris-Wolfgang/ETL-FixedWidth";


    private const string RuntimePdbFileName = "Wolfgang.Etl.FixedWidth.pdb";


    private const string RawHost = "raw.githubusercontent.com";


    private static readonly Guid SourceLinkGuid = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");



    /// <summary>
    /// Portable PDBs start with the four bytes 'B','S','J','B' — the ECMA-335
    /// metadata-blob magic. Full-format Windows PDBs start with
    /// "Microsoft C/C++ MSF 7.00". SourceLink is portable-PDB only, so
    /// full-format is an immediate fail.
    /// </summary>
    [Fact]
    public void Runtime_pdb_is_portable_format()
    {
        var pdbPath = LocateRuntimePdb();
        Assert.True(File.Exists(pdbPath), $"Runtime PDB not found at {pdbPath}");

        Span<byte> magic = stackalloc byte[4];
        using (var fs = File.OpenRead(pdbPath))
        {
            Assert.Equal(4, fs.Read(magic));
        }

        Assert.Equal((byte)'B', magic[0]);
        Assert.Equal((byte)'S', magic[1]);
        Assert.Equal((byte)'J', magic[2]);
        Assert.Equal((byte)'B', magic[3]);
    }



    /// <summary>
    /// The runtime PDB must carry a SourceLink record whose JSON maps this
    /// repo's source paths to GitHub raw URLs. Third-party packages contribute
    /// their own mappings, so only entries pointing at this repo are asserted.
    /// </summary>
    [Fact]
    public void Runtime_pdb_has_sourcelink_pointing_at_github_raw()
    {
        var mappings = ReadOurSourceLinkMappings();

        Assert.NotEmpty(mappings);

        foreach (var (_, url) in mappings)
        {
            AssertIsOurRawGitHubUrl(url);
        }
    }



    /// <summary>
    /// Resolves a real source URL out of the SourceLink mapping and checks that
    /// GitHub serves it. This is what catches a force-pushed or deleted commit
    /// that would leave a consumer's debugger with a dead raw URL — the
    /// structural checks above cannot see that.
    /// </summary>
    /// <remarks>
    /// A SourceLink mapping is a prefix pair, e.g.
    /// <c>"/_/*" -&gt; "https://raw.githubusercontent.com/{slug}/{sha}/*"</c>.
    /// Probing the mapping value verbatim is useless: it still contains the
    /// literal <c>*</c> and would 404 for that reason alone. A real URL only
    /// exists once an actual document path is substituted into it, which is
    /// what this test does. The HTTP probe is skipped when the SHA has not been
    /// substituted on a local, unpushed build (and fails in CI, where it is a
    /// defect); see <see cref="ShouldProbe"/>.
    /// </remarks>
    [Fact]
    public async Task Sourcelink_github_raw_url_resolves_for_a_real_source_file()
    {
        var probeUrl = BuildProbeUrl(ReadOurSourceLinkMappings());

        // No document matched a mapping prefix: a malformed local-prefix mapping or a
        // document-table mismatch. That is a defect wherever the suite runs, so it fails
        // rather than letting a broken PDB skip the resolution check this test exists for.
        Assert.True(probeUrl is not null, "No .cs document in the PDB matched one of this repo's SourceLink mapping prefixes.");

        if (ShouldProbe(probeUrl))
        {
            // 404 means the SHA no longer resolves (force-push, repo rename).
            // 403/429 is GitHub rate-limiting the runner, which is infra noise
            // rather than a SourceLink defect; null means the network was
            // unreachable (see TryGetStatusCodeAsync).
            var status = await TryGetStatusCodeAsync(probeUrl);

            Assert.True
            (
                status != HttpStatusCode.NotFound,
                $"SourceLink URL 404s — the commit SHA no longer resolves: {probeUrl}"
            );
        }
    }



    // ------------------------------------------------------------------


    /// <summary>
    /// Asserts that <paramref name="url"/> is an absolute HTTPS URL served by
    /// GitHub's raw host whose path names this repository.
    /// </summary>
    /// <remarks>
    /// The host is compared for equality rather than with a substring test. A
    /// substring test would accept a look-alike host such as
    /// <c>raw.githubusercontent.com.example</c>, or an unrelated host carrying
    /// that text somewhere in its path, and so would not actually prove the
    /// mapping points where a debugger needs it to.
    /// </remarks>
    private static void AssertIsOurRawGitHubUrl(string url)
    {
        Assert.True
        (
            Uri.TryCreate(url, UriKind.Absolute, out var uri),
            $"SourceLink mapping is not an absolute URI: {url}"
        );

        Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        Assert.Equal(RawHost, uri.Host, ignoreCase: true);

        Assert.True
        (
            uri.AbsolutePath.StartsWith($"/{RepoSlug}/", StringComparison.OrdinalIgnoreCase),
            $"SourceLink mapping path does not name {RepoSlug}: {uri.AbsolutePath}"
        );
    }



    private static string LocateRuntimePdb()
    {
        // ProjectReference copies the runtime assembly's PDB into this test
        // project's output directory.
        return Path.Combine(AppContext.BaseDirectory, RuntimePdbFileName);
    }



    /// <summary>
    /// Returns the SourceLink prefix mappings that point at this repository,
    /// as (localPathPrefix, urlPrefix) pairs with the trailing '*' removed.
    /// </summary>
    private static List<(string LocalPrefix, string UrlPrefix)> ReadOurSourceLinkMappings()
    {
        var pdbPath = LocateRuntimePdb();
        Assert.True(File.Exists(pdbPath), $"Runtime PDB not found at {pdbPath}");

        using var stream = File.OpenRead(pdbPath);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();

        var payload = ReadSourceLinkPayload(reader);
        Assert.False(string.IsNullOrEmpty(payload), "PDB has no SourceLink CustomDebugInformation record.");

        using var doc = JsonDocument.Parse(payload);
        Assert.True
        (
            doc.RootElement.TryGetProperty("documents", out var documents),
            $"SourceLink payload has no 'documents' property: {payload}"
        );

        // Deliberately a loose, slug-only filter. Its job is to separate our
        // mappings from the ones third-party packages contribute, nothing
        // more. Applying the strict host check here instead would mean a
        // mapping with the right repo but a WRONG host got silently filtered
        // out, and the only symptom would be an empty-collection failure.
        // Selecting it loosely and asserting strictly reports the actual
        // defect instead. See AssertIsOurRawGitHubUrl.
        return documents
            .EnumerateObject()
            .Select(entry => (entry.Name, Url: entry.Value.GetString()))
            .Where(entry => entry.Url is not null && entry.Url.Contains(RepoSlug, StringComparison.OrdinalIgnoreCase))
            .Select(entry => (entry.Name.TrimEnd('*'), entry.Url!.TrimEnd('*')))
            .ToList();
    }



    /// <summary>
    /// One shared client for the whole suite. A per-call <see cref="HttpClient"/> is disposed
    /// while its socket lingers in TIME_WAIT, so repeated creation exhausts sockets; the analyser
    /// flags it for that reason. A static instance also removes the object-initialiser-inside-using
    /// shape, where a throw during initialisation would leak the half-built client.
    /// </summary>
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };



    /// <summary>
    /// Picks a source document from the PDB, matches it against a SourceLink
    /// prefix mapping and substitutes the remainder into the URL, yielding a
    /// URL that names an actual file. Returns <c>null</c> when nothing matches.
    /// </summary>
    private static string? BuildProbeUrl(List<(string LocalPrefix, string UrlPrefix)> mappings)
    {
        var pdbPath = LocateRuntimePdb();
        using var stream = File.OpenRead(pdbPath);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();

        return reader.Documents
            .Select(handle => reader.GetString(reader.GetDocument(handle).Name))
            .Where(name => name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .SelectMany
            (
                name => mappings
                    .Where(m => m.LocalPrefix.Length > 0 && name.StartsWith(m.LocalPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.UrlPrefix + name.Substring(m.LocalPrefix.Length).Replace('\\', '/'))
            )
            .FirstOrDefault();
    }



    /// <summary>
    /// Infrastructure check: whether <paramref name="url"/> carries a real commit
    /// SHA and can be probed. A URL still holding the unresolved <c>*</c>
    /// placeholder is the ordinary case for a local, unpushed build, so the probe
    /// is skipped there; in CI, which always builds a pushed commit, it fails.
    /// GitHub Actions sets <c>CI</c>, as does every other common CI.
    /// </summary>
    /// <remarks>
    /// Excluded from coverage because its placeholder branch never runs in CI; it
    /// is the one exclusion the coverage policy allows (a dedicated
    /// infrastructure check).
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private static bool ShouldProbe([NotNullWhen(true)] string? url)
    {
        if (url is null || !url.Contains('*', StringComparison.Ordinal))
        {
            return url is not null;
        }

        Assert.True
        (
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")),
            $"SourceLink URL still holds the unresolved '*' SHA placeholder in CI: {url}"
        );

        return false;
    }



    /// <summary>
    /// Infrastructure check: the HTTP status GitHub returns for
    /// <paramref name="url"/>, or <c>null</c> when the network is unreachable or
    /// the request times out. The deterministic checks above carry the per-PR
    /// gate, so an infra outage must not fail the test.
    /// </summary>
    /// <remarks>
    /// Excluded from coverage because its catch branches only run when the
    /// runner has no network, which never happens in CI; it is the one
    /// exclusion the coverage policy allows (a dedicated infrastructure check).
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private static async Task<HttpStatusCode?> TryGetStatusCodeAsync(string url)
    {
        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return response.StatusCode;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }



    private static string ReadSourceLinkPayload(MetadataReader reader) =>
        reader.CustomDebugInformation
            .Select(reader.GetCustomDebugInformation)
            .Where(cdi => reader.GetGuid(cdi.Kind) == SourceLinkGuid)
            .Select(cdi => Encoding.UTF8.GetString(reader.GetBlobBytes(cdi.Value)))
            .FirstOrDefault()
        ?? string.Empty;
}
