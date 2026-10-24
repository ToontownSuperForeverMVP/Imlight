# Shadow School — continuation status

The live checkpoint for continuing this mod. Read this first, then the targeted diffs; do not re-audit the
repository from scratch. The authoritative design is `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md`; the
execution contract is `Shadow_School_DeepSeek_V4.1_Flash_1017_Handoff.md` (both at the repository root).

## Latest checkpoint — 2026-10-23 (Stage D2.4, Ravenwood conquest audit)

- **Scope complete:** audited all seven instructor quest templates/profiles/variants, campaign progression and replay, Umbral Sight gates, private-zone exit handling, boss-cheat dispatcher/effect paths, combat representations, and the installed ID data. Ambrose and the pact remain absent; Stage D3 was not started.
- **Progression stabilization:** added `SevenVerifiedInstructorVictoriesCompleteEveryChapterAndEveryCampaignMilestone`, which runs all seven instructors in order through the real `BattleOutcomeEvents` observer, event validators, campaign milestones, return goals and chapter completion. Before the seventh victory, `AllInstructorsDefeated` remains false; after all seven chapter completions, `AllInstructorsComplete` is true. No production bug was reproduced, so no production source/data was changed.
- **Validation:** focused Conquest + Umbral Sight tests **252/252 three consecutive runs**; all ShadowSchool tests **367/367 three consecutive runs**; full CoreLib **380/380**; installer **2/2**; CoreLib and Director Release builds succeeded with 0 errors; isolated QA Director startup loaded ShadowSchool and 19 SpiralDB files against a new database on temporary ports, reached ready state, then was stopped. `git diff --check` result is recorded in `docs/stage-d2.4-audit.md`.
- **Combat scripts:** 15 additions for Wu/Ashthorn/Wethersfield were structurally parsed and source-traced through trigger dispatch, target resolution, effect application, counters and per-duel reset. No live battle executed them; runtime behavior, animation, effect ordering and balance remain **NOT TESTED**.
- **Positions and presentation:** Balestrom/Drake circles are offset from the NPC by 90 horizontal units; Wethersfield's circle is about 37.7 units below his NPC placement. Ravenwood floor contact and every circle/arrival remain **NOT TESTED**. Dworgyn and Alhazred remain Ashthorn/Wethersfield's compatible school-matched combat stand-ins; their client appearance remains **NOT TESTED**. No speculative asset changes were made.
- **Safety:** no access to the live player database or Rika CrypticTheatre; no client launch, executable/Root.wad modification, regular deployment config change, or commit. Isolated config/database/logs are under `build/qa-data/d2.4-audit`.
- **Artifacts:** full severity/evidence matrix in `docs/stage-d2.4-audit.md`; quest gates in `docs/quest-dependency-map.md`; D3 prerequisites and risks in `docs/stage-d3-handoff.md`.

## Latest checkpoint — 2026-10-23 (Stage D2.3, Claude Sonnet 5.5)

- **Done:** Moolinda Wu (Life), Malorn Ashthorn (Death) and Arthur Wethersfield (Balance) are encounters 5, 6 and 7 in
  `ConquestEncounters.Encounters`, wired into the existing `ShadowSchool-Conquest` milestones (no second tracker). All seven
  instructors are now playable in order; Ambrose's finale and the binding pact do **not** exist and stay locked. Data: chapter
  quests `ShadowSchool-Conquest-Wu/Ashthorn/Wethersfield`, customs `1880005/6/7`, private variants
  `WC_SchoolLife@ShadowConquest-Wu`, `WC_Ravenwood@ShadowConquest-Ashthorn`, `WC_Ravenwood@ShadowConquest-Wethersfield`,
  Umbral bindings `LifeClassroom` / `DeathInterim` / `BalanceCircle`, text (`ConquestWu*`, `ConquestAshthorn*`,
  `ConquestWethersfield*`, `Conquest_*_Enter/Won/Lost`, `Umbral_*`). Ids, positions, Start/Exit locations and spell data were
  read from the installed game (QA server `/eval`, ports 32xxx, fresh DB `build/qa-data/d2.3`).
- **Two of the three do not stand in a school.** `WC-RAV-NPC08` (Ashthorn, 81102) and `WC-RAV-NPC09` (Wethersfield, 81103) are
  placed in `WizardCity/WC_Ravenwood` itself, at (229.29, 6034.79, -30.48) and (558.59, 444.97, 7.18). Their anchors, private
  copies and exits are all Ravenwood. Wu (38204) is in `WC_SchoolLife` at (760.20, 15.49, 100.00) (also in `WC_SchoolLife_C02`,
  not used). Circles for the two Ravenwood fights sit exactly on the game's own `Player Stand In` objects (the only ground
  Ravenwood's data names near them); arrival/exit is the game's `Loc_WC-COMMONS-MAIN-002-DEATH` / `-BALANCE`.
- **Same-zone exit bug, fixed.** A Ravenwood copy whose exit is Ravenwood used ZoneService's same-zone shortcut and only
  teleported the wizard inside the copy. `ConquestWorld.Leave` now sets `ForceReload` when `ExitZone == Zone`, and
  `ZoneService.RequestZoneTransfer` treats "plain zone requested while in a noted variant copy of it" as leaving the copy
  (`ZoneService.LeavesVariantCopy`: reload and forget the note). That covers the duel's flee/loss path (`MSG_SENDTOHUB`), doors
  and triggers. No existing variant (Malistaire's mausoleum, the school copies) exits to its own zone, so none is affected.
- **No combat creature of their own for Ashthorn and Wethersfield.** Only Wu has a Treasure Tower floor-30 creature
  (1475615). The Death and Balance floor bosses (1475616 Dworgyn, 1475617 Alhazred) stand in, and the customs set `Name` to
  "Malorn Ashthorn" / "Arthur Wethersfield". The visual stand-in is **NOT TESTED** in the client.
- **The creature AI was checked and cannot be trusted with heals, wards or drains** (code read + QA-server filter calls):
  - `CombatCreatureAIComponent`: aggressiveness on these creatures is 9 (always), so a creature attacks whenever it has a damage
    spell it can pay for; it only "prepares" (heal, ward, buff, debuff) on a round with no castable attack. Buffs/heals meant
    for itself are aimed at `SlotIndex` only when "selfish" (0.8), otherwise at `SlotIndex+1` (another circle).
  - `SpellEffectFilter.IsDamageEffect` ignores `kStealHealth`. Confirmed on the server: Vampire, Wraith and Ghoul
    are damage=False, heal=False, buff=False, debuff=False, so a deck of drains is never cast (and would pass every round).
  - Therefore heals, wards, drains and shield adaptations are the engine's own **BossCheats** on the customs
    (`"Cheats": {"Added": [...]}`; `CombatResolver.BossCheats.cs`, `CheatSteps.cs`): free casts of the real spell through the
    normal effect path, with `MaxTimes`, health thresholds and a growing damage boost from round 9/10 so no fight can stall.
    Verified on the server: all 15 scripts load for the three keys (`BossCheats.For(Customs.Key(id))`), every cast spell
    resolves in `SpellFactory`, and `ApplyStealHealthEffect` heals the caster by damage x healModifier (0.5 for Vampire/Wraith).
    **No live duel has run these scripts** (no client; the agent bridge fights only as a logged-in player), so the scripted
    moves and the fight balance are **NOT TESTED in a battle**.
  - Pre-existing, not changed: Drake's Myth Shield/Mythblade (D2.2) and Greyrose's Tower Shield/Iceblade (D2.1) are friendly
    buffs in a deck, so they are subject to the same `SlotIndex+1` aim 20% of the time.
- **Fights** (all cards from installed templates; levels continue 10-13 -> 14-16):
  - Wu, Life, level 14, 1300 hp: Leprechaun (155-195) and Nature's Wrath (240-300) unlimited, one Life Trap. Scripts: Spirit Armor
    (absorb 400) at the start and in rounds 4 and 8; Pixie (self-heal 400) once under 65% and once under 35%; +15% damage
    every second round from round 9. At most 800 healing and 1600 absorb in total (four Spirit Armors).
  - Ashthorn, Death, level 15, 1200 hp, "modest by design": Death Troll (160-200), Banshee (245-305) unlimited, Poison x2,
    Skeletal Pirate, Feint, Plague. Scripts: Curse at the start; Vampire (drain 335, heals half) in rounds 2, 5, 8, 11 (4 casts);
    Wraith (drain 500) once under 40%; a pip stolen from a wizard who casts a 4+ pip card (3 times); +10% from round 9. He can
    take back at most 920 hp.
  - Wethersfield, Balance, level 16, 1500 hp, 10% pierce: Scorpion (160-200), Locust Swarm (245-305) unlimited, Spectral Blast
    once (Fire 440 / Ice 365 / Storm 550), Weakness, Hex. Scripts: Balanceblade at the start; Muzzle (-25% damage, -45% accuracy)
    in rounds 3 and 7; a 25% shield after each damage spell that lands on him (3 times); one re-balance under 50% (35% Shadow
    shield and a blade); +10% from round 10. No healing. No elemental/spirit shield card (they do not reduce Shadow damage).
- **Campaign:** the seventh logic entry now completes `ShadowSchool-Conquest` (it used to neither add a goal nor complete, which
  logs a warning and left completion to a fallback). `ConquestEncounters.AllInstructorsDefeated/AllInstructorsComplete` derive
  "all seven" from the seven durable stages (+ chapter completions); nothing starts the finale.
- **Verification:** CoreLib **379/379** (81 new tests: `ConquestCampaignTests.LifeDeathBalance.cs` and the
  `ConquestCampaignTests.StormMyth.cs` theories now cover Wu/Ashthorn/Wethersfield); Conquest + Umbral tests **251/251** on three
  consecutive runs; installer **2/2**; CoreLib and Director Release builds 0 errors; `git diff --check` clean. Deployed to
  `build/server` (DLLs + `mods/ShadowSchool` spiraldb/text); isolated QA startup loaded ShadowSchool and the new files, applied
  all three variants (trainer removed, creature -> custom, circle at the verified coordinates) and resolved the three profiles
  and 15 scripts. QA ini changes (ports 32xxx, QA database, bridge token) were reverted: `Config/Imlight.ini` is byte-identical
  to the pre-D2.3 deployed ini (backup `Config/Imlight.ini.pre-D2.3.bak`). No server or client was left running.
- **Client resources:** no client was running; `install.py install --generated build/server/mods/ShadowSchool/generated` was run
  once (original `Root.wad`/exe backups untouched); the installed `Root.wad` now holds the D2.3 strings and
  `generated_resource_drift` against that output reports invalid=[] stale=[]. Note `install.py status` ignores `--generated`
  and reads the newest `bin/*/*/mods/ShadowSchool/generated`, so it can report "UPDATE AVAILABLE" for an older output.
- **GRAPHICAL CLIENT ACCEPTANCE: NOT TESTED for all seven encounters.** Not verified in a client: the Dworgyn/Alhazred model
  and name tag as stand-ins, the circles' ground (Wu's platform extent; both Ravenwood circles, and Wethersfield's slope from
  Z 7.18 to -30.5), aggro/joining, the reload into and out of a private Ravenwood, the journal, any fight (balance, turn
  counts, scripted heals/drains/shields), and the lost/fled return.
- **Character safety:** Rika CrypticTheatre and the live player database were not accessed (QA used `build/qa-data/d2.3` and
  unsaved synthetic wizards in tests only).
- **Next:** Stage D3 (Ambrose and the pact): `docs/stage-d3-handoff.md`.

## Latest checkpoint — 2026-10-23 (Stage D2.2, Claude Sonnet 5.5)

- **Done:** Halston Balestrom (Storm) and Cyrus Drake (Myth) are encounters 3 and 4 in `ConquestEncounters.Encounters`, wired
  into the existing `ShadowSchool-Conquest` milestones (no second tracker). Data: chapter quests
  `ShadowSchool-Conquest-Balestrom/Drake`, customs `1880003/1880004`, private variants
  `WC_SchoolStorm@ShadowConquest-Balestrom` and `WC_SchoolMyth@ShadowConquest-Drake`, Umbral bindings `StormClassroom` /
  `MythClassroom`, text (`ConquestBalestrom*`, `ConquestDrake*`, `Conquest_*_Enter/Won/Lost`, `Umbral_*Classroom`). Ids,
  positions, Start/Exit locations and spell data were read from the installed game (QA server `/eval`, ports 32xxx, fresh
  DB `build/qa-data/d2.2`). Falmea/Greyrose and the D2.1 recovery fixes are untouched except: `MeetsPrerequisites` now needs
  every earlier encounter complete; Greyrose/Balestrom/Drake chapters also require the previous chapter's Complete entry; the
  Greyrose portrait file name was corrected to a real game file.
