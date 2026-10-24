# Wizard City quest dependency map (installed client r806919.Wizard_1_610)

Research snapshot for Stage C1. The quest templates are from the server's loaded SpiralDB checkout at commit `91ad27ddb44d50b1964c6041bff36b7b33e306cc`; localized labels and dialogue strings are from the installed client's `Root.wad` (`F01B3932366FFC810920F403F4EC728F80E03F935C2370B62A15764BE8AE64B1`). The English resources are `Locale/en-US/QuestTitle.lang`, `Locale/en-US/WizQst1628CB.lang`, `Locale/en-US/WizQst1625D8.lang`, and the street quest `WizQst*.lang` resources referenced by each template.

## Enrollment and the shared transition

`WC-UNICORN-MAIN-008` (**Grim Tales**) requires `WC-UNICORN-MAIN-007` complete. Once Grim Tales is complete, one school-specific `To Ravenwood!` quest is eligible. The seven installed variants are:

| School | Exact quest template | School gate | Goal 2 mentor persona |
|---|---|---|---|
| Balance | `WC-COMMONS-MAIN-002-BALANCE` | `ReqIsSchool Balance` | `WC-RAV-NPC09` |
| Death | `WC-COMMONS-MAIN-002-DEATH` | `ReqIsSchool Death` | `WC-RAV-NPC08` |
| Fire | `WC-COMMONS-MAIN-002-FIRE` | `ReqIsSchool Fire` | `WC-RAV-NPC04` |
| Ice | `WC-COMMONS-MAIN-002-ICE` | `ReqIsSchool Ice` | `WC-RAV-NPC03` |
| Life | `WC-COMMONS-MAIN-002-LIFE` | `ReqIsSchool Life` | `WC-RAV-NPC01` |
| Myth | `WC-COMMONS-MAIN-002-MYTH` | `ReqIsSchool Myth` | `WC-RAV-NPC02` |
| Storm | `WC-COMMONS-MAIN-002-STORM` | `ReqIsSchool Storm` | `WC-RAV-NPC05` |

Every variant also requires `WC-UNICORN-MAIN-008` complete. Each has the same four-goal progression: Ravenwood introductions, school-specific mentor, `WC-GTW-Registrar`, then `WC-HUB-NPC01` (Ambrose). The seven variants use `QuestTitle_00001718` and shared enrollment dialogue keys in `WizQst1628CB`; the client title resource labels the quest **To Ravenwood!**. Its four sequential goals are persona goals: Ravenwood introductions (`WC-HUB-NPC08-B`), the school-specific mentor, registration (`WC-GTW-Registrar`), then the Headmaster (`WC-HUB-NPC01`). The exact shared dialogue keys include professor introductions (`WizQst1628CB_00000007`–`00000014`), dorm guidance (`00000023`–`00000024`), and the return to the Headmaster (`00000032`). Every variant retains the same goal logic and end reward: `WC-COMMONS-MAIN-002-STORM` drop-table result.

Stage C1 adds `WC-COMMONS-MAIN-002-SHADOW`, gated by the same Grim Tales completion plus `ReqIsSchool Shadow`. It preserves the shared introduction, registrar, Ambrose, goal logic and reward. Its school-specific mentor goal targets the existing `ShadowSchool-Tutor` persona (Vespera Quillmoor) and Library Book Stacks location.

The shared join is `WC-COMMONS-MAIN-003`, **Panic in Three Streets!** (`QuestTitle_1625D8`). Its existing single goal is to speak to `WC-ST02-NPC01` (Sergeant Muldoon); its prerequisite list is an OR over the seven original enrollment completion flags. Stage C1 adds the Shadow enrollment completion flag to that OR list and leaves the goal, dialogue, and reward intact.

## Three Streets branches

After `WC-COMMONS-MAIN-003` completes, its three Olde Town route quests are independently eligible:

| Route | First route quest | Localized title | Street mainline chain | Completion flag for C1 check-in |
|---|---|---|---|---|
| Triton Avenue | `WC-OLDE-MAIN-002A` | Trouble on Triton Avenue | `WC-TRITON-MAIN-001` → `002` → `003` → `004` → `005` → `006` → `007` → `008` → `009` | `WC-TRITON-MAIN-009` |
| Firecat Alley | `WC-OLDE-MAIN-002B` | Putting Out the Fire! | `WC-FIRECAT-MAIN-002` → `003` → `004` → `005` → `006` → `007` | `WC-FIRECAT-MAIN-007` |
| Cyclops Lane | `WC-OLDE-MAIN-002C` | A Good Day to Cyclops | `WC-CYCLOPS-MAIN-001` → `002` → `003` → `004` → `005` | `WC-CYCLOPS-MAIN-005` |

Each Olde Town route quest requires `WC-COMMONS-MAIN-003` complete. Each following street quest requires the immediately preceding quest in its own chain; no street chain requires completion of either other street. Therefore all six permutations of the three street routes satisfy the installed quest graph.

The street templates preserve these installed goal structures; each quest completion flag is its template name with `_Complete` in that character's quest registry:

