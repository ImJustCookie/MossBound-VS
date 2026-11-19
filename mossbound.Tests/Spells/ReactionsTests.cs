using Mossbound.Core;

namespace Mossbound.Tests;

public class ReactionsTests
{
    [Fact]
    public void FrostFreezesWhatIsWet() =>
        Assert.Equal(new[] { Reaction.Frozen }, Reactions.For(new[] { Condition.Wet }, Aspect.Frost));

    [Fact]
    public void EmberOnWetTurnsToSteamInsteadOfFire()
    {
        var reactions = Reactions.For(new[] { Condition.Wet }, Aspect.Ember);
        Assert.Equal(new[] { Reaction.SteamBurst }, reactions);
        Assert.True(Reactions.ReplacesPayload(reactions[0]));
    }

    [Fact]
    public void BothWaterAspectsDouseFire()
    {
        Assert.Equal(new[] { Reaction.Doused }, Reactions.For(new[] { Condition.Burning }, Aspect.Water));
        Assert.Equal(new[] { Reaction.Doused }, Reactions.For(new[] { Condition.Burning }, Aspect.Rain));
    }

    [Fact]
    public void EveryConditionMetReactsOnce() =>
        Assert.Equal(new[] { Reaction.SteamBurst, Reaction.Thawed }, Reactions.For(new[] { Condition.Wet, Condition.Chilled }, Aspect.Ember));

    [Fact]
    public void NoConditionNoReaction() => Assert.Empty(Reactions.For(new Condition[0], Aspect.Frost));
}
