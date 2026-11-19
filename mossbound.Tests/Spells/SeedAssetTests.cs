using System.Text.RegularExpressions;
using Mossbound.Core;

namespace Mossbound.Tests;

// The shipped seed entity: every aspect a heart can become flies as a Seed, so each needs its variant (or the cast
// finds no entity type and nothing flies), and a texture for every slot its model paints.
public class SeedAssetTests
{
    static readonly string[] slots = { "core", "shell", "glass", "rock", "vein" };
    static readonly string assets = TestAssets.Mod;
    static readonly string entity = File.ReadAllText(Path.Combine(assets, "entities/seed.json"));

    // The model a variant uses: its own shapeByType line, else the "*" one.
    static string ModelOf(string code)
    {
        var match = Regex.Match(entity, $@"""\*-{code}"": {{ base: ""mossbound:entity/seed/([\w-]+)""");
        if (!match.Success) match = Regex.Match(entity, @"""\*"": { base: ""mossbound:entity/seed/([\w-]+)""");
        return match.Groups[1].Value;
    }

    static IEnumerable<string> SlotsOf(string model)
    {
        using var shape = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, $"shapes/entity/seed/{model}.json")));
        return shape.RootElement.GetProperty("textures").EnumerateObject().Select(t => t.Name).ToList();
    }

    [Fact]
    public void EveryCastableAspectHasASeedVariantAndTheTexturesItsModelPaints()
    {
        var states = Regex.Match(entity, @"code: ""aspect"", states: \[([^\]]*)\]").Groups[1].Value;
        Assert.All(Recipes.CastableAspects, aspect =>
        {
            string code = aspect.ToString().ToLowerInvariant();
            Assert.Contains($"\"{code}\"", states);
            var textures = Regex.Match(entity[entity.IndexOf("texturesByType")..], $@"""\*-{code}"": {{ (.*) }},?\r?\n").Groups[1].Value;
            foreach (var slot in SlotsOf(ModelOf(code)))
            {
                Assert.Contains($"\"{slot}\": {{ base: \"mossbound:entity/seed/{code}/{slot}\" }}", textures);
                Assert.True(File.Exists(Path.Combine(assets, $"textures/entity/seed/{code}/{slot}.png")), $"no seed {slot} texture for {code}");
            }
        });
    }

    [Fact]
    public void EverySeedModelPaintsOnlyTheSharedSlots()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(assets, "shapes/entity/seed"), "*.json"))
            foreach (var slot in SlotsOf(Path.GetFileNameWithoutExtension(file)))
                Assert.True(slots.Contains(slot), $"{Path.GetFileName(file)} paints unknown slot {slot}");
    }
}