| Street | Template chain and installed goal types |
|---|---|
| Triton | `WC-TRITON-MAIN-001` Bounty + Persona; `002` Persona + BountyCollect + Persona; `003` BountyCollect + Persona; `004` Persona; `005` Usage + BountyCollect + Persona + Waypoint + Usage + Persona; `006` Persona; `007` Waypoint + Bounty + Persona + Persona; `008` Persona + Usage + Persona + Persona; `009` Persona |
| Firecat | `WC-FIRECAT-MAIN-002` Persona + Bounty + Persona; `003` Persona + Bounty + Persona + Waypoint + BountyCollect + Persona; `004` BountyCollect + Persona + Waypoint + Bounty + Persona; `005` Persona + Persona + Waypoint + Usage + Persona; `006` Waypoint + Bounty + Persona + Persona; `007` Persona + Persona |
| Cyclops | `WC-CYCLOPS-MAIN-001` Bounty + Persona; `002` six Persona goals; `003` Bounty + Waypoint + BountyCollect + three Usage goals + Persona; `004` Bounty + Persona; `005` Waypoint + Bounty + Usage + two Persona goals |

The route entry objectives are also unchanged: Triton (`WC-OLDE-MAIN-002A`) is Waypoint + Persona; Firecat (`WC-OLDE-MAIN-002B`) is Waypoint + Persona + Bounty + Persona; Cyclops (`WC-OLDE-MAIN-002C`) is Waypoint + Persona.

Stage C1 adds three separate Vespera check-in quests. Each is school-gated to Shadow and requires only its own street's final completion flag above. Each has one persona goal at Vespera in the Library, no reward, and a street-specific offer/reaction. None requires another check-in, mentions another street, mutates another character's registry, or changes the canonical street quest objectives.

## Presentation and state ownership

The Shadow enrollment and check-ins are ordinary SpiralDB quest templates. Their school and completion requirements are evaluated against the interacting wizard. Quest instances and completion registry are held on that character's `ServerQuestBehavior` and saved with the wizard record. `InteractQuestOfferComponent` refreshes offer state by `CharId`. These code paths support per-character presentation; mixed-client behavior and persistence through a live reconnect still need in-client verification.

## Verified localization anchors

- `QuestTitle_00001718` → **To Ravenwood!**
- `WizQst1628CB_00000007`–`00000014` → the seven professor introductions; `00000023`–`00000024` → dorm guidance; `00000032` → return to the Headmaster.
- `QuestTitle_1625D8` → **Panic in Three Streets!**
- `WizQst1625D8` contains the Muldoon crisis and its three-street handoff.
- `QuestTitle_1627AE` → **Menacing Minions**; this is the first Triton Avenue street quest after its route quest.
- `QuestTitle_1627AD` → **Trouble on Triton Avenue**; this is the route quest `WC-OLDE-MAIN-002A`.
- `WC-OLDE-MAIN-002B` and `WC-OLDE-MAIN-002C` localization resolves to **Putting Out the Fire!** and **A Good Day to Cyclops** respectively.

## Stage C2 — canonical mainline reconnaissance (read-only, 2026-10-22)

This section maps the installed `r806919.Wizard_1_610` campaign after the three streets. Quest fields and goal/reward objects were read from the live server's loaded `SpiralDB` templates; localized text and actor-dialog keys were read from the installed `Root.wad` without modifying it. English text is in the `Locale/en-US` files named below. Titles are also confirmed by the repository's phrase lookup. Quest-completion flags use the template name plus `_Complete` in the character quest registry. “Drop table” below means the template's actual `ResDropTable` result; it does not imply a custom Shadow reward.

### Dependency spine

```text
WC-COMMONS-MAIN-003 Panic in Three Streets! (requires any one enrollment branch; Shadow added by C1)
  └─ all three independent routes can be done in any order:
       WC-TRITON-MAIN-009_Complete
       WC-CYCLOPS-MAIN-005_Complete
       WC-FIRECAT-MAIN-007_Complete
          └─ WC-OLDE-MAIN-003A A Brief History of the Spiral (AND across all three flags)
               └─ WC-HAUNTED-MAIN-001 Throwing Nightshade
                    └─ WC-HAUNTED-MAIN-002 Things and Stuff
                         └─ WC-COMMONS-MAIN-004 Take a Walk on the Nightside
                              └─ WC-COMMONS-MAIN-005 A Whole New World
```

The first node's stock title is `QuestTitle_1625D8`; its sole goal is persona `WC-ST02-NPC01` (Sergeant Muldoon). Its prerequisites are the eight school-specific enrollment-completion flags with `ROP_OR` (the seven stock schools and the C1 Shadow branch); reward is `ResDropTable(WC-COMMONS-MAIN-003)`. The C1 branch changes only the OR list. The street chain names above are the actual three final completion flags checked by the reunion quest; the earlier section records their independent quest chains and objective types.

### Per-quest installed structure

