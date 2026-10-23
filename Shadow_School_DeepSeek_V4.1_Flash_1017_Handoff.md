# SHADOW SCHOOL: CONTINUATION HANDOFF FOR DEEPSEEK V4.1 FLASH 1017

**Project:** Imlight Shadow School, Release 1: complete Wizard City and playable level 1–50 school  
**Date:** 22 October 2026  
**Purpose:** Continue Opus 5.5's *existing* implementation after the Claude weekly limit. This is a continuation order, **NOT** a fresh implementation brief.  
**Authoritative design:** `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md` (the 649-line release specification). Attach or make that file available alongside this handoff. This document takes precedence for **handoff status, implementation sequence and protection of existing work**; the original plan controls the desired story, spell values, assets and final acceptance.

> ## PRIMARY DIRECTIVE
> **Resume in place. Do not reinvent, regenerate, replace, restructure, or roll back working features from the prior Opus session.** Inspect the actual repository, local changes, installed client, mod folders, and test evidence first. Treat Opus's final report as *claims requiring confirmation*, not an exhaustive change log. Diagnose the level-5 Shadow Troll training failure first. Then continue the unfinished release through real-client verification, keeping existing code, mod identity, story choices and asset-reuse constraints intact.

---

## 0. How to work effectively in this fresh model session

You have no reliable memory of the prior agent's tool calls or private reasoning. Everything needed is either in the **current workspace**, Opus's final handoff (reproduced here), or the original design document. **Do not infer file changes from this document alone.** Discover actual Git status and diffs.

DeepSeek Flash should work in **short, verified vertical slices**, rather than attempting a giant simultaneous rewrite:

1. Identify one bounded next task and inspect only the relevant files/tests.
2. State a brief hypothesis and the acceptance conditions for that task.
3. Modify the smallest practical set of files, favoring existing extension points.
4. Compile and run targeted tests, then real-client tests where available.
5. Record a concise evidence checkpoint: what changed, what passed, what failed, how to resume.
6. Move to the next stage **only after the current blocker is understood**. If a tool is unavailable, do not fabricate a pass. Continue nondependent work and mark the unavailable test unverified.

Avoid repeated whole-repository exploration, huge undifferentiated source dumps, made-up method names/resource IDs, parallel refactors, context-saturating logs, and repetitive speculative edits. Prefer small targeted searches, diffs, compile errors, protocol traces and explicit repros. Do not prematurely end with a plan when implementation is possible, but do not claim a release is finished because code compiles.

**No approval is needed for routine implementation and non-destructive testing.** Never destroy user data, original assets or unrelated modifications. If destructive work would be required, find a reversible alternative first.

---

## 1. Prior Opus session: reported status, not guaranteed facts

Opus stopped because the **user reached the Claude weekly usage allowance** after roughly four hours, *not* because it had finished the project. The last test was inconclusive.

### Claimed working / tested by Opus

- An eighth Shadow starting-school path is integrated into the modified client/server at least far enough to perform Milestone A tests; verify **which** creation/login actions really passed from evidence.
- The Shadow tutor **Vespera Quillmoor** is placed at the end of **Library Book Stacks**. **KEEP THIS NAME AND LOCATION.** Do not revert to an unnamed tutor or relocate her without a concrete technical blocker.
- Her trainer window lists **all 14 specified level 1–50 spells**, with corresponding installed card art, displayed unlock levels and **Free** training costs. Unavailable spells are greyed out.
- A fabricated **level-5 training request sent from a level-1 wizard was rejected by the server**. Client visibility and server-side checks appear aligned for that negative test.
- Original `WizardGraphicalClient.exe` and `Root.wad` were preserved as matching `*.shadowschool-backup` files. `python install.py uninstall` reportedly restored the originals; uninstall and reinstall were tested.
- `Imlight.ini` contains an enabled `[Mods]` section.
- The 14 Shadow spell templates reportedly load server-side with the intended numeric values. **Only starter spells received earlier real-client coverage; the other 12 have NOT been battle-tested.**

### Known unresolved issue: FIRST PRIORITY

A level-5 wizard clicked **Train** for **Shadow Troll** and it was **not learned**. It is unknown whether:
- The click missed, a row wasn't selected, or the training window didn't send the request.
- The request had a wrong spell ID, character reference, school value or UI selection state.
- A new trainer-eligibility condition incorrectly rejected a valid purchase.
- Training succeeded in-memory but failed persistence or spellbook refresh.
- The test character's **Administrator** privilege or other test mode affected the behavior.

