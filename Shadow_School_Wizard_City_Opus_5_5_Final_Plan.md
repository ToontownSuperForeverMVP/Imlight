# SHADOW SCHOOL: WIZARD CITY
## Final implementation specification for Opus 5.5
**Project:** An eighth starting school, a fully adapted Wizard City campaign, and the foundation for Morganthe's future apprentice  
**Release:** 1.0 / Wizard City  
**Status:** Implementation plan, not a claim of completed client compatibility  
**Primary reference:** *Shadow School: Wizard City and the foundations of the Queen's apprentice* (user-supplied revised plan, 22 October 2026)

> **Directive to Opus 5.5:** Implement the COMPLETE Wizard City release, including the playable school, character creation, trainers, spell and equipment progression through level 50, existing main-quest adaptations, one substantial Ravenwood conquest finale, personal aftermath, and successful Krokotopia handoff. Do not implement later-world quest rewrites in this release. Preserve the inspected game's source, asset, quest, and protocol realities. Prove each capability in a running client and server. Do not replace delivery with scaffolding, fake screenshots, tests alone, or an ambitious roadmap.

---

# 1. Vision, scope, and non-negotiables

## 1.1 The player fantasy

A wizard born with an affinity for Shadow enters Ravenwood under provisional supervision. They genuinely help Wizard City, increasingly resent institutional restrictions on knowledge, discover an anomalous ability through Ambrose's Nightside enchantment, and use careful preparation to seize Ravenwood's defensive bindings. After defeating Ravenwood's representatives in constrained, nonlethal duels, they impose a limited pact. They leave to pursue Malistaire as an **independent Shadow wizard**, motivated by self-preservation, ambition, and interest in forbidden knowledge. Across Arc 1, those achievements and abilities eventually attract Morganthe's attention. In Celestia, after an actual evaluation, she can recruit them.

This is a coherent alternate-history campaign, **not** a canon claim that Shadow was always an eighth primary school. The existing seven schools and their campaigns remain intact.

**Macro progression:** Unlisted/Provisional Student → Ravenwood Conqueror → Independent Shadow Wizard → Queen's Candidate → Morganthe's Apprentice. Release 1 implements the first two stages and sets up the third. Do not grant candidate/apprentice status in Wizard City.

## 1.2 Release 1 MUST ship

1. Real eighth Shadow selection at character creation, including persistence, stats, school identity, spellbook/decks, social displays, login, and relog.
2. A specialist trainer available immediately, fourteen trainable spells through level 50 (including the two level-one spells), combat-safe Shadow Pip behavior, school-specific health progression, usable gear, decks, and vendors.
3. All of Wizard City's **existing required main quest progression**, preserving original quest IDs/goals/rewards except for the Shadow enrollment branch and the new finale's own objectives.
4. Player-specific Shadow dialogue in meaningful original quest encounters, not a second parallel twenty-four-quest campaign.
5. Persistent, story-only Umbral Sight beginning at the proper Nightside reveal, with clear limitations and a concrete role in the finale.
6. One dedicated, stage-based Ravenwood finale quest with eight named duels, distinct NPC motivations and mechanics, saved checkpoints, restrained narrative power scaling, and a three-rights pact.
7. Personal, persistent aftermath in Ravenwood and a credible, unblocked Krokotopia transition.
8. Preservation of all vanilla school choices, quests, services, audio/visual assets, original enemy spells, player saves, and mixed-party behavior.
9. End-to-end proof in the **actual** Imlight client and server, including a fresh character and repeatable test evidence.

## 1.3 Strict scope boundaries

- No implementation of Krokotopia, Marleybone, MooShu, Dragonspyre, Celestia, Zafaria, Avalon, Azteca, or Khrysalis story changes yet. Their narrative contracts are specified for later work.
- No replacement art, custom animated spells, synthesized sound, recoloring, or new 3D assets. Reuse installed Shadow school graphics, existing spell donors and cinematic assets, NPC models, effects, UI resources, props, sounds, and interiors.
- No unconditional global NPC/dialogue mutation. Other schools must experience the normal game.
- No premature ability to decode arbitrary magic, no omnipotent pact, no immediate fealty to Morganthe, and no early Dragon Titan spoilers.
- No spell training gated by story flags. Shadow school identity and story allegiance are different.
- **No invented wizard dialogue boxes.** The ordinary Wizard101 player character is largely silent in main quest dialogue. Tell the story through NPC dialogue, quest text, visible actions and responses unless an actual player-choice dialogue UI is verified and implemented. Any lines attributed to the Wizard in earlier notes are *writing direction*, not literal voiced dialogue.

## 1.4 Quality tiers for prioritization

- **P0: Ship blockers:** creation, data compatibility, core spells, enrollment, quest graph, finale, pact, travel, save safety.
- **P1: High-quality world adaptation:** major NPC voice-consistent dialogue; three-street order-aware reactions; tutor scenes; personalized finale mechanics; aftermath; world services; optional-zone continuity.
- **P2: Nice-to-have polish:** extra conditional side-quest one-liners, atmospheric text or effects where existing client assets allow them, badges/titles if fully supported, minor post-pact responses, extensive optional-zone coverage.

If time or engine support is limited, **never sacrifice P0 for P2**. Record all incomplete optional content honestly.

---

# 2. Canon, source-of-truth, and timeline rules

The user's revised plan reports direct inspection of local Imlight quest templates, English dialogue, trainer inventories, training and combat code, character creation, WAD indexes, spell and cinematic templates. **Treat those inspected installed resources as the implementation source of truth**. Public guides identify story beats but are not safe replacements for the installed client's exact IDs or dialog payloads. Opus must reconfirm the actual hashes, IDs, layouts, indices, status handlers, dependencies, assets, and binary revision before patching.

Grounded guideposts:
- Official Wizard101 player guidance describes Shadow as a later learned magic rather than a primary school; this mod deliberately diverges from that rule: https://www.wizard101.com/home5/w101playersguide/page_8ad6a412422cbe0b01425dc4fe846009
- Wizard City current quest ordering/three-street layout: https://finalbastion.com/wizard101-guides/w101-quest-guides/main-quest-line-wizard-city/
- Arthur Wethersfield is a **Balance instructor** in Ravenwood, while the principal Balance teacher is Alhazred in Krokotopia: https://eu.wizard101.com/game/balance-school
- Shadow Pip, transformation, and Pact mechanics differ across versions. The installed revision, not an outdated wiki summary, controls which effects and UI elements work: https://eu.wizard101.com/game/updatenotes/darkmoor
- Morganthe's Celestia motive concerns Astral secrets, but the future claim that Umbral Sight is uniquely useful to her is **our original story inference**: https://akamaieu.wizard101.com/game/worlds/celestia
- Arc 2's eventual Khrysalis climax concerns the Song of Creation and rewriting the Spiral: https://akamaieu.wizard101.com/game/worlds/khrysalis