| Quest / completion flag | Prerequisites | Goals, in installed order | Reward and localization |
|---|---|---|---|
| `WC-OLDE-MAIN-003A` / `WC-OLDE-MAIN-003A_Complete` — **A Brief History of the Spiral** | AND: `WC-TRITON-MAIN-009`, `WC-CYCLOPS-MAIN-005`, `WC-FIRECAT-MAIN-007` complete | 1. Persona `WC-HUB-NPC01` (Ambrose). 2. Bartleby proximity tag `AR-MidEM-MAIN-001_Explore_Bartleby` (Eye of History / failing-memory scene). 3. Enter `WizardCity/WC_Streets/Interiors/WC_OldeTown_T2`. 4. Kill one `Foulgaze` (`BT_MOB_KILL`, adjective `Foulgaze`, target descriptor `WizardMobs_00000030`). 5. Enter `WizardCity/WC_Streets/WC_HauntedCave`. 6. Persona `WC-TRITON-NPC06` (Daisy Willowmancer). | `ResDropTable(WC-OLDE-MAIN-003A)`: 250 XP, no gold; generic Low Wooden Table plus school rings for Balance, Death, Fire, Ice, Life, Myth and Storm (no Shadow ring row). Title `QuestTitle_1731AB`; dialogue `WizQst1731AB_00000001`–`_00000009`; objective descriptors include `_00000017`, `_00000021`, `_00000025`. |
| `WC-HAUNTED-MAIN-001` / `_Complete` — **Throwing Nightshade** | AND: `WC-OLDE-MAIN-003A` complete | 1. Enter `WizardCity/WC_Streets/Interiors/WC_HauntedCave_T1` (Stormdrain Tower). 2. Kill one `Lord_Nightshade` (`BT_MOB_KILL`, descriptor `WizardMobs_00000006`). 3. Speak to Daisy (`WC-TRITON-NPC06`). | `ResDropTable(WC-HAUNTED-MAIN-001)`. Title `QuestTitle_1731AD`; actor dialogue `WizQst1731AD_00000001`–`00000004` for Daisy and `_00000006`–`00000009` for Nightshade. Daisy is the actual quest giver and souvenir/investigation handoff; Nightshade stays hostile. |
| `WC-HAUNTED-MAIN-002` / `_Complete` — **Things and Stuff** | AND: `WC-HAUNTED-MAIN-001` complete | 1. Defeat Field Guards and collect two Pumpkin Heads (`Harvest_Guard`, mob descriptor `WizardMobs_00000360`; tally `WizQst1731AE_00000010`, count 2). 2. Speak to Daisy (`WC-TRITON-NPC06`). 3. Speak to Ambrose (`WC-HUB-NPC01`). | `ResDropTable(WC-HAUNTED-MAIN-002)`. Title `QuestTitle_1731AE`; Daisy dialogue `WizQst1731AE_00000001`–`00000007`, with the return/hand-off passages at `_00000008`–`00000009`. Her installed dialogue explicitly proposes testing fiery/ghostly vision from the Pumpkin Heads, cannot complete the reveal herself, and sends the player to Ambrose. |
| `WC-COMMONS-MAIN-004` / `_Complete` — **Take a Walk on the Nightside** | AND: `WC-HAUNTED-MAIN-002` complete | 1. Enter `WizardCity/WC_NightSide`. 2. Speak to Mortis (`WC-RVW-DeathTree`). 3. Enter `WizardCity/Interiors/WC_SchoolDeath`. 4. Speak to Dworgyn (`WC-NIS-NPC01`). 5. Return to Ambrose (`WC-HUB-NPC01`). | `ResDropTable(WC-COMMONS-MAIN-004)` plus six existing `ResLearnSpell` results (IDs `1730822561`, `927448295`, `975200773`, `534093744`, `533943727`, `1627273980`); the spell results are ordinary class-specific canonical rewards, not a new Shadow spell. Title `QuestTitle_1731AF`; Ambrose's quest-start dialogue `WizQst1731AF_00000000`–`_00000004` is the enchantment scene; Mortis/Dworgyn/Death School passages use the same `WizQst1731AF` family. |
| `WC-COMMONS-MAIN-005` / `_Complete` — **A Whole New World** | AND: `WC-COMMONS-MAIN-004` complete | 1. Bartleby proximity tag `AR-MidEM-MAIN-001_Explore_Bartleby`. 2. Enter `Krokotopia/KT_WorldTeleporter`. 3. Speak to `KT-HUB-NPC01` (Sergeant Major Sylvester Quimby Talbot III). | `ResDropTable(WC-COMMONS-MAIN-005)`. Title `QuestTitle_1731B0`; Ambrose's dialogue `WizQst1731B0_00000000`–`_00000010` covers the Death School news, Bartleby/Eye, two Spiral Keys and the Krokotopia handoff. Its existing prerequisites and all three goals remain the travel contract. |

#### NPC and dialogue resources

Quest persona/resource bindings inspected in the loaded templates and actor-dialog entries:

- Muldoon is `WC-ST02-NPC01`; the C2 reunion objective then targets `WC-HUB-NPC01` (Ambrose), `WC-RVW-DeathTree` (Mortis), `WC-NIS-NPC01` (Dworgyn), `WC-TRITON-NPC06` (Daisy), and `KT-HUB-NPC01` (Sergeant Major Quimby). Bartleby is a proximity/tag objective (`AR-MidEM-MAIN-001_Explore_Bartleby`), not a persona goal. Foulgaze and Lord Nightshade are bounty targets, not NPC dialogue personas in these templates.
- The reunion's `WizQst1731AB_00000001`–`_00000002` actor entries are bound to Muldoon template `38218`, portrait `GUI/NpcPortraits/Human_WCGuard_G.dds`, and `Muldoon_005_09/10.mp3`.
- Daisy's actual actor entries use persona template `1520044`, portrait `Portrait_Player_DaisyW_A_NPC.dds`, and `DaisyWillowmancer_005_03`–`_00000006.mp3` for `Throwing Nightshade`, then `_00000009`–`_00000012.mp3` for `Things and Stuff`.
- Ambrose's Nightside actor entries use template `38168`, portrait `GUI/NpcPortraits/Art_Portrait_Ambrose.dds`, and `MerleAmbrose_005_18`–`_00000021.mp3`. The temporary-vision lines are locale keys `WizQst1731AF_00000002`–`_00000004` (the actor entry carrying `_00000002` uses the existing `_19.mp3` voice). The Krok handoff entries use `_24`–`_28.mp3` in the same sound family.
- The voice recordings still contain the canonical lines. A Shadow text replacement cannot keep those recordings and pretend they say new words; the immutable per-player presentation resolver must choose verified compatible text/audio behavior at response time.

