# Shadow School — continuation status

The live checkpoint for continuing this mod. Read this first, then the targeted diffs; do not re-audit the
repository from scratch. The authoritative design is `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md`; the
execution contract is `Shadow_School_DeepSeek_V4.1_Flash_1017_Handoff.md` (both at the repository root).

## Session checkpoint — 2026-10-22 18:05 (DeepSeek V4.1 Flash)

- **Current branch/working tree:** `quality-assurance`. Everything Shadow-related is **uncommitted**:
  `src/Imlight.CoreLib/Mods/`, `src/Imlight.Director/mods/`, `test/Imlight.CoreLib.Tests/` are untracked, and
  the core files listed in §1 are modified. Nothing was staged, reverted or cleaned.
- **Current target:** Stage B. Gear, decks and the vendor are now **implemented and server-verified** (§8);
  in-battle casting and in-combat pips are still blocked on a game client (§5, §6.3).
- **Confirmed working (evidence):** all 14 spell templates carry the agreed cost, accuracy, damage, target and
  effect data (§6); the Shadow Pip schedule is pinned by tests (§7); all 80 Shadow items exist with the right
  level, school and bonuses, and the vendor's 80-item inventory resolves (§8).
- **Changes made this session:** `src/Imlight.CoreLib/Mods/ShadowSchool/ShadowGear.cs` (new), `ShadowSchoolContent.cs`
  (`BuildGear`, `BuildVendor`, `Retarget*`), `ShadowSchoolMod.cs` (vendor ids), the mod's `text/en-US.txt`
  (vendor + 80 item names), `spiraldb/NpcInventory/NPCInventories_ShadowSchool-Vendor.json` (new) and
  `spiraldb/ZoneObjects/WizardCity-Interiors-WC_LibraryBookStacks.json` (vendor placed), plus
  `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ShadowGearCatalogTests.cs` (6 tests).
  **No core game code was changed** — the trainer and the spell data are untouched, as instructed.
- **Remaining defects:** none known in what is implemented. The gap is environmental: **no graphical client is
  available**, so nothing that happens on screen is verified (§5).
- **Next immediate action:** with a client, the walkthrough in §4 plus the gear and vendor checks at the end of
  §8; without one, Stage C (the Wizard City story) is the next buildable work — nothing of it is started.
- **Installer/client backup state:** `python install.py status` reports **Installed for r806919.Wizard_1_610:
  32 Root.wad files; WizardGraphicalClient.exe and Root.wad as installed**. Read-only check; nothing was
  installed, uninstalled or patched, and no backup was touched.
- **User data / unrelated changes:** preserved. This session read the database only (no writes).
- **Acceptance status:** A1 server-side PASS; B spell data PASS; B gear/decks/vendors PASS server-side,
  client NOT TESTED; B casting/pips in combat NOT TESTED; C–F not started.

## 1. Inventory (what is actually here)

| Area | Path |
|---|---|
| Mod source | `src/Imlight.CoreLib/Mods/ShadowSchool/` — `ShadowSchoolMod.cs`, `ShadowSchoolContent.cs`, `ShadowCurriculum.cs`, `ShadowGear.cs`, `ShadowSchoolFiles.cs`, `ShadowText.cs`, `ShadowPipRules.cs` |
| Director content | `src/Imlight.Director/mods/ShadowSchool/` — `text/en-US.txt`, `spiraldb/NpcSpellInventory/NPCSpellInventories_ShadowSchool-Tutor.json`, `spiraldb/NpcInventory/NPCInventories_ShadowSchool-Vendor.json`, `spiraldb/ZoneObjects/WizardCity-Interiors-WC_LibraryBookStacks.json`, `spiraldb/QuestTemplates/questtemplates_Tutorial_Intro.json` |
| Client patch | `src/Imlight.Director/mods/ShadowSchool/client/exe_patch.py`, `install.py` |
| Mod toggle | `[Mods] Enabled = ShadowSchool` in `src/Imlight.Director/Config/Imlight.ini` (confirmed on) |
| Core files touched by the prior session | `InteractTrainerComponent.cs`, `TrainService.cs`, `CombatDuelSubCircle.cs`, `CombatDuelComponent.cs`, `CharacterService.cs`, `SpiralDB.cs`, `CharacterHelper.cs`, `ZoneObjects.cs`, `AgentBridge.cs`, `RootArchiveLoader.cs` |
| Tests | `test/Imlight.CoreLib.Tests/` — `Game/Effects/TheOneRingStatsTests.cs` (pre-existing) and the new `Mods/ShadowSchool/ShadowCurriculumContractTests.cs` |

Tutor identity is intact: **Vespera Quillmoor**, template `1880000100`, object name `ShadowSchool-Tutor`, in the
Library Book Stacks at `6400,-1104,0, yaw 1.65`. The Shadow vendor is **Corvin Ashgrave**, template
`1880000101`, object name `ShadowSchool-Vendor`, beside her at `6400,-1004,0, yaw 1.65` (§8). These positions are within
the loaded room geometry; the previous `X=7380` placements were outside its approximate `X≤6590` footprint. The mod's strings live in
`text/en-US.txt` (`[ShadowSchool] TutorName = Vespera Quillmoor`, `VendorName = Corvin Ashgrave`).

## 2. Stage A1 — Shadow Troll training: diagnosis and evidence

### 2.1 What the client actually sent

`logs/log20261022.txt:7134` (session 55124, character Oscar RainbowFlower):

```
2026-10-22 16:03:52.803 [WRN] Wizard Oscar RainbowFlower (level 1) asked to train index 2 of
ShadowSchool Unlisted Tutor, which the trainer does not allow: needs level 5 and 0 training points.
```

Read this carefully — it settles several of the open questions in the handoff:

- The client **did** send `MSG_TRAIN`, with **index 2**, which is exactly Shadow Troll
  (`439559802`, level 5) in the tutor's 14-entry list. The index space, the trained spell and the packet are right.
- The wizard was **level 1**, so the server refused correctly. The refusal text also proves the cost rule:
  **0 training points**, i.e. a Shadow spell is free for a Shadow wizard as designed.
- This was the *only* training request in the whole log. The `.level 5` command ran nine seconds later
  (`logs/log20261022.txt:7135`, `shadowqa Uses command: level 5`), the client stayed connected until
  16:22:54, and **no further `MSG_TRAIN` was ever sent**.

So the reported "a level-5 wizard clicked Train and it was not learned" did not happen at level 5: the click
happened at level 1, was refused, and was never repeated after the level change.

### 2.2 Persisted state (RavenDB, character `wizards/193-A`, CharId 5020822986812991413)

At session start: school `Shadow`, **level 5**, `tp 0`, learned spells `[199150487, 2140065270]` —
the two starter spells only, i.e. Shadow Troll was never learned. The level change *did* persist.

### 2.3 Server-side registration (live server, `/eval`)

- `hash("Shadow") = 1429009101 = MagicSchool.Shadow` — the hash and the enum agree, so the deck instance-limit
  path (`spellSchool == PrimarySchool`) also works for Shadow spells.
- Template `439559802`: `ShadowSchool ShadowTroll`, school `Shadow`, rank 2, shadow pips 0, accuracy 85.
- `SpellFactory.GetSpell(439559802)` resolves it (templateID 439559802, pips 2).
- Tutor inventory for `1880000100`: 14 entries; index 2 = `439559802` level 5, requires `199150487`.

### 2.4 The decision itself (the same `Evaluate` the trainer list and `TrainService` use)

```
level 1 -> #2(5):no[level5/tp0]  #3(8):no  #4(10):no  #5(16):no
level 4 -> #2(5):no[level5/tp0]  #3(8):no  #4(10):no  #5(16):no
level 5 -> #2(5):TRAINABLE cost=0  #3(8):no[level8]  #4(10):no  #5(16):no
check#2 id=439559802 expected=439559802 match=True canTrain=True
LearnSpell -> True
after learn, DB spells=[199150487, 2140065270, 439559802]     <- persisted
duplicate LearnSpell -> False                                  <- no duplicate entry
restore UnlearnSpell -> True
restored DB spells=[199150487, 2140065270] level=5             <- character back as found
```