**Knowledge embargoes:**
- Wizard City characters may know Malistaire stole Bartleby's Eye, displaced the Death School, threatened Wizard City, and seeks the Order of the Fang; they do not know his Dragon Titan objective yet.
- Do not reveal Morganthe's specific future plans, the full Shadow Web command chain, or Celestia's significance prematurely.
- The tutor may know an incomplete account of Ambrose's prior student, but cannot truthfully claim Morganthe ordered any Wizard City events.
- Ambrose is concerned about genuine risks and coerced control, not because the writing needs a cartoon villain.
- Morganthe's expulsion has complicated historical causes; preserve the installed account of the dangerous Astral experiment and refusal of responsibility. No character's partisan telling becomes omniscient truth.
- Shadow's mere existence does not guarantee immunity to enemy Shadow attacks or automatic membership of the Shadow Web.

---

# 3. Dramatic structure: four acts inside Wizard City

## Act I: The provisional student (Tutorial → Enrollment)

**Emotional question:** Can an unusual student belong at Ravenwood?  
**Player behavior:** Helps people genuinely, obeys immediate safety directions, shows unusual Shadow affinity without possessing mastered Umbral Sight.  
**Ambrose:** Cautious but not hostile.  
**Tutor:** An independent specialist, not a secret recruiter.  
**Result:** Legitimate registration and an accessible mentor, with a restrained hint that Shadow doesn't fit Ravenwood's curriculum.

## Act II: The capable outsider (Three Streets, any order)

**Emotional question:** If Ravenwood can judge your magic, should it get to control it?  
**Player behavior:** Rescues threatened students, cures magical afflictions, stops Malistaire's operatives, and notices how authority and magical binding work.  
**Three themes:** Triton = forbidden knowledge and Malistaire's influence; Firecat = misuse versus cure; Cyclops = coercion and command.  
**Result:** Player earns credibility as a defender and acquires personal reasons to distrust parts of the institution.

## Act III: Knowledge with a cost (Foulgaze → Nightshade → Nightside)

**Emotional question:** Is an unstable gift better suppressed or understood?  
**Player behavior:** Completes ordinary investigations. Ambrose's **existing** temporary ghostly-sight enchantment interacts abnormally with their native Shadow affinity.  
**Result:** Umbral Sight persists. It has initially distressing or disorienting story effects and can identify designated magic traces, but not automatically break wards.

## Act IV: The unbound student (Finale → Krokotopia)

**Emotional question:** How far will this wizard go to control their future?  
**Trigger:** The player seeks to study and stabilize the sight; Ambrose wants it contained and imposes supervision. Their negotiations fail after the player chooses to exploit Ravenwood's binding structure. Neither party should begin the scene wanting a fight.  
**Player behavior:** Prepares a constrained magical ritual, defeats eight officials/representatives in nonlethal private duels, and forces a limited pact.  
**Result:** The student is neither forgiven nor banished. They depart as an independent and capable wizard, while Ravenwood has a continuing reason to distrust them.

**Crucial motivation:** The protagonist should not suddenly become bloodthirsty because the finale quest began. Each step is escalation from conflict, curiosity, refusal of supervised containment, deliberate preparation, and finally coercion. Victory has moral consequences even when residents continue using Ravenwood services.

---

# 4. Shadow school from character creation

## 4.1 Eighth-school onboarding

- Use the existing Shadow school hash/identity and resource indices; do not invent a competing school constant when a canonical client/server representation exists.
- Installed character creation currently has seven enumerated schools and named native selection handlers; the XML/config addition alone is **not** sufficient evidence that eighth selection works. Implement/verify the native dispatch adapter for the exact installed client revision.
- Present Shadow as **an alternate story route with future antagonistic actions** before final confirmation. Use concise school description and explicit warning about Ravenwood consequences; avoid prematurely promising Morganthe's apprenticeship as an in-world fact.
- The eighth button must be navigable by mouse and relevant input modes, highlight properly, occupy safe layout coordinates across supported resolutions, select the correct icon/description, and survive Back/Next/Confirm navigation.
- Reuse existing Shadow school art and visible spell symbols. The UI must not display blank art, guessed placeholders, another school's card face, or a seventh-school fallback after selecting Shadow.
- Newly created Shadow characters start level 1. Do not retroactively rewrite old characters or force-migrate other school IDs.
- Initialize starting health/stats, spell/deck and equipment correctly at creation, then test death/revive, relog, pet, mount, bank, shared housing, social and school markers, and fresh zone transitions.
- Confirm every relevant server-side school whitelist: character-create validator, identity display, school quests, trainer filtering, pet stats, drops/vendors, cross-school damage/resist, deck/equip prerequisites, spells/pips, party, and persistence.

## 4.2 The Unlisted Tutor: presentation and identity

Use an original character name selected once and consistently. Before writing dialogue, inspect available NPC models, portraits, voice/silence behavior, indoor locations, and interactable trainer assets. A quality alternative if no unique NPC name can be displayed reliably is **The Unlisted Tutor** as a persistent in-world title.

The tutor's existing-room location must be accessible **from the initial Ravenwood enrollment onward**, independent of any street, with an obvious locator or landmark. The student should have an in-world reason to locate them after Mr. Lincoln's registration.

Personality:
- Inquisitive, practical, restrained; morally fallible without being a secretly omniscient schemer.
- Disagrees with excessive restrictions but believes Shadow is risky and discipline matters.
- Knows fragmentary records about an earlier student of Ambrose and may interpret them too sympathetically.
- Teaches the normal Shadow curriculum. Training availability does **not** change with conquest or future allegiance.
- **Does not propose the conquest at enrollment.** After Nightside, discusses how to *understand* Umbral Sight; the player decides to weaponize it.
- Reacts to the pact with mixed feelings. They are impressed by ingenuity but disturbed by coercion. Do not turn them into a cheerleader for defeating their colleagues.
- Their presence is a continuous narrative anchor: enrollment → three-street check-in(s) → sight → conflict → departure. Keep scenes brief and skippable where appropriate.

## 4.3 Prevent awkward enrollment behavior

A Shadow-specific “To Ravenwood!” variant should retain the same core four interactions of the installed route (registration, school/tutor introduction, registration closeout, Ambrose handoff), housing/dorm access and services, XP/rewards, and follow-on prerequisites. It must register as a valid predecessor of “Panic in Three Streets!” without altering what completing any other school enrollment does.

No mentor questline that must be completed before unlocking a chosen street; all three routes stay open and independently completable.

---

# 5. Full Wizard City main-quest adaptation matrix

**General rule:** Keep the installed quest IDs, objective types, targets, XP, rewards, and completion flags unless an exception is named. Adapt only appropriate offer text, dialogue, active journal, hints, and turn-in text for the Shadow character. The following quest titles are *human-readable anchors*; Opus must match the installed template version and not create IDs based on strings. 'Retain' means deliberately preserve rather than forcing a gratuitous Shadow rewrite.

## 5.1 Tutorial, Unicorn Way, and school enrollment

