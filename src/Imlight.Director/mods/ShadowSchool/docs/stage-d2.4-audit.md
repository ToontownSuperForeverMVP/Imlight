# Stage D2.4 — Ravenwood conquest audit and stabilization

Audit date: 2026-10-23. Scope: the seven instructor campaign and the existing private-variant/quest/combat infrastructure before Stage D3. Ambrose's encounter and the binding pact were not implemented.

## Evidence labels and result

- **VERIFIED** — automated tests/builds passed in this checkout, or the isolated QA server reached the logged ready state.
- **SIMULATED** — deterministic server-side tests exercised the progression, event, outcome-observer or transfer decision path without a live client battle.
- **SOURCE-INSPECTED** — traced through the current implementation and installed-data indices, but not observed during a duel/client session.
- **NOT TESTED** — requires a live battle, graphical client, collision evidence or another unavailable runtime observation.

No production defect was reproduced during this pass. The stabilization change adds an end-to-end campaign regression test that was missing: prior tests proved each encounter independently and tested the all-seven predicate by directly setting registry values. The new test earns each stage through the battle-outcome observer and quest hooks.

## Scope and safety

Read the current continuation checkpoint, dependency map, Stage D3 handoff, Opus 5.5 plan, all seven encounter quest templates/custom profiles/private variants, conquest and Umbral code/tests, battle outcome reporting, boss-cheat dispatch/effect code, `ZoneService` same-zone exit logic, and `ZoneVariants` persistence. The pre-existing worktree was already broadly dirty; no existing changes were reverted, staged or committed. The live player database and Rika CrypticTheatre's record were not accessed. No graphical client was started and no client executable or Root.wad was modified.

## Progression audit

| Requirement | Result | Evidence |
|---|---|---|
| Campaign and chapters require Shadow, completed Nightside (`WC-COMMONS-MAIN-004`), completed Umbral Sight lesson and the persisted Umbral Sight state | **SOURCE-INSPECTED / SIMULATED** | All eight quest templates carry these requirements; `MeetsPrerequisites` repeats the state checks on the server. Umbral Sight itself only unlocks after the recorded Nightside walk. |
| Order Falmea → Greyrose → Balestrom → Drake → Wu → Ashthorn → Wethersfield | **SIMULATED** | Each encounter requires every earlier chapter to be complete. Campaign milestones are in the same order and advance serially. Existing tests reject later chapters/milestones early. |
| Binding discovery and private battle initiation | **SIMULATED** | Active chapter anchor goal, valid sight, movement then focus delay at the exact binding, correct zone and no current session are required. The next deliberate look opens the private copy. |
| Per-character verified victory | **SIMULATED** | The observer requires an open `InCopy` session for that character, the encounter's exact zone and variant, an instance battle, a win, and the expected custom creature. A helper, wrong copy, unrelated battle, loss, flee or disconnect earns no victory. |
| Campaign milestone cannot be forged by a command/event | **SIMULATED** | Commands are denied for campaign goals and duel goals. The zone-event validator requires the matching event plus a persisted `Defeated`/`Complete` registry stage. |
| Reconnect/event replay and quest recovery | **SIMULATED** | Victory registry state is saved before posting the journal event; attach replays verified defeat events. A defeated but unfinished chapter can be reaccepted and its anchor/victory replayed. Login reconciliation completes a chapter only when both the durable defeat and completed quest flag exist. |
| Seven legitimate wins and chapter turn-ins satisfy D3's final gate | **SIMULATED** | New `SevenVerifiedInstructorVictoriesCompleteEveryChapterAndEveryCampaignMilestone` advances all seven via `BattleOutcomeEvents`, event validation, return goals and chapter completion. `AllInstructorsDefeated` becomes true only at Wethersfield; `AllInstructorsComplete` requires all seven chapter completion flags/stages. |
| Finale absent | **VERIFIED** | No Ambrose encounter, milestone, chapter quest, or finale logic was added. Existing tests assert absence. |

## Encounter data audit

The seven public Ravenwood instructor NPCs remain canonical. Variant files remove only the encounter's public instructor and place one custom creature plus one combat sigil in the private copy; no base public school ZoneObjects file is replaced. Each custom has explicit empty drops, XP and gold, so a battle win cannot duplicate a loot reward. Chapter completion is the reward.

| Order | Instructor | Public NPC ID | Combat representation → custom | Variant zone | Result |
|---:|---|---:|---|---|---|
| 1 | Dalia Falmea | 38208 | 403089 → 1880001 | Fire School | Fire profile, level 10, 950 HP. |
| 2 | Lydia Greyrose | 38172 | 1475612 → 1880002 | Frost School | Ice profile, level 11, 1000 HP. |
| 3 | Halston Balestrom | 38141 | 1475613 → 1880003 | Storm School | Storm profile, level 12, 900 HP. |
| 4 | Cyrus Drake | 38206 | 1475614 → 1880004 | Myth School | Myth profile, level 13, 1100 HP. |
| 5 | Moolinda Wu | 38204 | 1475615 → 1880005 | Life School | Life profile, level 14, 1300 HP. |
| 6 | Malorn Ashthorn | 81102 | Dworgyn 1475616 → 1880006 | Ravenwood | Death profile, level 15, 1200 HP. |
| 7 | Arthur Wethersfield | 81103 | Alhazred 1475617 → 1880007 | Ravenwood | Balance profile, level 16, 1500 HP. |

