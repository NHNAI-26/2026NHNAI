# Happy Ending Cinematic Plan

> One authoritative living planning document. The document, not the chat, is the current planning state.

## Document State

| Field | Value |
| --- | --- |
| Interview state | `explicitly-finished` |
| Working language | Korean (interview), English (authoritative source) |
| Current revision | 5 |
| Last updated | 2026-09-06 (KST) |
| Project or workspace root | `C:\myGame\2026NHNAI` |
| Base path | `docs/specs/happy-ending-cinematic-spec.md` (this file, authoritative) |
| Korean mirror path | `docs/specs/happy-ending-cinematic-spec.ko.md` |
| Explicit finish received | `yes` ("계획확정하고 구현을 해줘", revision 4) |
| Next authorized action | Run the EditMode suite and verify in Game View once Play Mode is free. Look development and the Timeline swap (R-016) need separate authorization |

## Current Snapshot

- **Outcome:** when the game ends in victory (`GameWon == true`), play a happy-ending cinematic that answers the prologue, instead of the current text panel.
- **Primary users or audience:** players who clear the game. Secondarily, the developer who must show the ending in demos.
- **In scope:** the seven-beat cinematic (date card → phone dialogue → night launch → cut to moon → moon transit → newspaper → fade out), its trigger point, how the final rocket's appearance is preserved, and the implementation technology.
- **Out of scope:** sad-ending presentation, credits, save/load, any change to the win/lose rules.
- **Current decision focus:** none. All structural decisions are closed.
- **Material unresolved items:** OI-007
- **Active question IDs:** none
- **Settled axes (rev 2~4):** on final success, skip the 6-second parachute presentation and the result newspaper and enter the cinematic directly (UD-004). Use the real launched rocket, preserved just before the simulation scene unloads (UD-005). Timeline only for the 3D stretch; everything else is prologue-style coroutines (UD-006). Fade out to `00_Title` (UD-007). Three to four phone lines in a plain tone (UD-008). Total length 40~60 seconds (UD-009).

## Outcome and Context

### Desired Outcome

Victory must not end on the single line "MISSION COMPLETE". It must end on a shot that visually repays the promise the prologue made — "land a crewed spacecraft on the Moon by 2026". The player should watch the project they ran for eight in-game years actually leave for the Moon.

### Problem and Background

The win/lose branch already exists in code and is locked by tests (SF-001, SF-002, SF-009). What is missing is the presentation. Today the happy and sad endings share one prefab, one body text and one button, and differ only in a title string. The user chose to fill in the happy ending first (UD-001, UD-002).

The prologue already established the "black screen + fading text + comms tone" grammar (SF-004). The first two beats of the ending deliberately reuse that grammar so the ending rhymes with the opening.

### Planning Boundary

This plan decides the scope, beat structure, trigger point, data ownership and implementation technology of the happy ending. It does not decide the final dialogue copy or sound asset production, and it does not cover the sad ending. Implementation was authorized separately, in the same message that finished this plan; commits, scene edits, package installation and deployment were not.

## Users and Stakeholders

| User or stakeholder | Need, responsibility, or concern | Evidence / source IDs | Status |
| --- | --- | --- | --- |
| Player who cleared the game | Emotional payoff for winning. Must be skippable so replays are not punishing | UD-001, SF-004 | active |
| Developer (Hong) | Must attach without scene conflicts and without breaking the existing ending tests | SF-009, SF-011 | active |
| Demo / presentation owner | Needs a way to force the ending to play | AR-005 | proposed |

## Scope and Non-Goals

### In Scope

| Scope item | Source IDs | Status | Notes |
| --- | --- | --- | --- |
| The seven-beat happy ending | UD-002 | active | See Primary Flow |
| Trigger point change | UD-004, SF-003 | active | Final winning launch skips parachute and result newspaper, goes straight to the ending |
| Preserving the final rocket's appearance | UD-005, SF-008 | active | Clone the `Rocket` root just before scene unload, use it in the ending, destroy it after |
| Hybrid implementation | UD-006, SF-005 | active | B3~B5 on Timeline + Cinemachine, the rest in coroutines |
| Skip / input handling | SF-004 | active | Same click-to-skip as the prologue |

### Out of Scope / Non-Goals