| Installed/mainline quest anchor | Required Shadow treatment |
|---|---|
| Tutorial / initial encounter | One restrained observation by Ambrose that Shadow affinity is unfamiliar. Keep combat tutorial and rescue intact; don't imply active Umbral Sight. Verify tutorial damage and targeting with the Shadow starter spell. |
| Unicorn Way | Standard rescue/investigation. No unearned villain reputation. |
| Ghost Hunters | Retain ghost investigation; introduce sensitivity to unusual magic as observation only, not reading hidden seals. |
| Skeleton Crew | Retain defeated skeleton objective and consequences. Player is helping the district. |
| Monsters and Mazes | Retain exploration and quest logic; avoid adding a Shadow-only detour. |
| Finding Out Why | A line may frame the player's curiosity about why magic can compel the dead; never reward unearned knowledge. |
| Who Could It Be Now? | Preserve clue handoff and urgency; no forecast of the full Malistaire plot. |
| Sinister Skeleton / Rattlebones | Boss remains hostile; victory is evidence of talent, not instant mastery of Shadow. |
| Grim Tales | Harold/Ambrose research foreshadows restricted records; no implied account of Morganthe beyond what the character can know. |
| To Ravenwood! (Shadow variant) | Provisional registration, tour/instructor introduction, tutor access, dormitory and normal rewards; strong distinct school identity. |
| Panic in Three Streets! | Predecessor join accepts all seven original variants plus Shadow. Sergeant Muldoon's route remains the same. |
| Secret Strife of Pets (if included in inspected version) | Preserve pet tutorial, handoffs, rewards, unlocks, and access. No unrelated Shadow story detour. |

## 5.2 Triton Avenue: knowledge and its misuse

| Quest anchor | Treatment |
|---|---|
| Trouble on Triton Avenue | Character treats the threat seriously despite disliking institutional restrictions. |
| Menacing Minions | Retain combat and rescue urgency. |
| The Shadow of Death Magic | Duncan's admiration for Malistaire becomes a nuanced counterpoint: the Shadow wizard can be curious about forbidden study **without** accepting Malistaire's treatment of people. Preserve Suzie's wand retrieval. |
| Quest for Clues | Existing clue work grounds the player's interest in how magical commands propagate. |
| Catching a Glimpse | Don't turn ordinary investigation into full Umbral Sight before Nightside. |
| Clear as Crystal | Crystal/machinery clues provide **ordinary** experience with magical apparatus, not a new permanent ability. |
| Luminate | Duncan pushes his interpretation of Ravenwood. Allow NPCs to disagree; don't make Duncan automatically a Shadow ally. |
| The Enemy Revealed | Keep Harvest Lord hostile and Suzie's rescue integral. Boss's control of minions is an example of dangerous coercion. |
| To Tame a Tempest | Balestrom appreciates the rescue but cautions the wizard about confusing power with control. This creates a payoff when he later opposes them. |
| Triton Report | Muldoon recognizes the player solved the crisis, giving their eventual betrayal real weight. |
| Calming the Chaos (where installed) | Preserve original closure; do not replace with a new Shadow victory flag. |

## 5.3 Firecat Alley: the difference between cure and domination

| Quest anchor | Treatment |
|---|---|
| Putting Out the Fire! | Immediate concern for residents and afflicted Fire Elves. |
| She Ran Crying Wildfire | Preserve investigation; distinguish suffering from evil intent. |
| Which Hunt? | Explore Gretta's position and why institutional judgments can be incomplete; verify exact installed dialogue before assigning blame. |
| 100% Not That Witch | Do not replace the existing culprit or absolve the player from helping. |
| Which Craft? | Highlight research and preparation as alternatives to brute force. |
| The Cure | Gretta's cure remains the moral victory. The Shadow wizard learns that **reversing** an enchantment is different from **exploiting** it. |
| Firecat's In the Bag | Quinn/Muldoon reward the service, not an invented conquest objective. |

## 5.4 Cyclops Lane: coercion and authority

| Quest anchor | Treatment |
|---|---|
| A Good Day to Cyclops | Preserve missing students and local urgency. |
| A Stroll With Trolls | Preserve main combat beat. |
| Run and Done | Cyrus remains severe, personally testing discipline and qualifications. He must not preemptively know about the later pact. |
| The Cave Escape | Keep captured students and dispelled bindings as the concrete lesson that authority can be magically imposed and undone. |
| Oh Me, Oh Minotaur | Keep the rescue/cyclops dispute intact. |
| Akilles' Deal | Preserve Akilles and his crystal; the lesson is that control can be broken with knowledge and preparation, *not* that the wizard can dominate anyone yet. |

## 5.5 Reunion, Nightshade, Nightside, departure

| Quest anchor | Treatment |
|---|---|
| A Brief History of the Spiral | Only after all three streets. Preserve Bartleby's failing memory, missing Eye of History, Foulgaze, and leads about Order of the Fang. Three-street references must be conditional on actual completion. |
| Throwing Nightshade | Keep Lord Nightshade hostile. He represents crude dominion that the player finds ineffective or distasteful; the player still defeats him. |
| Things and Stuff | Preserve **Daisy Willowmancer's** actual role, item investigation and handoffs. No reversion to older Ambrose-only quest history. |
| Take a Walk on the Nightside | Ambrose bestows the **existing temporary ghostly-sight spell** at its legitimate story point. Visit Mortis, Dworgyn and displaced Death School as normal. On the way back, the Shadow wizard's vision unexpectedly persists. This is the *first* active Umbral Sight beat. |
| NEW: The Unbound Student (one finale quest; working title) | The player explores unstable sight, rejects restrictive containment, prepares Ravenwood bindings, duels representatives in private instances, bargains with Ambrose, signs the pact. |
| A Whole New World | Gate only the Shadow departure **interaction** on finale/pact completion. Preserve original key/travel prerequisites, Bartleby, Krokotopia arrival objectives and other-school conditions. Change Shadow motivation only. |

**Do not add the finale as an accidental global prequest on the shared world transition.** For Shadow use a personal conditional requirement; other seven schools must be unchanged. Check quest offer, completion, transition, teleport, instance portal, and any alternate access path.

## 5.6 Branch-state correctness

The three main streets can be completed in all six orders. The tutor and central characters should have short order-independent reactions to each completed street, plus an optional combined reaction once all three are complete. Never mention a rescue, professor encounter, or completed cure the character has not performed. Do not make quest adaptation contingent on side quests or an unvisited tutor conversation. Resume missed tutor commentary safely through a generic catch-up line rather than blocking main progression.

---

# 6. Umbral Sight: a restrained story mechanic

## 6.1 Origin and stages

1. **Before Nightside:** Native Shadow affinity exists but the character does *not* possess the active magic-reading sight. Foreshadow it with cautious observations and unusual reactions, not ghost outlines or anchor hints.
2. **Nightside:** Ambrose's own temporary ghostly-sight enchantment is applied as usual.
3. **Return:** The effect persists after its expected expiration. The player notices disorientation, strange lines or recurring residual traces **using existing assets/text only**.
4. **Examination:** Ambrose argues for protective containment. Tutor explains plausible ways to interpret traces but warns that identification isn't mastery.
5. **Preparation:** The player identifies known defensive bindings in Ravenwood, then must perform specific ordinary interactions to prepare them. They cannot read every secret in the Spiral.
6. **After pact:** Sight remains an ability to recognize selected historical/magical anchors, not a hidden permanent stat buff or a universal skeleton key. Its expanded importance belongs to future worlds.

## 6.2 Implementation constraints

- Store only necessary activation and stage facts; derive the rest from existing quest objectives where possible.
- Per-character triggers and interaction authorization. A non-Shadow character entering Ravenwood should see no binding targets.
- Reuse installed ghostly sight visuals, interaction markers, text, particles, camera cues or UI feedback *only when verified present*. If a fancy screen overlay cannot be activated reliably, use a subtle valid existing visual plus strong NPC/journal feedback, not a broken overlay.
- Mark every exposed interaction with the correct quest objective and idempotent server validation. Out-of-order interactions must fail cleanly.
- Never let sight substitute for a Spiral Key, bypass locked zone access, grant invisible combat bonuses, or reveal the full Shadow Web.