#### Resolved reward table contents

The template reward entries point to drop tables, so their actual loaded table values are recorded here as well (XP / gold / item pool):

- `WC-COMMONS-MAIN-003`: 10 XP / 10 gold / no item rows.
- `WC-OLDE-MAIN-003A`: 250 XP / 0 gold / item pool includes Low Wooden Table and school rings for Balance, Death, Fire, Ice, Life, Myth and Storm; no Shadow ring row.
- `WC-HAUNTED-MAIN-001`: 275 XP / 0 gold / item pool includes Lectern and school athames for Balance, Death, Fire, Ice, Life, Myth and Storm; no Shadow athame row.
- `WC-HAUNTED-MAIN-002`: 150 XP / 18 gold / no item rows.
- `WC-COMMONS-MAIN-004`: 320 XP / 66 gold / no item rows, plus six `ResLearnSpell` results (IDs in the quest table above).
- `WC-COMMONS-MAIN-005`: 115 XP / 0 gold / item pool includes Crystal Ball (`160218`).

This exposes a reward-coverage issue to handle during implementation planning: the two randomized school-specific reward pools for the Shadow route omit Shadow items. Do not silently edit those shared drop tables in C2; determine whether a Shadow-only reward path or a narrowly scoped table extension is compatible with non-Shadow reward behavior before changing them.

**Resolved in C2.2:** the mod's `spiraldb/DropTables` overrides append one Shadow-only row (`ShadowSchool-Ring-5` to `WC-OLDE-MAIN-003A`, `ShadowSchool-Athame-5` to `WC-HAUNTED-MAIN-001`) and leave every original row, XP and gold value as installed; see `continuation-status.md`, Stage C2.2.

### What the installed story actually says

- **Bartleby / Foulgaze / Order of the Fang:** In `WizQst1731AB`, Ambrose explains that Bartleby sees through time with the Eyes of History and the Future, that the stolen Eye of History has left Bartleby's memory addled and vision limited, and that Foulgaze must be investigated. He connects Malistaire to the Order of the Fang while explicitly saying they do not yet know why. These are existing dialogue beats during the reunion quest; there is no separate Eye recovery goal in this slice.
- **Daisy / Nightshade:** The actual sequence is `A Brief History` → Daisy gives `Throwing Nightshade` → the player defeats Nightshade and returns to Daisy → `Things and Stuff` collects two Field Guard Pumpkin Heads and returns first to Daisy, then Ambrose. Daisy's curiosity is sincere and eccentric; Nightshade's lines present him as a self-important tyrant and reveal the books as gifts from the concealed Necromancer. Adaptations should let the Shadow wizard recognize coercion and seek knowledge without endorsing either character's methods.
- **Nightside / Mortis / Dworgyn:** `Things and Stuff` is the direct prerequisite. Ambrose's original `WizQst1731AF_00000002` says he enchants the ghostly Pumpkins so their vision transfers to the wizard “at least for a short time”; `_00000003`–`_00000004` completes the cast and sends the wizard to find the hidden path. Mortis, the relocated Death School and Dworgyn are then visited in the existing goal sequence, followed by the return to Ambrose. Dworgyn's locale lines establish the displaced school's history and his confinement; he is not a Ravenwood authority or a suitable additional duel opponent.
- **Krokotopia:** `A Whole New World` is the canonical continuation after Ambrose reports Bartleby's condition. The installed text says the Eye is still missing, Ambrose gives two Spiral Keys, stabilizes Bartleby in his care, and sends the wizard to follow Malistaire to Krokotopia. Preserve those motivations and ordinary teleporter/Quimby objectives.

### Exact Umbral Sight origin

Use the existing **Ambrose quest-start enchantment for `WC-COMMONS-MAIN-004`**, specifically the Pumpkin-vision transfer at `WizQst1731AF_00000002`–`_00000004`, which occurs before the first goal (entering Nightside). That is the only canonical, in-scene event in this mapped sequence that gives the wizard ghostly vision. The Shadow adaptation may let native affinity make this existing temporary vision unusually resonant; do not set the persistent story flag or show active sight before this cast. Keep Mortis, Death School, Dworgyn, Nightside discovery and return goals unchanged. Reveal persistence/disorientation only after the normal Nightside visit/return interaction, when the canonical temporary enchantment should have faded. This preserves Nightshade/Daisy's ordinary preparation and makes Umbral Sight a persistence anomaly of Ambrose's spell, not a replacement Nightside route or an early power.