| Excluded item | Source IDs | Status | Why excluded or deferred |
| --- | --- | --- | --- |
| Sad-ending presentation | UD-002 | active | The user scoped the request as "the happy ending first" |
| Changes to win/lose rules | UD-001 | active | Keep the current branch (grade B or better wins / running out at 2026 Q4 loses) |
| Credits, staff roll | none | active | Not requested. A separate plan if it becomes needed |
| Save/load, ending gallery | none | active | Not requested |

## Core Experience / Operating Flow

### Primary Flow

Entry condition: `ResearchPrototypeModel.HasGameEnded == true && GameWon == true` (SF-001).

Entry point (UD-004): immediately after the final mission is judged a success. On this path the 6-second parachute presentation (`MissionSuccessPresentation`) and the result newspaper (`ShowResultReport`) do not play; the simulation scene is unloaded and B1 begins. Success on any mission other than the final one keeps the existing parachute and newspaper path.

1. **B1 — Date card.** Full black screen. `2026.04` fades in, holds, fades out. Same grammar as the prologue's `2017.12` card (SF-004).
2. **B2 — Phone dialogue.** On the same black screen, several "you've done well" lines fade in and out in sequence, with a comms tone. Three to four lines, plain tone (UD-008).
3. **B3 — Night launch.** Cut to 3D. The rocket launches from a night-time pad. This rocket is the player's final successful design (UD-002, UD-005).
4. **B4 — Cut to the Moon.** After some ascent, the camera cuts. The Moon fills the frame.
5. **B5 — Moon transit.** The rocket's nose slides into frame from the lower right, gradually revealing the back of the spacecraft as it flies toward the Moon.
6. **B6 — Newspaper.** The success article is shown in the same newspaper presentation as always (SF-006).
7. **B7 — End.** Fade out, then load `00_Title` (UD-007). The happy ending never shows `ResearchEndingController`'s record panel. The deadline loss keeps the existing ending screen and restart path, so the two endings deliberately end differently.

### Length budget per beat (UD-009, 40~60 s total)

| Beat | Stretch | Target length | Owner |
| --- | --- | --- | --- |
| B1 | `2026.04` date card | ~5 s | Coroutine + data |
| B2 | 3~4 phone lines | ~15 s (~4 s per line) | Coroutine + data |
| B3~B5 | Night launch → cut to Moon → moon transit | 20~30 s | Timeline + Cinemachine |
| B6~B7 | Newspaper + fade → title | ~10 s | Existing newspaper UI + coroutine |

### Alternate, Error, or Edge Flows

| Condition | Expected behavior | Related requirement or decision IDs | Status |
| --- | --- | --- | --- |
| Player clicks the screen | Same as the prologue: drop the remaining beats and go straight to the final fade | R-008, SF-004 | active |
| Cinematic prefab / references missing | Abandon the cinematic and fall back to the existing `ResearchEndingController` path. Never leave the player stuck on a black screen | R-009, SF-004 | active |
| The final rocket's appearance could not be preserved | Play with a substitute rocket; do not abort the cinematic | R-010, UD-005 | active |
| Deadline loss (`GameWon == false`) | This cinematic does not play. Existing path unchanged | R-011, UD-002 | active |
| Component disabled mid-cinematic | Restore camera, rocket parent, audio. Same rule as `MissionSuccessPresentation` | R-012, SF-003 | active |

### State, Data, or Lifecycle Notes

- The current final-success path is `SimulationStageHost.CompleteLaunch(true)` → 6-second parachute presentation → simulation scene unload → result newspaper → acknowledgement → `ShowEndingScreen()` (SF-003).
- The simulation scene is loaded additively and unloaded (SF-008). Rocket part placement is not serialized anywhere, so unless something is done at unload time the final rocket's appearance is lost. That is what UD-005 addresses.
- `ResearchFlowSession.LaunchPhoto` holds the launch photo `Texture2D` (SF-007). It is a still image and cannot substitute for the 3D beats B3~B5; it remains valid only as the newspaper photo.
- `SoundManager` is a `DontDestroyOnLoad` singleton, so BGM/SFX transitions inside the cinematic can use it directly.

## Requirements