**VERIFIED / SOURCE-INSPECTED:** all seven IDs, order, variant metadata, profile IDs, creature decks, spell IDs and empty reward data are asserted by conquest tests. The first four use the available installed encounter creatures; Wu has her own installed creature. Installed ID data contains NPC records for Malorn and Arthur but no creature templates named for either; the available Death/Balance Treasure Tower creatures are the current compatible representations. Their custom display names are set, and their school/stats/decks match the represented combat school.

**NOT TESTED:** battle portrait/name-tag rendering, model/pose mismatch for Dworgyn and Alhazred, spell animations, client aggro, journal/quest presentation, or whether the custom creature names render as intended. No speculative model or portrait replacement was made.

### Position and terrain findings

- **SOURCE-INSPECTED:** the Balestrom and Drake circles sit 90 horizontal game units from their respective instructor placements, at the instructor's Z. This is a plausible intentional side offset, not verified floor contact.
- **SOURCE-INSPECTED:** Wu's circle is also offset from the Life School instructor on the same nominal Z=100 platform.
- **SOURCE-INSPECTED / NOT TESTED:** Ashthorn and Wethersfield are placed in Ravenwood, not school interiors. Their circles are at the nearby installed `Player Stand In` coordinates. Wethersfield's NPC placement is Z=7.18 while the circle/stand-in is Z=-30.5, a vertical difference of about 37.7 units. The installed terrain is sloped; collision or graphical contact was not observed.
- **NOT TESTED:** actual floor contact, fall hazards, aggro radius, walkability, circle activation, arrival height, and exit height for every encounter. The current coordinates remain candidates, not gameplay-verified placement.

## Scripted boss mechanics audit

The 15 custom scripts are the five additions each for Wu, Ashthorn and Wethersfield. Profile tests parse all 15, verify supported trigger/action/target names, check every cast ID/name/school against the installed-data spell table, and verify the caps and enrage shapes. **SOURCE-INSPECTED:** `CombatResolver.BossCheats` dispatches BattleStart, RoundStart/RoundEnd, HealthBelow, PlayerCasts and FromRound; per-cheat counts and once-state live on a resolver created per duel. Cast steps go through the normal queued-action/effect applicator path with zero pip cost. `StepTargets` resolves explicit `Self`, `FirstEnemy` and `Caster` targets. `AddCharm` creates ordinary hanging charm/ward effects; shields are used by the normal combat ward path. `Boost` adds an aura-like hanging effect; `StealPips` takes from the triggering player and transfers the amount to the boss. `kStealHealth` applies damage and heals the caster by its configured heal modifier (0.5 for Vampire/Wraith when unset). A new duel gets fresh usage counters.

| Custom | Scripted moves | Audited trigger/effect contract |
|---|---:|---|
| Wu / 1880005 | 5 | BattleStart Spirit Armor on self; Pixie under 65% and 35% (one each); Spirit Armor at rounds 4 and 8 (two maximum); +15% damage every second round from round 9. Maximum scripted healing 800 and absorb 1600. |
| Ashthorn / 1880006 | 5 | BattleStart Curse on first enemy; Vampire at rounds 2/5/8/11 (four maximum); Wraith once under 40%; steal one normal pip from a player casting a 4+ pip card (three maximum); +10% damage every second round from round 9. Maximum modeled healing is 920 before mitigation. |
| Wethersfield / 1880007 | 5 | BattleStart Balanceblade on self; Muzzle rounds 3 and 7; add a 25% all-school shield after landed damage on him (three maximum); below 50%, add a 35% Shadow shield and blade once; +10% damage every second round from round 10. No scripted healing. |

**NOT TESTED:** no real battle executed any of these 15 triggers or effects. Startup does not cause custom encounters to spawn, so the isolated QA startup is not proof of script execution. Spell registration/profile parsing is not execution. Runtime uncertainties include client presentation/timing, dispels, effect consumption order, threshold boundary timing, action ordering when multiple conditions coincide, and practical fight length/balance. The resolving code retries legacy dispel-immune scripted casts; actual animation and fizzle behavior still need a client duel.

## Private-zone isolation audit

**SIMULATED / SOURCE-INSPECTED:** public→private entry marks a per-character variant and forces a same-zone reload; private→different public zone clears the variant through attach; private→same public zone is detected by `ZoneService.LeavesVariantCopy`, which clears the note and forces reload. `ConquestWorld.Leave` clears the note before transfer, and explicitly forces reload for Ashthorn/Wethersfield whose destination is Ravenwood itself. Variant notes persist for restart/reconnect and are discarded on attach to a different zone. Tests cover other-variant copies, transfer timeout, reconnect without an open session, victory replay, loss/flee retry, and note clearing predicates.