### Shadow narrative adaptation outline

These are presentation-only adaptations. Keep every existing objective, target, completion edge, drop-table reward, access condition and party-visibility rule above.

1. **Three Streets / Muldoon:** Let the line acknowledge a capable student, but keep crisis urgency and the existing enrollment gate. C1 enrollment must still be legitimately complete; do not use C2 to bypass it.
2. **Reunion / Ambrose:** Make his concern concrete: Bartleby's memory is impaired and the missing Eye leaves Wizard City without its usual witness to history. The Shadow student can be curious about the Eye as a source of authority/knowledge while accepting the immediate rescue/investigation. Ambrose stays prudent and protective, not accusatory. Gate street-specific praise on the actual three completion flags; the canonical reunion quest itself already requires all three.
3. **Foulgaze and the Order:** Keep the one Foulgaze battle. Treat the Eye as a missing source of insight and the Order as a consequential lead; do not imply the player has deciphered either. Malistaire's purpose remains unknown. The player notices that access to knowledge is controlled by institutions and begins questioning who should decide.
4. **Daisy / Nightshade:** Preserve Daisy's Pumpkin Head experiment and all normal handoffs. Her fascination with the under-plane gives the player an invitation to learn, not a Shadow recruiter. Nightshade remains an enemy whose grandiose command over ghosts is a warning about dominion, not an ally or a source of Umbral Sight.
5. **Ambrose's enchantment / Nightside:** Keep the canonical temporary spell and path-finding goal. The Shadow wizard's affinity responds unexpectedly, but does not grant a pre-cast outline, skip a door, reveal arbitrary secrets or alter Mortis/Dworgyn objectives. Only after the trip does the afterimage persist. Ambrose's request to examine it is legitimate risk management; the tutor can warn that perceiving a binding does not make it safe to alter.
6. **Dworgyn / Mortis:** Preserve Dworgyn's dry, gentle voice and Mortis's grave-tree presence. Their original displacement story is not rewritten. Any Shadow reaction is brief and recognizes exile/loss without converting them into faculty-conquest participants.
7. **Krok handoff:** Let the player's curiosity about the Order and access to older knowledge reinforce an independent motive to depart. They pursue Malistaire and information, not Morganthe. Ambrose's two keys, Bartleby's care, the teleporter route and Quimby's arrival beat remain canonical.

### Presentation infrastructure, gaps and risks

Existing quest/service state is already per character (`ServerQuestBehavior`, character quest registry, `CharId`-scoped offer refresh); the C1 Shadow enrollment/check-ins prove that new Shadow-only quest templates and per-character requirements can be represented. Existing production quests, however, currently load shared `QuestTemplate`/`ActorDialogList` data and send `MSG_ACTORDIALOG` using those shared presentation entries. The C1 work has **not** implemented a player-specific resolver for vanilla quest/cinematic dialogue, journal text, objective hints, or shared NPC responses. There is no safe global-text switch keyed only by Shadow school.

Before adaptation code, add an immutable presentation resolver keyed at minimum by character ID, primary school, canonical quest ID, active/completed quest state, completed prerequisite flags, and locale. It must return a per-recipient presentation copy (including dialog string key, compatible sound behavior, portrait/camera/action metadata) and be used at every relevant offer/underway/turn-in path. Keep the canonical loaded template immutable; do not cache a Shadow variant in the shared template, mutate a shared NPC actor, or leak one party member's dialogue to another. C2 also needs a locale delivery mechanism for new text keys compatible with the current generated Root.wad/locale overlay; translated-locale fallback, text wrapping and the installed voice/audio mismatch must be resolved. Existing voice lines cannot be represented as if they speak newly written words. Retain original voice where the line still matches, or use a verified silent/text-only actor-dialog presentation if supported; do not invent voice or audio assets.

Other risks: quest start/turn-in dialogue may be emitted through several components and `QuestService` paths; journal/hint/objective strings are not the same as `MSG_ACTORDIALOG`; per-player changes in shared locations require two-client isolation validation; quest state must remain per character on reconnect; a sequential actor conversation is shared even if text differs per wizard; title/completion strings may be client-cached; the six Nightside reward spell effects must not be mistaken for Umbral Sight; and the existing Shadow enrollment prerequisite must remain enforced.

### First independently testable C2 slice — implemented in C2.1 (2026-10-23)

**Status:** built as `ShadowMainlineDialogue.AmbroseReunion` on the core `QuestDialogueResolver` (see the Stage C2.1 section of `continuation-status.md`). Script manifest row: quest `WC-OLDE-MAIN-003A`; NPC Ambrose (`WC-HUB-NPC01`, goal `Goal`, tag `Completion`); vanilla keys `WizQst1731AB_00000003`–`_00000008`; Shadow condition = Shadow school + `WC-COMMONS-MAIN-002-SHADOW_Complete` + the three street flags + reunion not yet complete; replacement = three added unvoiced lines (`ShadowSchool_ReunionAmbroseStreets`, `_ReunionAmbroseForbidden`, `_ReunionAmbroseEye`) placed between the six kept, voiced canonical lines; journal/hint unchanged; objectives unchanged; pre-pact only (no pact exists yet). Automated tests pass; client rendering is NOT TESTED. The original proposal follows.