- **Not done on purpose:** Wu, Ashthorn, Wethersfield, Ambrose's finale and the binding pact do not exist and stay locked.
- **Verification:** CoreLib **298/298** (47 new tests in `ConquestCampaignTests.StormMyth.cs`); Conquest + Umbral tests
  170/170 on three consecutive runs; installer 2/2; CoreLib and Director Release builds 0 errors; `git diff --check` clean
  (only existing LF/CRLF notices). Deployed to `build/server` (DLLs + `mods/ShadowSchool`); isolated QA startup loaded
  ShadowSchool, 16 SpiralDB files, applied both variants (trainer removed, creature -> custom, circle at the instructor's Z)
  and resolved both profiles with real spell templates. Ports/ini restored (backup `Config/Imlight.ini.pre-D2.2.bak`);
  no server or client was left running. Client installer status: Root.wad/exe as installed, resources current, original
  backups present.
- **GRAPHICAL CLIENT ACCEPTANCE: NOT TESTED for all four encounters** (Falmea, Greyrose, Balestrom, Drake). Visuals, journal,
  teleport, circle placement/platform extent, aggro and the actual fights (balance) are unverified in gameplay.
- **Character safety:** Rika CrypticTheatre and the live database were not accessed; QA used synthetic unsaved wizards and
  the QA database only.
- **Stage D2.3 handoff (Moolinda Wu, Malorn Ashthorn, Arthur Wethersfield):** add three rows to `ConquestEncounters.Encounters`
  (orders 5-7; ids `Wu`, `Ashthorn`, `Wethersfield`, which the campaign milestone names already use), each with: the trainer
  NPC (candidates from `id-lookup/ids.csv`, to verify in each zone's `gamedata.bin` as `.scratch/agent/d22zones.cs` does:
  Wu `WC-RAV-NPC01` 38204, Life, creature 1475615 exists; Ashthorn `WC-RAV-NPC08` 81102; Wethersfield `WC-RAV-NPC09` 81103; find
  their schools, zones, positions and combat creatures the same way Balestrom/Drake were found),
  customs `1880005-7`, variant ZoneObjects files, chapter quests (copy via `.scratch/agent/make_d22.py`, which clones
  Greyrose with new goal-id hashes from `StringHash.Compute`), `Umbral_*` binding rows, text keys, milestone
  `m_locationName`/`m_destinationZone` in `questtemplates_ShadowSchool-Conquest.json`, then update the tests that assert
  "the last three are absent" (`TheFourImplementedInstructors...`, `AnUnimplementedMilestone...`, `Order > 4`, the Customs
  and QuestTemplates directory listings) and extend `s_stormMyth` in `ConquestCampaignTests.StormMyth.cs`. Do not touch the
  Ambrose finale or binding pact. Run QA servers from the repository root (so the 3.1G `cache` is used) with ports 32xxx.

## Latest checkpoint — 2026-10-23 (Stage D2.1 recovery, GPT-6 Luna High)

- **Current target:** D2.1 is structurally complete for its available server and automated acceptance. The campaign quest
  and Falmea/Greyrose encounters are deployed with ShadowSchool enabled. **GRAPHICAL CLIENT ACCEPTANCE: NOT TESTED**;
  neither battle has been completed through the client, so visuals, journal presentation, teleport and combat remain
  unverified in gameplay.
- **Recovery fixes:** verified victories now persist the instructor registry before the journal event is queued, reconnect
  replays the recorded defeat, a dropped/reaccepted chapter replays its anchor before the victory, and quest commands or
  unvalidated milestone events cannot fabricate campaign progress. A custom profile's explicit `Drops: []` now suppresses
  inherited creature drops instead of falling back to the base drop tables.
- **Latest verification:** conquest campaign + encounter tests **85/85**, repeated three more times (**85/85** each);
  full CoreLib **251/251**; client installer **2/2**; CoreLib and Director Release builds succeeded (0 errors); `git diff
  --check` clean. Known build warnings include the existing Akka.Remote NU1904 advisory.
- **Deployment:** copied the Release assemblies and ShadowSchool mod data into `build/server`, preserved the prior deployed
  config as `Config/Imlight.ini.pre-D2.1-20261023.bak`, and enabled `[Mods] Enabled = ShadowSchool`. Installed client
  locale resources are current (113 Root.wad files reported); original installer backups remain in place. An isolated
  QA Director startup loaded ShadowSchool and its 14 SpiralDB files and started patch/login/game listeners on temporary
  ports with a fresh QA database. The smoke used the machine's x64 .NET 10 `dotnet` host to run the deployed DLL; launching
  the apphost directly chose the deployment-local hostfxr and failed to find a shared framework. It was stopped and the
  normal deployment ports restored. Startup also logged one unrelated base-data error for `ReduceDamageInvunerable` with
  no stat table; this symbol is absent from ShadowSchool.
- **Character safety:** Rika CrypticTheatre and the live player database were not accessed. QA startup used
  `build/qa-data/d2.1`; no graphical client or credentials were used.
- **Fixed in D1.5 (previous stage, still in place):** the private-copy transfer was a same-zone transfer without
  `ForceReload` (the copy never loaded; `ZoneService.cs:273`), and the quest's goals had no client navigation (the "unknown
  zone" class of defect) with a copied Muldoon tag. D1.5's own run: 213/213, server restarted on that build, locale reinstalled.
- **Not touched:** Rika CrypticTheatre (`wizards/257-A`) — no database open, no character record read or saved. No client
  was started and no credentials were entered.

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

## Stage C2.1 — per-recipient mainline dialogue resolver + Ambrose reunion — 2026-10-23 (Claude Opus 5.5)

- **Start state / reinstall:** the mod had been uninstalled on both sides: `[Mods] Enabled =` was blank in
  `src/Imlight.Director/Config/Imlight.ini`, and `install.py status` said "not installed", with
  `WizardGraphicalClient.exe` and `Root.wad` byte-identical to their `*.shadowschool-backup` originals
  (`544AB726…7CF7E`, `4E09B3DA…42C66`). Re-enabled `Enabled = ShadowSchool`, started the server (log: mod enabled,
  "Mods put 113 files in Root.wad", 1,088 quest templates), then ran `install.py install` with no game process
  running. After the C2.1 locale was added, the installer status reported the client resources stale; they were
  reinstalled again (client still closed) and now report **current**. Both backups were re-hashed after each
  install and are unchanged.
- **Unrelated build break fixed (one character):** the server would not build because of a stray `}` at
  `src/Imlight.CoreLib/Game/Effects/CharacterEffectHelper.cs:384` (part of uncommitted, non-Shadow stat-effect work,
  edited 2026-10-23 11:49). Only that brace was removed; nothing else in the file was touched.
- **Checkpoint (review snapshot):** `refs/heads/shadow-school/checkpoint-pre-c2.1` = `18b388ec`, built through a
  temporary git index so HEAD, the index, the working tree and `quality-assurance` were not touched. It holds the mod,
  its tests and docs, the whole-file Shadow core hooks (`TrainService`, `InteractTrainerComponent`,
  `InteractServiceMementoComponent`, `RootArchiveLoader`, `Imlight.CoreLib.csproj`), only the Shadow/mod hunks of
  mixed core files (keyword-selected), and only the `[Mods]` section of `Imlight.ini` (the Agent Bridge token was
  verified absent). Because mixed hunks were keyword-selected, the snapshot is for review and recovery and is not
  guaranteed to build alone. Nothing was pushed. C2.1 itself is **not** in that commit; it is uncommitted.

### Dialogue dispatch paths inspected

Every place a quest template's `ActorDialog` reaches a client was traced before writing the resolver:

| Path | Tag(s) | Recipient known by | Now resolved |
|---|---|---|---|
| `QuestService.ShowQuestStartDialogue` | quest `Start` | the session's wizard | yes |
| `QuestService.ShowGoalStartDialogue` | goal `Prep` | session | yes |
| `QuestService.ShowGoalCompletionDialogue` (persona pending-close path **and** `CompleteGoal`) | goal `Completion` | session | yes |
| `QuestService.ShowQuestCompletionDialogue` | quest `Complete` | session | yes |
| `InteractQuestOfferComponent.ShowQuestInfoDialog` (NPC offer) | quest `Prep` | `playerCharacter` | yes |
| `InteractQuestOfferComponent` → `MSG_SENDQUESTOFFERCACHEOPTION` (whole template, dialogues included) | all | `playerCharacter` | yes (`ResolveTemplate`: per-wizard copy) |
| `InteractQuestUnderwayComponent.ShowQuestUnderwayDialog` | quest `Underway` | `playerCharacter` | yes |
| `CommandQuestProtocol` `.quest offer` (QA) | quest `Prep` + cache | `Context.Character` | yes |
| `QuestService` `FindDialogue` before a persona completion | goal `Completion` | — | presence check only; the resolver never adds or removes a dialogue |
| `InteractQuestSelectComponent.CompletionDialogKey` | — | — | not a send (matches usage goals); unchanged |
| `ResActorDialogHandler` (`ResActorDialog` results) / `CinematicService` | result/cinematic dialogs | — | **not covered**: not quest-template dialogue lists; outside C2.1 |

Each quest-dialogue `MSG_ACTORDIALOG` above goes to one player's socket or actor (`SendToSocket` /
`playerActor.Tell`); none is broadcast to the zone or party. A party member completing the same goal runs their
own `QuestService`, so each member's dialogue is resolved for that member.

### What was built

- **Core hook (generic, mod-agnostic):** `src/Imlight.CoreLib/Game/Quests/QuestDialogueResolver.cs` —
  `QuestDialogueRequest(Recipient, Quest, Goal, Canonical)`, `IQuestDialogueProvider`, and
  `QuestDialogueResolver.Resolve` / `ResolveTemplate`. Providers are the enabled mods implementing the interface
  (`ModRegistry.Enabled`). A provider returns a **new** dialogue or null; a throw, a different tag or an empty
  dialogue is logged and ignored, and the canonical object is returned. `ResolveTemplate` returns the shared
  template itself when nothing changes, else a C# record copy (`with`) for that one wizard; goals that are not
  adapted stay the same objects.
- **Call sites changed:** `QuestService.cs` (the four `Show*Dialogue` helpers now take the quest template and resolve
  for `GetActiveWizard()`), `InteractQuestOfferComponent.cs`, `InteractQuestUnderwayComponent.cs`,
  `CommandQuestProtocol.cs`. No goal, requirement, result, reward, teleport or completion logic changed.
- **Mod-owned adaptation table:** `src/Imlight.CoreLib/Mods/ShadowSchool/ShadowMainlineDialogue.cs`
  (`ShadowSchoolMod` now also implements `IQuestDialogueProvider` and delegates to it). Each adaptation names quest,
  goal, tag, the **exact canonical text keys it expects**, an eligibility predicate and a script of kept canonical
  lines and added lines. If the installed dialogue no longer holds the expected keys (a game revision change), it logs
  one warning and falls back to canonical.
- **First adaptation — Ambrose's reunion** (`WC-OLDE-MAIN-003A`, goal `Goal` = persona `WC-HUB-NPC01`, tag
  `Completion`). Canonical: six voiced Ambrose lines `WizQst1731AB_00000003`–`_00000008`
  (`MerleAmbrose_005_04`–`_09.mp3`). Shadow version, nine lines:
  `ShadowSchool_ReunionAmbroseStreets` (new) · `_03` · `_04` · `_05` · `ShadowSchool_ReunionAmbroseForbidden` (new) ·
  `_06` · `_07` · `ShadowSchool_ReunionAmbroseEye` (new) · `_08`.
  Eligible only when the receiving wizard is Shadow, has `WC-COMMONS-MAIN-002-SHADOW_Complete` and all three street
  flags (`WC-TRITON-MAIN-009`, `WC-FIRECAT-MAIN-007`, `WC-CYCLOPS-MAIN-005` `_Complete`), and has **not** completed
  `WC-OLDE-MAIN-003A`.
- **Narrative constraints honoured:** the canonical exposition stays, in Ambrose's recorded voice (the caves, Sylvia,
  "I forbade him", the Order of the Fang, Bartleby's Eyes). The added lines give: recognition of all three streets with
  a nod to Vespera (continuity with the C1 enrollment); why his "forbade" is a legitimate warning to a student whose
  magic lies outside Ravenwood, without holding Shadow against them; and the student's interest in the Eye as the
  keeper of what the Spiral remembers and who may know it ("Want it found, Wizard, not owned"). Ambrose does **not**
  say Malistaire took the Eye (canonically he learns that from Bartleby in Goal 1), and no line mentions Morganthe,
  Umbral Sight, the Nightside or a pact.
- **Audio and presentation:** added lines are **text-only** (`m_soundFile = ""`, with no SFX or music), the same
  treatment the C1 Vespera check-ins used and that rendered in the client. Each added line copies the presentation of a
  named canonical Ambrose line: speaker (actor 0, the persona NPC), portrait `Art_Portrait_Ambrose.dds`, camera
  (`Cinematic Camera - Merle Ambrose` for the opener; the no-camera-change entries for the other two), camera zone,
  action and animation list. Kept lines are the canonical objects themselves, so their voice, camera and timing are
  unchanged.
- **Locale:** three keys in `src/Imlight.Director/mods/ShadowSchool/text/en-US.txt` under `[ShadowSchool]`, built into
  `Locale/<every locale>/ShadowSchool.lang` by the existing overlay. The live server's Root.wad and the installed client
  carry them.

### Evidence

