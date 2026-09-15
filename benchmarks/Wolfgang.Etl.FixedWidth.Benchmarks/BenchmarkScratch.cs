using System.IO;

namespace Wolfgang.Etl.FixedWidth.Benchmarks;

/// <summary>
/// Where the file-backed benchmarks put their scratch files.
/// </summary>
/// <remarks>
/// The <c>File_*</c> benchmarks exist to measure the <see cref="FileStream"/> / <see cref="StreamWriter"/>
/// buffering path, not the runner's disk. On a shared GitHub-hosted runner a single stalled flush is an
/// order of magnitude larger than the whole benchmark (#355), so when a RAM-backed tmpfs is available
/// (<c>/dev/shm</c> on the Linux runners) the scratch files go there: same stream code path, no disk
/// latency. Anywhere else, the system temp directory is used as before.
/// </remarks>
internal static class BenchmarkScratch
{
    private const string LinuxTmpfs = "/dev/shm";



    /// <summary>
    /// Gets the directory the file-backed benchmarks write to: the RAM-backed tmpfs when present and
    /// writable, otherwise the system temp directory.
    /// </summary>
    public static string Directory { get; } = Resolve();



    /// <summary>
    /// Builds the full path of a scratch file inside <see cref="Directory"/>.
    /// </summary>
    /// <param name="fileName">The file name, without a directory.</param>
    /// <returns>The combined path.</returns>
    public static string PathFor(string fileName) => Path.Combine(Directory, fileName);



    private static string Resolve()
    {
        if (System.IO.Directory.Exists(LinuxTmpfs))
        {
            try
            {
                var probe = Path.Combine(LinuxTmpfs, Path.GetRandomFileName());
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return LinuxTmpfs;
            }
            catch (IOException)
            {
                // Not writable here; fall through to the system temp directory.
            }
            catch (System.UnauthorizedAccessException)
            {
                // Not writable here; fall through to the system temp directory.
            }
        }

        return Path.GetTempPath();
    }
}