Implement one resolver-backed Shadow-only **Ambrose reunion response** in `WC-OLDE-MAIN-003A` after the three street flags, leaving its six goals, AND prerequisites and reward byte-for-byte unchanged. First make a structural contract check that canonical template data is unchanged and that resolver output is selected only for a Shadow recipient. Then test two same-zone recipients (Shadow and a non-Shadow wizard) against the same Ambrose interaction and verify distinct per-character text while both receive the same quest/objective/reward state. This slice exercises the hardest prerequisite—recipient-specific dialogue from shared canonical quest data—without altering Nightside or introducing Umbral Sight early. Actual client playback, locale wrapping and audio treatment remain required acceptance evidence; a unit/structure pass alone is not visual or gameplay proof.

### Stage C3 — Umbral Sight implemented (2026-10-23)

The origin above is now enforced in code. `WC-COMMONS-MAIN-004` is unchanged; the mod records, per character, a trail of
the run (accepted from Ambrose, then `Goal` Nightside, `Goal 1` Mortis, `Goal 2` Death School, `Goal 3` Dworgyn, `Goal 4`
Ambrose) and unlocks `ShadowSchool_UmbralSight` only when the quest completes through play with every step on the trail.
QA commands, late-join replays and out-of-order goals leave a gap and never unlock it. The C2.3 afterimage lines
(`Goal 4` Completion) now also require the trail through Dworgyn, so a QA completion of Goal 4 shows the canonical return.
After the unlock: an unvoiced Ambrose opener in `WC-COMMONS-MAIN-005` Prep (the sight has lasted; see Vespera), and the
mod quest `ShadowSchool-UmbralSight-Lesson` (Vespera, gated on the registry entry, no reward). Details:
`continuation-status.md`, Stage C3.

### Stage D1 — first Ravenwood conquest encounter (2026-10-23)

`ShadowSchool-Conquest-Falmea` (The Hearth Binding): Shadow only, requires `ShadowSchool_UmbralSight`, `WC-COMMONS-MAIN-004` and `ShadowSchool-UmbralSight-Lesson` complete; offered by Vespera. Goals: anchor (Umbral Sight look at Falmea, Fire School) -> defeat Falmea in a private copy of `WC_SchoolFire` (variant `ShadowConquest-Falmea`) -> return to Vespera. No canonical quest, goal, reward or NPC changed. Details: `continuation-status.md`, Stage D1.

### Stage D1.5 — the encounter's transfer and navigation (2026-10-23)

The same-zone handoff needed `ForceReload` (without it `ZoneService` teleports inside the public Fire School and the variant
copy never loads), and the quest's goals now carry their client navigation (`ShadowSchool_ConquestFalmeaLocation` -> Fire
School for the anchor and duel; the Library Book Stacks for the return to Vespera, with the copied Muldoon tag removed). Its
prerequisites, completion flags, goals, events and reward-free completion are unchanged. Stage D2 adds one overarching
`ShadowSchool-Conquest` quest whose per-instructor milestone goals listen for the same `ShadowSchool_Conquest_<Id>_Defeated`
events, and keeps the per-encounter registry as the gate for the Ambrose finale. Details: `continuation-status.md`, Stage D1.5.

### Stage D2.1 — implemented (2026-10-23)

The installed Shadow-only campaign quest is `ShadowSchool-Conquest`. It requires the Umbral Sight unlock, completion of
`WC-COMMONS-MAIN-004`, and the Umbral Sight lesson. Its seven milestone goals are ordered Falmea, Lydia Greyrose, Halston
Balestrom, Cyrus Drake, Moolinda Wu, Malorn Ashthorn, and Arthur Wethersfield. Only Falmea and Greyrose currently have
registered encounters and chapter quests, so the later five milestones are intentionally unreachable. Ambrose is not in
the quest and remains inaccessible as a finale until a future stage.

The playable chapters are:

| Order | Quest | Instructor / public template | Private encounter | Combat identity |
|---|---|---|---|---|
| 1 | `ShadowSchool-Conquest-Falmea` | Falmea / existing Fire School trainer | `ShadowConquest-Falmea`, `WC_SchoolFire` | existing combat representation, custom 1880001 |
| 2 | `ShadowSchool-Conquest-Greyrose` | Lydia Greyrose / 38172 | `ShadowConquest-Greyrose`, `WC_SchoolFrost` | combat representation 1475612, custom 1880002 |

Each chapter retains the anchor -> private duel -> return-to-Vespera flow and no quest completion reward. Verified battle
wins are saved to the character's encounter registry before their journal events are queued; quest event validators only
accept a matching event while the registered encounter has a persisted win. Reconnect replay and reaccept replay rebuild
the chapter and campaign journal in order. Command completion and direct unverified milestone events cannot create
conquest credit. Loss, flee, unrelated combat, another character's battle, and duplicate outcomes do not advance a
milestone or repeat a reward.

Greyrose uses the validated Ice combat representation and custom profile `1880002`, with an Ice-only deck, 1000 health,
zero XP/gold, and explicit empty drops. Her room variant removes Greyrose only inside the wizard's private Frost School
copy and replaces combat representation 1475612 with the custom creature associated with battle circle 560. Same-zone
entry requests `ForceReload`; the original Falmea/Fire room and public instructor templates remain intact. Explicit empty
custom drop lists are now honored by the combat loot resolver; omitted lists continue to inherit the base creature's
tables.

