# Shadow School handoff for Claude Opus 5.5

Checkpoint stage: **D2.4 complete; D3 not started.** The dedicated development branch is `feature/shadow-school` in the
`ToontownSuperForeverMVP/Imlight` fork. The checkpoint commit SHA is filled in after the branch commit is created.
The fork's default branch remains `quality-assurance`.

## Current implementation

The mod implements a selectable Shadow school and its enrollment/progression, fourteen trainable Shadow spells, school gear and
vendors, Shadow Pip rules, Vespera Quillmoor and Corvin Ashgrave, Shadow-specific Wizard City story dialogue, Umbral Sight and
binding perception, and the seven Ravenwood faculty encounters. Each encounter uses a per-character quest chain and a private
zone variant. Victory is credited only after the server observes that character winning against the expected custom creature in
the active encounter's private copy. Persistent stages back the chapter milestones and reconnect replay. The seventh completed
chapter is the offer-ready point for D3.

Ambrose's encounter, a pact quest, and pact state are intentionally absent. Do not start D3 implementation as part of this
checkpoint.

## Architecture and entry points

- `src/Imlight.CoreLib/Mods/ShadowSchool/ShadowSchoolMod.cs` registers the mod and its lifecycle hooks.
- `ShadowSchoolContent`, `ShadowCurriculum`, `ShadowGear`, `ShadowPipRules`, and `ShadowMainlineDialogue` implement the school,
  training, items, combat rules, and story integration.
- `UmbralSight` and `UmbralPerception` gate and resolve binding discovery.
- `ConquestEncounters` owns the ordered seven-instructor campaign, durable milestone predicates, battle-outcome validation, and
  event replay. `ConquestWorld` owns private-copy entry and exit. `ConquestCommands` is diagnostic/admin support, not a source of
  legitimate campaign progress.
- Shipped templates and profiles live under `src/Imlight.Director/mods/ShadowSchool/spiraldb`; English strings are in `text`.
  The client installer and optional executable patch scripts are under `client`.
- Shared engine integration includes mod content loading, custom creature lookup, quest-progress source validation, battle
  outcome observation, private-zone variant persistence/transfers, Shadow Pip handling, and quest dialogue resolution.

## Seven-instructor gate

Order: Falmea → Greyrose → Balestrom → Drake → Wu → Ashthorn → Wethersfield. Each chapter requires Shadow, completed Nightside,
working Umbral Sight, and completion of all earlier chapters. The wizard discovers the instructor's binding while its Anchor goal
is active, deliberately looks again to enter that instructor's private variant, wins with the expected custom creature, and
returns to complete the chapter.

The exact D3 offer prerequisite is `ConquestEncounters.AllInstructorsComplete(wizard)`. It requires working Umbral Sight, all
seven `ShadowSchool_Conquest_<Id>` registry stages at `Complete`, and the corresponding chapter quest completion flags. Use
`AllInstructorsDefeated` only if D3 must begin immediately after the last battle, before the Wethersfield return/turn-in. Do not
use the campaign quest's complete flag alone and do not create another victory tracker. The registry is persisted before its
journal event; event handlers validate durable state and attach replays missing events.

The D2.4 regression `SevenVerifiedInstructorVictoriesCompleteEveryChapterAndEveryCampaignMilestone` drives all seven victories
through the real server-side observer and quest hooks with synthetic state. It proves the all-seven gate cannot become true early
in that simulated sequence; it is not a graphical battle test.

## Build and test

Prerequisites: the repository's .NET 10 SDK and Python 3. Initialize the pinned `Imcodec` submodule and provide its compatible
`GeneratorInput` files from installed game data under `submodule/Imcodec/src/Imcodec.MessageLayer` and
`submodule/Imcodec/src/Imcodec.ObjectProperty`. Those generated inputs are machine-local and excluded from the repository.
The full quest-dialogue test also reads the local `spiraldb-cache/QuestTemplates` mirror; it contains game data and is not
committed. Run from the repository root:

```powershell
dotnet test test/Imlight.CoreLib.Tests/Imlight.CoreLib.Tests.csproj -c Release
python -m unittest discover -s test/ShadowSchoolClientInstaller.Tests -v
dotnet build src/Imlight.CoreLib/Imlight.CoreLib.csproj -c Release
dotnet build src/Imlight.Director/Imlight.Director.csproj -c Release
```

The mod's generated client resources require a compatible installed Wizard101 client and its `Root.wad`; those proprietary files
are not part of this repository. Use the installer status/backup workflow before installing client-generated resources. A QA
server requires installed game data and an isolated database/configuration. Never point tests or QA at the live player database.

## D2.4 results and remaining risks

The D2.4 audit run passed Conquest + Umbral Sight **252/252 on three runs**, all ShadowSchool tests **367/367 on three runs**,
full CoreLib **380/380**, installer **2/2**, CoreLib and Director Release builds, and an isolated QA startup on a fresh database.
After removing copied game quest/drop-table fixtures from this public checkpoint and switching those regression tests to the
local installed SpiralDB cache, the included CoreLib suite passes **366/366**, the ShadowSchool filter **364/364**, Conquest +
Umbral **251/251 on three consecutive runs**, and installer **2/2**. The earlier 380-test total included eleven unrelated tests
from the mixed local checkout; those were kept out of this branch. The isolated startup loaded the mod and reached ready state;
it did not execute an instructor battle. Re-run these checks after any code change rather than treating these numbers as a
guarantee for later revisions.

The 15 scripted boss cheats for Wu, Ashthorn, and Wethersfield are data-validated and source-traced through dispatch and effects,
but none has been observed executing in a real battle. Fight balance, animation, target presentation, timing, dispels and effect
ordering remain unverified. Ashthorn uses a Dworgyn-based combat representation and Wethersfield uses an Alhazred-based one;
custom names and school/stat/deck data are set, while portraits, models, name tags, and animation presentation are not verified.

Circle coordinates and installed zone placements have been inspected, but no graphical or collision evidence establishes floor
contact. Wethersfield's Ravenwood circle is about 37.7 units below his NPC placement on sloped terrain. No instructor encounter
has graphical-client acceptance. The Ravenwood same-zone private exit has source and unit coverage, but actual transfer,
defeat/flee, disconnect during transfer, restart/reconnect, and journal presentation remain runtime checks.

See `stage-d2.4-audit.md` for the severity-ranked findings and exact validation matrix. No D2.4 production fix was justified;
the only D2.4 code addition was the end-to-end progression regression.

## D3 and D4 scope

- **D3 — Ambrose finale:** implement the final binding/encounter only after choosing a supported room, installed creature/spell
  data, and client-compatible entry/exit. Gate its offer with `AllInstructorsComplete`. Keep it private, per-character, and
  consistent with the plan's conflict over methods and Ambrose's refusal of coercion. Do not assume untested effects or assets.
- **D4 — binding pact and aftermath:** implement the plan's narrow, enforceable pact after D3 victory: freedom of study, limited
  Spiral access, and bounded authority over the specific defensive bindings prepared in the ritual. Persist explicit per-character
  pact state; do not grant global control over Bartleby, Wizard City, NPCs, or other players.

## Data safety

Use synthetic characters and isolated QA databases. Do not read or modify Rika CrypticTheatre or any live player record. Keep
authentication tokens, local configuration secrets, logs with user data, generated game caches, client executables, `Root.wad`,
extracted game assets, backups, and build/database outputs out of commits. Do not patch or replace a client while it is running;
preserve original client backups and the user's existing local worktree changes.
