using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Mossbound.Items;

// How a held medium moves above the palm: it turns slowly and bobs, and spins faster and faster while a cast
// charges. Done in code rather than in the animation, because an eased-out animated rotation unwinds backwards.
// Speed and lift ease toward their targets, so ending or cancelling a cast slows the spin down smoothly.
// Client side; one state per holder seen, dropped with its entity.
sealed class HeldMediumMotion
{
    const float IdleSpinSpeed = 40, CastSpinSpeed = 1400; // degrees per second
    const float CastLift = 0.08f, BobHeight = 0.03f;      // blocks

    sealed class State { public float Angle, Speed = IdleSpinSpeed, Lift; public long LastMs; }
    readonly ConditionalWeakTable<Entity, State> states = new();

    // Moves the holder's medium on to now and returns the seconds since its last frame
    // (0 for a second render in the same frame, such as the shadow pass).
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

    // The item's hand transform turned and lifted by the holder's current motion.
    public ModelTransform Apply(ModelTransform hand, Entity holder, long nowMs)
    {
        var state = states.GetValue(holder, _ => new State { LastMs = nowMs });
        var tf = hand.Clone();
        // In the hold pose the hand's X axis is vertical (pointing down): rotating around it keeps the medium upright.
        tf.Rotation.X += state.Angle;
        tf.Translation.X -= (BobHeight * MathF.Sin(nowMs / 400f) + state.Lift) / tf.ScaleXYZ.X;
        return tf;
    }
}