Automated evidence: `ConquestCampaignTests` + `ConquestEncounterTests` 85/85 (four consecutive passing runs), full CoreLib
251/251, installer 2/2, CoreLib and Director Release builds 0 errors, and `git diff --check` clean. The Director deployment
loaded ShadowSchool and its 14 SpiralDB files on an isolated QA database; installed client resources report current.
**GRAPHICAL CLIENT ACCEPTANCE: NOT TESTED** — no instructor battle, journal rendering, NPC animation, combat-circle
presentation, or teleport visual has been verified through the graphical client. See the D2.1 and D2.2 sections of
`continuation-status.md` for exact deployment evidence and the next instructor prerequisites.

### Stage D2.2 — Balestrom and Drake implemented (2026-10-23)

Chapters 3 and 4 of `ShadowSchool-Conquest` now exist; the last three milestones (Wu, Ashthorn, Wethersfield) remain
unreachable and Ambrose's finale does not exist. Each chapter has the same shape as Falmea/Greyrose (anchor -> private duel
-> return to Vespera, no reward) and now additionally requires the previous chapter's `Complete` quest entry; the
framework (`ConquestEncounters.MeetsPrerequisites`) requires **every** earlier encounter complete in the persisted registry,
not only the one before.

| Order | Quest | Public trainer | Private encounter | Combat identity |
|---|---|---|---|---|
| 3 | `ShadowSchool-Conquest-Balestrom` | Halston Balestrom, `WC-RAV-NPC05` / 38141, Storm School (938.90, -1.20, 202.09) | `ShadowConquest-Balestrom`, `WC_SchoolStorm`, anchor binding `StormClassroom` | creature 1475613, custom 1880003 |
| 4 | `ShadowSchool-Conquest-Drake` | Cyrus Drake, `WC-RAV-NPC02` / 38206, Myth School (807.46, 13.78, 100.00) | `ShadowConquest-Drake`, `WC_SchoolMyth`, anchor binding `MythClassroom` | creature 1475614, custom 1880004 |

Campaign milestones `Balestrom` and `Drake` now navigate to their schools (`ShadowSchool_ConquestBalestromLocation`,
`ShadowSchool_ConquestDrakeLocation`). Exits go to Ravenwood outside each school's door (Storm `2752.043,818.4474`,
Myth `-3281.591,3876.725`, from the Ravenwood `StormSchool Exit` / `MythSchool Exit` locations). Arrival is the zone's own
`Start`. Battle circles (template 560, `CombatSigil8Actor`, radius 700) sit 90 units from the instructor at the instructor's
own Z; the Storm and Myth zones have no path-node data, so platform extent is **NOT TESTED in the client**.

Also corrected: the Greyrose chapter named a portrait file that is not a game file (`Art_Portrait_Lydia_Greyrose.dds`); it
now uses the game's `Portrait_Human_Greyrose_A_NPC.dds`.

Fight design (all spells read from installed templates on a QA server): Balestrom, Storm, level 12, **900 hp**, 15% incoming
boost against Shadow, 70%-accurate Thunder Snake/Lightning Bats (unlimited), Storm Shark, Kraken (4 pips, 520-580), Stormblade,
Storm Trap, no wards. Drake, Myth, level 13, 1100 hp, 10% pierce, Pierce x2 (removes a ward), Troll/Cyclops unlimited,
Minotaur, Myth Trap, Mythblade, one Myth Shield. **No minions**: a variant spawns one creature per encounter and no summon
path is verified, so Drake's distinction is ward removal. **Fight balance is NOT TESTED** against a played Shadow wizard.


### Stage D2.3 — Wu, Ashthorn and Wethersfield implemented; all seven instructors playable (2026-10-23)

Chapters 5, 6 and 7 of `ShadowSchool-Conquest` now exist, with the same shape as the first four (anchor -> private duel ->
return to Vespera, no reward, previous chapter's `Complete` required, every earlier encounter complete in the persisted
registry). The seventh campaign logic entry now completes the quest. Ambrose's finale has no encounter, goal, binding or
quest and stays locked.

| Order | Quest | Public NPC (installed placement) | Private encounter | Combat identity |
|---|---|---|---|---|
| 5 | `ShadowSchool-Conquest-Wu` | Moolinda Wu, `WC-RAV-NPC01` / 38204, `WC_SchoolLife` (760.20, 15.49, 100.00) | `ShadowConquest-Wu`, `WC_SchoolLife`, anchor `LifeClassroom` (r 350) | creature 1475615 (her own), custom 1880005 |
| 6 | `ShadowSchool-Conquest-Ashthorn` | Malorn Ashthorn, `WC-RAV-NPC08` / 81102, **`WC_Ravenwood`** (229.29, 6034.79, -30.48) | `ShadowConquest-Ashthorn`, `WC_Ravenwood`, anchor `DeathInterim` (r 300) | stand-in 1475616 (Dworgyn), custom 1880006 named "Malorn Ashthorn" |
| 7 | `ShadowSchool-Conquest-Wethersfield` | Arthur Wethersfield, `WC-RAV-NPC09` / 81103, **`WC_Ravenwood`** (558.59, 444.97, 7.18) | `ShadowConquest-Wethersfield`, `WC_Ravenwood`, anchor `BalanceCircle` (r 300) | stand-in 1475617 (Alhazred), custom 1880007 named "Arthur Wethersfield" |