**Do not call this a known server bug or a missed click until the actual request/response and persisted state identify the cause.**

### Explicitly NOT complete in prior report

- Shadow variant of `To Ravenwood!` and its prerequisite integration with `Panic in Three Streets!`.
- Adapted existing Wizard City dialogue and journal presentation.
- Umbral Sight, staged Ravenwood binding ritual, eight representative duels, Ambrose's confrontation, limited pact and personalized aftermath.
- Shadow-aware `A Whole New World` / Krokotopia transition.
- Real combat tests for the other 12 spells, complete Shadow gear, decks and vendor tiers through level 50.
- Mixed-party/vanilla-school regressions, full narrative route, requested documentation and final release evidence.

### Workspace locations Opus reported

- Core mod additions: `src/Imlight.CoreLib/Mods/`
- Director content: `src/Imlight.Director/mods/ShadowSchool/`
- Client patcher/installer: `src/Imlight.Director/mods/ShadowSchool/client/`
- Mod toggle: `[Mods]` in `Imlight.ini`
- Existing code touched, some with **pre-existing user uncommitted edits**: `CombatDuelSubCircle.cs`, `CombatDuelComponent.cs`, `SpiralDB.cs`, `CharacterHelper.cs`, `Imlight.Director.csproj`, `Imlight.ini`.
- Other modified files: `TrainService.cs`, `InteractTrainerComponent.cs`, `CharacterService.cs`, `RootArchiveLoader.cs`, `ZoneObjects.cs`, `AgentBridge.cs` (last one includes a fix preventing duplicate agent-bridge startup).
- The patched client remains **installed**. Original backups reportedly exist.
- Test leftovers: a privileged `shadowqa` account and character **Oscar**. These are existing QA assets, **do not delete automatically**. Use a non-admin fresh test account to validate normal gameplay, because admin rights may hide bugs.
- **Nothing is committed.** Do not assume any clean repository state.

---

## 2. Mandatory intake: inspect without erasing anything

Perform these operations before edits:

1. Locate the repository root and confirm the **correct current project checkout**, build tooling and installed client version.
2. Run `git status --short`, inspect `git diff --stat`, targeted diffs, untracked files, and any staged work. Determine which modifications precede ShadowSchool where evidence exists; **do not falsely label all dirty changes as Opus's work**.
3. Inventory the contents and exact names of the current mod folders, manifest, config, installer, scripts, tests, spell registration, NPC registration, training validators and client patches. Inspect existing architecture before adding new modules.
4. Confirm `*.shadowschool-backup` assets exist and hash integrity if prior original hashes are recorded. Record client exe version/hash and current patched/backup state. **Do not run uninstall, reinstall, or patch repeatedly unless needed to diagnose a test.**
5. Locate Opus-generated tests/logs/reports if present. Identify confirmed passes versus untested claims.
6. Determine precisely how the graphical client is being automated or launched, and what can be tested safely.
7. Create `src/Imlight.Director/mods/ShadowSchool/docs/continuation-status.md` (or the established docs location) containing current findings, test evidence, blockers and completed actions. Keep it concise, factual and frequently updated.
8. If test tools are accessible, make a safe **external workspace backup/diff snapshot** without overwriting original client files or changing user-owned Git history. Do not run `git reset --hard`, `git clean -fd`, wholesale `git checkout -- .`, `git stash --all` or broad overwrite commands.

When Git checkpoints are appropriate, stage **explicit reviewed paths or hunks** only. Never silently commit unrelated pre-existing dirty edits. Because Shadow work may touch files with older user changes, prefer a separate patch/archive of relevant diffs and commit only after carefully verifying scope; a commit containing mixed user/agent changes requires explicit review. Do not `git add .` as a shortcut.

**Intake acceptance:** a file-by-file inventory, build/test route, accurate status table and enough evidence to proceed without rebuilding prior work.

---

## 3. STAGE A1 — Fix the Shadow Troll training defect before broader work

Reproduce a **valid** training attempt with a level-5 Shadow character, alongside a level-4 denial and a level-1 denial, using the current **Vespera Quillmoor** trainer and a non-admin normal game profile. The spell is described in the design as **Death Troll — Shadow**, and Opus called it **Shadow Troll**; identify the actual registered spell/template ID instead of guessing which label is authoritative.

### Instrument both sides of the purchase

