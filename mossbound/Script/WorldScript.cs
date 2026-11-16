using System;
using System.Collections.Generic;

namespace Mossbound.Script;

// The world's secret script: which rosette figure means which glyph. Built from the world seed on
// the server only; never send it to clients.
public sealed class WorldScript
{
    const int MinLinks = 4;
    const int MaxLinks = 6;
    const int MinExtraMarks = 1;
    const int MaxExtraMarks = 2;

    readonly Dictionary<GlyphFigure, GlyphDef> byFigure = new();
    readonly Dictionary<string, GlyphFigure> figureByCode = new();
    readonly Dictionary<GlyphKind, (int point, Mark mark)> motifByKind = new();

    public WorldScript(long worldSeed)
    {
        // Family motif: every glyph of a kind carries the same mark on the same point.
        // Kinds are processed in enum order (append-only) so existing motifs never change.
        var usedMotifs = new HashSet<(int, Mark)>();
        foreach (GlyphKind kind in Enum.GetValues<GlyphKind>())
        {
            var rng = new Random(StableHash(worldSeed, "kind:" + kind));
            (int, Mark) motif;
            do motif = (rng.Next(GlyphFigure.PointCount), (Mark)rng.Next((int)Mark.Dot, (int)Mark.DoubleRing + 1));
            while (!usedMotifs.Add(motif));
            motifByKind[kind] = motif;
        }

        // Per-glyph RNG in registry order, so appending glyphs never changes earlier ones.
        foreach (var glyph in GlyphRegistry.All)
        {
            var rng = new Random(StableHash(worldSeed, glyph.Code));
            while (true)
            {
                var figure = RandomFigure(rng, motifByKind[glyph.Kind]);
                if (byFigure.TryAdd(figure, glyph))
                {
                    figureByCode[glyph.Code] = figure;
                    break;
                }
            }
        }
    }

    public GlyphDef? Recognize(GlyphFigure figure) => byFigure.GetValueOrDefault(figure);

    public GlyphFigure FigureOf(string code) => figureByCode[code];

    public (int point, Mark mark) MotifOf(GlyphKind kind) => motifByKind[kind];

    static GlyphFigure RandomFigure(Random rng, (int point, Mark mark) motif)
    {
        var figure = new GlyphFigure(0, 0).WithMark(motif.point, motif.mark);

        // Connected links: each new link starts from a point already in the figure.
        int linkCount = rng.Next(MinLinks, MaxLinks + 1);
        var touched = new List<int> { rng.Next(GlyphFigure.PointCount) };
        int added = 0;
        while (added < linkCount)
        {
            int a = touched[rng.Next(touched.Count)];
            int b = rng.Next(GlyphFigure.PointCount);
            if (!GlyphFigure.IsAllowedLink(a, b) || figure.HasLink(a, b)) continue;
            figure = figure.WithLink(a, b);
            if (!touched.Contains(b)) touched.Add(b);
            added++;
        }

        // One or two extra marks, each on its own point, never on the motif point.
        int marks = rng.Next(MinExtraMarks, MaxExtraMarks + 1);
        while (marks > 0)
        {
            int p = rng.Next(GlyphFigure.PointCount);
            if (p == motif.point || figure.MarkAt(p) != Mark.None) continue;
            figure = figure.WithMark(p, (Mark)rng.Next((int)Mark.Dot, (int)Mark.DoubleRing + 1));
            marks--;
        }
        return figure;
    }

    // string.GetHashCode is randomised per process; this must be identical across runs and machines.
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
