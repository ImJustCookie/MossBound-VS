namespace Mossbound.Tests;

// The repository's files, found from the test binaries by walking up to the solution.
static class TestAssets
{
    public static readonly string Repo = FindRepo();
    public static readonly string Mod = Path.Combine(Repo, "mossbound/assets/mossbound");

    static string FindRepo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "mossbound.sln"))) dir = dir.Parent!;
        return dir.FullName;
    }
}
