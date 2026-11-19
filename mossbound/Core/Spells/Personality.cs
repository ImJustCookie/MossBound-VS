using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mossbound.Core;

// How an aspect travels, meets what it hits, and what it leaves behind: read by the forms.
public enum Motion { Straight, Arc, Heavy, Drift, Wobble, Hover }
public enum Impact { Burst, Splash, Shatter, Stick, PassThrough }
public enum Trace { None, Patch, Cloud }

// What a spell does to what it hits. Burn, Freeze and Bash are damage of that kind; Soak and Chill leave a condition.
public enum EffectKind
{
    Burn, Freeze, Bash, Heal, Push, Ignite, Extinguish, Slow, Shield,
    Soak, Chill, FreezeWater, Root, Regrow, Cure, PoisonOverTime, HealOverTime, Wither, Lift, Scatter, Calm, Melt,
}

// How an aspect's particles move: each aspect has its own, so spells read apart by motion, not only colour.
public enum ParticleStyle { Flame, Splash, Shards, Dust, Motes, Ooze, Gust, Bubble, Steam, Rain, Smoke }

// One effect of a payload: its amount at strength 1, and how long it lasts (0 for the effect's own default).
public sealed record PayloadEffect(EffectKind Effect, float Amount, float Seconds = 0);

// How an aspect looks and sounds: colours as "#rrggbb", its particle style, the sound when it lands, and the sound
// when something it left behind ends (the Ward bubble popping).
public sealed record Kit(IReadOnlyList<string> Colours, ParticleStyle Particles, string Sound, string? EndSound = null);

// An aspect's character (docs/spell-system.md): every form expresses it in its own way.
public sealed record Personality(Motion Motion, Impact Impact, Trace Trace, IReadOnlyList<PayloadEffect> Payload, Kit Kit);

// personalities.json: every aspect's personality, and the look of a mishap.
public sealed record PersonalityTable(IReadOnlyDictionary<Aspect, Personality> Aspects, Kit Mishap)
{
    sealed record File(Dictionary<string, Personality> Aspects, Kit Mishap);

    static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // Every name in the file is an enum, so a typo fails here.
    public static PersonalityTable Parse(string json)
    {
        var file = JsonSerializer.Deserialize<File>(json, options)!;
        var aspects = file.Aspects.ToDictionary(entry => Enum.Parse<Aspect>(entry.Key, ignoreCase: true), entry => entry.Value);
        return new PersonalityTable(aspects, file.Mishap);
    }
}