Trace and record, in order:
- Selected row / displayed spell template ID and level in the client UI.
- Whether `Train` emits the expected request, including selected trainer and requested spell ID.
- Server receipt and lookup of account/character/school, level, prerequisites, training price and trainer-offering membership.
- Eligibility decision **using the same predicate** as display requirements, with an explicit rejection reason on failure.
- Actual taught-spell mutation, save transaction, acknowledgment packet and client spellbook refresh.
- Known-spell state after closing/reopening the trainer, switching zones and reconnecting.

### Checks that MUST pass

- Level 4: spell appears locked; a forged request is rejected; the spell remains unlearned.
- Level 5: select row and train successfully; it is learned, persists, and can be placed in deck.
- Repeated valid training request: no duplicate known-spell entry, reward or corrupt trainer state.
- Unrelated trainers and other schools retain existing training behavior.
- `Free` means **no primary-school training-point deduction**; ordinary resource rules remain intact.
- If click automation is unreliable, use explicit UI observations plus server protocol evidence to distinguish a GUI miss from a code defect.

**Gate A1:** compile + server unit/integration tests + a verified *legitimate* successful purchase and relog in the actual client, or a clearly documented environmental inability with a minimal reproducible alternative. Only then advance to substantive expansion.

---

## 4. STAGE A2 — Stabilize previously implemented core features

Do not recreate the school. Test and patch only defects:

- Create a **new non-admin** Shadow wizard; verify eighth-school display, confirm/back selection, correct stored school ID, school-specific stats, starting spells and login/relog.
- Visit **Vespera Quillmoor** in the Library Book Stacks on a fresh character. Verify location, rendering, trainer interaction, exact 14 spell list, localization, card art, appropriate greyed-out rows and free training.
- Confirm the user-selected resource rules: **one Shadow Pip at battle start; capacity one; refill on rounds 4, 7, 10…; unused refills discarded**. Shadow-primary ordinary Power Pips count double for Shadow spells; Shadow Pips are separate from regular Power Pips.
- Check the starter spells: **Dark Sprite — Shadow (75–95 Shadow damage, 1 normal pip)** and **Dark Empower — Novice (1 Shadow Pip to gain one ordinary pip)**, including artwork, cinematic, sound, targeting, resource consumption and relog.
- Check explicit starting health curve (rounded-up Fire/Myth midpoint) and ensure original seven schools remain unaffected.
- Exercise uninstallation only in a controlled test after recording backups and installer state; existing install/uninstall behavior should be **maintained**, not rewritten on speculation.

**Gate A2:** documented results for client creation, save/load, trainer, starter battle and a regular-school regression. Mark incomplete tests exactly.

---

## 5. STAGE B — Complete and test the selected level 1–50 school

Retain the **exact agreed spell list and initial balance** from the original plan. Opus already registered all 14; do not register a second competing set or swap their donor spell IDs casually.

| Level | Spell | Cost and initial effect |
|---:|---|---|
| 1 | Dark Sprite — Shadow | 1 normal pip; 75–95 Shadow damage |
| 1 | Dark Empower — Novice | 1 Shadow Pip; gain 1 normal pip |
| 5 | Death Troll — Shadow | 2 normal pips; 160–200 Shadow damage |
| 8 | Tower Shield — Shadow | 0 pips; -40% next incoming damaging hit |
| 10 | Banshee — Shadow | 3 normal pips; 245–285 Shadow damage, no extra trap |
| 16 | Shadow Trap | 0; +25% next incoming Shadow attack |
| 18 | Shadowblade | 0; +30% next outgoing Shadow attack |
| 22 | Humongofrog — Shadow | 4 normal pips; 265–325 AoE Shadow damage |
| 26 | Helephant — Shadow | 4 normal pips; 345–405 Shadow damage |
| 30 | Shadowspear | 1 Shadow Pip; +10% pierce to next outgoing Shadow attack |
| 33 | Minotaur — Shadow | 5 normal pips; hits 50 then 420 Shadow damage |
| 38 | Storm Shark — Shadow | 3 normal + 1 Shadow Pip; 380–420 damage |
| 42 | Triton — Shadow | 6 normal pips; 610–650 Shadow damage |
| 50 | Frost Giant — Shadow | 7 normal + 1 Shadow Pip; 475 AoE Shadow damage + 1-round stun |

Initial accuracy: attacks 85%, except Triton and Frost Giant at 80%; utilities 100%. Balance values can be adjusted **with test evidence and an explicit changelog**, not silently redesigned. Preserve original donor artwork/cinematics/models/animations/particles/audio without altering enemy originals. Audit nested effects, school damage/resistance, ward/blade stacking, AoE targets, two-hit sequencing, stun immunity and client-visible cost.