| ID | Requirement | Type | Source IDs | Priority | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- |
| R-001 | Play the happy-ending cinematic only when the game ended with `GameWon == true` | functional | UD-002, SF-001 | must | active | EditMode test: plays on victory, does not play on deadline loss |
| R-002 | B1 fades the `2026.04` date card in and out on black | functional | UD-002 | must | active | Game View check |
| R-003 | B2 defines the phone lines as data and fades them in and out in sequence; adding or removing a line must not require a code change | functional | UD-002, SF-004 | must | active | Change the line count in data, then play |
| R-004 | B3 shows the launch from a night-lit pad | functional | UD-002 | must | active | Game View check |
| R-005 | The rocket in B3~B5 is the player's final successful design | functional | UD-002 | must | active | Two clears with different engine layouts show different rockets |
| R-006 | B4~B5 cut to a frame containing the Moon, and the rocket's back enters gradually from the lower right | functional | UD-002 | must | active | Game View check |
| R-007 | B6 shows the success article in the existing newspaper format | functional | UD-002, SF-006 | must | active | Existing newspaper UI is reused |
| R-008 | The whole cinematic can be skipped with a click | quality | SF-004 | must | active | A click jumps to the final fade |
| R-009 | Missing references never lock the game; fall back to the existing ending path | operational | SF-004 | must | active | Play with references cleared |
| R-010 | If the rocket appearance cannot be preserved, the cinematic continues with a substitute | operational | UD-005 | should | active | Play with the source removed |
| R-011 | The existing `ResearchCompletionFlowTests` keep passing | quality | SF-009 | must | active | EditMode tests pass |
| R-012 | On completion or interruption, restore camera, rocket parent, audio and time state | operational | SF-003 | must | active | Disable-mid-cinematic test |
| R-013 | There is a developer-only way to force the cinematic to play | operational | AR-005 | should | proposed | Editor menu or debug button |
| R-014 | A winning final launch plays neither the parachute presentation nor the result newspaper; non-final launches keep the existing path | functional | UD-004, SF-003 | must | active | One final win and one ordinary success, each played through |
| R-015 | Preserve the final rocket's visual hierarchy before the simulation scene unloads, and destroy it when the ending finishes | functional | UD-005, SF-008 | must | active | No leftover objects after the ending |
| R-016 | Timeline is used only for the B3~B5 3D stretch; B1, B2, B6 and B7 stay on the UI coroutine path | operational | UD-006, SF-005 | must | active | Code review: no UI/text tracks on the Timeline |
| R-017 | After the B7 fade completes, load `00_Title`. The deadline-loss path keeps the existing ending screen and restart | functional | UD-007, SF-002, SF-012 | must | active | Victory reaches the title; loss keeps the old screen |
| R-018 | Returning to the title must not leak session progress into the next new game | operational | UD-007, SF-012 | must | active | After a clear, a new game starts at initial year, funds and missions |
| R-019 | B2 runs 3~4 lines in about 15 seconds; the total must stay within 40~60 seconds | quality | UD-008, UD-009 | should | active | Summed length check, in the style of the prologue's `TotalSeconds` |

## Constraints

| Category | Constraint | Source IDs | Consequence | Status |
| --- | --- | --- | --- | --- |
| compatibility | Prefer prefabs, ScriptableObjects and code over scenes (`.unity`). `01_Main.unity` is already dirty | SF-011 | Build the cinematic as a prefab and keep scene references to a minimum | active |
| quality | The ending flow is locked by EditMode tests | SF-009 | Changing the trigger point forces test changes | active |
| technical | The simulation scene is loaded/unloaded additively and rocket placement is not serialized | SF-008 | The rocket appearance must be captured before unload | active |
| technical | `com.unity.timeline` 1.8.10 and `com.unity.cinemachine` 3.1.7 are already installed | SF-005 | Adopting Timeline adds no new dependency | active |
| technical | `SkyEnvironment` is altitude-driven and has no time-of-day parameter | SF-010 | The night pad must be built as its own lighting/skybox state | active |
| policy | Play Mode and Game View verification belong to the user; the agent stops at compilation | user memory | The verification plan must name the user-owned step | active |

## Success Evidence

