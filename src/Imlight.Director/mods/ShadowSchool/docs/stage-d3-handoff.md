# Stage D3 handoff — Merle Ambrose, the binding pact (for Opus 5.5)

Updated at the end of Stage D2.4 (GPT-6 Luna High, 2026-10-23). Read `continuation-status.md` and `quest-dependency-map.md`
through their D2.4 checkpoints first; the design is `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md`, sections 7.2-7.7
(Scenes 6-9, the eight stages, the pact). This file is what D3 needs from what exists now.

## D2.4 audit checkpoint

- No D2.1-D2.3 production defect was reproduced, so D2.4 made no production-code or encounter-data change. It added an
  end-to-end synthetic campaign regression that plays the seven legitimate victories in order and verifies that neither
  completion predicate becomes true before Wethersfield, every chapter and campaign milestone completes, and the final campaign
  completion succeeds only afterward.
- The final encounter offer should require `ConquestEncounters.AllInstructorsComplete(wizard)`, not merely seven battle wins.
  That predicate requires all seven per-character registry stages and all seven chapter quest entries to be complete while Umbral
  Sight works. Continue using the existing registry and journal event replay path; do not add a second victory tracker.
- All seven profiles, private variants and the 15 scripted cheats were inspected and loaded by the isolated QA server. No real
  instructor combat was run. Positions, floor contact, slope, model/portrait presentation, client spell effects and actual cheat
  execution remain unverified; see the severity-ranked evidence and test matrix in `stage-d2.4-audit.md`.
- The Ravenwood same-zone exit path has unit coverage and an isolated server startup, but no real client transfer, defeat/flee, or
  disconnect/reconnect cycle was exercised. Preserve this as a D3 runtime acceptance item.

## State you inherit

- All seven faculty encounters exist and are wired into `ShadowSchool-Conquest` (seven milestones). Order: Falmea (Fire),
  Greyrose (Ice), Balestrom (Storm), Drake (Myth), Wu (Life), Ashthorn (Death), Wethersfield (Balance). Each is one row of
  `ConquestEncounters.Encounters` plus data files; the framework (anchor -> private duel -> return) is unchanged.
- **Ambrose has no encounter, goal, binding, quest or text.** `ConquestEncounters.ById("Ambrose")` is null on purpose, and tests
  assert it (`AllSevenInstructorsAreInThePlansOrderAndTheFinaleIsAbsent`, `TheFinaleStaysAbsent...`). There is no pact state of
  any kind.
- The campaign quest `ShadowSchool-Conquest` **completes** when the seventh milestone (Wethersfield) completes (its last
  `m_goalLogic` entry has `m_completeQuest: true`; D2.3 changed it, tests changed with it). If D3 wants the quest to stay open for an
  Ambrose goal, add the goal and `m_goalsToAdd` to that entry and set `m_completeQuest` false again, and update
  `TheCampaignIsShadowOnlyWithSevenOrderedMilestonesAndNoRewards` and `PlayingTheSevenEncountersInOrder...`. Otherwise gate the
  finale with a new quest that requires `ShadowSchool-Conquest` `Complete`.

## The prerequisite for the finale (use these, do not invent a second tracker)

- Durable truth: `ShadowSchool_Conquest_<Id>` registry stages (`ConquestStage` 1 anchor found, 2 defeated, 3 complete) for the
  seven ids, written only from play (`QuestProgressSource.Play`), only in order, never twice, after a verified private-copy
  battle (`BattleOutcomeEvents` -> `ConquestEncounters.OnBattleEnded`), and before the journal event is queued.
- `ConquestEncounters.AllInstructorsDefeated(wizard)`: Shadow, Umbral Sight works, all seven stages >= Defeated (true as soon as
  the seventh victory is saved). `AllInstructorsComplete(wizard)`: all seven chapters complete (stage 3 and the quest entry).
  Gate the finale's offer on `AllInstructorsComplete`; use `AllInstructorsDefeated` only for anything that must start right after
  the Wethersfield win. Neither writes anything.