Dialogue personas and portraits are the game's own (`WC-RAV-NPC01/08/09_Persona`; `Portrait_Cow_Wu_A_NPC.dds`,
`Art_Portrait_Malorn_Ashthorn.dds`, `Dog_Wethersfield_A_NPC.dds`, read from installed quest data). Campaign milestones
navigate to `WC_SchoolLife` and `WC_Ravenwood` (`ShadowSchool_ConquestWuLocation`, `...AshthornLocation`,
`...WethersfieldLocation`). Exits: Wu goes to the Ravenwood `LifeSchool Exit` (`-2646.668,797.2547`); Ashthorn and
Wethersfield arrive at and leave to the game's `Loc_WC-COMMONS-MAIN-002-DEATH` (641.84, 5968.79, -28.79) and
`Loc_WC-COMMONS-MAIN-002-BALANCE` (322.98, -171.08, -28.79), both outside their circle's radius and every binding's reach.

Because Ashthorn and Wethersfield live in Ravenwood, their copy, anchor and exit are one zone: `ConquestWorld.Leave` forces a
reload for a same-zone exit, and `ZoneService.LeavesVariantCopy` makes any transfer to the plain zone from a noted variant copy
of it (duel flee/loss `MSG_SENDTOHUB`, doors, triggers) reload and forget the copy. The Ravenwood copies include the plain
Ravenwood file's extra sigils like any copy; only the owner is in them.

Battle circles (template 560, `CombatSigil8Actor`): Wu at (850.20, 15.49, 100.0), 90 units from her, same convention as the
other four school rooms (radius 700). Ashthorn at (297.50, 6041.75, -30.48), Wethersfield at (509.93, 270.59, -30.50): the
game's own `Player Stand In` objects for them (radius 300 and 400). Ravenwood has no path-node data read here; **ground
extent, Wethersfield's slope (his Z 7.18 versus -30.5) and Wu's platform are NOT TESTED in the client**.

Scripted fights (the creature AI cannot heal, ward or drain reliably; see `continuation-status.md`, Stage D2.3): each custom
carries engine BossCheats (`"Cheats": {"Added": [...]}`) with `MaxTimes` limits and an enrage from round 9/10:
Wu mends twice (Pixie 400) and casts Spirit Armor four times; Ashthorn drains with Vampire (x4) and Wraith (x1), curses,
and steals a pip from costly casts (x3); Wethersfield opens with Balanceblade, casts Muzzle twice, shields after hits (x3)
and re-balances once against Shadow. **Fight balance and the scripts are NOT TESTED in a live battle.**

After the seventh victory the registry holds seven `Defeated`/`Complete` stages; `ConquestEncounters.AllInstructorsDefeated`
(stage >= Defeated for all seven) is true at the moment the seventh victory is saved, `AllInstructorsComplete` once the last
chapter is handed in. Both require a Shadow wizard whose Umbral Sight works.

### Stage D2.4 — full-chain audit and D3 gate evidence (2026-10-23)

The current source gate for Ambrose is `ConquestEncounters.AllInstructorsComplete(wizard)`: Umbral Sight must work, and each of the seven encounter registry stages must be `Complete` with that chapter's quest completion flag. `AllInstructorsDefeated(wizard)` is the earlier post-battle predicate (all seven durable stages `Defeated` or `Complete`); it does not prove that Wethersfield's return conversation and final chapter turn-in happened. D3's quest offer should use `AllInstructorsComplete`. Neither predicate writes state or can be advanced by a campaign quest command.

The server-side completion chain is now exercised end to end in `SevenVerifiedInstructorVictoriesCompleteEveryChapterAndEveryCampaignMilestone`: each encounter is opened at its anchor, earns a matching private-copy outcome, passes the verified-event validator, advances exactly its active campaign milestone, completes the return goal and records the chapter completion. The milestone becomes complete only after a valid per-character victory. Invalid commands/events, wrong variants, helper/other-character outcomes, unrelated creatures, loss/flee, malformed state and incomplete earlier chapters remain rejected by the existing regression coverage. **SIMULATED:** this drives the real mod event hooks with deterministic server outcomes; it does not play a graphical duel.

For placement triage, the school-room circles for Balestrom and Drake are each 90 horizontal units from the instructor and use the instructor's Z. Wu's circle is likewise 90 units from the NPC on nominal Z=100. Wethersfield's Ravenwood NPC is at Z=7.18 while the circle sits on the nearby `Player Stand In` at Z=-30.5. Treat those as coordinates from installed data, not floor-contact proof. Ravenwood collision/slope, all circle activations, the presentation of the Dworgyn/Alhazred stand-ins, and every battle remain **NOT TESTED** in a graphical client.

The full evidence and validation table is in `stage-d2.4-audit.md`. The D2.4 run passed 252 Conquest + Umbral Sight tests three times, all 367 ShadowSchool tests three times, CoreLib 380/380, installer 2/2, both Release builds and an isolated QA startup on a fresh `ShadowSchoolD24Audit` database. Startup loaded ShadowSchool and 19 SpiralDB files and reached ready state; it did not start a battle or execute the 15 boss scripts.