---

# 7. One substantial finale: **The Unbound Student**

## 7.1 Why this quest exists

A high-quality Shadow story cannot jump directly from discovering Nightside to casually assaulting the faculty. The quest must earn the turn and show the cost. Use **one** persistent quest with multiple staged objectives; do not create twenty-four unrelated filler quests or require a special world map.

## 7.2 Scene and objective order

**Scene 1 — Afterimage:** After normal Nightside completion, a Shadow-only return interaction reveals that the ghostly effect did not fade. Tutor gives context but no immediate battle order. Ambrose seeks examination for everyone's safety.

**Scene 2 — The proposed remedy:** Ambrose offers supervised study or temporary containment and is sincere. The player refuses the restrictive terms (portrayed through actions, journal, and NPC reactions; no fake player voice line). Ambrose states the boundaries of continued study. This supplies cause and tension without turning him irrational.

**Scene 3 — The historical parallel:** The tutor cautiously shares incomplete records of an earlier extraordinary Ravenwood student whose experiment endangered others. This gives a **perspective**, not an objective verdict that Morganthe was innocent. The tutor warns that trying to seize academy wards would be dangerous.

**Scene 4 — The choice made:** The player deliberately starts investigating Ravenwood's defensive bindings. The quest journal makes explicit that **the wizard chooses coercion**; the tutor neither commands nor secretly rigs the plan for Morganthe.

**Scene 5 — Binding preparation:** Umbral Sight identifies a limited set of exposed anchors. Require a small number of well-telegraphed interactions using verified existing props/markers; three staged binding interactions are recommended if the assets support them (e.g. Elemental, Spirit, Threshold). They are functional categories, **not new canonical school lore**. If the installed assets cannot support three stable unique anchors, collapse to one verified anchor and use three sequential interactions. Preserve the original plan's single persistent quest either way.

**Scene 6 — Faculty confrontation:** Eight representative duels occur privately, nonlethally, using existing spell/cinematic assets. Explain that the prepared ritual temporarily restricts the bindings through which the opponents can access their full power; the player has *not* surpassed every professor in unrestricted combat.

**Scene 7 — Ambrose:** Ambrose resists because the wizard's methods are a genuine threat. He recognizes a recurring pattern from his former student's history; acknowledges that he may have failed as a teacher while rejecting coercion. His private duel is both the technical and emotional climax.

**Scene 8 — The negotiated pact:** Victory forces a narrow, enforceable agreement. The tutor and representatives react differently. The wizard has achieved academic independence at a moral cost, not mind control or absolute rule.

**Scene 9 — Departure:** The existing departure route opens. Ambrose cooperates with access to the Spiral under the pact's terms but does **not** endorse the player's actions. The next motive is investigating Malistaire, preventing him from jeopardizing Ravenwood, and reaching forbidden knowledge before he does.

## 7.3 Why each representative fights

All eight encounters must feel like different *people*. **Arthur is an instructor, Malorn is the interim Death instructor, and neither should be called a full professor.** Their public service NPCs remain intact.

| Stage | Opponent | Individual position | Combat identity using existing systems |
|---|---|---|---|
| 1 | Dalia Falmea | Talent without discipline burns others. She previously appreciated the player's Firecat rescue and now feels betrayed. | Aggressive Fire damage-over-time and offensive tempo. |
| 2 | Lydia Greyrose | Asks the wizard to reconsider before others get hurt. She defends the academy out of care, not anger. | Defensive shields, moderate damage, controlled freezing if installed capabilities allow. |
| 3 | Halston Balestrom | Remembers the Triton rescue; worried the wizard now mistakes domination for cleverness. | Storm pressure/accuracy and tempo. |
| 4 | Cyrus Drake | Respects calculated preparation but rejects abusing students or institutions. His brother Malistaire is a warning, not an excuse. | Myth traps and minions **only if supported**; otherwise dual-strike tactics. |
| 5 | Moolinda Wu | Argues the people whom Ravenwood protects are more important than anyone's freedom to control them. | Heals, shields, survivability. Do not write her as eager to injure a child. |
| 6 | Malorn Ashthorn | An interim instructor with first-hand knowledge of Malistaire's shadow over the Death School; he is not blindly loyal to either side. | Death-drain/sustain, modest by design; don't upstage faculty. |
| 7 | Arthur Wethersfield | The pragmatic Balance instructor insists that a coerced agreement is not a balanced one. | Weakness, precision debuffs, mixed-school pressure if supported. |
| 8 | Merle Ambrose | Knows this route is dangerous and recalls Morganthe's case. Genuine regret does not mean surrender. | Wards/control, phase-dependent defense **only if supported** by existing AI/effects. Otherwise a tested constrained duel with appropriate presentation. |

Why no Dworgyn? He remains at the displaced Death School in Nightside, not a resident teacher positioned inside the Ravenwood defensive circuit. He should have a thoughtful optional reaction, not be inserted as an arbitrary ninth duel. Bartleby must not be chained, harmed, or treated as a school administrator under the pact. Gamma can express concern through supported dialogue but need not become a boss.

## 7.4 Fight pacing and fairness

- This finale is an extraordinary set-piece for a relatively low-level Wizard City character. The binding ritual is **not optional hand-waving**: communicate before combat what is suppressed, why the combatants retain agency, and why the player can survive.
- Design fights as tactical demonstrations, not eight repetitive health sponges. Target roughly a few meaningful turns per representative at expected levels, adjusting for gear/pets/solo and a player who levels through side quests; record actual solo turn counts instead of assuming them.
- Distinguish at least three encounter behaviors: damage pressure, control/defense, resource/sustain. Prefer supported existing spell/AI data over brand-new boss scripts.
- No hard requirement to win eight fights in one sitting. Save each victory and activation in atomic, versioned state. On retry/relog, offer a coherent resume line and direct the player to the next living objective.
- Match boss difficulty to the **quest owner's** level/equipment expectations with bounded scaling; guard against abusive overleveling or joining an inappropriate instance. Do not scale other schools' NPC services.
- Give clear quest guidance between duels. Do not silently teleport the player through nonrecoverable points.
- Co-op can help in combat, but the owner must qualify, activate bindings, and receive conquest credit. Helper wizards must not inherit or accidentally advance personal Shadow progress.
- Fights are **nonlethal private manifestations/duels**. Public teachers still teach, shops still work, and no global actor is despawned.
- Prefer a single final quest completion reward, not eight farmable progress payouts. Repeated victory packets must not duplicate XP, gear, titles, story rights, or quest credit.

## 7.5 The pact, precisely

**Right 1 — Freedom of study:** Ravenwood cannot forbid this wizard's Shadow research or seize it merely because it is Shadow. The wizard is still accountable for harming others.  
**Right 2 — Access to the Spiral:** Ravenwood's ordinary services and lawful access routes remain open. No free Spiral Keys, travel skips or personal immunity from quest prerequisites.  
**Right 3 — Recognition of limited authority:** The faculty recognizes the player's captured control over the **specific defensive bindings prepared during this ritual**. They retain free will, jobs, powers outside those bindings, and a right to dissent.