| Related requirement IDs | Evidence or acceptance condition | Verification method | Owner or reviewer | Status |
| --- | --- | --- | --- | --- |
| R-001, R-011 | Correct play/no-play per branch, and the existing ending tests pass | EditMode tests | Developer | proposed |
| R-002~R-007 | The seven beats play in the intended order and timing | Game View inspection | User | proposed |
| R-005 | Two clears with different engine layouts show different rockets on screen | Game View inspection | User | proposed |
| R-008, R-009, R-010, R-012 | Skip, missing references, failed preservation and interruption never lock the game | EditMode or PlayMode tests | Developer | proposed |
| R-014 | The final win goes straight to the ending; missions 0~4 keep the existing path | EditMode tests | Developer | proposed |
| R-015 | No preserved rocket object remains after the ending | PlayMode test or Hierarchy check | Developer | proposed |
| R-016 | The Timeline asset covers only the 3D stretch | Code / asset review | Developer | proposed |
| R-017, R-018 | Victory reaches the title, loss keeps the old screen, and a new game starts clean | PlayMode check + EditMode test | Developer / User | proposed |
| R-019 | Summed length stays within 40~60 seconds | EditMode test, prologue `TotalSeconds` style | Developer | proposed |

## Decision and Evidence Ledger

| ID | Kind | Statement | Evidence / rationale | Status | Consequence / linked IDs |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | Accept the current branch behavior: failing the final mission does not end the game, and the sad ending only fires when 2026 Q4 is consumed | "1번은 맞는 말이야" | active | No rule change. Out of Scope |
| UD-002 | user decision | Build the happy ending first. Beat order: `2026.04` date card → phone dialogue → night pad launch with the final successful rocket → cut to the Moon → rocket's back entering from lower right → success newspaper → fade to end | Original user request | active | Primary Flow, R-002~R-007 |
| UD-003 | user decision | Timeline is acceptable as an implementation candidate, not yet a decision | "타임라인으로 구현해도 좋을 거 같고" | active | Q-003, UD-006 |
| UD-004 | user decision | On final success, skip the 6-second parachute presentation and go straight to the cinematic. The newspaper appears exactly once, as the last beat | Q-001 answer "낙하산 생략, 바로 엔딩" (rev 2) | active | R-014, OI-001 resolved, RK-005 |
| UD-005 | user decision | Take the rocket appearance from the real launched `Rocket` root, preserved just before the simulation scene unloads (AR-003 accepted) | Q-002 answer "발사 로켓 보존" (rev 2) | active | R-005, R-015, OI-002 resolved, RK-001 |
| UD-006 | user decision | Hybrid: Timeline + Cinemachine for the B3~B5 3D stretch only; B1, B2, B6, B7 stay prologue-style coroutine + data (AR-002 accepted) | Q-003 answer "하이브리드" (rev 2) | active | R-016, OI-003 resolved, RK-003 |
| UD-007 | user decision | After the B7 fade, return to `00_Title`. The happy ending never shows the record panel | Q-004 answer "타이틀로 복귀" (rev 3) | active | R-017, R-018, OI-004 resolved, RK-007 |
| UD-008 | user decision | B2 is 3~4 lines in a plain, understated tone | Q-005 answer "3~4줄, 담담하게" (rev 3) | active | R-019, OI-005 resolved |
| UD-009 | user decision | Total length budget is 40~60 seconds | Q-006 answer "40~60초" (rev 3) | active | R-019, OI-006 resolved, length table |
| UD-010 | user decision | Finish the plan and implement it | "계획확정하고 구현을 해줘" (rev 4) | active | Interview state `explicitly-finished`; implementation authorized |
| SF-001 | sourced fact | Victory is `LowPowerZoneHold` best grade B or better; loss is `RemainingTurns <= 0` (2026 Q4). `EvaluateGameEnd` sets `HasGameEnded`/`GameWon` | `Assets/01. Scripts/Research/ResearchPrototypeModel.cs:1524-1534` | active | R-001 |
| SF-002 | sourced fact | There is one ending screen, `ResearchEndingController`; only the title string differs between `MISSION COMPLETE` and `MISSION FAILED` | `Assets/01. Scripts/Research/ResearchEndingController.cs:27-28` | active | OI-004 |
| SF-003 | sourced fact | The final-success path is parachute presentation → result newspaper → `ShowEndingScreen()` on acknowledgement | `Assets/01. Scripts/Research/ResearchOperationUIController.cs:956-988`, `docs/mission-success-cinematic.md` | active | Q-001, R-012 |
| SF-004 | sourced fact | The prologue is `PrologueController` (coroutine) + `PrologueSequenceSO` (beat list with fade/hold/typing/SFX id/RevealSeconds), with click-to-skip and a self-destruct guard when references are missing | `Assets/01. Scripts/Prologue/PrologueController.cs`, `PrologueSequenceSO.cs` | active | R-003, R-008, R-009 |
| SF-005 | sourced fact | `com.unity.timeline` 1.8.10 and `com.unity.cinemachine` 3.1.7 are already in the project | `Packages/manifest.json:12,21` | active | Q-003 |
| SF-006 | sourced fact | The newspaper is `ResearchResultReportController` + `LaunchNewspaperArticle`; the medium is forced to `Newspaper` for the final mission or a final win | `Assets/01. Scripts/Research/LaunchNewspaperArticle.cs:44-57` | active | R-007, Q-001 |
| SF-007 | sourced fact | The launch photo is held as `ResearchFlowSession.LaunchPhoto` (`Texture2D`) | `Assets/01. Scripts/Research/ResearchFlowSession.cs:31`, `Assets/01. Scripts/Simulation/LaunchPhotoCapture.cs` | active | Reusable as the newspaper photo; cannot replace the 3D beats |
| SF-008 | sourced fact | The simulation scene loads and unloads additively, and no code serializes rocket part placement | `Assets/01. Scripts/Simulation/SimulationStageHost.cs:121,165`; placement-serialization search found nothing | active | RK-001, Q-002 |
| SF-009 | sourced fact | The ending flow is locked by `ResearchCompletionFlowTests` (early-victory blocking, final failure report first, restart screen reuse, and more) | `Assets/Tests/EditMode/Research/ResearchCompletionFlowTests.cs` | active | R-011, RK-005 |
| SF-010 | sourced fact | `SkyEnvironment` drives sky/sun/fog from altitude and has no night preset or time-of-day parameter | `Assets/01. Scripts/Simulation/SkyEnvironment.cs` | active | RK-004 |
| SF-011 | sourced fact | Project rules make scene edits a last resort, and `01_Main.unity` is already modified | `CLAUDE.md`, session-start git status | active | Prefab-first design |
| SF-012 | sourced fact | Only the title→game direction exists. `TitleMenu.NewGame()` calls `ResearchFlowSession.PrepareNewGame()` (which calls `ResetResearch()`) then `SceneManager.LoadScene("01_Main")`. There is no code path back to the title. `ResearchFlowSession` is `DontDestroyOnLoad` and survives scene loads | `Assets/01. Scripts/Title/TitleMenu.cs:38-44`, `Assets/01. Scripts/Research/ResearchFlowSession.cs:253-257,283` | active | R-017, R-018, RK-007 |
| AR-001 | agent recommendation | Reuse the prologue's assets for B1 and B2 — either `PrologueSequenceSO` itself or an ending SO with the same shape — so lines and timing stay data | The prologue already implements this grammar (SF-004); almost no new code | proposed | R-003. Accepting creates a new UD |
| AR-002 | agent recommendation | Build B3~B5 as one Timeline plus two or three Cinemachine cameras, and keep B1, B2, B6, B7 on the existing UI coroutine path | 3D camera cuts and timing are far easier in Timeline, and there is no reason to rebuild text fading that already works | accepted | Promoted to UD-006 |
| AR-003 | agent recommendation | Preserve the rocket by cloning the `Rocket` root before unload with `DontDestroyOnLoad`, reuse it in the cinematic, then destroy it | Placement is not serialized (SF-008), so preserving beats rebuilding and needs less code | accepted | Promoted to UD-005 |
| AR-004 | agent recommendation | Build the night look as a dedicated ending prefab (pad + night lights + skybox + moon) rather than reusing the simulation scene | `SkyEnvironment` has no time-of-day concept (SF-010) and scene edits must be avoided (SF-011) | proposed | R-004, RK-004 |
| AR-005 | agent recommendation | Ship a debug entry point that forces the ending to play | A cinematic that requires clearing six missions is far too expensive to check repeatedly | proposed | R-013 |
| OI-001 | unresolved item | Relationship between the existing result newspaper and the ending newspaper, and whether to keep the parachute | The final success already shows a newspaper once (SF-003) | resolved | Resolved by UD-004; created R-014 |
| OI-002 | unresolved item | Source of the rocket appearance for B3~B5 | The appearance is lost when the simulation scene unloads (SF-008) | resolved | Resolved by UD-005; created R-015 |
| OI-003 | unresolved item | Implementation technology (all Timeline / all coroutine / hybrid) | The user offered Timeline only as a candidate (UD-003) | resolved | Resolved by UD-006; created R-016 |
| OI-004 | unresolved item | Where B7's fade lands (keep the ending screen / return to title / buttons only) | Today the record panel with a restart button is the final screen (SF-002) | resolved | Resolved by UD-007; created R-017, R-018 |
| OI-005 | unresolved item | Line count, copy, speaker and sound for the B2 phone dialogue | The user only said "a few times" | resolved | UD-008 fixed count and tone; the actual copy is written during implementation |
| OI-006 | unresolved item | Total length budget | The prologue has a 20~30 s budget; the ending had none | resolved | Resolved by UD-009; created the per-beat table |
| OI-007 | unresolved item | Whether to compensate for the parachute presentation no longer being visible on the final mission | An already built and tested presentation disappears from the final mission (SF-003, UD-004) | open | Presentation asset utilization. Close as-is if no compensation is wanted |

