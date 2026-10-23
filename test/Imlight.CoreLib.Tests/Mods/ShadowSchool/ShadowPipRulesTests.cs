using Imlight.CoreLib.Mods.ShadowSchool;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// The Shadow Pip rules of a Shadow wizard (ShadowPipRules.cs), as the handoff fixes them: a battle starts with
/// one Shadow Pip, a wizard holds at most one, and one more comes at the beginning of rounds 4, 7, 10, ... A refill
/// that arrives while the pip is still held is dropped, never banked.
/// </summary>
public sealed class ShadowPipRulesTests {

    [Fact]
    public void ABattleStartsWithOneShadowPipAndHoldsAtMostOne() {
        Assert.Equal(1, ShadowPipRules.STARTING_SHADOW_PIPS);
        Assert.Equal(1, ShadowPipRules.MAX_SHADOW_PIPS);
        Assert.True(ShadowPipRules.STARTING_SHADOW_PIPS <= ShadowPipRules.MAX_SHADOW_PIPS);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(16)]
    [InlineData(49)]
    public void ARefillComesOnRoundsFourSevenTenAndEveryThirdAfter(int round)
        => Assert.True(ShadowPipRules.RefillsAt(round));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(12)]
    public void NoRefillComesOnAnyOtherRound(int round)
        => Assert.False(ShadowPipRules.RefillsAt(round));

    [Fact]
    public void TheRefillScheduleNeverGivesTwoInARow() {
        var rounds = Enumerable.Range(1, 60).Where(ShadowPipRules.RefillsAt).ToArray();

        Assert.Equal([4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46, 49, 52, 55, 58], rounds);
    }

}