That is the whole A1 chain: eligibility at level 5, the taught-spell mutation, the save transaction and a
fresh read of the database. The only link not exercised is packet delivery at level 5 — and §2.1 shows the
client sends the right packet with the right index, so the two halves meet.

### 2.5 A1 acceptance

| Condition | Result |
|---|---|
| Level 4: spell locked, forged request rejected | PASS (`Evaluate` at level 4; also the live level-1 refusal in §2.1) |
| Level 5: train succeeds | PASS server-side (§2.4) |
| Learned spell persists | PASS (database re-read) |
| Repeated request creates no duplicate | PASS (`LearnSpell` returns false when known) |
| `Free` = no training-point deduction | PASS (cost 0, `tp` unchanged) |
| Can be placed in deck | NOT TESTED |
| Unrelated trainers / other schools unchanged | NOT TESTED |
| **Real-client click at level 5** | **NOT TESTED** — see §4 |

### 2.6 The rest of the curriculum, same way (Stage B, training half)

Every remaining spell was driven through the same `Evaluate` → `LearnSpell` → database path, one level below
its level and then at it, then unlearned and the database re-read:

```
#2  lvl5  locked@4=True  trainable@5=True  learned=True  cost=0
#3  lvl8  locked@7=True  trainable@8=True  learned=True  cost=0
#4  lvl10 locked@9=True  trainable@10=True learned=True  cost=0
#5  lvl16 locked@15=True trainable@16=True learned=True  cost=0
#6  lvl18 locked@17=True trainable@18=True learned=True  cost=0
#7  lvl22 locked@21=True trainable@22=True learned=True  cost=0
#8  lvl26 locked@25=True trainable@26=True learned=True  cost=0
#9  lvl30 locked@29=True trainable@30=True learned=True  cost=0
#10 lvl33 locked@32=True trainable@33=True learned=True  cost=0
#11 lvl38 locked@37=True trainable@38=True learned=True  cost=0
#12 lvl42 locked@41=True trainable@42=True learned=True  cost=0
#13 lvl50 locked@49=True trainable@50=True learned=True  cost=0
DB after learning: 14 spells, all 12 new ids present; failures=0
restored DB spells=[199150487, 2140065270] level=5
```

So the **training half of Stage B is done for all 14 spells**: level gate, prerequisite chain, zero cost,
learned-spell write and persistence. **Casting** is still only proven for the two starter spells (§3).

## 3. Where the release actually stands

| Stage | State | Evidence |
|---|---|---|
| A1 training | server-side complete | §2 |
| A2 creation & persistence | works | Shadow characters exist in the database (school, level, spells persisted) |
| A2 trainer list (14 spells, levels, Free) | present | inventory + curriculum + contract test |
| A2 starter spells | cast in battle | `logs/log20261022.txt` 16:45–16:50 (DarkSprite, DarkEmpower) |
| A2 Shadow pip rules | implemented | `ShadowPipRules` wired in `CombatDuelSubCircle` (start 1, cap 1, refill rounds 4/7/10, unused dropped). Runtime NOT TESTED this session |
| A2 health curve | implemented | `ShadowSchoolContent.BuildMagicXpConfig` (rounded-up Fire/Myth midpoint) |
| B: training all 14 | **PASS** — level gate, chain, cost, persistence | §2.6 |
| B: spell data (damage, targets, effects, cost, accuracy) | **PASS** — all 14 match the plan | §6 |
| B: casting all 14 in a battle | **only the 2 starters cast**, in the previous session | §6.3 |
| B: Shadow Pip generation/cap/refill | rules implemented + **unit tested**; in-combat NOT TESTED | §7 |
| B: gear, decks, vendors | **IMPLEMENTED — server-verified**: 80 items, deck rules, 80-item vendor shop | §8 |
| B: other-school compatibility | unchanged by construction; runtime regression NOT TESTED | §9 |
| C1: enrollment + Three Streets | implemented as Shadow-only quest data; structural and requirement tests PASS; client flow NOT TESTED | `docs/quest-dependency-map.md`, §11 checkpoint |
| C2+: later Wizard City dialogue and story | NOT STARTED | |
| D: Umbral Sight + finale | NOT STARTED | |
| E: aftermath + Krokotopia | NOT STARTED | |
| F: final QA | NOT STARTED | |

## 4. The one thing to do next (5 minutes, in the client)

Log in as **Oscar RainbowFlower** (level 5, Shadow), teleport to the Library Book Stacks
(`.teleport WizardCity/Interiors/WC_LibraryBookStacks`), open Vespera Quillmoor's trainer, click
**Shadow Troll** and **Train**. Expected: the row is enabled and Free, the spell is learned, and it is still
known after relog. Then watch `logs/` — a refusal logs
`... asked to train index N of ShadowSchool Unlisted Tutor, which the trainer does not allow: ...`,
a success logs nothing but sends `MSG_ADDSPELLTOBOOK` + `MSG_SPELLTRAINCOMPLETE`.

Note for QA: the trainer window is a snapshot. If it is open while the level changes, reopen it (walk away and
back) before clicking — the rows are not refreshed in place. This is what produced the original confusion:
the window showed level 1 rules when the click was made.

To re-run the checks in §2.3–2.4 without the client, start the server (`start-dev.bat`) and post C# to the
agent bridge (`POST http://127.0.0.1:12400/eval`, header `X-Agent-Token` from `[Agent Bridge]` in
`Imlight.ini`). Scripts can use CoreLib internals; a multi-statement script must not contain `using` directives
(use fully-qualified names). Working copies of the scripts used here are in `.scratch/agent/` (git-ignored).

## 5. Client verification: what is and is not possible here

Before this stage's work, the graphical client was checked for: no `WizardGraphicalClient` (or any Imlight)
process is running, and this environment has no way to drive a game window (no UI automation tool; the agent
bridge talks to the server, not the client). So everything that needs the game window is out of reach in this
session — in-battle casting, animation and sound, card tooltips, vendor and equipment screens, creation UI.

The agent bridge can fight a battle for a player (`/combat/creatures`, `/combat/start`, `/combat/move`,
`/combat/auto` in `AgentCombat.cs`), which would close the in-battle items in §6.3 and §7 — but it fights **as a
logged-in player** (`AgentBridge.GetLivePlayers`), and logging a wizard in is exactly what the missing client
provides. So it does not make combat reachable here either.

The client *install* was checked read-only and is intact:

```
python src/Imlight.Director/mods/ShadowSchool/client/install.py status
Installed for r806919.Wizard_1_610: 32 Root.wad files.
  WizardGraphicalClient.exe: as installed
  Root.wad: as installed
```

Nothing was installed, uninstalled, patched or restored, and no `*.shadowschool-backup` file was touched. When a
client is available, the checks that need it are listed in §4 and §6.3.

## 6. Stage B — the spell data, as the server actually holds it

Read from a running server with `/eval`: every field of every `SpellTemplate` the curriculum generates, and its
whole effect tree (nested random rolls included). This is what the combat code consumes; it is **not** a
substitute for watching the spell play (§6.3).