## Question Register

| ID | Decision needed | Why it matters | Related IDs | State | Asked / updated revision | Resolution |
| --- | --- | --- | --- | --- | --- | --- |
| Q-001 | Relationship between the existing result newspaper, the parachute presentation and the ending newspaper | Changes the trigger point, whether the newspaper repeats, and how much of the ending test suite must change | OI-001, SF-003, SF-009, R-007 | answered | 1 / 2 | UD-004 |
| Q-002 | Where the B3~B5 rocket appearance comes from | Decides whether "the rocket as it was on final success" (R-005) is achievable, and at what cost | OI-002, SF-008, AR-003 | answered | 1 / 2 | UD-005 |
| Q-003 | Implementation technology | Maintenance cost, how timing is tuned, and code volume all diverge | OI-003, UD-003, SF-005, AR-002 | answered | 1 / 2 | UD-006 |
| Q-004 | Where B7's fade lands | Decides the last screen of the game and where the restart affordance lives | OI-004, SF-002 | answered | 2 / 3 | UD-007 |
| Q-005 | Line count and tone of the B2 phone dialogue | Sets the cinematic's length and emotional curve, and the data volume | OI-005, R-003 | answered | 2 / 3 | UD-008 |
| Q-006 | Total length budget | Sets per-beat timing and the ceiling for the Timeline stretch | OI-006, R-016 | answered | 2 / 3 | UD-009 |