**Work in small batches:** first confirm levels 5–18; then 22–33; then 38–50. In each batch verify level lock, legal training, persisted known spell, deck insertion and **actual casting** (not just loaded templates). Record a pass/fail/untested matrix for all 14.

**Next:** implement missing Shadow gear, school decks, vendor items through level 50 with existing appearances and comparable stats, vendor tiers every five levels. Verify restrictions and persistent purchase/equip behavior. Do not create new graphics. Any school-vs-school or PvP behavior not verified must be documented and if needed safely disabled, never silently treated as supported.

**Gate B:** all 14 train/cast/store correctly and gear/decks/vendors work. Vanilla school/enemy donor spells unchanged.

---

## 6. STAGE C — Implement the Shadow Wizard City quest integration

**Important:** This work was NOT completed by Opus. Add it onto the existing mod without replacing the current school/trainer/spells.

### C1. Shadow enrollment: small proof of quest architecture

- Read actual quest templates, prerequisites, rewards and English text for the installed revision.
- Implement a **Shadow-specific version of `To Ravenwood!`** preserving registration (Mr. Lincoln), professor introductions, housing, ordinary service access and rewards.
- Add Shadow enrollment as an accepted prerequisite to `Panic in Three Streets!`, preserving all seven original schools.
- Validate a fresh Shadow character from tutorial → Unicorn Way → Grim Tales/enrollment → Three Streets.
- Keep canonical quest IDs/objectives/flags unless a new variant requires its own identifier; derive correct joins from installed data.

### C2. Per-player presentation system

- Implement/adapt a single per-player resolver for dialogue, NPC offers, turn-ins, tracker, and quest journal; do not mutate global cached quest definitions.
- Select variants using **character's school, actual quest completion/stage and later pact state**; party members and other schools must not inherit owner-specific text.
- Modified lines with contradictory existing voice audio must use a verified safe audio-suppression path, or retain matched vanilla text/audio in that scene and report the limitation. Never invent a compatible audio event.
- The player is normally silent: tell the story through NPCs, journal text and visible actions, not fake voiced wizard dialogue/unsupported dialogue choices.

### C3. Cover the existing main quest route; do not invent filler

1. **Tutorial/Unicorn Way:** Ambrose notices unusual magic but isn't hostile; genuine rescues remain canonically equivalent.
2. **Triton Avenue:** Duncan/forbidden knowledge, while preserving Suzie's rescue and existing plot. Avoid prematurely claiming Malistaire's later Dragon Titan objective.
3. **Firecat Alley:** maintain Gretta's cure and elf/banshee outcomes; emphasize curing versus control and her outsider status.
4. **Cyclops Lane:** keep Cyrus's errands, threatened expulsion, captured students and Akilles; explore magical command without making Cyrus cartoonishly cruel.
5. **Three-street reunion:** all six completion orders; preserve Bartleby's failing memory, Eye of History, Foulgaze, Order of the Fang leads.
6. **Nightshade/Daisy:** preserve Daisy's role, Nightshade and book investigation.
7. **Nightside:** preserve Ambrose's temporary ghostly-sight enchantment, Death School relocation, Mortis and Dworgyn, then reveal that Shadow affinity causes the enchantment to persist.
8. **Krokotopia departure:** implement the handoff in Stage E, after the finale. No free keys/quest skips.

**Gate C:** Shadow goes through required Wizard City progression, retaining original objectives and rewards, with meaningful Shadow interpretation; other schools' main quest paths unchanged. Coverage of all six Three Streets orders is mandatory.

---

## 7. STAGE D — Umbral Sight and **The Unbound Student** finale

**Story integrity requirements:** This is a young wizard exploiting specifically prepared defensive bindings, **not** defeating full-power professors by raw level-10 superiority. Ravenwood retains services and characters; victory is personal, nonlethal and has consequences. Keep tutor **Vespera Quillmoor** independent of Morganthe's Shadow Web.

### D1. Umbral Sight

- Activate only after the installed Nightside ghost-sight event; persist per Shadow character.
- Reveal only explicitly designated traces/anchors. It cannot read minds, break arbitrary locks or give permanent combat stats.
- Reuse installed effects/markers and resource references. Gracefully handle missing visuals; never claim effects work without testing.
- Give Vespera restrained explanation and warnings; Ambrose's sincere containment proposal begins the conflict.

### D2. One persistent quest: `The Unbound Student`