| Spell | Lvl | Cost | Acc | Effect data (verified) |
|---|---:|---|---:|---|
| Dark Sprite | 1 | 1 | 85 | `kDamage` ×5 rolls 75/80/85/90/95, Shadow, `kEnemySingle` |
| Dark Empower | 1 | 1 Shadow | 100 | `kModifyPips` +1 to `kFriendlySingle`; **no** power-pip effect (donor's removed) |
| Shadow Troll | 5 | 2 | 85 | `kDamage` ×5 rolls 160/170/180/190/200, Shadow, single |
| Tower Shield | 8 | 0 | 100 | `kModifyIncomingDamage` −40, `All`, friendly, `protected = false` |
| Shadow Banshee | 10 | 3 | 85 | `kDamage` ×5 rolls 245/255/265/275/285, single; donor's trap removed |
| Shadow Trap | 16 | 0 | 100 | `kModifyIncomingDamage` +25, Shadow, enemy single |
| Shadowblade | 18 | 0 | 100 | `kModifyOutgoingDamage` +30, Shadow, friendly |
| Humongofrog | 22 | 4 | 85 | `kDamage` ×5 rolls 265/280/295/310/325, Shadow, `kEnemyTeam` (AoE) |
| Helephant | 26 | 4 | 85 | `kDamage` ×5 rolls 345/360/375/390/405, single |
| Shadowspear | 30 | 1 Shadow | 100 | `kModifyOutgoingArmorPiercing` +10, Shadow, friendly |
| Minotaur | 33 | 5 | 85 | two `kDamage` effects, 50 then 420, Shadow, single |
| Storm Shark | 38 | 3 + 1 Shadow | 85 | `kDamage` ×5 rolls 380/390/400/410/420, single |
| Triton | 42 | 6 | 80 | `kDamage` ×5 rolls 610/620/630/640/650, single |
| Frost Giant | 50 | 7 + 1 Shadow | 80 | `kDamage` 475, Shadow, `kEnemyTeamAllAtOnce`; `kStun` 1 round, `kEnemyTeam` |

Every one has `m_sMagicSchoolName = Shadow`, `m_Treasure = false`, `m_levelRestriction = 0`, and the donor's
presentation untouched: `m_spellBase` (`Troll Death`, `Shadow Banshee`, `HumongofrogDeath`, `Triton`…),
`m_imagename` / `m_imageindex` and, where the donor's wording would be wrong, the mod's own description key.
Because the cinematics are the donor's own references, no animation, sound or artwork was invented; the client
plays what it already plays for that donor.

### 6.3 What could not be checked here

See §5: no client window can be driven in this environment. So these are **NOT TESTED**, not passed:
animation and sound playback, in-battle casting, card tooltips, and anything about how the spell *looks*.
To cover them, log in with a level-50 Shadow wizard, `.level 50`, learn the spells, and cast each in a duel:
the server log shows `Caster is casting spell <id>`, the hit targets and the wait time (see the DarkSprite
lines in `logs/log20261022.txt`).

## 7. Stage B — Shadow Pips

The rule lives in `ShadowPipRules.cs` and is wired in `CombatDuelSubCircle` (start 1, cap 1 for a Shadow wizard,
refill at rounds 4/7/10…, dropped when the pip is still held, and Shadow Pips never stand in for other pips).
`ShadowPipRulesTests` now pins the schedule, the starting pip and the cap (19 cases). In-combat behaviour
(round transitions, spending, reconnect) is **NOT TESTED** for the same reason as §6.3.

## 8. Stage B — gear, decks and vendors (IMPLEMENTED, server-verified; client NOT TESTED)

Built this session, exactly as the plan asks: ten tiers on every five levels through level 50, all seven
equipment slots plus a deck for each tier, sold by a dedicated Shadow vendor.

**How a piece is made** (`ShadowGear.cs`, `ShadowSchoolContent.BuildGear`). Each of the 80 pieces is a Death item
the game already has (the donor), read from Root.wad and written to a file of its own:

| What | A piece of Shadow gear |
|---|---|
| template name | `ShadowSchool-<Slot>-<Level>` (e.g. `ShadowSchool-Hat-15`) |
| template id | `StringHash` of that name, so the client and the server agree without a table |
| file | `ObjectData/ShadowSchool/<Slot>-<Level>.xml` |
| display name | `ShadowSchool_Item_<Slot>_<Level>` → "Adept's Shadow Hat" (ten tier titles, Novice's → Umbral) |
| `m_school` | `Shadow` |
| requirement | `ReqMagicLevel` level = the tier, school `Shadow`; `ReqSchoolOfFocus` school `Shadow` (added when the donor had none) |
| stat effects | the donor's, with every school it names renamed to Shadow (`CanonicalDeathDamage` → `CanonicalShadowDamage`, including a second school's bonus such as `CanonicalFireAccuracy`). A bonus with no Shadow version is left off rather than mislabelled. |
| deck | the donor deck's `DeckBehaviorTemplate`, with `m_primarySchoolName = Shadow` so its school limit counts Shadow spells |
| look, icon, rarity, price | the donor's, untouched |

Donors are Death items of the same slot and tier (7 tiers have no Death donor of their own — athame 20/40, amulet
20/30/40, ring 30, deck 40 — and use the best donor below them, so the piece is a little weaker, never stronger).
The tier-5 deck is the game's school-neutral level-5 deck, since a deck carries no school stats. Donors are never
changed: a Death wizard's gear is exactly as it was, and every piece goes through the existing `AddTemplate`,
which refuses an id or file the game already uses.

**The vendor.** Template `1880000101`, object name `ShadowSchool-Vendor`, display name
`ShadowSchool_VendorName` = **Corvin Ashgrave**. He is the tutor's Arcanum model with the quest and training
behaviors removed and a shopkeeper's `WizShoppingBehaviorTemplate` (`m_shopTitle = ShadowSchool_VendorShopTitle`
= "Shadow Equipment", shop type 0, no test/housing flags), so `InteractVendorComponent` attaches to him and opens
an equipment shop. He stands beside Vespera (`6400,-1004`, yaw 1.65). Stock is
`spiraldb/NpcInventory/NPCInventories_ShadowSchool-Vendor.json`: the 80 ids, in slot order, with
`ShopTitle = ShadowSchool_VendorShopTitle`.

**Verified on the running server** (`/eval`, after a clean restart, log shows the mod's files built with no
error):

```
80/80 templates resolve; 80 distinct ids                       <- all present, none collides
every piece: ReqMagicLevel level = its tier, school = Shadow   <- 80/80
every piece: ReqSchoolOfFocus school = Shadow                  <- 80/80
every piece: m_school = Shadow                                 <- 80/80
no piece boosts another school (no Fire/Ice/Storm/Myth/Life/Death/Balance effect)   <- 0 findings
decks: Deck-5 max=16 generic=3 school=3 treasure=6 primary=Shadow
       Deck-50 max=60 generic=5 school=7 treasure=25 primary=Shadow
vendor inventory: 80 entries, 80 distinct, 0 unresolved, title ShadowSchool_VendorShopTitle
shop order keeps all 80          prices: Hat-5 139g, Hat-15 1930g, Hat-50 9281g, Deck-50 8000g
item instantiates to WizClientObjectItem (the purchase path's own call)
InteractVendorComponent.ShouldAttachToEntity(vendor) = True    <- he is a shop
```

The build fails loudly on a bad donor rather than shipping wrong goods; that path was seen working: the first
attempt used the school-neutral level-5 deck and the server refused it ("is not a Death item"), which is why
decks are the one slot allowed a non-Death donor.