## Corrections and Revision History

| Revision | Trigger | Change | Corrected / superseded IDs | Downstream sections and IDs reconciled |
| --- | --- | --- | --- | --- |
| 1 | Initial request plus code and document inspection | Initial best planning hypothesis | none | Snapshot, Scope, Flow, R-001~R-013, RK-001~RK-006, Ledger, Q-001~Q-003 |
| 2 | Q-001~Q-003 answers | Settled skip-parachute/immediate ending, rocket preservation, hybrid implementation | AR-002 and AR-003 accepted, OI-001~OI-003 resolved, Q-001~Q-003 answered | Snapshot, Scope, Primary Flow entry point, R-014~R-016, RK-001, RK-003, RK-005, new OI-007, new Q-004~Q-006 |
| 3 | Q-004~Q-006 answers | Settled title return, 3~4 lines, 40~60 s budget. Confirmed via SF-012 that the title-return path is new code | OI-004~OI-006 resolved, Q-004~Q-006 answered | Snapshot, Primary Flow B7 and the length table, R-017~R-019, SF-012, RK-006~RK-008, Coverage, Checkpoint |
| 4 | Explicit finish plus implementation authorization | Interview closed; English source written and Korean mirror synchronized | new UD-010; OI-004~OI-006 status corrected in the Open Items table, where they had lagged the ledger | Document State, Snapshot, Ledger, Open Items, Finalization |
| 5 | First implementation pass | Recorded where the implementation departs from the plan | R-003, R-004, R-006 marked partial; R-013 not implemented; R-016 deviated; RK-005 corrected as overstated | "Implementation record" below |

## Implementation record (revision 5)

The code landed. Only the departures are written here; everything else is already in the tables above.

### What landed

