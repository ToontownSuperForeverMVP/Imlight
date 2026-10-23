/*
 * Shadow Pips of a Shadow wizard (one whose school of focus is Shadow), in place of the shadow pip rating:
 *
 *   - a battle starts with 1 Shadow Pip, and a Shadow wizard holds at most 1;
 *   - one more comes at the beginning of rounds 4, 7, 10, ... A refill that comes while the pip is still held is
 *     lost: unused refills are never saved up.
 *
 * Shadow Pips are their own currency: they never stand in for normal, power or school pips, nor those for them.
 * The cheat modes (.harmony, .lightnshadow, .pip give) still work as for any wizard.
 */

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal static class ShadowPipRules {

    internal const int MAX_SHADOW_PIPS = 1;
    internal const int STARTING_SHADOW_PIPS = 1;

    /// <summary>Whether a Shadow Pip comes at the beginning of <paramref name="round"/> (rounds count from 1).</summary>
    internal static bool RefillsAt(int round) => round >= 4 && (round - 1) % 3 == 0;

}