**NOT TESTED — needs the game client (§5).** The shop window, the item icons and tooltips ("Level N, Shadow"),
buying (gold deducted, item delivered, `MSG_SHOPBUYCONFIRM`), equipping, the deck editor with a Shadow deck, and
whether the vendor's position looks right in the room. To check them: log in, `.teleport
WizardCity/Interiors/WC_LibraryBookStacks`, talk to Corvin Ashgrave, buy a hat and a deck, equip both, and open
the deck editor.

## 9. Stage B — other schools

By construction the mod adds templates and never replaces one: `AddTemplate` throws if an id or file is already
in the manifest, all spells are written to `Spells/ShadowSchool/`, and the only existing files it rewrites are
`GUI/CreationClassSelect.gui` (one extra button and sigil), `MagicXPConfig.xml` (only the `Shadow` class's level
list is replaced; the other eleven class entries are left as read), the locale files (keys added, same-key text
replaced) and `TemplateManifest.xml`. The one shared asset to keep an eye on is the tutorial quest override (§10).
The gear follows the same rule: every piece is a new template and a new file, and no game item is rewritten.
A real multi-school regression run (a Fire and a Death wizard next to a Shadow one) is **NOT TESTED**.

## 10. Risks and notes for the next session

- `questtemplates_Tutorial_Intro.json` replaces the game's tutorial quest for **every** school. It must keep
  the seven original starter grants exactly as the installed revision has them; the contract test asserts
  nine grants (7 schools + 2 Shadow) so a loss is caught. Re-check it if the game revision changes.
- `wizards/161-A` is an older Shadow test wizard (level 1) whose learned spells are `1880000001`/`1880000002`,
  stale ids from an earlier build of the curriculum. Harmless; do not treat them as current spell ids.
- Do not call the tutor "the Unlisted Tutor" in new content — the display name is Vespera Quillmoor; that
  string is only the internal object name (`ShadowSchool Unlisted Tutor`).- `ShadowSchoolContent.BuildSpells` throws at startup if a donor is no longer a Shadow spell or an effect shape
  changed (e.g. Minotaur no longer having two hits). That is deliberate: it turns silent wrong behaviour into a
  startup failure. `BuildGear` follows the same policy: a missing donor file, a donor that is not a Death item
  (decks excepted) or an unreadable requirement list stops the mod's files being built, and the log says which
  donor. When that happens the mod is left out entirely (no spells either), so read the log after every game
  revision.
- The gear ids in `NPCInventories_ShadowSchool-Vendor.json` must equal `ShadowGear.Pieces`:
  `ShadowGearCatalogTests.TheVendorSellsExactlyTheCatalogue` asserts it, so a new or renamed piece without a
  fresh inventory file fails the tests rather than vanishing from the shop.

## 11. Stage C — C1 implemented; later story work not started

Research and the first campaign slice are in `docs/quest-dependency-map.md`. The installed data identifies the
seven exact `WC-COMMONS-MAIN-002-{BALANCE,DEATH,FIRE,ICE,LIFE,MYTH,STORM}` enrollment templates and their shared
`WC-COMMONS-MAIN-003` join. C1 adds `WC-COMMONS-MAIN-002-SHADOW`, gated by Grim Tales plus Shadow school, and
adds its completion flag to the existing Three Streets OR list. It preserves the seven original files, the
shared enrollment goals/rewards, Sergeant Muldoon's objective and the original street quest chains.

Vespera has three optional, independent check-ins, each offered only after the same character completes the final
mainline quest for that street. The requirements do not chain check-ins together. They use the current quest
offer and persona-goal components, the existing tutor identity, and the Library Book Stacks location. Lines are
text-only because no matching voice audio exists; each reaction refers only to its own completed street.

## Session checkpoint — 2026-10-22 18:41 (GPT-6 Luna High)

- **Current branch/working tree:** `quality-assurance`; the pre-existing Shadow work and many unrelated Imlight
  changes remain uncommitted. This slice changed only the Shadow School mod quest data, its text, tests and docs.
- **Current target:** Stage C1, Shadow enrollment through the shared Three Streets join, plus independent Vespera
  check-ins after each street.
- **Confirmed working (automated evidence):** the Shadow enrollment and four goals deserialize using the same
  `SpiralJsonOptions` serializer as SpiralDB; original seven enrollment variants are not overridden; Three
  Streets keeps its one Muldoon persona objective and seven original OR predecessors plus Shadow; each check-in
  requires its own street final flag and Shadow school; all six street completion orders are valid in the static
  contract; requirement evaluation isolates a completed flag to one Shadow wizard. Director Release build passed;
  test suite passed **42/42**.
- **Changes made:** `spiraldb/QuestTemplates/questtemplates_WC-COMMONS-MAIN-002-SHADOW.json` (Shadow school
  enrollment clone using Vespera); `questtemplates_WC-COMMONS-MAIN-003.json` (add Shadow predecessor only);
  `questtemplates_ShadowSchool-CheckIn-{Triton,Firecat,Cyclops}.json` (independent Vespera check-ins);
  `text/en-US.txt` (enrollment and check-in text); `docs/quest-dependency-map.md` (installed IDs, locale keys,
  objectives and dependencies); `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ShadowQuestContractTests.cs`
  (eight-branch, order, serialization, dialogue isolation contracts); and this checkpoint.
- **Tests executed:** `dotnet test test/Imlight.CoreLib.Tests/Imlight.CoreLib.Tests.csproj -c Release --no-restore`
  — 42 passed, 0 failed; `dotnet build src/Imlight.Director/Imlight.Director.csproj -c Release --no-restore`
  — 0 errors. Build reports existing NU1904 Akka.Remote vulnerability warnings and NU1510 package warning.
- **Client-dependent tests still NOT TESTED:** fresh Shadow path from Grim Tales through Vespera registration; Three
  Streets unlock in the client; all six actual play orders; each in-client Vespera check-in and dialogue rendering;
  different characters seeing separate offers simultaneously; completion/reconnect persistence; voice/camera
  presentation; Fire/Death/Balance regressions. No graphical client was running or controllable in this session.
- **Known defects:** none confirmed in this slice. Client presentation and reconnect behavior remain unverified.
- **Installer/client backup state:** read-only status reports installed for `r806919.Wizard_1_610` with 113 Root.wad
  entries recorded by the installer; the client EXE and Root.wad still match the installed mod state. No install,
  uninstall or patch command ran. Backups remain present and unchanged: Root.wad SHA-256
  `4E09B3DA242A6F5650624497359DFAEBF6CD54E72A0AE1EE21BDCF7436B42C66`; EXE SHA-256
  `544AB726EF7B920A5CCB67AA3D06B245DB7E0012317D2FE7856D58C17C31CF7E`.
- **User data / unrelated changes:** preserved; no pre-existing dirty path was reverted, staged, cleaned or edited by
  this slice. No character save or database was written.
- **Acceptance status:** C1 server data, serializer compatibility and automated contract checks PASS. Actual client
  campaign flow and reconnect proof NOT TESTED. C2+ dialogue resolver, later quest adaptations, Umbral Sight,
  finale, pact, aftermath and Krokotopia remain NOT STARTED.
- **Next implementation slice:** client walkthrough of C1 on a fresh Shadow wizard. After that, begin C2 with one
  player-specific mainline dialogue adaptation in the first completed street chain, retaining its vanilla goals,
  completion flag, reward and audio-safe text behavior.

## Client launch attempt — 2026-10-22 19:32 (GPT-6 Luna High)

- Launched `WizardGraphicalClient.exe` by opening the user's supplied
  `C:\ProgramData\KingsIsle Entertainment\Wizard101\Bin\IML.lnk` shortcut.
- The client reached its account login dialog. No credentials were entered; authentication is user-operated.
- **C1 gameplay result:** NOT TESTED. No character was logged in, so enrollment, Three Streets, Vespera dialogue,
  character isolation and reconnect persistence remain unverified in the client.
- **Next action:** user signs in and loads the fresh Shadow wizard, then resume the C1 client walkthrough.

## Imlight server startup — 2026-10-22 19:35 (GPT-6 Luna High)

- Started the server with the existing `start-dev.bat` launcher after the client launch attempt.
- Startup log confirms `ShadowSchool` loaded, 113 mod files applied to Root.wad, and SpiralDB loaded 1,088 quest
  templates among 6,921 data files. No quest-template load error appeared in the current startup log.
- Login port 12000, game ports 12333–12335, and patch port 12500 are listening under the same Director process.
- **C1 gameplay result:** still NOT TESTED. The client is at its account login dialog; user sign-in is required
  before the walkthrough can continue.

## MCP-assisted C1 playtest — 2026-10-22 19:53 (GPT-6 Luna High)

- The user signed in as the Shadow QA character. Imlight MCP identified the character as Shadow, level 5, in the
  Library Book Stacks. Its pre-test quest registry contained only `Tutorial_Intro_Complete`; the Grim Tales
  prerequisite was not complete, so the normal enrollment offer gate was not exercised.
- I used the MCP QA `quest offer` command to exercise the Shadow enrollment template. Advancing the first goal
  through the MCP `quest completegoalfor` helper (which bypasses physical NPC interaction) exposed a real client
  defect: its copied Goal 1 result teleported the player into Storm School, with Goal 2 assigned to Vespera.
- Corrected Goal 1's destination to Vespera's Library position, and corrected the enrollment tutor portrait and
  camera zone. The three independent check-ins had inherited Sergeant Muldoon's Olde Town camera and portrait;
  they now use Vespera's established portrait and Library camera zone as well. Added contract assertions for
  these fields and the tutor template ID.
- **Post-fix server validation:** MCP restart rebuilt Release successfully (0 errors); startup log confirms
  `ShadowSchool` enabled and all 1,088 quest templates loaded. Focused C1 tests passed 6/6; the full CoreLib test
  suite passed 42/42.
- **Actual client observation:** the pre-fix Storm teleport was visible in the client. The corrected teleport and
  Vespera dialogue have NOT yet been observed in-client. The server restart disconnected the QA character, and
  no player is currently online. Enrollment prerequisite gating, Three Streets objective completion, all six
  real street orders, check-in interactions, multiplayer isolation and reconnect persistence remain client tests.
- **QA account cleanup:** removed the temporary active Shadow enrollment quest before the restart. No client
  patches, backups, unrelated working-tree changes or player files were modified by this correction.
- **Known defect status:** the copied Storm teleport/portrait/camera defects are corrected in source and the
  server has reloaded the data; client-side post-fix confirmation remains pending. The check-in quest's inherited
  client tag is still `Ddl_WC_OT_SergeantMuldoon`; verify the in-client quest marker targets Vespera before changing
  this tag, since no installed Vespera-specific tag has been identified.
- **Next implementation slice:** resume the live walkthrough after the QA character reconnects; verify the
  enrollment teleport and Vespera dialogue, then Three Streets progression, check-in markers/dialogue and
  reconnect persistence. Do not claim these client-dependent checks as passed until observed.

## Continued MCP/client verification — 2026-10-22 20:03 (GPT-6 Luna High)

- **Enrollment transition:** with the QA wizard connected, MCP advanced Goal 1 through the normal quest service.
  Both server state and the client placed the wizard in `WizardCity/Interiors/WC_LibraryBookStacks` at
  `6400,-1104,0`; Vespera's name, portrait and interaction prompt were visible. The copied Storm-school
  destination defect is fixed and verified in-client.
- **Vespera presentation:** advancing Goal 2 showed the next objective without the previous bookshelf camera
  clipping. The client rendered two new Shadow-specific Halston entries as blank dialogue. The generated server
  locale contains their strings, but the installed client patch was left untouched per the no-overwrite boundary;
  client rendering of those new keys remains unverified until a safe client-resource test is available. The
  pre-fix client run had shown the misleading Storm enrollment line, which those replacements address in source.
- **Three Streets unlock:** after completing the Shadow enrollment quest, the normal `.quest grant`
  requirements accepted `WC-COMMONS-MAIN-003`. The client showed `Talk To Sergeant Muldoon in Old Town` and the
  quest marker, confirming the Shadow predecessor unlocks the shared join in the live client.
- **Persistence:** after a server restart and client reconnect/Play, the server restored
  `WC-COMMONS-MAIN-002-SHADOW_Complete` and active `WC-COMMONS-MAIN-003`; the client returned to Library Book
  Stacks with the Muldoon objective. This verifies persisted quest/zone state across reconnect for this path.
- **QA cleanup:** removed the temporary active `WC-COMMONS-MAIN-003` and Shadow enrollment completion entry.
  The final registry returned to its original `Tutorial_Intro_Complete` only, with no active quests.
- **Verification after the latest source edits:** focused Shadow quest contracts passed 6/6; full CoreLib tests
  passed 42/42; Director Release rebuild passed with 0 errors; runtime startup loaded `ShadowSchool` and 1,088
  quest templates. Tests cover the untouched seven vanilla enrollment templates, Shadow branch, serialization,
  the six street orders and per-character requirements.
- **Still client-dependent:** a normal, prerequisite-qualified offer from Grim Tales; completion of the Vespera
  persona goal by direct in-world interaction; actual Three Streets battles and all six play orders; the three
  street-specific Vespera check-ins/markers/dialogue; multiplayer presentation isolation; rendering the new
  Shadow dialogue keys with an updated-but-isolated client resource set; Fire/Death/Balance regressions.
- **Known issue:** check-in client tags still carry `Ddl_WC_OT_SergeantMuldoon`. The persona and destination target
  Vespera, but verify the objective marker in-client before replacing the tag; no installed Vespera tag is known.
- **Next implementation slice:** resolve the Shadow locale presentation in an isolated client test without
  touching existing client patches/backups, then finish the in-world Vespera and Three Streets/check-in
  walkthrough. Keep the Grim Tales prerequisite gate, seven stock school variants and all unrelated client and
  working-tree state unchanged.


## Continued live client verification — 2026-10-22 20:12 (GPT-6 Luna High)

- **Client/server reconnect:** the user reconnected after the prior restart; Imlight MCP confirms Oscar RainbowFlower is online in `WizardCity/Interiors/WC_LibraryBookStacks`. Server bridge is up and its last-30-minute error digest is empty.
- **Vespera in-client:** the Library rendered Vespera Quillmoor and Corvin Ashgrave with their nameplates and character art. Pressing X on Vespera opened the Shadow trainer panel; the panel showed Shadow Troll at level 5 and the remaining Shadow curriculum entries, confirming the live trainer interaction. Closing the panel restored the Library view.
- **Enrollment dialogue:** after temporarily force-granting the Shadow enrollment quest for QA, I completed the Mr. Lincoln goal through the normal quest service, which replayed its client dialogue and placed the wizard in the Library. The stock Mr. Lincoln, Ice, Fire, Life, Balance, Myth and Death introductions rendered. The substituted Storm introduction and the new Vespera introduction each rendered only the fragment `in` on the installed client resources. The yellow quest marker appeared over Vespera, but an in-world X/click attempt did not advance the persona goal; direct Vespera completion remains unverified.
- **QA cleanup:** `.quest remove WC-COMMONS-MAIN-002-SHADOW` returned the character to zero active quests and its original registry (`Tutorial_Intro_Complete=1` only), confirmed through the live server's `QuestBehavior`. No player progression remains from this follow-up.
- **Validation in this follow-up:** live MCP server/player/error checks and direct client screenshots/interactions. No build or automated test was rerun in this follow-up; the immediately preceding Release build and automated suite remain recorded above (42/42 CoreLib tests, 0 build errors).
- **Known client issue:** the Storm and Vespera custom enrollment entries do not resolve to their intended text in this installed client session and display only `in`. Their source keys exist, but client-visible text is not accepted as passing. The Vespera persona goal also lacks direct in-world completion proof.
- **Client-dependent tests still open:** a prerequisite-qualified offer from Grim Tales; direct Vespera goal completion; Three Streets objective markers, actual battles and all six real completion orders; Vespera's three street check-ins; multiplayer presentation isolation; Fire/Death/Balance enrollment regressions; correct Shadow dialogue rendering with client resources matching the latest mod output.
- **Exact next implementation slice:** trace how ShadowSchool's generated locale additions reach the client patch/cache and validate an isolated client resource update without replacing the installed Root.wad, existing client patches or backups. Then complete the direct Vespera-to-Three-Streets walkthrough. Keep the seven vanilla enrollment branches, Grim Tales requirement and unrelated dirty worktree/player data intact.

## Triton check-in client probe — 2026-10-22 20:13 (GPT-6 Luna High)

- Temporarily force-granted `ShadowSchool-CheckIn-Triton` through Imlight MCP to inspect the rendered quest UI, then removed it.
- The client displayed the literal title key `SHADOWSCHOOL_TRITONCHECKINTITLE` and an empty goal line. This confirms the unresolved locale problem also affects ShadowSchool quest titles/goals, not only the new enrollment dialogue. Do not treat client localization as verified.
- Cleanup verification: `QuestBehavior` has zero active quests and its registry contains only `Tutorial_Intro_Complete=1`; Oscar remains online in the Library. The server error digest remains empty for the last 30 minutes.
- **Next slice update:** resolve delivery/loading of the generated `Locale/en-US/ShadowSchool.lang` in a disposable client-resource test before claiming any ShadowSchool quest presentation works; retain original Root.wad, client patches and backups. Re-run the client probe for the enrollment entries and Triton check-in title/goal, then resume the Vespera and street walkthrough.

## Stage C1.5 — localization pipeline and safe client refresh — 2026-10-22 20:30 (GPT-6 Luna High)

- **Cause confirmed:** the active client was using a stale ShadowSchool locale. Its installed `Locale/en-US/ShadowSchool.lang` SHA-256 was `b15646cb814e7c1dc759bf8cfeb3cf620e1002cf003cf0edbd102ad6f44c93f1` and lacked the C1 enrollment/check-in keys. The latest server-generated file was a different SHA-256 (`8e30a0b3d719bf204da5a93fe6b691175a5ef38a6babf809d58c7a1d53862bbf`) and contained them. `RootOverlay` writes locale files into `generated/root` and `manifest.json`; the client installer must apply that generated package. The locale filename, key prefix/format and generated pack are valid.
- **Safe packaging test:** copied the preserved original Root.wad and client EXE backups into `.scratch/shadow-c15-client`, then ran the existing installer there. It built a 113-file patched Root.wad; all enrollment, Storm replacement and three street check-in title/goal keys were present. `QuestTitle.lang` and `WizQst1628CB.lang` matched the originals byte-for-byte in the patched output.
- **Active install safety:** before applying the update, compared every active Root.wad entry against the preserved original backup. The 113 changed/added entries matched the install record exactly; there were no unrecorded active changes. Ran the existing installer against the active game using its already-validated, never-overwritten backups. `install.py status` now reports the generated ShadowSchool resources as current. The original Root.wad backup remains SHA-256 `4E09B3DA242A6F5650624497359DFAEBF6CD54E72A0AE1EE21BDCF7436B42C66`; the original EXE backup remains unchanged at SHA-256 `544AB726EF7B920A5CCB67AA3D06B245DB7E0012317D2FE7856D58C17C31CF7E`.
- **Code/test changes:** `client/install.py` now compares the latest generated manifest against the installed Root.wad and reports stale or invalid generated resources instead of only saying the prior install is intact. Added `test/ShadowSchoolClientInstaller.Tests/test_install.py` for the generated-update drift detector.
- **Validation:** installer Python regression tests: 2 passed; Python compile check passed; `git diff --check` passed; CoreLib suite passed 42/42; Release Director check-only build succeeded with 0 errors (copy to the running server output was intentionally unavailable while that server remained active). Active installer status is current. The server and Oscar remain online; no new server errors were reported.
- **Client state:** the running Wizard101 process had already loaded its old Root.wad before the disk update. It has not been restarted, so the prior `in` fragment/literal-key screenshot is still the only real-client rendering evidence. No client authentication UI was automated.
- **Still open:** after a full client restart and return to character selection, verify enrollment dialogue and all three check-in title/goal/dialogue strings, direct Vespera goal completion and markers; then test actual Grim Tales gating, Three Streets access/check-ins, street battles/orders and other-school regressions. Do not mark client localization as visually verified until the fresh process displays the new text.
- **Next executable slice:** user restarts the client (server stays up) and selects Play; then capture the Shadow enrollment and check-in UI again. Resume the in-world Vespera/C1 progression walkthrough immediately after that UI check.

## Stage C1.5 — fresh-client localization and enrollment verification — 2026-10-22 20:33 (GPT-6 Luna High)

- **Fresh client:** after the user restarted and pressed Play, the Wizard101 process reconnected to the still-running Director as Oscar RainbowFlower in the Library. The active Root.wad now includes the generated ShadowSchool locale. Both Vespera Quillmoor and Corvin Ashgrave rendered in the Library.
- **Shadow enrollment presentation:** force-granted the quest only for QA and advanced goals 1 and 2 through the quest service. The client displayed the existing `To Ravenwood!` title, localized Lincoln objective/dialogue, all seven existing school introductions, the Shadow-specific Halston entries, and Vespera's full tutor introduction. The two Halston lines rendered their complete custom text, replacing the earlier `in` fragment. The check-in goal banner also displayed `Speak with Vespera Quillmoor`.
- **Three Streets check-ins:** in separate force-granted QA probes, the client displayed the localized titles `Questions in the Dark`, `A Cure, Not a Chain`, and `Orders and Answers`; Vespera's Triton, Firecat, and Cyclops reactions and objectives rendered fully. Each reaction referred to its own street's discovery. These were presentation/quest-service probes, not actual street-completion playthroughs.
- **Normal offer gate:** `.quest grant WC-COMMONS-MAIN-002-SHADOW` was refused because the character does not meet the existing `WC-UNICORN-MAIN-008` completion requirement. The server message identified the prerequisite and directed QA to `forcegrant`; this does not indicate a Shadow-branch regression. A normally qualified enrollment offer remains untested on this character.
- **QA command feedback:** the `.quest forcegrant` test command emits a server-message toast containing the internal quest name and `(requirements skipped)`. This is command feedback, not the normal quest tracker. The actual new-quest toast, center goal banner, and dialogue titles/text localized correctly.
- **Direct persona interaction:** with the Triton check-in objective active and Vespera visible, an X interaction and a click did not open the persona-goal dialogue. The quest service's normal goal-completion helper did display the authored dialogue correctly. This leaves physical in-world Vespera completion and its marker unresolved; no code defect has been isolated from the failed input probe.
- **Cleanup/persistence:** removed each temporary check-in and enrollment quest. Live `QuestBehavior` returned to zero active quests, no Shadow/enrollment registry keys, and the original `Tutorial_Intro_Complete=1`. No player progression from the probes remains. The player stayed online in the Library; the last-30-minute server error digest was empty.
- **Automated validation:** no source implementation changed during this client verification. Re-ran the installer suite (2 passed), Python compile, `git diff --check`, CoreLib suite (42/42 passed) and Release check-only Director build (0 errors).
- **Still client-dependent:** normal enrollment offer after completing Grim Tales; physical in-world Vespera objective completion; real Triton/Firecat/Cyclops quest objectives and battles in all six street orders; check-in marker target; other-school enrollment regressions; multiplayer per-character presentation/state and a separate reconnect test of active check-in state. Earlier reconnect persistence evidence covers the enrollment completion/shared Three Streets quest path.
- **Exact next implementation slice:** continue the in-world C1 walkthrough with a character that meets `WC-UNICORN-MAIN-008`: determine why a physical X/click did not open Vespera's active persona goal, validate its marker, then test actual Three Streets objectives, battles and all six completion orders. The current QA character's normal enrollment grant is correctly gated by its incomplete prerequisite. Do not expand into C2 or later campaigns until those C1 interactions are validated.

## Stage C1.6 — Vespera interaction refresh and live check-in — 2026-10-22 21:09 (GPT-6 Luna High)

- **Files changed:** `src/Imlight.CoreLib/Game/Zone/Components/InteractServiceMementoComponent.cs`; `test/Imlight.CoreLib.Tests/Game/Zone/InteractServiceMementoTests.cs`; this continuation status.
- **Cause isolated and fixed:** Vespera's service options were sent when a player entered interaction range, but not refreshed when a persona-goal quest became active while that player stayed beside her. The recurring WizBang update refreshed markers only. `InteractServiceMementoComponent` now tracks each character's available persona-goal option IDs and resends service options when that set changes in range. The refresh is deferred while the Shadow trainer spell picker is open.
- **Automated coverage:** added `InteractServiceMementoTests` for stable, sorted persona-goal signatures and signature changes across quest/goal IDs. Focused tests passed 2/2; full CoreLib suite passed 44/44; Director Release check-only build passed with 0 errors; the Release server restart/build completed and the server started. `git diff --check` passed.
- **Direct client verification after rebuild:** reconnected Oscar RainbowFlower through the client's Reconnect and Play flow. At Vespera in `WizardCity/Interiors/WC_LibraryBookStacks`, force-granted the Firecat check-in for QA while remaining in range. The client showed localized `A Cure, Not a Chain` and `Speak with Vespera Quillmoor`; pressing X opened Vespera's menu with both the Firecat quest and `Spell List`. Selecting the quest rendered her Firecat reaction, and Done displayed `QUEST COMPLETE: A CURE, NOT A CHAIN`. The server then recorded `ShadowSchool-CheckIn-Firecat_Complete=1` and no active quest. Pressing X again opened the Shadow trainer spell panel, confirming training remained available.
- **QA cleanup:** removed the force-granted Firecat check-in. Live `QuestBehavior` returned to `Tutorial_Intro_Complete=1`, with no active quests; Oscar remains online in the Library. Temporary speed effects were already cleared before restart. No normal character was touched and no client backups or patches were changed during this stage.
- **Normal enrollment gate:** still unverified through the ordinary sequence. The available QA Shadow characters do not have `WC-UNICORN-MAIN-008` (Grim Tales) complete; the normal grant correctly refuses them. I did not bypass or weaken the production prerequisite. A legitimately qualified Shadow character is needed for the complete Mr. Lincoln → Vespera → reward → Three Streets unlock walkthrough.
- **Three Streets scope:** the physical Vespera service refresh and Firecat check-in turn-in now pass. This Firecat quest was force-granted, so it does not prove its street completion requirement, offer timing or marker target. Triton interaction was physically observed in the preceding C1.6 probe, but the actual street objectives/battles and all six real completion orders remain unplayed. Multiplayer story isolation and reconnect persistence for an active check-in remain unverified. Existing C1.5 client screenshots prove localized enrollment/check-in presentation only for the tested force-grant/quest-service paths.
- **Regression limits / known issue:** the seven stock enrollment variants were left untouched; existing contract tests cover those templates and the prior C1.5 presentation probe rendered all seven school introductions. Full ordinary enrollment regression, other-school live paths, multiplayer isolation and active-check-in reconnect still need client validation. The inherited check-in client marker tag is still `Ddl_WC_OT_SergeantMuldoon`; direct interaction succeeds, but the marker target has not been verified, so the tag remains unchanged.
- **Next gameplay acceptance slice:** finish C1 on a Shadow character whose `WC-UNICORN-MAIN-008` completion was earned through the ordinary campaign: enrollment/rewards, all three street routes and bosses, each Vespera check-in, order-independent street completion, active-state reconnect and second-character isolation. This is still the C1 acceptance gap; the C2 research map is now complete, but no C2 quest changes are implemented.

## Stage C1.7 — normal campaign acceptance checkpoint — 2026-10-22 (GPT-6 Luna High)

- **Branch and preservation:** stayed on `quality-assurance`. Inspected the existing dirty tree and left all unrelated files, client patches/backups, server data and player records in place. No live quest state or character progression was changed in this stage.
- **QA prerequisite audit (Imlight MCP/RavenDB):** the live server is running (PID 4592) with Oscar RainbowFlower online in Library Book Stacks. Oscar's registry contains only `Tutorial_Intro_Complete=1`. The other Shadow QA record has `WC-UNICORN-MAIN-001_Complete` and `002_Complete`, but not `WC-UNICORN-MAIN-008_Complete`. No available Shadow QA character is eligible for normal Shadow enrollment. A qualified high-level Life character exists on another account; it was not used or altered.
- **Normal enrollment walkthrough:** **NOT TESTED.** No eligible Shadow QA character was available. I did not forcegrant, alter a prerequisite, clone another player's progress, or substitute quest-service completion for actual gameplay. Thus the normal Tutorial/Unicorn Way/Grim Tales sequence, Mr. Lincoln → Vespera → registrar → Ambrose, reward delivery, Three Streets unlock through the ordinary offer, and reconnect persistence remain unaccepted in this stage. Existing C1.6 evidence remains limited to its documented QA probes.
- **Three Streets gameplay:** **NOT TESTED** in this stage. No ordinary route quest, objective, boss, reward, marker, Vespera check-in, logout/reconnect, second-player, or Fire/Death/Balance client regression was exercised. Prior C1.6 direct Firecat check-in evidence was a force-granted QA probe and is not upgraded to ordinary progression proof.
- **Automated quest coverage:** updated `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ShadowQuestContractTests.cs` so the six street orders now feed each completed final-quest flag into the real `RequirementDispatcher`, then assert that only the matching Vespera check-in becomes eligible. The per-character test now checks all three streets against two independent Shadow wizard states and a non-Shadow wizard with the same completion flag. This is requirement/state simulation, not gameplay.
- **Validation:** focused `ShadowQuestContractTests` passed 6/6; full CoreLib suite passed 44/44; Director Release check-only build succeeded with 0 errors; `git diff --check` passed. The test build reported existing NU1904 Akka.Remote vulnerability advisories and NU1510 cryptography package warning. The live server error digest was empty over 30 minutes. The MCP bridge and Oscar remained online.
- **Changes this stage:** only the automated quest contract test above and this status document. No production quest data, game behavior, client resources, backups, or QA character records changed.
- **Known unresolved acceptance gap:** the `Ddl_WC_OT_SergeantMuldoon` tag inherited by Vespera check-ins still has no in-client marker-navigation verification. No new product defect was confirmed in this stage; missing proof is recorded as NOT TESTED.
- **Stage C2 gate:** the requested read-only reconnaissance of the canonical reunion-through-Krokotopia chain is recorded in `docs/quest-dependency-map.md` below. C2 quest implementation is still **NOT STARTED**. The user's instruction authorizes research before Unicorn Way is complete; it does not change C1 acceptance or permit progression edits.
- **Exact next gameplay acceptance slice:** obtain a Shadow QA character whose `WC-UNICORN-MAIN-008_Complete` flag was earned through the legitimate campaign; then test normal enrollment rewards/unlock in the client, ordinary Three Streets routes and bosses, each Vespera marker/check-in, one or more real street orders, and active-check-in reconnect plus second-character isolation. The C2 mainline map is completed in the section below; implementation remains unstarted.

## Stage C2 — reconnaissance and preparation checkpoint — 2026-10-22

- **Scope followed:** read the current continuation checkpoint, existing C1 dependency map and `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md`. Did not repeat the C1.7 prerequisite audit, alter character/quest progression, implement quest changes, or claim C1 acceptance.
- **Read-only evidence:** inspected the live server's loaded quest templates and the installed `r806919.Wizard_1_610` Root.wad. Confirmed the archive is still installed with the expected 113 generated files before and after reconnaissance; no install, restore, patch, backup, client, database or player-data write occurred. Quest dependencies, objective targets, rewards and localization keys are now in `docs/quest-dependency-map.md`.
- **Canonical chain:** all three independent street-final flags gate `WC-OLDE-MAIN-003A` by AND; then `WC-HAUNTED-MAIN-001` → `WC-HAUNTED-MAIN-002` → `WC-COMMONS-MAIN-004` → `WC-COMMONS-MAIN-005`. Reunion includes Bartleby proximity, one Foulgaze kill and Daisy; `Things and Stuff` is Daisy's two-Pumpkin-Head investigation; Nightside retains the Mortis → Death School → Dworgyn → Ambrose objective sequence; departure retains the Bartleby → Krokotopia teleporter → Quimby handoff.
- **Umbral Sight origin:** the exact canonical source event is Ambrose's temporary ghostly Pumpkin vision transfer in `WC-COMMONS-MAIN-004` start dialogue, `WizQst1731AF_00000002`–`_00000004`, before its Nightside-entry goal. Story persistence is only revealed after the ordinary trip/return, when the temporary effect should have faded. No early sight, route bypass or replacement objective is authorized by this research.
- **Adaptation and technical result:** the dependency map contains the Shadow-specific narrative outline, voice/character constraints, missing per-player vanilla-dialogue resolver and locale/audio risks, and the first independently testable slice: an Ambrose reunion response selected per recipient while canonical quest state stays unchanged. This is a proposal only; no C2 production change was made.
- **C1 status:** ordinary enrollment and Three Streets remain incomplete pending a Shadow QA wizard legitimately completing `WC-UNICORN-MAIN-008`. C1.7's 44/44 test result is unchanged and no new production defect was confirmed.
- **Progression ownership:** the user is handling the legitimate QA wizard questline. No character progression was changed in this reconnaissance.
- **Next step:** leave C2 implementation untouched until assigned. The first candidate slice and its two-recipient test contract are in the dependency map; normal C1 gameplay acceptance remains separately outstanding.

## Stage C1.8 — live-playthrough enrollment navigation repair — 2026-10-22

- **Live reproduction and post-fix state:** read-only Imlight MCP first confirmed Rika CrypticTheatre online as a level 5 Shadow wizard in `WizardCity/Interiors/WC_LibraryBookStacks`, with Goal 1 complete and Goal 2 active. To load the repaired server template, the Director process was restarted; no quest command, quest reset, forcegrant, removal, completion, or NPC interaction was issued. After a client crash during the follow-up and the user's relaunch/login, Rika returned to the same zone and the same goal progress. The client is now using the refreshed Root.wad locale resources.
- **Cause:** `Goal 2` (Vespera's persona goal) had the correct persona `ShadowSchool-Tutor`, correct `m_destinationZone` (`WizardCity/Interiors/WC_LibraryBookStacks`), and no client tags. Its `m_locationName` was empty, unlike the functioning vanilla school mentor goals, which provide a localized `ZoneLocName_*`. Goal 1's teleport result was independently correct, explaining why the player arrived despite the tracker lacking location labels. The persona goal itself supplies Vespera targeting; no unsupported Muldoon marker tag was added.
- **Fix:** Goal 2 now uses `ShadowSchool_EnrollmentLocation`, localized to `Wizard City|Library Book Stacks`, while retaining the known working destination and persona. Restored the Shadow clone's Halston entry to its vanilla Storm line and moved the Shadow referral line to Mr. Lincoln, before Goal 1's existing Library teleport. Lincoln now identifies Vespera and explains why she is in the Library; the existing Vespera tutor introduction remains on Goal 2. Four-goal order, Grim Tales/Shadow requirements, teleport coordinates, rewards, shared Three Streets join, and all seven vanilla school templates are unchanged.
- **Regression coverage:** extended `ShadowQuestContractTests` to assert the Vespera goal's localized destination, unchanged persona/tag contract, and Lincoln's referral speaker/text/location. The existing assertions continue to protect the seven vanilla enrollment paths and canonical progression.
- **Real-client acceptance:** after the user resumed the existing character, the quest journal rendered `Wizard City` and `Library Book Stacks` under the active `To Ravenwood!` card, with the active yellow star marker on that quest. The same live client showed Vespera Quillmoor's yellow quest marker and the direct `Press X or click to Talk` prompt. The server still reports Goal 1 complete, Goal 2 active, Goals 3-4 untouched. I did not activate the prompt: doing so would complete/advance the exact active objective the user asked to preserve. Thus tracker labels, destination, marker targeting and available direct-interaction prompt are live-verified; the actual NPC dialogue and goal completion were intentionally not exercised. The new Mr. Lincoln referral dialogue is covered by source contract assertions but was not replayed in this already-advanced real playthrough.
- **Client crash note:** the client crashed while closing the in-game book after the Root.wad refresh. The user independently relaunched via `IML.lnk` and completed login; I resumed the selected character without relaunching or handling authentication. No quest progress changed.
- **Validation:** the focused `ShadowQuestContractTests` passed 6/6. The Director Release check-only build succeeded with zero errors; copying over the running server output was unavailable. `git diff --check` passed. The client installer reported current generated resources after installation.
- **Scope:** C1 enrollment defect only. C2 remains untouched.

## Stage C1.9 — global Quest Helper arrow investigation — 2026-10-22

- **Scope and preservation:** read-only investigation of the live client, server, quest state, logs, crash evidence, and quest metadata. No quest command, progression edit, client/server restart, Root.wad install/restore, backup change, or production-code edit was made. Existing unrelated work and the active Rika CrypticTheatre wizard were preserved. C2 implementation remains **NOT STARTED**.
- **Client enablement:** both `preferences.xml` and `config.xml` in the installed Wizard101 `Bin` directory say `GameSettings.QuestHelperEnabled=1`. `CompassVisible=0` is a separate compass setting. The Quest Helper preference is enabled.
- **Rika's current state:** Rika is online as a level 5 Shadow wizard in `WizardCity/WC_Hub`. The server has two tracked quests: `WC-UNICORN-SIDE-001` (ID `5581784920439214535`, its first goal active) and `WC-COMMONS-MAIN-002-SHADOW` (ID `5269496231416357112`, Goal 4 active). Shadow Goals 1–3 are complete; Goal 4 is pending. The client's last logged selection is the Shadow quest ID and Goal 4 ID `5659213296229725484`, matching server state. No server-side selected-quest field exists; the client owns this selection.
- **Goal 2 → Goal 3 trace:** server log `logs/log20261022.txt` records Vespera's Goal 2 completion at `22:02:36.910`. At `22:02:36`, `WizardClient.log` records `QuestHelper::SetActiveGoal` to Goal 3 ID `4909129492914143260`. Goal 3 completed after the Registrar interaction at `22:04:40.770`; the client recorded active Goal 4 ID `5659213296229725484` at that time. At `22:04:45–46`, the client changed selection from the Unicorn side quest to the Shadow quest and set the corresponding goal IDs. Thus the client processed both objective advancement and tracked-quest switching; the server did not retain a stale goal or selected quest.
- **Destination check:** Goal 3 has `m_noQuestHelper=false`, location `ZoneLocName_818957`, destination `WizardCity/WC_Ravenwood`, and persona `WC-GTW-Registrar`. The destination and location key match the corresponding vanilla school-enrollment templates. Goal 4 also allows the helper; its empty destination is the Commons-local Ambrose objective and it carries the vanilla Commons target tags. The quest template is not marked `m_noQuestHelper`. `QuestService.SendGoalMessage` sends the goal's location, destination, tags, and `NoQuestHelper` flag; progress messages omit destination only for `sendType=2`, while Goal 3 activation is `sendType=1`.
- **Client presentation evidence:** `WizardClient.log` reports the Quest Helper arrow scene node was found after world entry (both after the earlier relaunch and after the current login). There are no Quest Helper errors in the log. The persisted preference, active quest/goal IDs, and goal transitions all update, so the evidence rules out a disabled preference, stale server tracking, and malformed Goal 3 destination. It localizes the regression to the client-side arrow presentation/target refresh after the helper accepts its active quest/goal state. The client log does not expose the arrow's runtime visibility or target, and this environment's Computer Use bridge returned `Trusted RPC service is not configured`, so an on-screen visibility check could not be performed.
- **Crash evidence:** the preserved dumps are `C:\ProgramData\KingsIsle Entertainment\Wizard101\Bin\WizardGraphicalClient_r806919.Wizard_1_610.dmp` (23,274,908 bytes) and `C:\Users\Shadow\AppData\Local\CrashDumps\WizardGraphicalClient.exe.49468.dmp` (24,228,278 bytes), both timestamped `21:55:46–57`. Windows Error Reporting records `BEX64`, fault module `ucrtbase.dll`, exception `0xc0000409`. The crash predates the Goal 2 → Goal 3 transition; the client subsequently relaunched, reconnected, and found the arrow node. The available report does not establish that the Root.wad refresh caused the crash or that the crash caused the later global arrow symptom. `install.py status` confirms the installed client and 113 Root.wad files are current; no installer action was taken.
- **Other characters:** only Rika is online. The same account's saved-character records include a level 1 Shadow wizard with no tracked quests and a level 5 Shadow wizard with no tracked quests, so neither provides a comparable active-quest repro. Other-school live navigation was not tested on another character.
- **Code/test decision:** no server defect is confirmed, so no production code or speculative regression test was added. The client acceptance checks (visible arrow for a normal quest and Shadow enrollment, visible target changes on quest switch/objective completion, reconnect recovery, and another school) remain **NOT VERIFIED**. Current evidence identifies the failing subsystem but not the exact native-client condition that hides or fails to retarget the arrow. Do not change quest progression or start C2 as a workaround.