| File | Change |
| --- | --- |
| `Assets/01. Scripts/Simulation/HappyEndingSequence.cs` | New. All seven beats, stage build/teardown, rocket preservation, click-to-skip, title return |
| `Assets/01. Scripts/Simulation/SimulationStageHost.cs` | `CompleteLaunch` branches on `result.FinalMissionWon` into the new `HappyEndingRoutine` |
| `Assets/01. Scripts/Research/ResearchOperationUIController.cs` | Added `SetEndingOverride`, intercepted in the single `ShowEndingScreen` chokepoint |
| `Assets/Tests/EditMode/Research/ResearchCompletionFlowTests.cs` | Added `EndingOverride_TakesOverInsteadOfShowingEndingScreen` |
| `Assets/01. Scripts/Simulation/HappyEndingDebugTester.cs` | New. Editor-only forced playback via **F8** and `Tools > Border > Debug > Play Happy Ending` |
| `docs/mission-success-cinematic.md` | Records that the final mission no longer plays the parachute presentation |

### Departures from the plan

| ID | Status | Reality |
| --- | --- | --- |
| R-016 | deviated | No Timeline. The stage is built at runtime, so a Timeline has nothing to bind to. B3~B5 are coroutine code. Moving to Timeline first requires the pad, moon and cameras to exist as a prefab, which is Editor work |
| R-004, R-006 | partial | Pad, ground and moon are primitive placeholders; "night" is expressed only through light intensity and colour. Look development is Editor work |
| R-003 | partial | The lines are serialized fields on `HappyEndingSequence`. The component is created at runtime, so no inspector exposes it, and changing the copy currently means editing code |
| R-013 | done | `HappyEndingDebugTester` forces playback from F8 or the menu. With no unacknowledged launch result it skips only the newspaper beat rather than fabricating a result |
| RK-005 | corrected | Overstated. `ResearchCompletionFlowTests` never traverses `SimulationStageHost`, and the test in question exercises the deadline-loss path, not a final win. No existing test needed changing |
| RK-007 | mitigated | The title return is a single `SceneManager.LoadScene("00_Title")`. Session reset is left to the existing `TitleMenu.NewGame`, with no duplicate call |

### Verification status

Compilation passes (the Editor entered Play Mode, which it refuses to do with compile errors). The new test and the
`ResearchCompletionFlowTests` suite have **not been run** — the user held Play Mode at the time. Game View
verification belongs to the user and has not happened yet.

## Risks, Conflicts, and Dependencies

| ID | Kind | Risk, conflict, or dependency | Likelihood / impact | Mitigation, decision, or owner | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | risk | The preserved rocket survives past the ending, or its physics/audio components stay live and misbehave during the cinematic | medium / medium | UD-005 removes the loss risk. Strip physics and scripts on preservation, keeping only the visual hierarchy. R-015 guarantees destruction | UD-005, R-015 | open |
| RK-002 | risk | Needing a scene edit causes a conflict in `01_Main.unity` | medium / medium | Build the cinematic as a prefab and keep scene references to at most one | SF-011 | open |
| RK-003 | risk | Timing, skip or fade desynchronizes at the boundary between the Timeline stretch and the coroutine stretch | medium / medium | UD-006 hybrid confirmed. There are only two boundaries, at the end of B2 and the end of B5, and both are designed to cross on a black screen | UD-006, R-016, R-008 | open |
| RK-004 | risk | With no existing means to build a night state, the look work is larger than expected | medium / medium | AR-004 dedicated prefab; lighting and skybox are fixed ending-only values | SF-010 | open |
| RK-005 | risk | Dropping the result-newspaper call on the winning path breaks `ResearchCompletionFlowTests`, in particular `Operation_FinalLaunchShowsResultThenFinalFailureReportBeforeEndingWithoutDuplicateRewards`, which assumes the newspaper is shown | high / high | Before implementing, re-express what that test guards (no duplicate rewards, ending entered after acknowledgement) in terms of the new path. Reward settlement already completes in `FinishLaunch`, not in the newspaper UI, so the judgement itself is unaffected | SF-009, UD-004, R-011, R-014 | open |
| RK-006 | dependency | Content assets: dialogue copy, comms tone, ending BGM | medium / medium | UD-008 fixes the volume at 3~4 lines. Timing runs off data even with no sound, as in the prologue (SF-004) | UD-008 | open |
| RK-007 | risk | There is no existing path back to the title, so it is new code, and `ResearchFlowSession` is `DontDestroyOnLoad`, so progress can leak into the next new game | medium / high | `TitleMenu.NewGame()` already calls `PrepareNewGame()`, so leave initialization to it and do not duplicate the call. Verified by R-018 | UD-007, SF-012, R-018 | open |
| RK-008 | conflict | The happy ending returns to the title while the sad ending keeps the record panel, so the two endings terminate differently | medium / low | Recorded as intentional asymmetry. Revisit when the sad ending is built | UD-002, UD-007, SF-002 | open |