- **Automated:** new `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ShadowMainlineDialogueTests.cs` (18 cases), run
  against the installed canonical quest (vendored as `Fixtures/questtemplates_WC-OLDE-MAIN-003A.json`, SHA-256
  `cc781813…6de1f81`, asserted equal to the server's `spiraldb-cache` copy when present). Each case resolves through
  `QuestDialogueResolver` with the real `ShadowSchoolMod` provider:
  1. Shadow at the reunion → the nine-line response. The added lines are unvoiced and use Ambrose's portrait, camera,
     actor and animation; the kept lines are the canonical (voiced) objects.
  2–3. Fire, Death and Life (and Ice, Storm, Myth, Balance) in the identical state → the canonical object itself.
  4. Interleaved Shadow/Fire/Shadow/Death recipients and a not-yet-eligible Shadow wizard → no contamination; each
     Shadow recipient gets its own object.
  5. Template JSON snapshot identical after resolving every dialogue and `ResolveTemplate` for all eight schools.
  6. The Shadow template copy keeps requirements, end results, start goals, goal logic, quest dialogues and five goals
     by reference. The adapted goal is equal apart from its dialogue. The quest still has three AND street flags, six
     goals and drop table `WC-OLDE-MAIN-003A`, and completion writes `WC-OLDE-MAIN-003A_Complete`.
  7. Missing any one street, missing Shadow enrollment, a fresh wizard or a completed reunion → canonical (and the
     canonical requirements also refuse the missing-street wizards).
  8. Other dialogues of the same quest, another quest/goal/tag, changed or shorter canonical keys, null inputs, no
     providers, a throwing provider, and wrong-tag or empty answers → canonical.
  9. Registry and quest instances round-tripped into a new wizard object → same quest state and same dialogue (Shadow
     and Fire); a completed reunion stays canonical. This is a persistence simulation, not a live reconnect.
  10. Across all 935 `spiraldb-cache` quest templates, `ResolveTemplate` returns the shared template unchanged for
     each original school; for Shadow only `WC-OLDE-MAIN-003A` differs.
  Two further cases: the MSG_ACTORDIALOG wire format (same `ObjectSerializer` and flags as `SendActorDialog`)
  round-trips the Shadow dialogue, and the three locale lines exist, keep the embargo and survive the `.lang` writer.
- **Runs:** focused suite 18/18; full CoreLib suite **65/65**; installer Python tests 2/2; Director Release build
  (through the server restart) 0 errors (existing NU1904/NU1510 warnings only).
- **Live server (in memory only):** after the restart, `/eval` confirmed `ShadowSchoolMod` is the enabled
  `IQuestDialogueProvider` and the live Root.wad's `ShadowSchool.lang` holds the three keys. Resolving the server's
  loaded `WC-OLDE-MAIN-003A` for synthetic, unsaved wizards gave: Shadow → the nine keys, `[text-only]` on the three
  new lines and `[voiced]` on the six canonical ones; Fire and Life → the same canonical object; template JSON
  unchanged. No database write, quest command or player interaction was involved.

### Client acceptance — NOT TESTED

The game client was not running and login is user-operated, so nothing was observed on screen. **NOT TESTED:** the
reunion dialogue rendering (text, wrapping of the ~210–230-character lines, portrait, camera moves between canonical
and added lines, silence on the three added lines while the canonical lines keep their voice); the canonical
response for a non-Shadow wizard in the client; two clients side by side; and an actual reconnect. A safe QA path
needs a Shadow character that has legitimately reached `WC-OLDE-MAIN-003A`. None exists, and progression was not
altered to manufacture one. Source tests do not prove visual correctness.

### User data and preserved state

- Rika CrypticTheatre (`wizards/257-A`, level 5 Shadow) was only read: `WC-COMMONS-MAIN-002-SHADOW` Goal 4 is still
  active alongside `WC-UNICORN-SIDE-001`, the registry runs through `WC-UNICORN-MAIN-008_Complete`, and the record was
  last modified 2026-10-23 02:53 (before this session). No player was online during the restart. No quest command,
  forcegrant, removal or reset ran.
- Client backups are unchanged (hashes above). No unrelated working-tree file was reverted, staged or cleaned. The
  only edits outside Shadow/C2.1 are the one-brace build fix above and `Enabled = ShadowSchool` in `Imlight.ini`.

### Outstanding C1 / client QA issues (unchanged; not attributed to Shadow School)

- **Quest Helper arrow missing across several quests** (C1.9): the preference is on, server goal state and selection
  packets are correct, and the arrow scene node exists; its runtime visibility and target were never established. No
  evidence links it to Shadow School, and no executable patch was attempted.
- **Client crash** `0xc0000409` in `ucrtbase.dll` (dumps preserved, C1.9): cause unknown; no causal link to the arrow
  or to the mod is established.
- Normal C1 acceptance (Grim Tales → Shadow enrollment → Three Streets → check-ins) is still the user's live
  playthrough on Rika; untouched here.

### Limitations / known gaps

- Journal, goal titles, tracker and objective text for the reunion stay canonical. They say "Talk to Headmaster
  Ambrose", which is still true. The plan's journal/hint overrides are not built yet.
- `ResActorDialog` results and cinematic dialogues are outside the resolver.
- The added lines are English for every locale, like the rest of the mod's text.

### Files (C2.1)

New: `src/Imlight.CoreLib/Game/Quests/QuestDialogueResolver.cs`, `src/Imlight.CoreLib/Mods/ShadowSchool/ShadowMainlineDialogue.cs`,
`test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ShadowMainlineDialogueTests.cs`,
`test/Imlight.CoreLib.Tests/Mods/ShadowSchool/Fixtures/questtemplates_WC-OLDE-MAIN-003A.json`.
Modified: `src/Imlight.CoreLib/Game/Services/QuestService.cs`, `.../Zone/Components/InteractQuestOfferComponent.cs`,
`.../Zone/Components/InteractQuestUnderwayComponent.cs`, `.../Commands/Protocols/CommandQuestProtocol.cs`,
`src/Imlight.CoreLib/Mods/ShadowSchool/ShadowSchoolMod.cs`, `src/Imlight.Director/mods/ShadowSchool/text/en-US.txt`,
`src/Imlight.Director/mods/ShadowSchool/docs/{continuation-status,quest-dependency-map}.md`,
`src/Imlight.Director/Config/Imlight.ini` (`[Mods] Enabled`), `src/Imlight.CoreLib/Game/Effects/CharacterEffectHelper.cs` (one brace).

### Next

1. **C2.1 client acceptance**, once a Shadow wizard legitimately reaches *A Brief History of the Spiral*: talk to
   Ambrose and check the nine lines, silence on the three added ones, and the portrait and camera. Then check a
   Fire/Death/Life wizard's canonical response, ideally with two clients side by side, followed by a relog.
2. **Stage C2.2 — the next implementation milestone:** Foulgaze (`WC-OLDE-MAIN-003A` Goals 2–3,
   `WizQst1731AB_00000018`–`_00000024`), Lord Nightshade and Daisy (`WC-HAUNTED-MAIN-001` / `-002`, `WizQst1731AD` /
   `WizQst1731AE`), and the lead-in to Ambrose's temporary Nightside enchantment (the `WC-HAUNTED-MAIN-002` Goal 3
   return to Ambrose, ending just before the `WizQst1731AF_00000002`–`_00000004` cast in `WC-COMMONS-MAIN-004`). Use
   new `ShadowMainlineDialogue` adaptations only (no template edits). Keep Nightshade hostile and Daisy's experiment
   intact, keep the Eye's thief unknown until Bartleby names him, and do not grant or show Umbral Sight. Extend the
   tests in the same pattern: per school, isolation, gating, fallback and the whole-database sweep.

## Stage C2.2 — Foulgaze, Nightshade and Daisy dialogue + Shadow reward rows — 2026-10-23 (Claude Sonnet 5.5)

**Scope kept:** presentation only. No goal, prerequisite, completion flag, XP, teleport or dependency of
`WC-OLDE-MAIN-003A`, `WC-HAUNTED-MAIN-001` or `WC-HAUNTED-MAIN-002` was changed; no Umbral Sight, instructor duels,
Ambrose pact, Morganthe or Krokotopia content; nothing past Daisy's referral to Ambrose. Rika CrypticTheatre's
character, database records, quests and inventory were not touched (the database was not opened at all).

### What was built

All on Opus's C2.1 resolver (`QuestDialogueResolver` + `ShadowMainlineDialogue`); no new dialogue system. Ten new
`Adaptation` rows (each names quest, goal, tag, the exact canonical keys it expects, a state predicate and a script).
Every canonical line stays as the canonical object, voice included; each added line is **text-only** and borrows the
speaker, portrait, camera and animation of a named canonical line.