- Replay: `OnAttached` replays missing `..._AnchorFound` / `..._Defeated` events from the registry at every attach; a finale
  that needs an event should do the same (persist first, post second).

## Ambrose's intended role (plan 7.2-7.5)

The final private duel is the technical and emotional climax: Ambrose resists because the wizard's method is a genuine threat; he
recognises a pattern from Morganthe, admits he may have failed as a teacher, and rejects coercion. His combat identity per 7.3:
"wards/control, phase-dependent defense only if supported by existing AI/effects; otherwise a tested constrained duel". Victory
forces a narrow, enforceable pact (7.5): Right 1 freedom of study, Right 2 access to the Spiral (no free keys), Right 3 limited
authority over the **specific defensive bindings prepared in the ritual**. Not granted: sovereignty over Wizard City, mastery of
Bartleby, permanent professor enslavement, authority over other players or innocent NPCs, allegiance to Morganthe. Do not turn
the final unlock into unrestricted control of bindings or NPCs.

Fits the code: the seven bindings are Umbral Sight perception rows (`UmbralPerception.Bindings`) used as anchors; the "captured
control" of 7.5 should be new, bounded state (a new registry key or stage value; the framework comment at the top of
`ConquestEncounters.cs` names this extension point), not a power given to Umbral Sight itself.

## Binding progression so far

`LifeClassroom` (WC_SchoolLife), `FireHearth`, `FrostClassroom`, `StormClassroom`, `MythClassroom` (school interiors) and
`DeathInterim`, `BalanceCircle` (WC_Ravenwood, at the instructors' own places). Anchors are perception rows only; the challenge
is the quest's `Anchor` goal (deliberate look: standing still `UmbralPerception.FocusDelay` seconds after a move, quest goal
active, prerequisites met), then `Duel` (a second deliberate look opens the private copy). Nothing starts a duel by proximity.
Existing text: "Seven teachers stand between you and the wards of Ravenwood" (`ConquestInfo`), Vespera's per-chapter lines, and
Wethersfield's lines already point at Ambrose ("After him there is only Ambrose", "Ambrose will not argue as I have").
Ambrose's house is the existing binding `AmbroseWards` (`WC_Headmistress_House`, a perception row from C3); `WC_Headmaster_Tower`
and `WC_Headmistress_House` are zones in `id-lookup/zones.txt`. Pick the finale's room from installed data as D2.x did.

## Combat primitives available (all verified in this repo)

- **Customs** (`spiraldb/Customs/<id>.json`, mod range 1880001+; D3 can use 1880008+): base creature, `Name` shown in battle,
  `Health`, `Level`, `School`, per-school damage/resist/incoming boost/pierce/critical/block, `StartingPips`, power-pip chance, `Deck`
  (card, `Copies`, 0 = unlimited), `Drops`/`XP`/`GoldMin`/`GoldMax` (explicit empty drops suppress inherited loot), `Forms`.
- **BossCheats on a custom** (`"Cheats": {"Added": [...]}`, doc at the top of `Game/Combat/BossCheats.cs`): triggers BattleStart,
  RoundStart/RoundEnd (Round, Every), HealthBelow (once as health first falls under Percent), FromRound (enrage), PlayerCasts
  (School/Kind/Category/CostMin/OnBoss), PlayerPasses, Pips, MinionDefeated, BossDefeated and more; steps Cast (free real spell,
  never fizzles), Heal, HealthTo, Boost, AddCharm (blade/weakness/shield/trap/absorb), Immune, StealPips, Stun, RemoveCharms/Wards/
  Auras, Reflect, Interrupt, SwapHealth, SetPhase/Phase, Minion/Form (a second creature), Say. `MaxTimes`, `Cooldown`, `Phase`,
  `HealthAbove/Under`, `PlayersMin/Max` limit them. This is the supported way to give a boss behavior; the creature AI itself is weak:
  - Aggressiveness on the Treasure Tower floor-30 creatures is 9, so the AI **always attacks if it can pay for a damage card**;
    heals/wards/debuffs in the deck are cast only on a round with nothing castable.
  - A friendly-target card (heal, ward, blade) from the deck is aimed at `SlotIndex` only 80% of the time ("selfish"); otherwise at
    `SlotIndex+1`, i.e. another circle. Do not put friendly buffs or heals in a deck. (Drake's Myth Shield/Mythblade and Greyrose's
    Tower Shield/Iceblade in earlier stages still do; fix the AI aim or script them if you touch those.)
  - `SpellEffectFilter.IsDamageEffect` ignores `kStealHealth`: a drain card in a deck is never chosen. Cast it from a cheat
    (`ApplyStealHealthEffect` heals the caster by damage x `m_healModifier`, 0.5 for Vampire and Wraith; confirmed in code).
  - Minions: `Minion`/`Form` actions exist (`ApplySummonCreature`, cheat `Spawn`); D2 chose not to use them because no summoned
    battle was run end to end. Verify before relying on them, and note a variant room spawns one creature per encounter.