**NOT TESTED:** network transfer ACK/NACK races, actual client disconnect during transfer, a real server process restart while a character is in a copy, and an actual defeat/flee transfer from a graphical battle. `LeavesVariantCopy`'s unit test exercises the decision predicate, not the complete socket/actor/client handshake. No player was left in a live private copy because no graphical battle was played.

## Findings by severity

| Severity | Finding | Status / handling |
|---|---|---|
| High | Campaign victory credit could be forged by an event/command or another player's battle | No defect found in current code. Multiple server-side gates and regression tests reject these paths. **SIMULATED.** |
| High | Fewer than seven wins unlock D3 | No defect found. Final gate derives from all seven durable stages and, for the offer-ready predicate, all seven completed chapters. New end-to-end progression simulation passes. **SIMULATED.** |
| Medium | Balestrom/Drake circle offset and Ravenwood terrain may be physically wrong | Suspicious coordinates documented above; no geometry mutation without graphical/collision evidence. **NOT TESTED.** |
| Medium | Ashthorn/Wethersfield use Dworgyn/Alhazred combat representations | Compatible school/stat/deck custom data is verified; visual/presentation fidelity is **NOT TESTED**. No speculative replacement. |
| Medium | Boss script runtime and balance | Trigger/effect path traced and script data validated, but no real duel. **NOT TESTED.** |
| Low | Same-zone private exit depends on ForceReload and variant note lifecycle | Existing fix is consistent in code and predicate/flow simulations; full client transfer remains **NOT TESTED**. |

No D2.4 production-code fix was justified by current evidence. The only code artifact added is a regression test for complete seven-stage progression. The user's prior D2.3 same-zone Ravenwood fix remains present and covered; this pass did not alter it.

## Validation matrix

| Validation | Result |
|---|---|
| New seven-victory end-to-end server-side campaign simulation | **PASS**, 1/1 |
| Conquest + Umbral Sight focused tests | **PASS**, 252/252 on three consecutive runs |
| All ShadowSchool tests | **PASS**, 367/367 on three consecutive runs |
| Full CoreLib tests | **PASS**, 380/380 |
| Client installer tests | **PASS**, 2/2 |
| CoreLib Release build | **PASS**, 0 errors |
| Director Release build | **PASS**, 0 errors |
| Isolated QA startup | **PASS**, fresh `ShadowSchoolD24Audit` DB, temporary ports 32300/32333/32500/32808 plus Akka 12999; ShadowSchool enabled, 19 SpiralDB files loaded, server reached ready state; server stopped after capture |
| QA database/config safety | **PASS**, separate `build/qa-data/d2.4-audit`; normal `build/server/Config/Imlight.ini` and deployment ports were not changed |
| `git diff --check` | **PASS**; no whitespace errors (Git printed existing LF-to-CRLF normalization notices for three unrelated modified files) |

Builds emitted the existing NU1904 Akka.Remote advisory and NU1510 package warning. Running the test/build tasks concurrently also caused transient RavenDB embedded content copy lock retries; the builds completed successfully with zero errors.

## Remaining D3 prerequisites

1. Gate Ambrose's offer with `AllInstructorsComplete(wizard)`; use `AllInstructorsDefeated` only if the scene must begin immediately after the Wethersfield victory. Do not trust the campaign completion flag by itself.
2. Preserve the verified-victory pattern: persist per-character state before posting an event; validate the event against the durable state; replay state-backed events on reconnect.
3. Resolve Ambrose's encounter room, entry/exit, combat representation and exact spell/effect IDs from installed game data. Do not reuse the faculty stand-ins speculatively.
4. Keep the pact limited to the named ritual bindings and per-character state. Do not grant world-wide or other-player control.
5. Before claiming acceptance, play each instructor and the finale in a graphical client and inspect battle scripts, all circles/terrain, private Ravenwood reload/exit, and journal/dialog presentation. Protect the normal deployment and use synthetic QA characters.

## Exact source files changed in this audit

- `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ConquestCampaignTests.StormMyth.cs` — added the seven-victory end-to-end regression simulation.
- `src/Imlight.Director/mods/ShadowSchool/docs/stage-d2.4-audit.md` — this audit report.
- `src/Imlight.Director/mods/ShadowSchool/docs/continuation-status.md` — D2.4 checkpoint.
- `src/Imlight.Director/mods/ShadowSchool/docs/quest-dependency-map.md` — D2.4 dependency and gate notes.
- `src/Imlight.Director/mods/ShadowSchool/docs/stage-d3-handoff.md` — D2.4 handoff and runtime boundary.

Isolated QA artifacts were written under `build/qa-data/d2.4-audit` (fresh database, temporary config, logs and captured PID); the QA server process has been stopped.