| Quest / goal / tag | Added key (`ShadowSchool_…`) | Speaker | Idea |
|---|---|---|---|
| 003A `Goal 1` Completion (Bartleby) | `BartlebyEye` | Ambrose | notices the student asking who may look through an Eye of History, and who decides |
| 003A `Goal 2` Completion (Foulgaze, tower) | `FoulgazeTaunt` | Foulgaze | hostile; smells Shadow, asks how many shelves are closed to them |
| 003A `Goal 3` Completion (after the fight) | `FoulgazeBooks` | Foulgaze | books hoarded for a master; every power keeps a locked room |
| 003A `Goal 5` Completion (Daisy, cave) | `DaisyCave` | Daisy | has found no book on Shadow magic in the Library |
| 001 Prep (Daisy's request) | `NightshadeOffer` | Daisy | "the curious kind of magic is the kind people warn you about" |
| 001 `Goal` Completion (tower) | `NightshadeTower` | Nightshade | tries to claim the Shadow wizard ("Kneel"); stays hostile |
| 001 `Goal 1` Completion (defeat) | `NightshadeDefeat` | Nightshade | "ten thousand voices, none dared say no… how quiet that is" (a warning, not an alliance) |
| 001 `Goal 2` Completion (Daisy) | `DaisyNightshade` | Daisy | the wizard watched power held too tight |
| 002 Prep (Daisy's experiment) | `PumpkinOffer` | Daisy | the wizard keeps glancing past her shoulder |
| 002 `Goal 1` Completion (Daisy returns) | `PumpkinReturn` | Daisy | the Pumpkin Heads keep turning toward the wizard; then her canonical referral to Ambrose |

Eligibility (all per receiving wizard): Shadow school + `WC-COMMONS-MAIN-002-SHADOW_Complete` + the three street flags,
and: 003A rows — 003A not complete; 001 rows — 003A complete, 001 not; 002 rows — 003A and 001 complete, 002 not.
Text is in `text/en-US.txt` (`[ShadowSchool]`), built into every locale's `ShadowSchool.lang` by the existing overlay.
No line mentions Umbral Sight, Morganthe, a pact, the Nightside or Krokotopia, and none says who took the Eye.
If the game's dialogue no longer holds the expected keys, the row logs one warning and the canonical dialogue is shown.

### Rewards (investigated; implemented narrowly)

`WC-OLDE-MAIN-003A` (250 XP) and `WC-HAUNTED-MAIN-001` (275 XP) roll one item picked at random among rows the wizard
meets (`ReqSchoolOfFocus`). The mod already loads `spiraldb/DropTables` after SpiralDB, so two override files
(`spiraldb/DropTables/droptables_WC-OLDE-MAIN-003A_Shadow.json`, `…WC-HAUNTED-MAIN-001_Shadow.json`) are copies of the
installed tables with **one appended row**: the mod's own `ShadowSchool-Ring-5` (`939277053`) and `ShadowSchool-Athame-5`
(`389938`), required `ReqSchoolOfFocus Shadow`. All original rows, XP, gold and flags are byte-for-byte the originals
(pinned by test); the picker re-rolls past rows a wizard does not meet, so the odds for the seven schools are unchanged.
Quest templates were not edited. **Caveats:** (1) the override is a snapshot of the installed table, so a game-revision
change to those two tables must be re-copied (the fixture-hash test fails when the installed table changes);
(2) tier-5 gear needs level 5, so a wizard below that keeps the item until they reach it; I did not compare the level of
the Death ring and athame this replaces; (3) dropped at quest completion only; not played in the client.

### Evidence

- **Tests:** new `ShadowCampaignDialogueTests.cs` (42 cases) and the two C2.1 cases that asserted "only the reunion goal
  is adapted" updated. Coverage: Shadow selection (all ten rows, exact keys, kept canonical objects and voices, unvoiced
  added lines with borrowed presentation); all seven original schools in the same state get the canonical dialogue and
  the shared template; quest-state matrix (too early, too late, not enrolled, each missing street, fresh wizard,
  quest finished); interleaved Shadow/Fire/Death recipients with distinct objects; templates snapshotted before and
  after; goals, requirements, results and flags preserved; localization keys, lengths, embargo words and Nightshade
  tone checks; reward rows (original rows identical, one Shadow-only row, per-school eligibility for all eight schools,
  guard against a vacuous comparison); fallbacks (changed, shorter and longer canonical dialogue, no provider, throwing
  or empty provider, other quest or goal). Fixtures: the three quests and two drop tables copied from the installed game,
  SHA-256 pinned and re-checked against `spiraldb-cache` when present.
- **Runs:** CoreLib suite **107/107** (was 65); installer Python tests **2/2**; Director **Release build** 0 errors
  (built to a scratch output folder because the running server holds `bin/Release`); whitespace: `git diff --check`
  reports only two pre-existing LF→CRLF notices and the new files have no trailing whitespace.

### NOT done / NOT TESTED

- The **running server was not restarted**, so it still serves the C2.1 build: the C2.2 dialogue, `ShadowSchool.lang`
  keys and reward overrides are not live, and the installed client resources do not yet contain the ten new strings
  (`install.py status` reports "current" for the older generation). To apply: restart the server, then with the game
  client **closed** run `python src/Imlight.Director/mods/ShadowSchool/client/install.py install`. Not done here because
  Rika is mid-playthrough and a restart interrupts her session. `Original *.shadowschool-backup` files were not touched.
- **Graphical client: NOT TESTED** (rendering, wrapping of ~150–215-character lines, silence on added lines inside voiced
  scenes, camera changes, the two-client check). Live reconnect: only simulated. Quest Helper arrow and the earlier client
  crash: untouched, no speculative fixes.
- Journal text, hints and objective strings are unchanged; the shared NPC `ResActorDialog` / cinematic paths are still not
  resolved (as in C2.1).

### Recommended Stage C2.3 slice

Ambrose's enchantment scene and the Nightside walk for `WC-COMMONS-MAIN-004` (dialogue only): adapt the quest-start
dialogue (`WizQst1731AF_00000000`–`_00000004`) so the Shadow wizard's response to the Pumpkin-vision spell is unusually
strong, add short Mortis and Dworgyn reactions, and stop before any persistence: no flag, no Umbral Sight. Reuse the
same `Adaptation` table, gate on `WC-HAUNTED-MAIN-002` complete, and first (with the client) accept C2.1/C2.2 on screen.

## Stage C2.3 — Nightside enchantment and story continuation — 2026-10-23 (Claude Sonnet 5.5)

**Scope kept:** dialogue only, on the C2.1 resolver. No goal, prerequisite, completion flag, XP, reward, teleport or quest
template of `WC-COMMONS-MAIN-004` changed; no Umbral Sight state, flag, effect or ability; no Ravenwood conquest, pact or
Morganthe. Rika CrypticTheatre's character, quests and database were not touched (the database was not opened).

### Phase 1 findings (C2.2 verification)

- **Working tree:** 72 changed paths on `quality-assurance`, all preserved. Nothing was staged, reverted or cleaned.
- **Reward audit (live server `/eval`):** the canonical Death ring (`4755`) and athame (`4714`) rewards both require
  **Magic Level 10**. C2.2's Shadow rows were level-5 items: `ShadowSchool-Ring-5` and `-Athame-5`. The Death ring's donor is
  `Ring-T1-006`, which is exactly `ShadowSchool-Ring-10` (same stat lookups 29/33). **Fixed:** the 003A Shadow row now grants
  `ShadowSchool-Ring-10` (`670841589`), the same stats and level as the canonical ring. The athame has no exact Shadow
  counterpart (Death `Athame-T1-027`, lookups 22/28, level 10; `ShadowSchool-Athame-10` is 32/38, stronger). The Shadow row stays
  `ShadowSchool-Athame-5` (9/13, level 5): weaker than canonical, never stronger. Decide later whether to add an exact match.
- **Resolver registration:** confirmed live; after the restart `ShadowMainlineDialogue.Adaptations` holds 15 rows (11 from C2.1/C2.2,
  4 new).
- **Stale client resources:** the C2.2 and C2.3 strings were stale (`install.py status` listed the stale locale files).

### Deployment (done, with the checks that allowed it)

No `WizardGraphicalClient` process, no player online (`game_players` empty), server error digest empty. So: Release build, server
restart (`pid 5220`, 0 errors), then `install.py install` with the client closed. Backups re-hashed and unchanged
(Root.wad `4E09B3DA…42C66`, EXE `544AB726…7CF7E`). `install.py status`: current. The installed Root.wad's
`Locale/en-US/ShadowSchool.lang` contains all 6 new keys and the C2.2 keys. **Real-client rendering: NOT TESTED** — the
client was not running.

### What was built (`ShadowMainlineDialogue.cs`, 4 rows; text in `text/en-US.txt`)

All eligible only when: Shadow + Shadow enrollment + three streets + 003A + 001 + 002 complete, and `WC-COMMONS-MAIN-004` not.
Canonical lines stay as the canonical objects (voice included); added lines are unvoiced and borrow a canonical line's presentation.

| Dialogue | Added key (`ShadowSchool_…`) | Idea |
|---|---|---|
| 004 `Prep` (after "Go now…") | `NightsideEnchantReaction`, `NightsideEnchantCaution` | the spell went in "like a key into a lock it already knew"; he does not know what it was, warns against forcing it |
| `Goal 1` Completion (Mortis) | `NightsideMortis` | the dead ignore him, but the wizard watches the dark between his roots as if it watched back; he does not know what that means |
| `Goal 3` Completion (Dworgyn) | `NightsideDworgyn` | the confused caretaker: shadows lean toward the wizard, "probably the dust" |
| `Goal 4` Completion (Ambrose) | `NightsideAfterimage`, `NightsideAfterimageHold` | the enchantment unravelled, yet the wizard still looks at something; Ambrose asks about an afterimage, says it should not linger, tells them to tell no one and not chase it |

The Nightside-entry (`Goal`) and Death School (`Goal 2`) waypoints have no dialogue in the game, so there is nothing to adapt.
The "afterimage" dialogue is tied to the Goal 4 completion, which the goal chain reaches only after Goals 1–3. The resolver
does not see goal state, so a QA command that completes Goal 4 directly would show it early.

### Evidence

- **Tests:** new `ShadowNightsideDialogueTests.cs` (+16 cases); fixture `questtemplates_WC-COMMONS-MAIN-004.json` (SHA-256 `e92a33f7…8535f`,
  re-checked against `spiraldb-cache`). Covers: Shadow selection and exact keys; kept canonical objects and voices; unvoiced added lines
  with borrowed presentation; all seven schools get the canonical objects and the shared template; each earlier stage, missing
  predecessor/street/enrollment, a fresh wizard and a finished quest get the canonical dialogue; interleaved recipients never leak;
  template snapshot and entry identity unchanged; the five goals, destinations, personas, the prerequisite, the drop table and the
  six `ResLearnSpell` ids preserved; no registry key or quest instance added by resolving (no Umbral Sight state); fallbacks
  (renamed, shorter and longer dialogue, other goal, no/throwing/empty provider, null wizard or goal); reconnect round-trip;
  locale keys, length, writer round-trip, embargo list and in-character checks. The C2.2 reward test now pins the ring at level 10.
- **Test infrastructure fix:** the four Shadow test classes each re-initialised the global `ConfigurationManager`, so xunit ran
  them in parallel and intermittently threw "concurrent update" (this reproduced as 1–11 failures on some runs). They now share
  `[Collection("SharedConfiguration")]`; 25 consecutive runs passed.
- **Runs:** CoreLib suite **128/128**; installer tests **2/2**; Director Release build 0 errors (also built into a scratch folder);
  `git diff --check` reports only the existing LF→CRLF notices.

### NOT done / NOT TESTED

- Graphical client: rendering, wrapping of the ~230–290-character lines, silence on the added lines inside voiced scenes, camera
  changes and the two-client check for C2.1, C2.2 and C2.3. No Shadow character is at these quests.
- Live reconnect (simulated only). Quest Helper arrow and the earlier client crash: untouched.
- Shadow wizards get no `ResLearnSpell` from this quest (the six canonical results are school-specific). Unchanged, flagged for later.

## Stage C3 handoff (Claude Opus 5.5) — superseded by the Stage C3 section below

1. **First, accept C2.1–C2.3 on screen** with a Shadow wizard who legitimately reaches the quests (Rika is mid-playthrough and untouched).
2. **Umbral Sight is not built.** Nothing sets a flag. The story hook is: Ambrose's `NightsideAfterimage`/`…Hold` lines at the 004
   Goal 4 completion (afterimage persists, "tell no one", "look into it together"). Choose the persistent state there (a quest
   registry flag written by a new quest or result that the Shadow-only branch grants), and add `NightsideAfterimage…`'s
   follow-up as the next quest rather than extending 004.
3. **Keep `WC-COMMONS-MAIN-005`** (Death School news, Krokotopia handoff, `WizQst1731B0`) as the next adaptation target; Ambrose
   already asks "tell me what you found down there".
4. Reuse `Adaptation`/`QuestDialogueResolver`; add fixtures and the shared `SharedConfiguration` collection to new test classes.
5. Open decisions: an exact level-10 Shadow athame for the 001 reward; whether Shadow wizards should get a school-specific spell
   from 004 (the other schools do).

## Stage C3 — Persistent Umbral Sight — 2026-10-23 (Claude Opus 5.5)

**Scope kept:** the first working Umbral Sight, perception only. `WC-COMMONS-MAIN-004` and `-005` keep their goals,
prerequisites, rewards, completion flags and templates (pinned by test). No spell, no effect on combat, no instructor
duels, no pact, no Morganthe. Rika CrypticTheatre (`wizards/257-A`) was only **read** (one RQL projection of five
registry flags): not online, no quest command, forcegrant, removal or reset; record last modified 02:53, before this session.

### Decisions

- **Origin (requirement 2).** Unlocked only at the *completion* of `WC-COMMONS-MAIN-004` played as a Shadow wizard, after
  the whole investigation. Not at the quest start (the enchantment scene only starts a trail), not for other schools.
- **Persistent state (3).** Two entries in the character's own quest registry (`ServerQuestBehavior.Registry`, saved
  with the wizard by `WizardCollection.UpdateCharacterQuestBehavior`), so no schema change and it follows the character
  through logout, reconnect, zone changes and restarts:
  - `ShadowSchool_UmbralSight` = `1` (unlocked by the walk) or `2` (staff reconciliation). Absent = locked. Any other
    value is **malformed**: the sight stays off, play never overwrites it, a warning is logged once; only
    `.umbral reconcile` may repair it.
  - `ShadowSchool_UmbralSight_Trail` = bits for this run: accepted through play (1), then `Goal` Nightside (2), `Goal 1`
    Mortis (4), `Goal 2` Death School (8), `Goal 3` Dworgyn (16), `Goal 4` Ambrose (32). Starting the quest again clears it.
  A goal is recorded only from play, only in order (every earlier bit present), only while the wizard is Shadow, enrolled,
  past the three streets, 003A, 001 and 002, and 004 is unfinished. The unlock needs all six bits **and** a play-sourced
  quest completion (or, at the next login, a complete trail plus the quest flag: recovery if the server stopped between
  the two writes). It is idempotent: an unlocked wizard is never written again.
- **Not dependent on a dialogue packet.** The unlock happens in `QuestService.CompleteQuest`, server-side, after the
  quest's own results. Persona goals still complete through the game's own dialog-close path, as before.
- **QA shortcut edge case (C2.3).** `QuestService` now knows when a quest command is running (`_commandDepth`, set
  around every `MSG_QUESTCOMMAND`: `.quest completegoal / complete / completeall / toquest / grant / forcegrant`, which
  `.maxall` and `.toquest` also go through). Those steps are reported as `Command` and never extend a trail.
  The `Goal 4` afterimage lines (`NightsideReturn`) now also require the trail through Dworgyn, so a command that
  completes Goal 4 directly shows the **canonical** return. Prerequisite quests are checked by their flags only (they
  may have been set by `toquest`); the Nightside itself must be played.
- **Reward gap (7): a noncombat character ability, not a spell.** The canonical quest's six `ResLearnSpell` results are
  school combat spells; a spellbook entry would put Umbral Sight in the deck/combat system, which the design forbids,
  and the client has no spell for "perceive bindings". So it is a per-character ability in the registry, delivered by
  the mod's own perception system. Shadow wizards get no canonical school spell from 004 (none of the six results
  matches Shadow; unchanged and tested).
- **Activation (4).** Ordinary players cannot use chat commands (`ChatService` requires `AuthLevel > None`), and the emote
  names a player could use are not known from evidence, so neither is the trigger. The mechanism uses only what the
  server already receives: **walking within a binding's radius** gives a one-time hint; **standing still there for
  2 s** (`[ShadowSchool] UmbralSightFocusSeconds`, 0.5–30) is the deliberate look, which reveals it. Moving 1.25× the
  radius away, or changing zone, lets it be noticed again.
