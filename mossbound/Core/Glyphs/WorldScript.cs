using System;
using System.Collections.Generic;

namespace Mossbound.Core;

// The world's secret script: which figure means which glyph, generated from the world seed. Lives on
// the server only and is never sent to players (except the admin atlas).
public sealed class WorldScript
{
    const int MinLines = 4;
    const int MaxLines = 6;
    const int MinExtraMarks = 1;
    const int MaxExtraMarks = 2;

    readonly Dictionary<GlyphFigure, GlyphDef> glyphByFigure = new();
    readonly Dictionary<string, GlyphFigure> figureByCode = new();
    readonly Dictionary<GlyphKind, (int point, Mark mark)> familyMarkByKind = new();

    public WorldScript(long worldSeed)
    {
        PickFamilyMarks(worldSeed);
        foreach (var glyph in GlyphRegistry.All) GenerateFigure(worldSeed, glyph);
    }

    public GlyphDef? Recognize(GlyphFigure figure) => glyphByFigure.GetValueOrDefault(figure);

    public GlyphFigure FigureOf(string code) => figureByCode[code];

    // Every glyph of a kind carries the same mark on the same point, so players can tell its kind.
    public (int point, Mark mark) FamilyMarkOf(GlyphKind kind) => familyMarkByKind[kind];

    // Kinds are processed in enum order, which only grows, so existing family marks never change.
    void PickFamilyMarks(long worldSeed)
    {
        var used = new HashSet<(int, Mark)>();
        foreach (GlyphKind kind in Enum.GetValues<GlyphKind>())
        {
            var rng = new Random(StableHash(worldSeed, "kind:" + kind));
            (int, Mark) familyMark;
            do familyMark = (rng.Next(GlyphFigure.PointCount), RandomMark(rng));
            while (!used.Add(familyMark));
            familyMarkByKind[kind] = familyMark;
        }
    }

    // Each glyph has its own random sequence, so appending glyphs never changes earlier ones.
    void GenerateFigure(long worldSeed, GlyphDef glyph)
    {
        var rng = new Random(StableHash(worldSeed, glyph.Code));
        GlyphFigure figure;
        do figure = RandomFigure(rng, familyMarkByKind[glyph.Kind]);
        while (!glyphByFigure.TryAdd(figure, glyph));
        figureByCode[glyph.Code] = figure;
    }

    static GlyphFigure RandomFigure(Random rng, (int point, Mark mark) familyMark)
    {
        var figure = GlyphFigure.Empty.WithMark(familyMark.point, familyMark.mark);
        figure = AddConnectedLines(rng, figure, rng.Next(MinLines, MaxLines + 1));
        return AddExtraMarks(rng, figure, rng.Next(MinExtraMarks, MaxExtraMarks + 1), familyMark.point);
    }

    // Each new line starts from a point the figure already reaches, so the lines stay connected.
    static GlyphFigure AddConnectedLines(Random rng, GlyphFigure figure, int count)
    {
        var reached = new List<int> { rng.Next(GlyphFigure.PointCount) };
        while (count > 0)
        {
            int a = reached[rng.Next(reached.Count)];
            int b = rng.Next(GlyphFigure.PointCount);
            if (!GlyphFigure.IsAllowedLine(a, b) || figure.HasLine(a, b)) continue;
            figure = figure.WithLine(a, b);
            if (!reached.Contains(b)) reached.Add(b);
            count--;
        }
        return figure;
    }

    static GlyphFigure AddExtraMarks(Random rng, GlyphFigure figure, int count, int familyPoint)
    {
        while (count > 0)
        {
            int p = rng.Next(GlyphFigure.PointCount);
            if (p == familyPoint || figure.MarkAt(p) != Mark.None) continue;
            figure = figure.WithMark(p, RandomMark(rng));
            count--;
        }
        return figure;
    }

    static Mark RandomMark(Random rng) => (Mark)rng.Next((int)Mark.Dot, (int)Mark.DoubleRing + 1);

    // string.GetHashCode differs between runs; this must give the same script on every machine.
    static int StableHash(long seed, string text)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (byte b in BitConverter.GetBytes(seed)) h = (h ^ b) * 16777619;
            foreach (char c in text) h = (h ^ c) * 16777619;
            return (int)h;
        }
    }
}