Implement staged objectives **within one finale quest**, not a new parallel 24-quest campaign:

1. Player returns after Nightside; altered sight persists, causing concern.
2. Ambrose proposes supervised containment/study; wizard's rejection is conveyed via actions, not made-up spoken dialogue.
3. Vespera shares an incomplete account of Ambrose's earlier brilliant student, without declaring Morganthe innocent or openly serving her.
4. Player deliberately prepares Ravenwood's defensive binding anchor(s) identified through sight. Use 1–3 existing-prop interactions depending on verified technical support.
5. Conduct **eight personal, private, nonlethal representative duels**, preserving each completed stage across defeat, logout and server restart:
   - **Dalia Falmea:** Fire pressure; disappointed by former rescuer turning to coercion.
   - **Lydia Greyrose:** defensive Ice; concerned for others' safety.
   - **Halston Balestrom:** Storm pressure; recalls Triton Avenue rescue.
   - **Cyrus Drake:** Myth tactics; respects preparation, opposes the seizure.
   - **Moolinda Wu:** Life heals/defense; compassionate refusal to endorse domination.
   - **Malorn Ashthorn:** temporary Death instructor; drain/sustain and personal Malistaire parallel.
   - **Arthur Wethersfield:** Balance instructor (not a full professor); anti-coercion pragmatism, debuffs.
   - **Merle Ambrose:** final constrained duel; recognizes his earlier student history, maintains agency and moral consistency.
6. Impose three **limited pact rights**: (a) freedom to study Shadow without arbitrary confiscation; (b) ordinary Ravenwood services and lawful access to other worlds; (c) acknowledgment of the player's control of **specifically captured defensive bindings**. **No absolute control, mind-slavery, Bartleby authority, automatic keys, immunity or server-wide world changes.**
7. Save pact completion and move to Stage E's Krokotopia handoff.

- Conquest ownership is **per character**. Friends can assist fights but cannot receive another owner's quest credit or activate anchors, and public NPCs are never globally despawned.
- Bosses should use reusable existing spell/cinematic assets and bounded difficulty that can actually be beaten at expected Wizard City level. Distinguish fights mechanically and in written motivation; avoid eight reskinned health sponges.
- Persist anchor state and each of eight victories transactionally. Prevent duplicate rewards on retries or spoofed quest packets. Log and handle interrupted instance, exit, reconnect, and progress migration.

**Gate D:** full finale can be completed and resumed, eight fights distinct and mechanically fair, single pact reward, no progression exploits, multiplayer/other schools unaffected.

---

## 8. STAGE E — Aftermath and functional Krokotopia handoff

After pact completion, update **Shadow-character-specific** dialogue for Ambrose, Falmea, Greyrose, Balestrom, Cyrus, Moolinda, Malorn, Arthur, Vespera, and Mr. Lincoln, with optional supported reactions by Gamma and Dworgyn. No one becomes mind-controlled or comically friendly; practical services still work. Bartleby remains an independent injured world character and operates keys normally.

- Main handoff `A Whole New World` must retain original Spiral Keys, Bartleby, Krokotopia destination and arrival objectives; require Shadow's finale for Shadow departure only, while normal schools use existing prerequisites.
- The player's **current** motive is Malistaire's disruption, stolen Eye, protection of the authority they seized and desire for forbidden knowledge. **No Dragon Titan reveal in Wizard City.**
- Only Release 1 story statuses should activate (provisional/enrolled, conqueror/pact). **Do not grant Morganthe candidate/apprentice status or claim she orchestrated Wizard City.**
- Leave future Arc 1 and Celestia narrative hooks possible without implementing those worlds.

**Gate E:** post-pact dialogue remains character-specific and coherent; normal services work; a fresh Shadow player reaches Krokotopia by the legitimate route and ordinary-school progression is untouched.

---

## 9. STAGE F — Safety, polish and final QA

Perform focused automated tests plus actual-client/end-to-end runs:

- Eighth school: fresh create, relog, character list, social, deck, equipment, damage/resist and health.
- 14 spells: trainer UI, valid/invalid purchases, persistence, full cinematic/sound, correct resources/targets/effects; ensure donor enemy spells unchanged.
- Trainer security: level, prerequisites, class, unlocks, double purchases, malformed requests and admin/test overrides disabled during acceptance.
- Quest graph: all six street completion orders; side quests mixed with main; interruptions/restart; wrong-stage interaction rejection; no accidental quest skip/soft lock.
- Story presentation: Shadow versus Fire/Death/Balance variants simultaneously; different Shadow stages; journals, NPC dialogue and any audio/text compatibility.
- Finale: solo, fair level scaling, defeat and retry, party helpers, instance exit/reconnect, idempotent flags and rewards, post-pact services.
- World compatibility: optional Colossus Boulevard, Sunken City, Crab Alley and Golem Court where installed; no new mainline gates.
- Krokotopia access: prerequisites and normal world transition truly work.
- Installer: hash-gated client version, integrity-preserving uninstall/reinstall, original backups untouched, no unrelated file replacement.
- Persistence: no save migration surprises, non-admin acceptance profile, no unauthorized school switching, isolated character state.

**No artwork, audio, models, animations or cinematics are to be invented.** Preserve the installed donor presentation exactly unless a justified compatible donor needs swapping (document this).

Document `PASS`, `FAIL`, or `NOT TESTED` **for each acceptance condition**, with log paths, test character, current build/client hash and reproduction steps. Compilation/unit tests are never substitutes for GUI and gameplay testing. No unsupported claims of complete Wizard City gameplay.

---

## 10. Checkpoint and context discipline for a long-running DeepSeek agent

Before the end of **each** substantial coding slice, update `continuation-status.md` with:

```markdown
## Session checkpoint — [date/time]
- Current branch/working tree: [actual status, dirty paths]
- Current target: [one narrowly defined feature]
- Confirmed working (evidence): [tests, log path, client observation]
- Changes made: [exact paths/classes, 1-line purpose]
- Remaining defects: [exact symptom, repro, logs]
- Next immediate action: [one concrete next step]
- Installer/client backup state: [installed/uninstalled, original hashes]
- User data / unrelated changes: [preserved; exceptions explicitly noted]
- Acceptance status: [pass/fail/not tested]
```

Do not write an exaggerated 'everything complete' summary. Record checkpoints in the repository so another fresh model can resume even if this session exhausts context or hits tool limits. Do not create a new plan or re-audit everything after every context reset; read this checkpoint and targeted diffs first.

**Priority if time runs short:** A1 training repair > A2 existing feature integrity > B mechanics > C main quest route > D finale > E Krokotopia aftermath > F broad polish. Preserve partial accomplishments and articulate the next blocker. Do not fake progress or promise future background activity.

---

## 11. Mandatory final report format

At each stopping point, provide:

1. **What was inherited, unchanged:** existing Vespera tutor, mod architecture, original installed client backups and validated prior code.
2. **What was actually fixed/added this session:** concrete files and behavior.
3. **Verification:** tests and real-client observations separately, with pass/fail/not-tested evidence.
4. **Blockers and outstanding stages:** distinguish genuine defects from unavailable test tools.
5. **Safety:** dirty file preservation, original backup status, install state, QA account/character state, commits or patch files (if any).
6. **Exactly where to resume next.**

**Final release definition:** A fresh level-one Shadow wizard can be created and played through authentic adapted Wizard City quests, learns/casts the entire fourteen-spell level 1–50 curriculum with existing presentation, gains story-only Umbral Sight after Nightside, completes **The Unbound Student** with eight distinct constrained duels, signs a limited per-character pact, continues to receive meaningful and correctly scoped aftermath dialogue, and reaches Krokotopia normally. Other schools and players retain vanilla progression, all saves/backups are preserved, and every claimed capability has real-client evidence.

---

## 12. One-message kickoff for DeepSeek (optional; paste this and attach both plans)

> Continue the **existing** Imlight Shadow School implementation left unfinished by Opus 5.5, using the attached `Shadow_School_DeepSeek_V4.1_Flash_1017_Handoff.md` as your immediate execution contract and `Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md` as the authoritative final design. This is **NOT a new project**. Inspect Git diffs and the existing mods, preserve all uncommitted user changes and `*.shadowschool-backup` assets, and keep Vespera Quillmoor in Library Book Stacks. Your first coding task is to reproduce and fix the **valid level-5 Shadow Troll training failure**; confirm the client request, server authorization, known-spell update and relog persistence. Then continue in bounded tested slices through the remaining school mechanics and full Wizard City storyline, including Umbral Sight, eight-duel Ravenwood conquest, limited pact, aftermath and Krokotopia handoff. No Arc 2 implementation, no rewriting working systems, no invented IDs or animations, no global quest changes, no false validation claims. Maintain `continuation-status.md` so later fresh sessions can resume without repeating research. Implement now, reporting only verifiable progress.