## Open, Skipped, and Deferred Items

| ID | Item | State | Why it matters / consequence | Current recommendation | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-001 | Newspaper duplication / parachute retention | resolved | Trigger point, test change scope | — | User | Closed by UD-004 |
| OI-002 | Rocket appearance source | resolved | Whether R-005 is achievable | — | User | Closed by UD-005 |
| OI-003 | Implementation technology | resolved | Code volume, maintenance | — | User | Closed by UD-006 |
| OI-004 | Where the fade lands | resolved | End of the flow | — | User | Closed by UD-007 |
| OI-005 | Phone dialogue content | resolved | Cinematic length and tone | — | User | Closed by UD-008; copy written during implementation |
| OI-006 | Total length budget | resolved | Per-beat timing | — | User | Closed by UD-009 |
| OI-007 | Whether to compensate for the parachute presentation | open | An already verified presentation disappears from the final mission | Do not compensate. It still plays on missions 0~4, so it is not wasted | User | After the first playthrough of the ending |

## Coverage and Consistency Check

| Planning area | State | Supporting IDs | Remaining gap or note |
| --- | --- | --- | --- |
| Outcome | covered | UD-002 | — |
| Users and stakeholders | covered | UD-002, AR-005 | — |
| Scope | covered | UD-002, UD-004~UD-006 | — |
| Non-goals | covered | UD-001, UD-002 | — |
| Core flow | covered | UD-002, UD-004, UD-007, UD-009 | Entry, exit and length budget all settled |
| Constraints | covered | SF-005, SF-008~SF-012 | — |
| Success evidence | partial | R-001~R-019 | Reference frames for the visual checks are not defined |
| Risks and dependencies | covered | RK-001~RK-008 | — |
| Unresolved decisions | covered | OI-007 | Six resolved, one left (presentation utilization, unrelated to implementation) |
| Handoff and authorization | covered | UD-010, Document State | Implementation authorized; commits and scene edits are not |

## Interview Checkpoint

- **Latest user message incorporated:** explicit finish plus implementation authorization (revision 4)
- **Latest sourced evidence incorporated:** SF-012 (`TitleMenu.cs`, `ResearchFlowSession.cs`)
- **Ledger transitions applied:** new UD-010; OI-004~OI-006 corrected to `resolved` in the Open Items table
- **Affected sections reconciled:** Document State, Snapshot, Ledger, Open Items, Coverage, Finalization
- **Contradictory active items check:** passed
- **Traceability check:** passed (R-001~R-019 all linked to UD/SF/AR/OI)
- **Current focus:** none; the interview is closed
- **Next question IDs:** none
- **Resume point:** if planning reopens, start at OI-007 and at whatever the implementation learned about RK-005

## Finalization and Handoff

- **Final interview state:** `explicitly-finished`
- **Authoritative English source:** `docs/specs/happy-ending-cinematic-spec.md`
- **Korean mirror:** `docs/specs/happy-ending-cinematic-spec.ko.md`
- **Synchronization check:** both files carry the same stable IDs, statuses, requirements, decisions, risks, unresolved items and next authorized action
- **Remaining gaps and consequences:** OI-007 (whether to compensate for the parachute presentation) — does not block implementation. RK-005 (test rework) must be handled during implementation
- **Assumptions still requiring confirmation:** AR-001, AR-004, AR-005 remain recommendations, not user decisions
- **Next authorized action:** implement R-001~R-019, authorized in the same message as the finish signal
- **Implementation handoff:** entry point `SimulationStageHost.CompleteLaunch` (R-014), preservation `Rocket` root clone (R-015), presentation B1~B7 (R-002~R-007), exit `SceneManager.LoadScene("00_Title")` (R-017, R-018), test rework `ResearchCompletionFlowTests` (R-011, RK-005)
- **Resume point if planning reopens:** OI-007, plus any RK-005 findings from implementation

> Finishing or approving this plan does not authorize commits, pull requests, package installation, deployment, publishing, messaging, purchasing, or external-system changes. Implementation of this specific plan was authorized separately by the user.
