using System.Text.RegularExpressions;
using Mossbound.Core;

namespace Mossbound.Tests;

// The shipped personalities.json: it loads, and every aspect a heart can become has a personality.
public class PersonalityTableTests
{
    static readonly PersonalityTable table = PersonalityTable.Parse(File.ReadAllText(FindFile()));

    static string FindFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "mossbound.sln"))) dir = dir.Parent!;
        return Path.Combine(dir.FullName, "mossbound/assets/mossbound/config/personalities.json");
    }

    [Fact]
    public void EveryCastableAspectHasAPersonality() =>
        Assert.All(Recipes.CastableAspects, aspect => Assert.True(table.Aspects.ContainsKey(aspect), $"{aspect} has no personality"));

    [Fact]
    public void EveryPersonalityDoesSomethingAndLooksLikeSomething()
    {
        foreach (var (aspect, p) in table.Aspects)
        {
            Assert.True(p.Payload.Count > 0, $"{aspect} has no payload");
            Assert.True(p.Kit.Colours.Count > 0 && p.Kit.Colours.All(c => Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$")), $"{aspect} colours");
            Assert.StartsWith("game:sounds/", p.Kit.Sound);
        }
    }

    [Fact]
    public void TheMishapHasALook() => Assert.StartsWith("game:sounds/", table.Mishap.Sound);

    [Fact]
    public void ElementsLookDistinct() =>
        Assert.Equal(8, Enum.GetValues<Element>().Select(e => table.Aspects[Enum.Parse<Aspect>(e.ToString())].Kit.Particles).Distinct().Count());
}