- **Delivery and fallback.** Everything goes to the one wizard's own `SessionActor`: the hint as
  `MSG_CLIENTNOTIFYTEXT` (on-screen text, the type the game's `ResDisplayText` result uses), the perception as
  notify text plus a `MSG_SERVERMESSAGE` chat-log line, which is the readable fallback if the notify text does not
  render. No zone broadcast, effect, object or asset; no client executable patch. A custom visual overlay was **not**
  attempted: nothing in the client is known to support one, so the text path is the safe fallback by design.

### What was built

- **Core hooks (generic, mod-agnostic):**
  - `src/Imlight.CoreLib/Game/Quests/QuestProgressEvents.cs` — `IQuestProgressObserver` (`OnQuestStarted`,
    `OnGoalCompleted`, `OnQuestCompleted`) with `QuestProgressSource` = `Play`, `Shared`, `Command`, `CatchUp`.
    A throwing observer is logged and skipped.
  - `src/Imlight.CoreLib/Game/World/PlayerPresenceEvents.cs` — `IPlayerPresenceObserver` (`OnAttached`, `OnMoved`,
    `OnStill`, `OnDetached`), fed by `MoveService` (attach, each client move, the 250 ms interaction tick, dispose).
  - `QuestService.cs`: `_commandDepth` around quest commands; reports goal completion (after it is saved), quest
    completion (after its end results and spell rewards) and quest start. No goal, result or reward logic changed.
  - `MoveService.cs`: `_stillSince` and the four presence calls; `_wizard` is refreshed at attach (a session can return
    to character select and pick another character, which the old cache would not notice).
- **Mod:** `src/Imlight.CoreLib/Mods/ShadowSchool/`
  - `UmbralSight.cs` — state, trail, unlock, login recovery, staff reconciliation, `Describe` for staff.
  - `UmbralPerception.cs` — five bindings and the per-wizard watch (hint, focus, re-arm, zone change, detach).
  - `UmbralSightCommands.cs` — `.umbral status` / `.umbral look` (QA, read-only) and `.umbral reconcile`
    (Administrator, own logged-in character only).
  - `ShadowSchoolMod.cs` implements both observers (an internal constructor takes the save action, for tests).
  - `ShadowMainlineDialogue.cs` — `NightsideReturn` gated on the trail; new `UmbralSightAmbrose` adaptation.
- **Bindings** (anchors are placements read from the installed zone data `gamedata.bin` on the live server):

  | Id | Kind | Zone | Anchor | Radius |
  |---|---|---|---|---|
  | `RavenwoodRoots` | binding | `WizardCity/WC_Ravenwood` | Bartleby (`WC-RAV-NPC07`) 45, 1420 | 700 |
  | `AmbroseWards` | binding | `WizardCity/Interiors/WC_Headmistress_House` | Ambrose (`WC-HUB-NPC01`) −377, 280 | 400 |
  | `NightsideThread` | residue | `WizardCity/WC_NightSide` | Mortis (`WC-RVW-DeathTree`) −776, −1197 | 600 |
  | `DeathSchoolCut` | residue | `WizardCity/Interiors/WC_SchoolDeath` | Dworgyn (`WC-NIS-NPC01`) 749, 8 | 500 |
  | `UnlistedAisle` | anchor | `WizardCity/Interiors/WC_LibraryBookStacks` | Vespera (mod placement) 6400, −1104 | 400 |

  Each text says only that something is bound and what kind of thing: never who bound it, what it guards, or how to
  break it; the cut binding in the Death School names no one.
- **Story (6):** Ambrose's existing C2.3 afterimage lines at the return (now only after the walk); at the start of
  `WC-COMMONS-MAIN-005` (Prep), two unvoiced Ambrose lines before his canonical briefing, only once unlocked — the
  sight has lasted, it is a responsibility before a gift, tell only Vespera, learn what it is rather than what it can
  do. Then the mod quest **`ShadowSchool-UmbralSight-Lesson`** ("What the Eye Keeps"): offered by Vespera when the
  wizard is Shadow, has `ShadowSchool_UmbralSight` and `WC-COMMONS-MAIN-004_Complete`; one persona goal at Vespera,
  three unvoiced lines (what Umbral Sight is; seeing is not understanding and understanding is not holding; how to
  practise and to leave what belongs to others as found); no reward, no result. Ambrose stays protective; the wizard
  has not joined Morganthe; no pact is mentioned.
- **Text:** 17 keys in `text/en-US.txt` under `[ShadowSchool]` (`UmbralSightAmbrose*`, `UmbralLesson*`, `Umbral_*`).
  The server reads the `Umbral_*` English text itself for what it sends.

### Migration / reconciliation

Audit (read-only RQL over `Wizards` where school = Shadow): four Shadow characters (`wizards/161-A`, `225-A`, `193-A`,
`257-A`), **none** has `WC-COMMONS-MAIN-004_Complete`, an Umbral state or a trail, so nobody needs reconciling today.
For a character who completed the Nightside before C3 (no trail can be reconstructed, and a flag alone could be a QA
shortcut), an administrator who has checked its history logs in on it and runs `.umbral reconcile`: refused unless the
character is Shadow, has the enrollment, the three streets, 003A, 001, 002 and 004 complete; writes `2`; never twice.

### Evidence

- **Tests:** new `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/UmbralSightTests.cs` (38 cases, `SharedConfiguration`
  collection); fixture `Fixtures/questtemplates_WC-COMMONS-MAIN-005.json` (SHA-256 `e7a83003…5d0e36b`, equal to
  `spiraldb-cache`). The unlock is driven through `QuestProgressEvents` to the real `ShadowSchoolMod` (a recorder in place
  of the database save) over the installed quest, walking the goals in the goal logic's order. Covered: a new Shadow
  wizard cannot perceive; before completion (even with every goal walked) it is locked; the full walk unlocks at the end;
  each of the seven original schools never gets any Umbral entry (only the quest's own `_Complete` is new) and a copied
  entry is not honoured; eight shortcuts (command goal, command start, catch-up, out of order, skipped goal, command
  completion, not enrolled, missing predecessor) never unlock, also not at the next login; granted once (no second write,
  registry identical); persistence through a JSON reload, twice; a restart of the quest clears an earlier trail; an
  interrupted unlock is recovered at login; malformed state values (0, 3, max) fail safe and are only repaired by staff;
  malformed trails; staff reconciliation rules; the afterimage only after a walked investigation (the C2.3 edge case and a
  mixed command/play run get the canonical return); Ambrose's 005 lines only once unlocked (canonical for locked Shadow,
  all seven schools, and after 005); across all 935+ `spiraldb-cache` quests an unlocked Shadow wizard sees only 005
  change and a Fire wizard none; perception hint → focus → no repeat → re-arm; isolation between an unlocked, a locked
  Shadow, a Fire and an other-zone wizard, and between two seers; zone changes, a reconnect (detach + reload) and a stale
  watch; an unlock while in a zone works without relogging; every binding's text present, short, and free of
  break/control/command/unlock/defeat/Morganthe/pact/Malistaire/Krokotopia; Vespera's and Ambrose's lines in character;
  the lesson quest's requirements, no rewards, Vespera as the offerer, every line unvoiced and localized; a throwing
  observer does not stop the unlock or the quest. The C2.3 tests now give a wizard at the return a walked trail (the
  premise they described), and the C2.2 embargo test leaves the C3 adaptation to the C3 tests.
- **Runs:** CoreLib suite **166/166** (was 128); installer tests **2/2**; Director **Release build** 0 errors (through
  the server restart; also a scratch `checkOnly` build); no trailing whitespace in the touched files.
- **Live server (in memory only, after the restart, pid 23072):** `ShadowSchoolMod` is the enabled dialogue provider,
  progress observer and presence observer; 16 adaptations; 1,089 quest templates (the lesson loaded); the server reads
  the `Umbral_*` text. Synthetic **unsaved** wizards through the live mod and the live templates: Shadow — lesson not
  offerable before, then the walk unlocks it (`state Unlocked (1)`, full trail), the lesson's requirements pass, and
  `WC-COMMONS-MAIN-005` Prep resolves to the two new lines + the five canonical ones; Fire — no Umbral state, lesson not
  offerable, canonical 005. No database write, no player, no quest command. Error/warning digest since restart: empty.
- **Deployment:** with no player online and no `WizardGraphicalClient` process: Release build + server restart, then
  `install.py install`. `install.py status`: current (7 locale files were stale before). The installed Root.wad's
  `Locale/en-US/ShadowSchool.lang` holds the new keys. Backups re-hashed before and after, unchanged:
  `WizardGraphicalClient.exe.shadowschool-backup` `544ab726…7cf7e`, `Root.wad.shadowschool-backup` `4e09b3da…42c66`.

### NOT TESTED / not done

- **Graphical client: NOT TESTED.** Not observed on screen: whether `MSG_CLIENTNOTIFYTEXT` shows the hint/notice (the
  chat line is the fallback either way), the chat lines' wrapping, the anchors' radii as they feel in play, Ambrose's two
  added 005 lines inside his voiced scene, Vespera's lesson offer/turn-in, and two clients side by side. No Shadow
  character has legitimately reached the Nightside, and no progression was manufactured. C2.1–C2.3 dialogue is also
  still not visually verified.
- Live reconnect and a real `QuestService` run of the quest (actor level) were not exercised; the unlock path was
  driven through the same event dispatcher the service calls, and the service wiring is covered by the build only.
- **Athame parity (8): documented as a separate balance task.** A level-10 Shadow athame equal to the Death reward
  (`Athame-T1-027`, lookups 22/28) needs a new, 81st item template (new Root.wad file, locale name, client reinstall)
  and a change to the 80-item catalog contract; not narrow, so not done. The 001 Shadow row stays
  `ShadowSchool-Athame-5` (weaker, never stronger). The ring already matches.
- Quest Helper arrow and the historical client crash: untouched.

### Files (C3)

New: `src/Imlight.CoreLib/Game/Quests/QuestProgressEvents.cs`, `src/Imlight.CoreLib/Game/World/PlayerPresenceEvents.cs`,
`src/Imlight.CoreLib/Mods/ShadowSchool/{UmbralSight,UmbralPerception,UmbralSightCommands}.cs`,
`src/Imlight.Director/mods/ShadowSchool/spiraldb/QuestTemplates/questtemplates_ShadowSchool-UmbralSight-Lesson.json`,
`test/Imlight.CoreLib.Tests/Mods/ShadowSchool/UmbralSightTests.cs`,
`test/Imlight.CoreLib.Tests/Mods/ShadowSchool/Fixtures/questtemplates_WC-COMMONS-MAIN-005.json`.
Modified: `src/Imlight.CoreLib/Game/Services/{QuestService,MoveService}.cs`,
`src/Imlight.CoreLib/Mods/ShadowSchool/{ShadowSchoolMod,ShadowMainlineDialogue}.cs`,
`src/Imlight.Director/mods/ShadowSchool/text/en-US.txt`,
`test/Imlight.CoreLib.Tests/Mods/ShadowSchool/{ShadowNightsideDialogueTests,ShadowCampaignDialogueTests}.cs`,
`docs/docs/modules/imlight/commands.md` (`.umbral`), this file and `quest-dependency-map.md`. Nothing was staged or
committed; no unrelated file was reverted.

## Stage D handoff — the Ravenwood instructor encounters (superseded by Stage D1 below)

1. **First, client acceptance** of C2.1–C3 with a Shadow wizard who legitimately reaches them (see NOT TESTED). For C3:
   finish the Nightside, check `.umbral status` (QA account), the Ambrose 005 opener, Vespera's lesson, then stand at
   Bartleby's face for 2 s and confirm the hint, notice and chat line. If notify text does not render, keep the chat
   line and consider dropping the notify call; do not patch the executable for it.
2. **Build on, do not rewrite:**
   - Gate anything Umbral on `UmbralSight.CanPerceive(wizard)` (Shadow + valid unlock), never on the raw registry value.
   - New perceivable things are rows in `UmbralPerception.Bindings`. For faculty bindings, add a binding per instructor
     (anchor = the instructor's placement in their school interior; read it from `gamedata.bin` as C3 did) with a new
     `Kind` if needed. Keep the text perception-only until the stage that allows more.
   - New capability tiers (identifying, then challenging a binding) should be new state values or a separate registry
     key (`ShadowSchool_UmbralSight_Tier`), recorded the same way: per character, from `QuestProgressSource.Play` only,
     idempotent, with a staff reconciliation path. Keep value `1`/`2` meaning "perception unlocked".
   - Story steps: new `ShadowMainlineDialogue` adaptations or mod quests gated by `ReqHasEntry` on the registry key
     (as the lesson is). `IQuestProgressObserver` tells the mod about any quest's steps and how they happened.
   - Presence-driven behaviour (e.g. an instructor's binding reacting to the wizard looking at it) goes through
     `IPlayerPresenceObserver`, to that wizard's session only.
3. **Instructor duels** are combat: keep Umbral Sight out of the spell/deck system. A challenge could be a mod quest
   whose goal is a duel (existing duel circle / combat goal paths), unlocked by a tier recorded as above.
4. **Open decisions:** exact Shadow athame parity (separate balance task above); whether `WC-COMMONS-MAIN-005`'s other
   speakers (Bartleby, Krokotopia) should react to the sight; localization beyond English (all mod text is English in
   every locale, as before).

## Stage D1 — Ravenwood conquest framework + first encounter (Dalia Falmea) — 2026-10-23 (Claude Opus 5.5)

**Scope kept:** one reusable encounter framework and exactly one encounter. No other instructor, no Ambrose finale, no
pact, no Morganthe, no Krokotopia change, no binding control. Umbral Sight stays perception: the anchor is an ordinary
`UmbralPerception` binding; the challenge comes from the quest and the server's own battle. Rika CrypticTheatre
(`wizards/257-A`) was only **read** (one RQL projection after deployment: no conquest or sight entry, last modified
02:53, before this session). No QA character was created or saved; every rule was exercised on unsaved wizards.

### Verified facts (installed resources, live server, read-only)

- **Order:** plan §7.3 stage 1 is **Dalia Falmea** (Fire): "talent without discipline burns others", aggressive Fire
  damage over time and tempo. She is `WC-RAV-NPC04`, template **38208** (trainer: `WizTrainingBehavior`, quests,
  services), persona `WC-RAV-NPC04_Persona`, portrait `Art_Portrait_Dalia_Falmea.dds`, in
  `WizardCity/Interiors/WC_SchoolFire` at (787, 9.45, 99.75) facing 1.578 (that zone's `gamedata.bin`). The zone's
  `Start` is (−134.3, −23.0, 0.5), 921 units from her. Ravenwood's `Target location (Ravenwood FireSchool Exit)` is
  (1616.2, 5983.3, 0).
- **Combat representation:** the game ships two Falmea combat templates. **403089 `DaliaFalmea-WCBoss-R7`**
  (Falmea's own model `AV_DaliaFalmea/WC_DaliaFalmea_01.nif`, boss, level 7, 4,000 health, no base effects, aggro
  800, one unique adjective; its school and deck are Cyrus Drake's Myth set, evidently reused data) — **used**.
  1475611 (`CE_TreasureTower_Floor_30_Fire`) carries Treasure Tower adjectives/triggers and +25% pierce, +150 flat
  damage, block 139, 80% power pips — **not used**.
- **Deck spells** (base templates, checked live): Fire Cat 103007158 (rank 1), Fire Elf 1243857701 (rank 2, damage
  over time), Sunbird 1599065470 (rank 3), Fire Trap 400321766, Fireblade 603748810.
- **Battle circle:** the Wizard City boss rooms (Nightshade's `WC_HauntedCave_T1`, Foulgaze's `WC_OldeTown_T2`) use
  template **560 "Duel Circle"**, `CombatSigil8Actor`, radius 700. The Fire School has no circle.

### Design (what was built)

- **Core hooks (generic):**
  - `Game/Combat/BattleOutcomeEvents.cs`: `IBattleOutcomeObserver` with `BattleOutcome` (Won / Lost / Fled / Left,
    wizard, session, zone path, **variant**, is-instance, creatures with their custom ids). Reported by
    `CombatDuelComponent` per wizard from `PlayerWin` (custom marks read before they are forgotten), `CreatureWin` and
    `HandleFleeAction` (flee = Fled, disconnect or logout = Left). PvP is not reported; a throwing observer is skipped.
  - `ZoneObjects.Applied.Variant`; `ZoneVariants.Of(charId)` (read-only query).
  - `Customs.FileOf`: an enabled mod's `spiraldb/Customs/<id>.json` (a number range of the mod's own; Shadow School =
    1880001 and up) wins over spiraldb-extra's. `BossOverrides.Parse` split out of `For` (same behaviour), so a profile
    can be tested.
- **Mod:** `Mods/ShadowSchool/ConquestEncounters.cs` (the rules), `ConquestWorld.cs` (the live server side),
  `ConquestCommands.cs` (`.conquest status`, QA, read-only), `ShadowSchoolMod.cs` (wires the quest, presence and
  battle observers), `UmbralPerception.cs` (binding `FireHearth`, radius 350 at Falmea).
- **An encounter is one `ConquestEncounter` row** (id, order, instructor and canonical template, quest, anchor binding,
  zone, variant, creature template, custom id, arrival, exit) plus three data files: the quest, the ZoneObjects variant
  file and the custom profile. State: registry `ShadowSchool_Conquest_<Id>` = 1 anchor found, 2 defeated, 3 complete;
  any other value is malformed and never overwritten. Written only from `QuestProgressSource.Play` (never Command,
  CatchUp or **Shared**, a helper's goal), only in order, never twice. Each encounter needs the one before it complete
  (`Order`), plus Shadow + working Umbral Sight (`CanPerceive`, so a copied or malformed entry fails) +
  `WC-COMMONS-MAIN-004` + Vespera's lesson.
- **Falmea's flow** (quest `ShadowSchool-Conquest-Falmea`, "The Hearth Binding", level 10, offered by Vespera, no
  reward of any kind):
  1. Prep (Vespera, 2 lines): Ambrose's terms refused; the wards are bound to the teachers; "you go by your own choice.
     I am not sending you".
  2. Goal **Anchor** (waypoint; only the generic event `ShadowSchool_Conquest_Falmea_AnchorFound` completes it, no
     zone or proximity tag can): in the Fire School, step to Falmea and stand still `FocusDelay` (2 s) within 350.
     Completion dialogue, Falmea (2 lines): she felt it, she spoke for you after Firecat Alley; look again and the
     binding takes you both where no one else can be hurt, holding back most of her power.
  3. Goal **Duel** (bounty "Defeat Dalia Falmea": `WizardQuestGoals_Kill` + `WizardMobs_00000845`; its adjective
     `ShadowSchool-Conquest-Falmea` matches no creature, so only the event `..._Defeated` completes it). A step and a
     second look at the anchor (not in a battle or the combat grace) opens a **session**: notify and chat text, then a
     `MSG_ZONETRANSFER` into a **fresh private copy** of `WC_SchoolFire` owned by the wizard, variant
     `ShadowConquest-Falmea` (noted in `ZoneVariants` as dungeon sigils do). The copy's file
     (`spiraldb/ZoneObjects/WizardCity-Interiors-WC_SchoolFire@ShadowConquest-Falmea.json`) leaves out the trainer
     (Remove 38208, in this copy only), spawns 403089 at her spot as custom **1880001** and adds a circle 560 at
     (880, 0, 100); its Exit (doors, loss, flee) is Ravenwood outside the Fire School. Custom 1880001
     (`spiraldb/Customs/1880001.json`): Fire, level 10, **950 health**, all damage/resist/pierce/critical/block rows 0,
     no power pips, 0 starting pips, deck Fire Cat + Fire Elf (unlimited), Sunbird / Fire Trap / Fireblade twice each,
     no drops, XP 0, gold 0 (nothing to farm). Falmea's aggro (800) starts the battle as the wizard walks up to her.
  4. A **win** in that copy, by the session's owner, over custom 1880001: the event completes the duel goal
     (dialogue: Falmea yields, "you won the ward, not the argument", her classroom is still open), a chat line, and
     after `[ShadowSchool] ConquestReturnSeconds` (6 s) the wizard is sent to Ravenwood. A **loss or flee**: a chat
     line; the game's defeat goes to the copy's Exit (Ravenwood); the goal stays open; another look opens a new copy.
  5. Goal **Return** (Vespera persona, 2 lines: the binding knows you now, that is all you have taken; Falmea teaches as
     before; six more bindings and Ambrose behind them). Quest completion records stage 3.
- **Safety:** the hostile Falmea exists only in one wizard's private copy; the public Fire School, trainer 38208, her
  quests and every other player are untouched (no mod file for the public zone; tested and checked on the live
  server). A battle in the copy counts only for the wizard whose session is `InCopy` there; a win in a public zone, a
  non-instance, another variant, without the session, or over another creature counts for nobody. Attaches: the
  transfer's arrival turns `Entering` into `InCopy`; a reconnect or restart that lands in a copy with no open session
  (or an `Entering` older than 60 s) is sent straight to the exit; any other attach closes the session; a detach
  closes it. At attach, stage 2 + quest complete (server stopped between the two writes) becomes stage 3.

### Evidence

- **Tests:** `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ConquestEncounterTests.cs`, **44 cases**, through the real
  `ShadowSchoolMod` with a recording `IConquestWorld` (server-side rule simulation, not a battle): eligibility and each
  prerequisite; all seven original schools (even with copied entries and a granted quest) get nothing; the quest
  contract (Shadow only, Vespera, no rewards, goal order, goal ids = `StringHash`, event-only goals, unvoiced
  in-character text); the anchor is a perceived binding at Falmea (a Fire wizard perceives nothing); the look finds the
  anchor once; without the quest it is perception only; the second look opens the copy (once; not inside the copy;
  not in a duel or the grace; not after a command-completed anchor); the room file (variant, Remove 38208, 403089 at
  her spot as 1880001, circle 560 within reach, Ravenwood exit); the custom profile; a win completes once and returns
  the wizard; a loss or flee leaves it open and a retry works; five no-credit cases; repeated wins and steps write
  nothing; Command, CatchUp and Shared never advance; persistence through a reload and recovery after a restart; a
  reconnect or restart into a copy is sent out; a transfer that never arrived; two Shadow wizards stay isolated (no
  cross-credit; a copied entry does not complete); a malformed stage is left alone; restarting the quest resets an
  unfinished run; the public Fire School and the trainer are untouched; a throwing battle observer. `UmbralSightTests`'
  binding text test now covers `FireHearth` too.
- **Runs:** CoreLib **210/210** (was 166); installer tests **2/2**; `git diff --check` clean (the new, untracked files
  were checked for trailing whitespace too); **Release build** 0 errors (through `server_restart`, pid 2020).
- **Live server, in memory (no player, no database write):** the mod is the enabled battle outcome observer; the quest
  is loaded (1,090 templates); custom 1880001 is read from the mod folder, its deck resolves (5 spells; unlimited =
  9999) and, laid over a creature's stats, gives Fire / 10 / 950 while the original stays at 4,000; `ZoneObjects`
  gives the variant copy one file and the public copy none; `CustomFor(403089)` is 1880001 in the copy and 0 in
  public; the copy's defeat exit is Ravenwood (1616, 5983); **the real Fire School zone data with the variant
  applied** has no 38208, the circle (560, `CombatSigil8Actor`, 700) at (880, 0, 100) and the spawner of 403089 at
  (787, 9.45, 99.75), while the same data for the public copy keeps 38208 and gains nothing. Warning/error digest since
  the restart: empty.
- **Deployment:** no player online, no `WizardGraphicalClient` process. `install.py status` listed 7 stale locale files;
  `install.py install`; status **current**; the generated `ShadowSchool.lang` holds the new keys. Backups re-hashed
  before and after, unchanged: EXE `544ab726…7cf7e`, Root.wad `4e09b3da…42c66`.

### NOT TESTED / known limitations

- **No battle was fought.** The *battle template* (custom profile, deck, zone file) is verified on the live server; the
  *server-simulated encounter* (every framework rule) is unit-tested; the *actual battle* (the spawn in a loaded
  private copy, aggro onto the added circle, the AI using the Fire deck, the win and loss reports, the return transfer)
  was **not exercised**: the agent combat bridge needs a logged-in player and none was available. Also unchecked:
  whether the circle at (880, 0, 100) sits on the platform (Z taken from nearby placements, not from collision), the
  transfer from the public Fire School into the private copy of the same zone path, solo turn counts and difficulty at
  level 10–12, and everything in the graphical client (dialogue, the bounty goal text, notify text, the transfer).
- **Solo only.** The copy is private to its owner; helpers cannot join (and a Shared goal never credits anyone).
  Co-op (plan §7.4) is a D2 decision.
- **Falmea's damage is not scaled down** by a negative damage row (how a negative `DamageBySchool` behaves in the
  damage formula was not verified); the restraint is the level-10 profile itself.
- The lesson quest's goal `m_goalNameID` (572984775) is the hash of "Goal 1", not of its own name (C3 data, left as it
  is; the D1 quest's ids are correct).
- The Quest Helper arrow and the historical client crash: untouched. C2 and C3 client acceptance is still open.

### Files (D1)

New: `src/Imlight.CoreLib/Game/Combat/BattleOutcomeEvents.cs`,
`src/Imlight.CoreLib/Mods/ShadowSchool/{ConquestEncounters,ConquestWorld,ConquestCommands}.cs`,
`src/Imlight.Director/mods/ShadowSchool/spiraldb/QuestTemplates/questtemplates_ShadowSchool-Conquest-Falmea.json`,
`.../spiraldb/ZoneObjects/WizardCity-Interiors-WC_SchoolFire@ShadowConquest-Falmea.json`,
`.../spiraldb/Customs/1880001.json`, `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ConquestEncounterTests.cs`.
Modified: `src/Imlight.CoreLib/Game/Zone/Components/CombatDuelComponent.cs`, `Game/Zone/Core/ZoneObjects.cs`,
`Game/World/ZoneVariants.cs`, `Game/Combat/{Customs,BossOverrides}.cs`,
`Mods/ShadowSchool/{ShadowSchoolMod,UmbralPerception}.cs`, the mod's `text/en-US.txt` (16 keys), this file,
`quest-dependency-map.md`, `docs/docs/modules/imlight/commands.md` (`.conquest`). Nothing was staged or committed.

## Stage D1.5 — Falmea acceptance: the private-copy handoff and the quest's navigation — 2026-10-23 (DeepSeek V4.1 Flash)

**Scope kept:** the D1 framework was not rebuilt. Two defects were fixed in existing D1 code and data, three regression
tests were added, and the Stage D2 contract was written down. No other instructor, no Ambrose, no pact, no co-op, no
canonical quest, NPC or reward changed. **Rika CrypticTheatre (`wizards/257-A`) was not touched**: the database was not
opened, no player was logged in and no client was started. The working tree (`quality-assurance`) was only read and edited;
nothing was staged, reverted, cleaned or committed.

### Confirmed defect 1 (fixed): the private copy was never loaded

`ConquestWorld.Enter` sent its transfer to `WizardCity/Interiors/WC_SchoolFire`, **the zone the wizard is already in**: the
anchor is Falmea herself, inside the school. `ZoneService.cs:273` short-circuits exactly that case — `!message.ForceReload &&
DestinationZone == wizard.Zone` calls `DoTeleport` and returns **before** `ReadyClientForZoneTransfer`, so the client never
reloads, never re-attaches and the server never loads the owner's variant copy. The wizard would have been teleported a few
steps inside the **public** Fire School: trainer 38208 still standing, no battle circle, no hostile Falmea — and because the
copy never arrived, the session would have stayed `Entering`, the variant note would never be satisfied by an attach, and no
battle could ever have been credited. `IsPrivate`, `OwnerCharId`, `ResetInstance` and `Variant` were all correct and all
irrelevant without the reload.

**Fix:** `ForceReload = true` in that transfer (`ConquestWorld.cs`), with the reason in a comment. The other flags keep their
jobs: `ResetInstance` drops the owner's stale copy, `IsPrivate` creates or reuses their instance container, `Variant` is what
the fresh copy loads with, and `ZoneVariants` is still set so the attach after the reload lands in that copy. `Leave` is
unchanged (a real zone change, no reload).

### Confirmed defect 2 (fixed): the quest had no client navigation

The quest's goals had no `m_locationName`, and the return goal still carried the `Muldoon` client tag copied from the
check-in templates. That is the same class of defect as the earlier Shadow enrollment's "unknown world" / "unknown zone":
`InteractQuestOfferComponent` builds the journal and offer entries from the goal's location name and destination zone.

**Fix:** anchor and duel goals now name `ShadowSchool_ConquestFalmeaLocation` ("Wizard City|Fire School") with destination
`WizardCity/Interiors/WC_SchoolFire`; the return goal names `ShadowSchool_EnrollmentLocation` ("Wizard City|Library Book
Stacks") with destination `WizardCity/Interiors/WC_LibraryBookStacks` and no client tags. The new key is in `text/en-US.txt`.
Goal ids, events, requirements, dialogue, reward-free completion and the goal order are unchanged.

### Verified in code (the profile really is the duel's)

`CombatDuelSubCircle.cs:1611` builds a creature's battle profile with `BossOverrides.For(Customs.KeyOf(ParticipantObject))`;
`KeyOf` returns the custom's key for a creature its spawner marked, and `BossOverrides.For` on a custom key reads
`Customs.FileOf` → the enabled mod's own `spiraldb/Customs/<id>.json`. So Falmea's Fire / level 10 / 950 profile, her spell
deck, `XP 0`, `GoldMin/GoldMax 0` and no drops are the profile the duel applies — not merely data sitting in a loaded file.
The base template 403089 stays Myth / 4,000 / its own deck in the public zone and in any other fight.

### Evidence

- **Tests:** `ConquestEncounterTests` **47** cases (was 44; CoreLib **213/213**, was 210). The three new cases:
  - `TheEncounterReloadsIntoAFreshPrivateCopyInsteadOfMovingInsideThePublicSchool` — drives the real `ConquestWorld`
    against a recording session and pins the whole handoff: `DestinationZone` = the Fire School with `ForceReload`,
    `IsPrivate`, `ResetInstance`, `OwnerCharId`, `Variant` and the `ZoneVariants` note, then `Leave` to Ravenwood with no
    reload and the note forgotten. (A same-zone transfer without the flag is the defect above; the test fails without it.)
  - `TheDuelFindsFalmeasProfileThroughTheCustomTheSpawnerMarks` — a `CoreObject` marked as custom 1880001, `Customs.KeyOf`
    → `Customs.Key(1880001)` → `BossOverrides.Parse` of the mod's file gives template id 1880001, base 403089, Fire, 10,
    950, XP 0, gold 0, no drops and the five Fire spells. (`BaseOf` itself needs an enabled mod's file lookup and is not
    asserted in the isolated test server; the file is read directly.)
  - `EveryGoalCarriesTheClientNavigationMetadata` — every goal has a location, the two school goals point at the Fire
    School, the return at the Library Book Stacks, the keys resolve in the mod text, and the return goal has no Muldoon tag.
- **Runs:** CoreLib **213/213**; installer tests **2/2**; CoreLib and Director Release builds 0 errors; `git diff --check`
  clean (only the existing LF→CRLF notices).
- **Test-infrastructure fixes:** the recording actor's constructor is `public` (Akka creates actors reflectively), and the
  earlier assertion that went through `Customs.BaseOf` was replaced by the key-chain assertions above.
- **Deployed and checked on the live server (read-only, no player, no client):** Director Release build copied the mod into
  `bin/Release/net10.0/mods/ShadowSchool`; the server was restarted (16:43, `pid 22688`, 0 errors and 0 warnings in the log,
  both game and login ports listening, 0 players). In memory through `/eval`: the quest has 3 goals, anchor and duel at
  `ShadowSchool_ConquestFalmeaLocation` / `WizardCity/Interiors/WC_SchoolFire`, the return at
  `ShadowSchool_EnrollmentLocation` / `WizardCity/Interiors/WC_LibraryBookStacks` with 0 client tags, and 0 end results;
  `Customs.BaseOf(Customs.Key(1880001))` is **403089** (the mod's file resolves on the live server, unlike the isolated test
  server), and `BossOverrides.For` on that key is Fire / 10 / 950 / XP 0 / gold 0 / 5 spells — the profile the duel reads.
  `install.py status` reported UPDATE AVAILABLE (7 locale files); `install.py install` with the client closed rewrote
  Root.wad (113 files), kept the original backups and now reports `current`.
- **Rika CrypticTheatre was not read or written** this session: no character record was opened, no quest state was touched,
  and no save was performed.

### NOT TESTED (unchanged acceptance gaps)

- **No battle was fought and no client was used.** Unverified on screen: the reload actually landing in the private
  variant copy, the trainer's absence there, the circle's and the custom Falmea's presence, aggro onto the circle, the AI
  casting the Fire deck, the win/loss/flee reports, the return transfer, the journal/objective text and map marker, the
  notify/chat lines, and the dialogue. The reconnect-out-of-a-copy path is unit-tested, not live-tested.
- The circle's Z (0 at 880,100) is taken from nearby placements, not from collision: unverified on the platform.
- Solo only; difficulty at level 10–12 is untuned by an actual fight. Falmea's damage is not scaled down by a negative row.

## Stage D1.6 — runtime acceptance attempt: what is proven, what is not — 2026-10-23 (DeepSeek V4.1 Flash)

**Environment.** The Director was already running the D1.5 build (`pid 22688`, started 16:43, 0 errors/0 warnings,
0 players). `WizardGraphicalClient` was **not** running. No credentials were entered, no login was attempted and the client
was not started, so the graphical-client walkthrough could not be performed: every visual/client criterion below is
**NOT TESTED**. No disposable QA wizard was created (that needs a client); **Rika CrypticTheatre was not read or written** —
the database was not opened and no character record was loaded or saved. No production quest requirement was weakened and
no synthetic eligibility state was written.

### Private-copy data through the real load path (live server, read-only)

Loaded the Fire School's own `gamedata.bin` with `ZoneLoader` and applied `ZoneObjects.AddTo` exactly as `Zone` does, for
both copies:

| | objects | trainer 38208 | circle 560 | custom of 403089 | spawner of 403089 | exit override |
|---|---|---|---|---|---|---|
| public copy | 15 | **1** | none | 0 | none | none |
| `ShadowConquest-Falmea` | 15 | **0** | (880, 0, 100), radius 700 | **1880001** | (787, 9.45, 99.75) | Ravenwood |

So the private copy removes the trainer, adds the battle circle, spawns the game's combat Falmea in her place as the mod's
custom and sends every way out to Ravenwood — while the public copy keeps its trainer and gains nothing. `Customs.BaseOf` for
1880001 is 403089 and `BossOverrides.For` on it is Fire / 10 / 950 / XP 0 / gold 0 / 5 spells (D1.5 evidence, re-checked).

### The battle circle's height (the "Z=0" question)

The placement format is **(X, Y, Z) with Z up**, established from the server's own data: the Ravenwood exit is
`1616.214,5983.343,0.0001,0.3877` (X, Y, height, yaw) and the Fire School's `Start` is Z **0.5**. Falmea stands at
(787, 9.45, **99.75**); the circle is at (880, 0, **100**) — horizontal distance 93.5, height 0.25 above her feet, the same
level as the zone's other platform-level objects at Z 100. The file's `Y: 0` is a horizontal coordinate, not a height, so
"Z=0" in the acceptance brief was a reading of the wrong axis. The circle is at the platform's height within a quarter of a
unit; **actual collision/floor contact is still unverified** (the server has no height query), and the new test pins the
height so a future edit cannot drop it back to the entrance floor.

### Difficulty arithmetic (measured values, not a battle)

- **Wizard:** Shadow level 10 base HP 622, level 11 645, level 12 667 (`MagicLevelsConfig` from Root.wad); the mod's
  level-10 gear adds +139 → about **761** at level 10 and about **806** at level 12.
- **Falmea:** 950 HP, Fire, level 10, no power pips, 0 starting pips (one ordinary pip per round), 75% accuracy on attacks.
  Deck damage, read from the spell templates: Fire Cat 80–120 (1 pip), Fire Elf 100 plus a 210 damage-over-time (2 pips;
  the DoT is **total** damage divided across its 3 rounds in `CombatEffectApplicator.ApplyDamageOverTime`, i.e. 70 a round,
  not 210 a round), Sunbird 295–355 (3 pips), Fire Trap +40% incoming, Fireblade +35% outgoing. Best case ≈ 110 damage a
  round; a realistic mix with 25% fizzles and the buff cards is closer to 80–100.
- **Wizard damage:** the level-10–12 Shadow kit is DarkSprite 75–95, ShadowTroll 160–200, ShadowBanshee 245–285, TowerShield
  −40% incoming (all reachable at level 10), i.e. roughly 90–120 a round.
- **Verdict:** both sides need about 8–10 rounds; the wizard wins by shielding the big hits and loses if they only race.
  That is a plausible level-10 challenge, so **no tuning change was made**: without a real battle, changing 950 HP or the
  deck would be guessing, and the brief forbids replacing difficulty with scripting.

### Failure paths checked in code (no client, so no live exercise)

- **Flee and defeat do not trap the player.** `HandleFleeAction` sends `MSG_COMBATDEFEAT` with `NewDefeat`, whose
  `ExitZone/ExitLocation` come from the copy's `Exit` (Ravenwood) exactly as a loss does, so the client leaves the copy.
- **No stale variant note.** `ZoneVariants.ForAttach` forgets a character's noted variant when they attach to any other
  zone, so the Ravenwood attach after a defeat/flee clears the Fire School note before the mod sees the attach. The loss/flee
  test's manual note removal now stands for that real behaviour.
- **No unintended rewards.** The custom's `XP: 0` and `GoldMin/GoldMax: 0` are applied *after* the `BattleGold` fallback in
  `CombatService.GrantMobLoot`, so a win pays 0 XP and 0 gold and drops nothing; the quest's `m_endResults` is empty.
- **Offer wiring:** the quest's `Prep` dialog's actor template id is 1880000100 (the tutor), which is the exact condition
  `InteractQuestOfferComponent` uses to attach a quest to an NPC; the existing test pins it.

No new defect was demonstrated. The two D1.5 defects (same-zone reload, missing navigation) are the only defects fixed in
this slice; they remain confirmed in code and on the live server.

### Tests added (3) and strengthened (1)

`ConquestEncounterTests` is now **50 cases**:

- `AWinThroughTheRealBattlePipelineDefeatsFalmeaOnce` — reports the win through `BattleOutcomeEvents.Report` (the pipeline
  `CombatDuelComponent` uses) instead of calling the mod; one defeat event, one return transfer, one stage advance, and a
  repeated report after the goal is done adds nothing.
- `AFleeThenARetryWinsAndCompletesOnlyOnce` — flee, leave, return to the anchor, a second copy, a win through the pipeline,
  the return to Vespera, completion; a later report or goal completion does not move it again.
- `ADisconnectDuringTheBattleLeavesNoSessionAndARetryOpensAFreshCopy` — `Left` (dropped connection/logout) credits nothing,
  the detach closes the session, the reconnect inside the copy is sent to the exit, and the next look opens a fresh copy.
- `TheBattleRoomIsAPrivateVariantOfTheFireSchool` now also pins the circle's height to the trainer's platform (within 1
  unit, 99–101), which is the D1.6 geometry finding as a regression guard.

### Evidence

- CoreLib **216/216** (was 213); installer **2/2**; CoreLib Release build 0 errors; `git diff --check` clean.
- The live server was **not** restarted and the deployed build is unchanged: this slice changed tests and docs only. Client
  resources remain `current` from D1.5 (113 Root.wad files, backups intact).

### NOT TESTED (client acceptance, unchanged)

The eligible wizard receiving the offer, Umbral Sight detecting the anchor, the goal completing in the journal, the second
look, the client's actual reload into the private copy, the trainer's absence on screen, the custom Falmea and the circle's
visual placement, aggro and combat start, her Fire deck in battle, victory/defeat/flee reports from a real fight, the
return transfer, the journal labels and markers, dialogue, and a crash-free transfer. **The first battle has not been
played.** Nothing here proves combat works; it proves the data, rules and failure handling around it.

## Stage D2 handoff — the remaining instructors

### Architecture decision (recorded here, nothing built yet)

The full Ravenwood conquest is **one overarching storyline with seven instructor milestones**, not seven unrelated main
quests. D1.5 deliberately left the Falmea proof-of-concept as it is; the pieces it already provides are what D2 integrates:

- **The registry stays authoritative.** Each instructor keeps `ShadowSchool_Conquest_<Id>` = 1 anchor / 2 defeated /
  3 complete, written only from play, only in order, never twice. That is the per-instructor battle outcome and binding
  state, independent of any quest text, and it is what the finale and staff read (`ConquestEncounters.StageOf`).
- **One overarching quest.** `ShadowSchool-Conquest` opens after Vespera's lesson; it carries one milestone goal per
  instructor, each with a `m_genericEvents` entry `ShadowSchool_Conquest_<Id>_Defeated` (no zone, proximity or adjective
  match).  `QuestService.ReceiveZoneEventForQuests` completes every active goal in any active quest that names the event,
  so the milestone advances from the same win report the per-instructor quest already uses, without a second code path.
  **Required framework change for D2:** today the event is posted only while the per-instructor quest's duel goal is active
  (`OnBattleEnded`), so the overarching quest's milestone would stall if the player dropped the chapter quest. Post the
  defeat event whenever a valid session's win is recognised (before the quest-goal check), and let both quests consume it;
  the registry stage still advances only from the encounter quest's own goal completion, so no credit is duplicated.
- **The per-instructor quests remain as milestone chapters** (text, dialogue, anchor/duel/return pacing, all tested). They
  are not replaced now; when the overarching quest is added, the per-instructor quests can become optional colour, and the
  registry — not the quest list — remains the gate.
- **Ambrose's finale stays locked** until every required instructor is `ConquestEncounters.IsComplete` (stage 3 **and**
  the quest completed). Gating on `IsComplete` rather than on the milestone goal means a defeat never unlocks the finale
  before the return step, and a `Shared`/`Command` goal completion cannot unlock it either. The finale is not D2.
- **Each new instructor is still one row and three files** (`ConquestEncounters.Encounters`, quest, ZoneObjects variant,
  custom profile), exactly as below; D2 adds the overarching quest and its seven goals once, not per instructor.

1. **First accept D1 in the client** with a QA Shadow character that reaches it legitimately (not Rika): Vespera's
   offer, stand at Falmea, the dialogue, a step and a look, the copy, the battle, a win (and once a loss), Ravenwood,
   Vespera. Use `.conquest status` and `.umbral look`. Record solo turns; tune `Health` and the deck in
   `Customs/1880001.json` (re-read each battle, no restart). If the circle is misplaced, move it in the variant file. If
   the same-zone private transfer misbehaves, arrive at another spot or zone first; do not patch the client.
2. **Each next instructor is one row and three files**, `Order` 2–7, custom ids 1880002 and up: quest
   `ShadowSchool-Conquest-<Id>` (copy Falmea's; the goal names and events follow from the id, and the gate through
   `Order` is automatic), variant `ShadowConquest-<Id>` of the school interior (Remove the trainer, spawn a verified
   combat template as the custom, add circle 560, Exit to that school's Ravenwood exit), the custom profile, an
   `UmbralPerception` binding at the instructor, text keys `Conquest_<Id>_Enter/Won/Lost` and the dialogue. Plan order;
   verify each identity and placement the same way (the zone's `gamedata.bin`, `id_lookup creatures <name>`):

   | # | Instructor | Canonical NPC (enrollment mentor) | School interior | Combat identity (plan 7.3) |
   |---|---|---|---|---|
   | 2 | Lydia Greyrose | `WC-RAV-NPC03` | `WC_SchoolFrost` | shields, moderate damage, freeze if supported |
   | 3 | Halston Balestrom | `WC-RAV-NPC05` | `WC_SchoolStorm` | storm pressure, tempo |
   | 4 | Cyrus Drake | `WC-RAV-NPC02` | `WC_SchoolMyth` | traps and minions only if supported, else dual strikes |
   | 5 | Moolinda Wu | `WC-RAV-NPC01` | `WC_SchoolLife` (check `_C02`) | heals, shields, survivability |
   | 6 | Malorn Ashthorn (interim Death instructor) | `WC-RAV-NPC08` | to locate (the Death School is displaced) | drain and sustain, modest |
   | 7 | Arthur Wethersfield (Balance instructor) | `WC-RAV-NPC09` | to locate (no Balance interior is listed) | weakness, precision debuffs |
   | 8 | Merle Ambrose (**finale, not D2**) | `WC-HUB-NPC01` | `WC_Headmistress_House` (`AmbroseWards`) | wards and control |

   The mentor ids come from the enrollment variants (quest-dependency-map) and are unconfirmed as names; check each
   before use. Look for a dedicated combat template per instructor as 403089 was found; where there is none, a stated
   stand-in is a decision for the user, not a silent swap.
3. **Later, not D2:** controlled binding work after the duels (a new stage value or key, recorded the same way: play
   only, idempotent, a staff path), the Ambrose finale and the pact, and co-op helpers. The "one overarching quest" question
   is settled above (one quest, seven milestones, registry authoritative); it does not change what the six instructors need
   themselves.

### First D2 slice (exact)

1. **The overarching quest skeleton, with nothing else added.** `ShadowSchool-Conquest` offers from Vespera after the
   lesson and carries seven milestone goals, each a bounty with an event-only completion (`ShadowSchool_Conquest_<Id>_Defeated`)
   and no creature adjective, so only the framework's event completes it; Falmea's is the only one reachable now. Make the
   framework change named above (post the defeat event for a valid session, not only while the chapter's duel goal is active)
   and add tests: a real win advances exactly one milestone; a win in the public school, another variant, without a session or
   over another creature advances none; the other six remain inactive; the chapter quest's return still completes the
   encounter at stage 3, and the overarching quest gives no reward. Do not add the finale or its gate yet.
2. **Then instructor 2, Lydia Greyrose**, as the first `Order` 2 row and three files: verify `WC-RAV-NPC03`'s identity and
   her position in `WC_SchoolFrost` (gamedata, as Falmea's was), find a verified combat template for her (the search that
   found 403089 for Falmea), and add quest `ShadowSchool-Conquest-Greyrose`, variant `ShadowConquest-Greyrose`, custom
   1880002, an `UmbralPerception` binding at her, the text keys and the same test set. Only after she works does the pattern
   repeat for the remaining five.

## Stage D2.1 — recovery, completion and deployment — 2026-10-23 (GPT-6 Luna High)

The interrupted D2.1 implementation is now integrated and deployed. This section supersedes the earlier “First D2 slice”
as a to-do list: the overarching campaign and first two reachable instructor chapters are implemented. The five later
instructors and Ambrose finale remain future work.

### Campaign and encounter contract

- `ShadowSchool-Conquest` is Shadow-only, gated by Umbral Sight and `WC-COMMONS-MAIN-004`, and lists seven instructor
  milestones in order: Falmea, Lydia Greyrose, Halston Balestrom, Cyrus Drake, Moolinda Wu, Malorn Ashthorn, Arthur
  Wethersfield. Only Falmea and Greyrose have encounter registrations and chapter quests. The other five milestone events
  cannot be supplied by a registered encounter; Ambrose is not a milestone and remains inaccessible.
- `ConquestEncounters` keeps the per-character registry as the authority. A verified win is saved before its quest event is
  queued, and the chapter/campaign journal consumes that event only when a matching registered encounter has a persistent
  `Defeated` or `Complete` stage. A chapter replay delivers anchor then defeat when the anchor is active after reaccepting;
  an existing duel goal can safely consume a duplicate anchor replay and the saved defeat. Disconnects cannot erase a win.
- The quest progress event validator rejects direct/unverified campaign events. Command completion rejects campaign goals,
  campaign completion, and the protected instructor duel/quest goals. Loss, flee, unrelated battles, invalid sessions and
  another wizard's encounter state do not earn credit. Replays are idempotent, and already credited encounters do not
  issue another reward.
- `ShadowSchool-Conquest-Falmea` remains the original Fire School chapter. Greyrose is Order 2, NPC template 38172, combat
  representation 1475612, custom profile 1880002, private variant `ShadowConquest-Greyrose` in
  `WizardCity/Interiors/WC_SchoolFrost`. Her seven-card Ice deck uses validated Ice spell IDs; profile health is 1000 with
  neutralized inherited stats, 0 XP, 0 gold and explicit empty drops. Same-zone private entry sets `ForceReload`; the
  private variant removes only public Greyrose and replaces the combat representation with the custom creature and circle
  560 at the room's measured platform height. Public instructor records and public school variants are unchanged.
- Explicit `Drops: []` previously fell through to the base creature's drop tables. `BossOverrides.DropTablesFor` now
  distinguishes an omitted list (keep base drops) from an empty list (no drops), and `CombatService` applies that choice.
  The regression test preserves ordinary base-drop fallback and verifies the Greyrose/Falmea suppression.

### D2.1 changed artifacts

- Runtime integration: `src/Imlight.CoreLib/Game/Combat/BossOverrides.cs`,
  `src/Imlight.CoreLib/Game/Services/CombatService.cs`, `src/Imlight.CoreLib/Game/Services/QuestService.cs`,
  `src/Imlight.CoreLib/Game/Quests/QuestProgressEvents.cs`, `src/Imlight.CoreLib/Game/World/ZoneVariants.cs`,
  `src/Imlight.CoreLib/Game/Combat/BattleOutcomeEvents.cs`, `src/Imlight.CoreLib/Mods/ModRegistry.cs`, and
  `src/Imlight.CoreLib/Mods/ShadowSchool/` (`ConquestEncounters`, `ConquestWorld`, `ConquestCommands`,
  `ShadowSchoolMod`, `UmbralPerception`, plus the existing Shadow content modules).
- Quest/resource payload: `spiraldb/QuestTemplates/questtemplates_ShadowSchool-Conquest.json`,
  `questtemplates_ShadowSchool-Conquest-Falmea.json`, `questtemplates_ShadowSchool-Conquest-Greyrose.json`,
  `spiraldb/ZoneObjects/WizardCity-Interiors-WC_SchoolFrost@ShadowConquest-Greyrose.json`,
  `spiraldb/Customs/1880002.json`, and `text/en-US.txt`. Falmea's original chapter, profile and room remain in place.
- Coverage: `test/Imlight.CoreLib.Tests/Mods/ShadowSchool/ConquestCampaignTests.cs` and
  `ConquestEncounterTests.cs`, including actor cleanup, actor/message delivery, individual wizard state, prerequisites,
  progression order, verified-victory gating, command rejection, event replay after disconnect/reaccept, duplicate wins,
  retry paths, custom profile and private-room data. No test grants or reads real player state.

### Verification and deployment

- Latest exact-code results: campaign + encounter **85/85**, plus three consecutive repeat runs **85/85** each; full
  CoreLib **251/251**; client installer **2/2**. CoreLib Release and Director Release builds: **0 errors**. The builds show
  pre-existing Akka.Remote NU1904 security advisory warnings and existing project compiler/analyzer warnings.
- `git diff --check` passed. Installer status reports the installed `r806919.Wizard_1_610` client current at 113
  Root.wad files; original install backups remain preserved. Graphical client acceptance is still **NOT TESTED**.
- `build/server` now contains the latest differing Release assemblies and the 139-file ShadowSchool mod payload, with
  `[Mods] Enabled = ShadowSchool`. The pre-deployment server config is backed up at
  `build/server/Config/Imlight.ini.pre-D2.1-20261023.bak`. An isolated startup used a fresh `build/qa-data/d2.1`
  database and temporary ports; logs confirmed mod enablement, all 14 mod SpiralDB files, and patch/login/game listeners.
  The smoke used the machine's x64 .NET 10 `dotnet` host because the deployment-local apphost resolved its local hostfxr
  but no shared framework; `start-dev.bat` uses `dotnet run`. The QA process is stopped and the final deployment config's
  normal ports are restored. The smoke log recorded one unrelated base data error (`ReduceDamageInvunerable` without a
  stat table); the name is not present in ShadowSchool source or payload. Static integration tests validate the campaign,
  Greyrose profile, encounter registration and room variants; no graphical encounter was claimed.
- No live player database was modified. Rika CrypticTheatre was not read or changed. No active graphical client was
  running during locale installation; no credentials were handled. Unrelated dirty worktree files were preserved and
  nothing was staged or committed.

### Exact Stage D2.2 prerequisites

1. Keep the deployed D2.1 build and the source changes above as the base; do not reset the existing dirty worktree or
   rewrite the public Fire/Frost school data.
2. Before adding Order 3, verify Halston Balestrom's identity and combat representation against installed Wizard City
   resources, identify his actual Storm School geometry, and confirm a valid Storm deck. Choose a new custom ID above
   1880002 (1880003 if still unused) and add only his instructor row, Shadow-only chapter quest, private Storm room,
   custom profile, Umbral Sight anchor and localization.
3. Preserve campaign milestone order and the authoritative, per-character encounter registry. Only a verified, session-
   owned win for the registered Halston encounter may credit his event; protect retries, duplicate/reconnect events,
   commands, public Storm, and other wizards with the same tests used here.
4. Keep the Ambrose finale, binding pact, the remaining four instructors after Halston, and Krokotopia changes out of
   D2.2. Require the targeted tests, full relevant regression, clean diff check, safe deployment and a real graphical
   battle only when a disposable eligible QA character/client is available.
