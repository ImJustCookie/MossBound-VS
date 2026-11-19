using System;

namespace Mossbound.Core;

// How a Seed flies, from its aspect's motion: speed (blocks per second), the gravity pulling it down (blocks per
// second squared: only heavy seeds fall), how far it sways side to side (blocks), and how long it flies before it
// fizzles. Scale reaches further: half the base range more per step past 1.
public readonly record struct SeedFlight(double Speed, double Gravity, double Sway, double Seconds)
{
    public const double BaseRange = 40;
    public const double MaxSeconds = 6; // bounds the server's work per seed

    // A heavy seed (one that falls) is solid: it sinks through water instead of bursting on it.
    public bool Sinks => Gravity > 0;

    public static SeedFlight For(Motion motion, int scale)
    {
        var (speed, gravity, sway) = motion switch
        {
            Motion.Straight => (30.0, 0.0, 0.0),
            Motion.Arc => (24.0, 0.0, 0.0),
            Motion.Heavy => (20.0, 12.0, 0.0),
            Motion.Drift => (16.0, 0.0, 0.0),
            Motion.Wobble => (22.0, 0.0, 0.6),
            Motion.Hover => (12.0, 0.0, 0.0),
            _ => (20.0, 0.0, 0.0),
        };
        double range = BaseRange * (1 + 0.5 * (Math.Max(1, scale) - 1));
        return new SeedFlight(speed, gravity, sway, Math.Min(MaxSeconds, range / speed));
    }
}