- Spell data: read templates through `CoreObjectFactory.GetCoreTemplate(id)` on a QA server (`scratch` scripts in `.scratch/agent/`
  `d23spells.cs`, `d23rnd.cs`, `d23verify.cs` are working examples). `s_verifiedSpells` in `ConquestCampaignTests.StormMyth.cs`
  records every id, name, school, accuracy and rank used so far.
- Rooms: `ZoneObjects/<Zone>@<Variant>.json` (Remove, Creatures, ReplaceSpawns, Objects with a 560 `CombatSigil8Actor` circle, `Exit`).
  A **same-zone** exit is now safe (`ConquestWorld.Leave` ForceReload + `ZoneService.LeavesVariantCopy`); it has only unit tests.
- Safe failure: a loss, flee or `Left` result credits nothing, keeps the duel goal open and the registry unchanged; reconnecting
  inside a copy with no open session sends the wizard to the exit; a victory is credited once per session.

## Unresolved client acceptance gaps (graphical client: NOT TESTED for any of the seven)

1. No instructor battle has been run in a client or through the agent bridge (it fights only as a logged-in player). Everything about
   the fights is unplayed: balance, round counts, whether scripted casts display and resolve as intended (Spirit Armor, Pixie,
   Vampire, Wraith, Curse, Balanceblade, Muzzle, `AddCharm`, `StealPips`, enrage), whether `HealthBelow` fires at the right moment.
2. Visuals: Dworgyn/Alhazred stand in for Ashthorn/Wethersfield (name set by the custom; model and name tag unverified); Wu's own
   creature; circle placement/ground in every room (no collision or path-node data read), Wethersfield's slope; arrival points.
3. The transfer into and out of a private **Ravenwood** (hub-sized zone, ~97 objects plus extra sigils): load time, the reload, the
   fall-back when the wizard is defeated there (`MSG_SENDTOHUB` + new `LeavesVariantCopy`), the journal and the quest helper arrows.
4. Journal/quest text rendering for the new strings (the client `Root.wad` locale was refreshed from `build/server/mods/ShadowSchool/
   generated`; `install.py status` reads a different generated folder and can mislead, see the status file).
5. `ShadowSchool-Conquest` now completes at the seventh milestone; what the client shows for a completed campaign quest is unseen.

## Risks to plan around

- Fight tuning is by arithmetic only; plan a test with a real Shadow wizard at levels 12-20 and record turn counts (plan 7.4).
- Never put the finale's state in the quest alone: keep it persisted and replayable like the encounters (persist, then post).
- A second private-copy owner (co-op helper) must not receive credit; the framework already ignores helpers
  (`QuestProgressSource.Play` only), keep that for the finale and the pact.
- `ZoneService.LeavesVariantCopy` is a general rule: any future variant whose exit is its own plain zone relies on it.
- `Server/spiraldb-extra/ZoneObjects/WizardCity-WC_Ravenwood.json` (outside the mod) adds two sigils to **every** Ravenwood copy,
  including the faculty copies.
- Do not read or write the live player database or the character Rika CrypticTheatre; use `build/qa-data/<stage>`, ports 32xxx and
  synthetic wizards, and restore `Config/Imlight.ini` (QA edits are listed in the D2.3 checkpoint).
