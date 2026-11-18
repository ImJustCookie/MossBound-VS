using System.Text.Json;
using System.Text.RegularExpressions;
using Mossbound.Core;

namespace Mossbound.Tests;

// A key missing from en.json shows up in game as the raw key, so every key the code uses must exist.
public class LangTests
{
    static readonly string repo = FindRepo();
    static readonly HashSet<string> keys = JsonSerializer
        .Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(repo, "mossbound/assets/mossbound/lang/en.json")))!
        .Keys.ToHashSet();

    static string FindRepo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "mossbound.sln"))) dir = dir.Parent!;
        return dir.FullName;
    }

    static string Key<T>(string prefix, T value) where T : Enum => prefix + value.ToString().ToLowerInvariant();

    [Fact]
    public void EveryKeyWrittenInTheCodeExists()
    {
        // Literal keys passed to ModText.Get/In/For, Tell and the inspect helper Say. Prefixes completed
        // with an enum name ("aspect-" + ...) are checked by the next test.
        var call = new Regex(@"(?:ModText\.(?:Get|In|For)\((?:[^,()]+,\s*)?|Tell\(|Say\()""([a-z0-9-]+)""");
        var used = Directory.EnumerateFiles(Path.Combine(repo, "mossbound"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
            .SelectMany(f => call.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Where(k => !k.EndsWith('-'))
            .ToList();
        var missing = used.Where(k => !keys.Contains(k)).Distinct().ToList();
        Assert.NotEmpty(used);
        Assert.Empty(missing);
    }

    [Fact]
    public void EveryNameShownInGameHasText()
    {
        var needed = GlyphRegistry.All.Select(g => "glyph-" + g.Code)
            .Concat(Enum.GetValues<Aspect>().Select(a => Key("aspect-", a)))
            .Concat(Enum.GetValues<Mishap>().Select(m => Key("mishap-", m)))
            .Concat(Enum.GetValues<Mishap>().Select(m => Key("mishap-hint-", m)))
            .Concat(Enum.GetValues<Form>().Select(f => Key("form-", f)))
            .Concat(Enum.GetValues<GlyphKind>().Select(k => Key("atlas-kind-", k)))
            .Concat(Enum.GetValues<CircleRing>().Select(r => Key("ring-", r)))
            .Concat(Enum.GetValues<LifeSource>().Select(s => Key("source-", s)));
        var missing = needed.Where(k => !keys.Contains(k)).ToList();
        Assert.Empty(missing);
    }
}
