using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Mossbound.Items;

// The held medium turning, bobbing and spinning up while a cast charges. Done here, not in the animation: an
// eased-out animated rotation unwinds backwards. Speed and lift ease, so a release or cancel slows it smoothly.
sealed class HeldMediumMotion
{
    const float IdleSpinSpeed = 40, CastSpinSpeed = 1400; // degrees per second
    const float CastLift = 0.08f, BobHeight = 0.03f;      // blocks

    sealed class State { public float Angle, Speed = IdleSpinSpeed, Lift; public long LastMs; }
    readonly ConditionalWeakTable<Entity, State> states = new();

    // Returns the seconds since the holder's last frame (0 for a second render in the same frame).
    public float Advance(Entity holder, float charge, long nowMs)
    {
        var state = states.GetValue(holder, _ => new State { LastMs = nowMs });
        float dt = Math.Clamp((nowMs - state.LastMs) / 1000f, 0, 0.1f);
        state.LastMs = nowMs;
        state.Speed += (IdleSpinSpeed + CastSpinSpeed * charge * charge - state.Speed) * Math.Min(1, dt * 3);
        state.Lift += (CastLift * charge - state.Lift) * Math.Min(1, dt * 5);
        state.Angle = (state.Angle + state.Speed * dt) % 360;
        return dt;
    }

    public ModelTransform Apply(ModelTransform hand, Entity holder, long nowMs)
    {
        var state = states.GetValue(holder, _ => new State { LastMs = nowMs });
        var tf = hand.Clone();
        // The hold pose keeps the hand's X axis vertical, so this spins the medium upright.
        tf.Rotation.X += state.Angle;
        tf.Translation.X -= (BobHeight * MathF.Sin(nowMs / 400f) + state.Lift) / tf.ScaleXYZ.X;
        return tf;
    }
}