**Not granted:** sovereignty over Wizard City, mastery over Bartleby, permanent professor enslavement, control of all spells in the Spiral, invulnerability, the ability to order innocent NPCs around, authority over other players, or allegiance to Morganthe.

The pact is a coerced compromise, not friendship. Its constraints keep later Ravenwood dialogue, Ambrose's world access, and multiplayer believable.

## 7.6 Character-specific aftermath

Build three presentation states where relevant: **before Nightside**, **sight revealed / finale active**, **pact completed**. At minimum, add post-pact dialogue to:

| NPC | Post-pact tone / functional requirement |
|---|---|
| Ambrose | Wary, dignified, cooperative only where pact requires; Krokotopia information and travel never blocked after valid completion. |
| Falmea | Firm, disappointed, still performs any trainer/service role. |
| Greyrose | Concern for player's future; does not deny ordinary services. |
| Balestrom | Disappointed but still respects the wizard's prior rescue. |
| Cyrus | Severe, intellectually engaged, never suddenly sycophantic. |
| Moolinda | Compassionate, not approving; willing to help prevent future harm. |
| Malorn | Especially unsettled by the parallel with Malistaire. |
| Arthur | Disapproves of coercion but insists agreed rules be followed. |
| Unlisted Tutor | Admiration mixed with disquiet; says this achievement carries real responsibility. |
| Mr. Lincoln | Administrative professionalism; school records remain correct. |
| Gamma | If a suitable interaction exists, reacts to Ambrose's decision and danger rather than behaving as comic relief. |
| Dworgyn | Optional Nightside acknowledgment of the strange situation, avoiding the misconception that he fought in Ravenwood. |
| Bartleby | Still injured/affected by Eye of History plot. No pact control. Normal keys and access. |

Store/derive state **per character**, not global. A Death wizard standing next to a Shadow conqueror continues to receive their own legitimate vanilla dialogue. In a private Shadow-owned finale instance, any helpers see the owner's coherent scene state, not individually conflicting cutscene variants. Services, housing, banking, Bazaar, pet activities, gardening and ordinary quest availability remain usable.

## 7.7 Suggested original dialogue style (examples, not locked strings)

All lines below are new creative material. Use them as tonal anchors; align any final written lines with actual scene lengths and dialogue UI constraints:

- **Early Ambrose:** “Your magic is unfamiliar, young wizard. That is a reason for caution, not for fear. We shall find a way to teach you.”
- **Tutor at enrollment:** “There is no eighth tower in Ravenwood. Perhaps one day there will be. For now, we'll use the room nobody thought to list.”
- **Duncan after investigation:** “They call every question dangerous once they have decided they dislike the answer.” *(Duncan's opinion, not narrator truth.)*
- **Falmea after Firecat:** “You helped them when they needed it. Remember that the measure of power is not merely whether it works.”
- **Tutor after Nightside:** “I can show you how to recognize a binding. I cannot promise you'll like what happens when you try to unmake one.”
- **Ambrose at failed negotiation:** “I will not punish you for what you are. I will stop you if what you do endangers those in my care.”
- **Greyrose before duel:** “You already know how to save people. Must you also learn how to frighten them?”
- **Cyrus before duel:** “A clever plan is still a foolish plan if you refuse to count its cost.”
- **Ambrose before last duel:** “Once before, I mistook warning a brilliant student for teaching one. I have carried that failure for years. But regret does not oblige me to surrender Ravenwood.”
- **Tutor after pact:** “You won the right to learn. You did not win the right to call every consequence unjust.”
- **Ambrose at Krokotopia handoff:** “Malistaire remains a danger to this school, and to you. You have the right to go. I pray you use it more wisely than the right you took here.”

Avoid literal protagonist dialogue unless the client supports real choices. Avoid excessive lore dumps, modern meme speech, villain cackling, or character assassination.

---

# 8. Optional Wizard City content: a deliberate polish layer

Wizard City is bigger than its mandatory main route. Some installed versions support optional **Colossus Boulevard, Sunken City, Crab Alley, Golem Court, Pet Pavilion**, plus side quests, housing, shopping, and later-level Wizard City locations. These must **not** become mandatory for Krokotopia or the pact. Verify that each area/quest actually exists in this Imlight content revision before targeting it.

| Optional area/system | Shadow enrichment | In-scope priority |
|---|---|---|
| Colossus Boulevard | Short quest-dependent dialogue about who gives permission to intervene and whether protection requires obedience. Keep original quests/objectives. | P1 if content exists |
| Sunken City | Existing undead and magical wards let the tutor/Nightside figures comment on limits of Shadow knowledge. Never pre-unlock Umbral Sight; only reactive text after Nightside. | P1 if content exists |
| Crab Alley / Deep Warrens | If visited, the wizard can comment through NPC responses on fixing magical infrastructure rather than commanding it. No required faction swap. | P2 |
| Golem Court / Golem Tower | Good optional combat-balance benchmark for an early Shadow deck; use existing tower and rewards. | P1 tests, P2 dialogue |
| Pet Pavilion and pet tutorials | Verify that Shadow characters can use pets and pet-based spells without school rejection. Preserve ordinary onboarding. | P0 compatibility |
| Shopping District, Bazaar, housing/dorm | School-filtered goods, school-specific equip rules, residence access, item vendors, banking and inventory must not break on an eighth school. | P0 compatibility |
| Side quests on Three Streets | High-impact characters may have a single post-rescue or post-pact line when already present. No new prerequisite chains. | P2 |
| Existing Wizard City badges/titles | Retain original achievements. Add a non-stat conquest title only if title localization, client display and persistence can be proven. | P2 |
| High-level / later-added Wizard City content | Do not pretend its story already recognizes conquest. Maintain compatibility and reserve future conditional dialogue hooks. | P0 compatibility, narrative adaptation later |

**Avoid optional-scope inflation:** Do not add a secret Shadow world, custom Shadow tower exterior, eight side dungeons, new spell animations, a global Ravenwood visual takeover, or optional content that has to be completed before the main finale. A seamless original-world feeling is more valuable than dozens of half-working additions.

---

# 9. Mechanical Shadow school specification (preserve the agreed curriculum)

## 9.1 Acquisition rules

- Start with **two** spells at level one. They count among **fourteen** spells trained through level 50.
- Use existing trainer infrastructure, one consistent server-authoritative eligibility calculation for UI offers *and* ReceiveTrain (level, previous spells, curriculum rights, free primary-school cost, already learned).
- Shadow primary-school spells cost **zero** training points; existing secondary-school training still behaves normally.
- No conquest/Umbral Sight/Morganthe quest unlock for ordinary Shadow curriculum. A level-eligible wizard can train normally regardless of story stage.
- Keep ordinary starting healing access and normal basic combat/pet features.
- Any old test trainer that grants powerful spells at level 1 remains behind explicit testing access; acceptance testing must disable randomizer/no-level bypass overrides.
- Use **separate player variants** of each installed donor spell. Never modify donor IDs or enemy spell behavior globally.

## 9.2 Agreed level 1–50 curriculum

These are the user's selected starting values, **not verified final balance**. Installed donor resources are the authoritative asset source.

| Level | Player variant (installed donor) | Cost | Starting behavior |
|---:|---|---|---|
| 1 | Dark Sprite — Shadow (Dark Sprite–Shadowbones) | 1 normal pip | 75–95 Shadow damage. |
| 1 | Dark Empower — Novice (Dark Empower) | 1 shadow pip | Gain 1 ordinary pip, **not** a Power Pip. |
| 5 | Death Troll — Shadow (Troll Death–Shadow) | 2 normal pips | 160–200 Shadow damage. |
| 8 | Tower Shield — Shadow (Shadow Tower Shield variant) | 0 | -40% next incoming damaging hit; remove protected status. |
| 10 | Banshee — Shadow (Banshee Shadow) | 3 normal pips | 245–285 Shadow damage; remove donor's extra trap. |
| 16 | Shadow Trap (Shadow Trap donor) | 0 | +25% next incoming Shadow attack. |
| 18 | Shadowblade (Shadow Creature Shadow Blade donor) | 0 | +30% next outgoing Shadow attack. |
| 22 | Humongofrog — Shadow (Shadow Humongofrog) | 4 normal pips | 265–325 Shadow damage to all enemies. |
| 26 | Helephant — Shadow (Helephant Shadow) | 4 normal pips | 345–405 Shadow damage to one enemy. |
| 30 | Shadowspear (Shadowspear donor) | 1 shadow pip | +10% armor piercing on next outgoing Shadow attack. |
| 33 | Minotaur — Shadow (Shadow Minotaur) | 5 normal pips | Two hits: 50, then 420 Shadow damage. |
| 38 | Storm Shark — Shadow (Shadow Storm Shark) | 3 normal + 1 shadow pip | 380–420 Shadow damage. |
| 42 | Triton — Shadow (Shadow Triton) | 6 normal pips | 610–650 Shadow damage. |
| 50 | Frost Giant — Shadow (Shadow Frost Giant) | 7 normal + 1 shadow pip | 475 Shadow damage to all enemies + 1-round stun. |

- Accuracy: 85% on attacks, except Triton and Frost Giant at 80%; utility 100% initially.
- Enforce targeting, damage school, prism/resistance interaction, blades/traps, stun immunity, two-hit ordering, AoE targeting and random effect bounds server-side.
- Recheck the unusual cost/damage relationships (e.g. four-pip Helephant versus six-pip Triton) in comparative tests, but don't silently redesign the selected spell list.
- Double-check whether 'gain one ordinary pip' at one shadow pip creates first-round resource exploits; it is intended as a tactical utility, not a free two-pip or endless chain.

## 9.3 Strict asset-reuse contract

For each donor-based spell preserve: card artwork reference + artwork index, spell base + cinematic references, models, animations, particles, cameras, sound, visible school symbols and card frames. Change only gameplay values, supported flags/restrictions and truthful display/localization text. Audit **nested** random effects, hits, targets, damage schools, protected flags and cinematic act assignments. Existing boss tuning cannot be copied unmodified into a player spell (e.g. donor Dark Sprite can be far stronger than the proposed level-one variant).

If a chosen donor's presentation fails end-to-end playback in this installed client, identify a compatible existing donor variant while preserving its intended identity or **report the limitation**; do not quietly render an incorrect model or invent a replacement animation.

## 9.4 Pips and progression

- Start battle with 1 shadow pip; maximum stored shadow pips = 1.
- Refill at **beginning of rounds 4, 7, 10, ...**; if full, drop the event. Do not turn unused refreshes into stored credits.
- Normal pips and Power Pips operate normally. Shadow-primary Power Pips have double value for Shadow-primary ordinary-pip spells. Shadow Pips are a separate currency; never silently substitute school pips.
- Verify UI display, first-turn hand/deck use, affordability, selection, spending, refund-on-invalid, double-cast protection, round transitions, reconnect, pip effects and co-op combat.
- Starting health curve: rounded-up midpoint of Fire and Myth at each level; use the game's shared mana, XP, training points, and normal Power Pip progression, with explicit level data instead of relying on the installed empty Shadow health curve.
- Give meaningful functional Shadow equipment/deck items through level 50 and vendor tiers every 5 levels using existing models and realistic bonuses. Validate character creation equipment, inventory, purchased items, school restrictions, item cards and duplicate stat application.
- Match solo PvE effectiveness to comparable schools. Do not grant innate piercing, critical, resist immunity, extra Power Pips, infinite Shadow Pips, or new combat advantages through Umbral Sight.
- PvP/inter-school interactions need a stated support policy: ideally fully correct, otherwise disabled for Shadow until tested rather than silently unfair. Normal social parties and PvE cooperation are mandatory.

---

# 10. Engineering architecture and compatibility

## 10.1 Before writing code: inspect and record

Produce a machine-readable inventory for this exact installed revision:
- Client executable hash/version and school-create/native dispatch resource offsets or hooks.
- Character creation config/layout/resources, localization and the Shadow school identity/index.
- Character creation packet shape, school validation, base stats, spellbook, gear and login serialization.
- Exact NPCs/quest templates and IDs for Shadow enrollment, all existing Wizard City beats, and Krokotopia handoff; dependency edges and any version-specific branching.
- Quest offer/turn-in/journal/localization/cinematic pathways.
- Trainer inventory format and ReceiveTrain validation path.
- All donor spell IDs, spell bases, cinematic IDs, artwork indices, audio/model/particle dependencies and supported effect types.
- Current SpiralLoader hook surface and any server/client bridge limitations.
- Normal-player and mixed-party assumptions; original-game baseline test saves.

Write actual verified facts into `docs/implementation-evidence.md`. Clearly label assumptions and blocked capabilities. Never assign guessed hashes, resource IDs, offsets or namespaces to real code.

## 10.2 Preferred package structure

Prefer a self-contained SpiralLoader mod with segmented modules (names illustrative only):

```
ShadowSchool/
  manifest + version/dependencies
  content/
    shadow-school-progression
    shadow-spells-and-donors
    shadow-gear-and-vendors
    shadow-trainer
    shadow-enrollment
    quest-presentation-overlays
    wizard-city-finale
    localization
  runtime/
    school-registration
    trainer-eligibility
    shadow-pip-controller
    story-state
    quest-presentation-resolver
    quest-instance-and-conquest-state
  client-adapter/
    exact-version eighth-school selector support
    installed assets/resources override maps
  tests/
    creation, spells, quests, migration, mixed parties, resume, regressions
  docs/
    install, compatibility, implementation-evidence, known-limitations, test-evidence
```

Actual locations depend on the real repo layout. **Do not claim SpiralLoader supports every hook simply because this structure is desirable.** Where the loader lacks a needed hook, use a small explicit, isolated, version-gated Imlight or client adapter patch with rollback, not undocumented edits scattered across the core.

## 10.3 Per-player presentation and authorization

Create a single resolver conceptually keyed by `(quest id, actor character id, school, quest stage, completed prerequisite set, pact state, locale)` where supported. Return new presentation objects rather than mutating cached quest templates. Apply consistently to offers, active dialogue, turn-in, hints/journal, tracker and objective completion messages. Avoid inconsistent UI where spoken text says one thing and journal says the opposite.

- Keep vanilla objectives and completed quest flags stable.
- Do not apply Shadow variants to non-Shadow characters or players who haven't reached the relevant stage.
- Make authored text unvoiced **and suppress inappropriate old recorded audio** when text and audio disagree. If audio suppression is unsupported, preserve the original line/audio in that scene rather than causing nonsensical mismatches, and flag the limitation.
- Validate client locale fallback, text encoding, long dialogue wrapping, special characters and content caching.
- Server revalidates trainer purchase, quest interaction, binding, entry, fight-win and pact completion; never trust a forged client request.
- Quest stages and rewards must be transactional/idempotent, safe across timeouts and reconnects.
- In shared locations, per-player dialogue can differ while object identity remains global. Private instance visuals follow the owner where party members observe one scene.

## 10.4 Story-state model

Separate four layers:

1. **School identity:** `Shadow`, immutable after normal character creation.
2. **Canonical quest progress:** unchanged existing IDs/flags and one new persistent finale quest.
3. **Local Shadow facts:** sight activated, anchor interactions, which representative duels are cleared, pact signed, content version.
4. **Later faction status:** neutral/independent/candidate/apprentice; reserved for later releases and **never prematurely set in Release 1**.

Prefer derived milestones. Example conceptual values (not API names):

```
provisional = completed(shadow_enrollment)
conqueror   = completed(shadow_finale) && pact_signed
independent = later_release_arc1_milestone
candidate   = later_release_celestia_evaluation
apprentice  = later_release_morganthe_induction
```

Version state so restart/resume/upgrade never duplicates rewards. If a character is mid-finale when a content version changes, either migrate exact stages safely or show a documented recovery path; do not silently reset or auto-award conquest.

## 10.5 Multiplayer and service safety

- Character A (Shadow, post-pact), B (Shadow, pre-finale), C (Fire) can coexist in one Ravenwood zone, use trainers, chat, teleport as allowed, and see the correct individual quest information.
- Each Shadow player's personal binding anchors appear/interact only when eligible. They cannot steal another player's finale credit.
- No global world takeover or server-wide replacement of professors. No permanent NPC hostility in public hub spaces.
- Helpers entering finale instances do not lose access to vanilla school NPCs afterward.
- Ordinary services, pet/housing/shops/bank/bazaar and trainer access are unaffected by narrative hostility.

## 10.6 Backups, rollback and deliverables

The package must verify exact client revision, server/mod dependency versions and asset hashes, then make backups of every changed client or content file. Do not overwrite unrelated user modifications, save files, configuration, generated Imcodec inputs, or upstream source commits. Provide a reversible installation with uninstall/restore instructions, manifest and compatibility matrix. Protect against partial deployment (server-only with missing client selection adapter, mismatched spell IDs, etc.) with clear fatal diagnostics rather than corrupted characters.

Deliver actual mod source, built package, client adapter where required, scripts/instructions, tests, evidence inventory, install/rollback documentation and an honest remaining-work report. Use reproducible builds wherever feasible.

---

# 11. Dialogue, UI, cinematic and narrative quality bar

## 11.1 Content-authoring workflow

For every adapted quest, write a compact row in a script manifest containing: verified quest ID, affected NPC, vanilla dialogue/key, Shadow condition, replacement text, voice handling, journal/hint replacement, expected objective state, and pre/post-pact applicability. Perform a lore/voice pass before installing. Proofread for character consistency, chronology, spelling and repeated lines.

**Minimum on-screen beats:** enrollment; at least one memorable moment on *each* street; recognition after street reunion; Nightside anomaly; tutor's warning; serious Ambrose negotiation; personalized confrontation introductions; pact; departure. More line coverage is welcome only when properly tested.

## 11.2 Production polish using existing assets

- Preserve cinematic camera transitions and animations that already fit. Reuse **actual compatible** gesture/portrait/camera resources where available, never assume a valid animation from a similar filename.
- No contradictory original voice playback under a modified subtitle.
- Give the tutor a discoverable map/quest marker if existing functionality permits; otherwise lead the wizard there with clear instructions and reusable map landmarks.
- Use stable existing FX for sight and binding feedback and avoid spam/flicker when entering/exiting a hub.
- Distinguish stage markers and quest titles enough that a player understands whether they are investigating, preparing, dueling, or bargaining.
- Keep NPC dialogue short enough for Wizard101 cadence; major scenes can have a few short steps, not 20 paragraphs of exposition in one box.
- A replayable tutor summary of the current Shadow status (if implemented) is a helpful optional feature, not a required new quest.

## 11.3 No false branching

There is **one chosen Shadow story** in Release 1. If a UI presents an option such as “accept containment” versus “conquer Ravenwood,” it must have a functional complete branch; otherwise do not fake a choice. The narrator may express tension through objectives, actions and NPC dialogue while the route remains linear. The player preview at character creation must communicate this premise honestly.

---

# 12. Detailed testing and acceptance matrix

## Milestone A — Integration proof (do this FIRST)

- [ ] Shadow button appears alongside seven originals; hover, focus, confirm, Back, and relaunch work with no wrong icon.
- [ ] Create level-1 Shadow wizard; card art, stats, school identity, starter gear, deck and spellbook persist after relog.
- [ ] Cast both starting spells in live combat with correct effects, card art, animation, audio, targeting and Pip cost.
- [ ] Show a legal tutor spell, enforce required level/prerequisite/ownership on both UI and server request, reject forged under-level purchase.
- [ ] Shadow “To Ravenwood!” enrolls correctly, opens trainer/housing, and leads to all three streets.
- [ ] One Fire, one Death and one Balance control character continue normal enrollment and progression.

## Milestone B — Complete mechanical school

- [ ] All fourteen spells unlock at the right levels, including at threshold and below threshold.
- [ ] Costs, school damage/resist, AoE, multiple hits, buffs, protected status, stuns, deterministic/random effect shapes all match player spec.
- [ ] Shadow Pip begins at one; exact 4/7/10 refill cadence; full-capacity refills discarded; normal/power pips correctly spent; first-round utility not exploitable.
- [ ] Server-and-client consistency after reconnect during battle and after unlock.
- [ ] Explicit health data and level 1–50 progression, gear tiers every five levels, school decks and vendor/accessory equip logic work.
- [ ] Golem Court and representative level-matched PvE comparisons do not show repeatable gross Shadow advantage; adjust numbers with documented reasons, not unannounced role changes.
- [ ] Existing enemy donor templates still cast unchanged; other school spells unaffected.

## Milestone C — Main Wizard City and narrative

- [ ] Actual client route: tutorial → Unicorn Way → Shadow enrollment → three streets → reunion → Foulgaze → Daisy/Nightshade → Nightside → one finale → pact → Krokotopia arrival.
- [ ] Each of the six street orders works, including skipping optional content and returning to tutor after multiple streets.
- [ ] Rescue/cure/student objectives remain intact; no premature Malistaire/Dragon Titan/Morganthe details.
- [ ] No Umbral Sight before the exact Nightside enchantment; persistent afterward and available only to eligible Shadow character.
- [ ] Prepared binding interactions cannot be triggered by other schools, out of stage, or twice for extra rewards.
- [ ] Eight duels work in correct sequence, with personalized dialogue; tests cover solo, helpers, level differences and defeat/resume.
- [ ] Exit instance, reconnect, crash/restart, retry and out-of-order packet scenarios preserve exact completed stages.
- [ ] Ambrose does not die/disappear; pact survives relog; public NPC services remain functional.
- [ ] Krokotopia handoff preserves original keys and quest objectives, with correct Shadow-specific motivation.

## Milestone D — World polish and adversarial regressions

- [ ] Fire/Death/Balance and other normal schools receive their original spoken lines and journals.
- [ ] Shadow pre-Nightside, active-finale and post-pact characters each receive correct lines in shared zones.
- [ ] Mixed party sees owner-consistent private instance; no shared-zone quest leaks or impossible NPC state.
- [ ] Optional Colossus/Sunken City/Crab Alley quests (where present) are not made mandatory or unavailable.
- [ ] Pet Pavilion, school gear, Bazaar, housing, banking and common services continue functioning.
- [ ] No mod reinstall/upgrade wipes or overwrites preexisting character or user files.
- [ ] Clean uninstall restores client resources and leaves user saves valid or provides a documented compatibility rollback path.
- [ ] Narrative proofread confirms each professor/instructor has a distinct stance and the tutor is not an unannounced Shadow Web handler.
- [ ] Record gameplay evidence/screenshots/logs for creation, each spell, one full Shadow mainline run, a representative conquest duel, pact aftermath, Krokotopia arrival, and baseline normal-school regression.

**Final acceptance is gameplay, not compilation.** The project is not “complete” merely because automated tests pass or quest IDs exist. A fresh Shadow character must actually finish the scenario through the installed client, and level 1–50 mechanical testing must succeed independently of campaign progression. If actual client testing is inaccessible, say precisely what could not be verified and never mark that item done.

---

# 13. Future-arc interface contract (design only; NO later-world implementation)

## Release 2 — Arc 1 continuity

- Krokotopia: independent Shadow wizard races Malistaire to restricted knowledge, studies authentic ancient binding practices and retains the Ravenwood pact's consequences.
- Marleybone: investigates magical records and Malistaire's movements without reducing local residents to pawns.
- MooShu: recognizes the danger of coercive magic, gathers legitimate assistance and gains access to Dragonspyre.
- Dragonspyre: learns Malistaire's full Dragon Titan plan **only when the actual story reveals it**; prevents that catastrophe in part because it would destroy everything the player has seized. After the final battle, receive an ambiguous invitation hinting toward Celestia, **without revealing its sender as Morganthe before the future story chooses to do so**.
- Umbral Sight grows by studied techniques, not unexplained universal recognition. Side effects of violating bindings can foreshadow the cost of Shadow power.

## Release 3 — Celestia: becoming Morganthe's apprentice

Morganthe's documented desire for Celestian Astral knowledge supplies her original motive. The independent Shadow wizard proves especially useful because they can identify (not automatically break) specialized protective enchantments. Shadow Weavers initially treat them as an intruder, then evaluate them. A Chancel audience, adapted Trial of the Spheres, and **actual private induction quest** establish the alliance. Only then set the apprentice milestone. Do not just alter a subtitle and claim recruitment happened.

## Later releases — beyond recruitment

Zafaria explores the Deck of Shadows and divided duties; Avalon challenges the apprentice's understanding of Morganthe's past; Azteca forces confrontation with consequences of loyalty; Khrysalis confronts the player's ambition against Morganthe's Song of Creation plan. Preserve the possibility that a devoted apprentice will eventually discover that Morganthe also imposes limits on what her student may learn.

**Future narrative invariant:** neither Ambrose nor Morganthe is a simplistic benevolent/evil teacher. One restricts power because misuse can endanger others; the other values extraordinary power but may demand obedience. The protagonist must face the tension between freedom to learn and responsibility for what they do.

---

# 14. Instructions for Opus 5.5: execution and reporting contract

1. **Read the supplied user plan and the installed files before making edits.** Compare the actual repository state and installed client revision with all claimed findings. Identify unsupported assumptions.
2. **Start with a compatibility and risk audit** covering native character creation, Shadow school ID, server school whitelists, active quest graph, trainer validation, donor cinematic integrity and SpiralLoader hooks.
3. **Implement vertical slices** in this order: eighth-school creation+starter duel+relog → teacher+training → entire school through 50 → Shadow enrollment → quest presentation resolver → Nightside sight → finale stage/encounters → aftermath+pact → travel+regression.
4. **Use exact installed asset identifiers.** Never make up hashes/IDs/cinematics, patch from filename resemblance, or copy donor effects without validation.
5. **Protect data and isolate patches.** No uncontrolled changes to shared quests, other schools, other players, original donor spells or user files. Preserve existing local modifications.
6. **Keep narrative quality and mechanics in sync.** Every Shadow-only claim should correspond to an actual objective, capability, service agreement, or later deliberate story seed.
7. **Deliver actual project outputs now**, not promises of background work. Produce a buildable mod/client adapter, scripts, docs, tests and reliable instructions.
8. **Run whatever verification the environment genuinely supports.** Never state 'fully tested in client' without evidence. If blocked, leave an explicit blocker with concrete reproduction steps and remaining requirements.
9. **Report completion by acceptance criterion** (pass/fail/not tested), plus major changes, compatibility risks, remaining defects, user action needed for anything requiring a GUI/client, and exact artifact/commit/build locations. Never substitute lines-of-code count for verified gameplay quality.
10. **Do not quietly expand the goal into Arc 2.** The release should feel complete in Wizard City yet create an unbroken narrative path to Morganthe later.

## Definition of done

> A fresh level-one Shadow wizard can be created in the actual client; train and cast original-asset Shadow spells under a fair level 1–50 curriculum; experience an authentic Shadow-specific Wizard City story with all standard world beats preserved; acquire Umbral Sight only after Nightside; complete a compelling staged Ravenwood conquest against distinct, constrained opponents; bind the faculty to a limited, comprehensible agreement without altering other players' worlds; return to a usable Ravenwood; then depart normally for Krokotopia as an independent Shadow wizard. No one confuses this with joining Morganthe yet. The future Celestia recruitment follows naturally from what the player has earned.

---

# 15. Original plan preservation / what changed

**Preserved by design:** eighth-school creation and legacy Shadow identity; level 1–50 curriculum, donor spells and starting balance; Shadow Pip schedule; Fire/Myth health midpoint; free primary-school training; explicit server trainer enforcement; seven original enrollments, full three-street/mainline graph and Daisy involvement; one finale quest with the same eight representatives; restricted pact; existing-asset-only presentation; per-character state; client version checks; save and rollback safety; later-world roadmap and real-client acceptance.

**Final amendments added in this document:** an explicit four-act dramatic spine; a 39-quest coverage map with meaningful/no-change calls; silent-player-dialogue compatibility; fuller tutor arc; a motivated conflict and visibly limited Umbral Sight; three escalating binding interactions as an **optional within-final-quest staging** rather than extra unrelated quests; distinctive roles for all eight opponents; accurate differentiation of Malorn and Arthur; graceful world-state aftermath; optional Wizard City locations without gating; quality tiers; detailed character/party/security/voice-handling rules; source-evidence inventory; progressive integration order; rigorous real-client release gate; and a future-arc interface that makes Morganthe's eventual recruitment earned rather than assumed.

**License / deployment note:** This is a community modification plan, not an official Wizard101/KingsIsle feature. Use only game assets already lawfully present in the user's installation, and do not bundle third-party proprietary artwork, audio or executable files unnecessarily.
