# Rocket Design Screen Interaction and UI Revision

> Single authoritative planning document. This file, not the chat, is the planning state.

## Document State

| Field | Value |
| --- | --- |
| Interview state | `explicitly-finished` |
| Working language | Korean (interview); English (authoritative document) |
| Current revision | 4 (final) |
| Last updated | 2026-09-04 (KST) |
| Project or workspace root | `C:\myGame\2026NHNAI` |
| Base path (authoritative) | `docs/specs/rocket-design-ui-spec.md` |
| Korean mirror | `docs/specs/rocket-design-ui-spec.ko.md` |
| Explicit finish received | `yes` — "로 하고 구현 시작해줘" (rev 4) |
| Implementation authorization | Granted in the same message, separately from planning approval |
| Unresolved at finish | **Q-004 / Q-005 / Q-009 and OI-003, OI-005, OI-006, OI-007, OI-008, OI-010** |
| Next authorized action | Implement Phase 1 under the stated assumptions AR-012 / AR-013 / AR-014 |

Related documents: `docs/specs/rocket-prototype-revision-spec.md` (prior prototype, `explicitly-finished`),
`docs/specs/engine-preset-stats-spec.md` (engine stat SO, **in progress in another session**),
`docs/rocket-simulation.md`, `docs/artemis-2026-gdd/07_로켓_설계.md`,
`docs/artemis-2026-gdd/11_UI_UX_화면설계.md`, `docs/artemis-2026-gdd/18_확정사항_및_변경금지선.md`.

> **Unresolved at the time of finish.** The interview ended before Q-004 (move button vs. existing
> left-drag), Q-005 (target scene), and Q-009 (engine removal path) were ever asked. They were **not**
> decided on the user's behalf. Implementation proceeds under explicitly labelled assumptions
> AR-012 / AR-013 / AR-014, which remain agent assumptions — not user decisions — until confirmed.

## Current Snapshot

- **Outcome:** In the rocket design screen the player hovers an engine in the left preset panel to read its
  stats, drags it out, drops it on the rocket surface to attach immediately or on the ground to place it there,
  then clicks an attached engine to get move and rotate buttons. A rotated engine thrusts along its own axis.
- **Primary users:** the developer (verifying the interaction), ARTEMIS: 2026 players (design stage).
- **In scope:** prefab-based left preset panel UI, hover stat display (phase 2, backed by the other session's SO), drag to
  attach or ground-place, selection with move/rotate buttons, **switch to a part-orientation-following thrust
  model**, keep the existing right-drag orbit camera, separate UI input from 3D input, revise GDD 07 §5 and
  `docs/rocket-simulation.md`.
- **Out of scope:** the engine stat SO data model itself (other session), launch probability maths, engine
  ON/OFF timeline, map and target path generation, design-fit display.
- **Delivery sequencing:** **Phase 1** = panel skeleton, drag placement/attachment, rotation and thrust model,
  input separation, document and test revision. **Phase 2** = wire hover stats once the other session's SO is
  final (UD-012).

## Outcome and Context

### Desired Outcome

Today the whole design interaction is "left-drag an engine object already placed in the scene onto the rocket
surface". This becomes **pull an engine out of a left-hand preset panel**. Hovering an entry previews its stats.
Dropping on the rocket surface attaches at that point; dropping on the ground places it there; dropping anywhere
else cancels (UD-007, UD-010). An attached engine can be selected to reveal move and rotate buttons, and
**the rotated orientation is the thrust direction** (UD-008), with no axis or angle limit; angles near a multiple of 45° are snapped as an aiming aid (UD-011 rev 4). Right-drag
camera orbit is unchanged.

### Problem and Background

- Only `RocketPart` instances pre-placed in the scene can be used. `RocketBuilder.BeginDrag` picks up an
  existing part under the cursor, so there is no path to spawn a new one (SF-002).
- Nothing on screen tells the player what an engine does. `RocketPart` exposes `thrust`/`fuel`/`burnRate` only
  in the inspector (SF-011).
- Rotation is impossible: `Rocket.Attach` overwrites part rotation with the rocket's (SF-003) and an EditMode
  test locks that rule (SF-013). UD-008 removes it.

### Planning Boundary

This document decides the **interaction model, the UI composition, and the resulting thrust-direction model**.
The engine stat data structure (SO fields, preset count, price) belongs to
`docs/specs/engine-preset-stats-spec.md`, which is being worked on in **another session** (UD-009, SF-012).

## Users and Stakeholders

| User or stakeholder | Need, responsibility, or concern | Evidence / source IDs | Status |
| --- | --- | --- | --- |
| Developer | Verify and tune the pull-out-and-attach interaction | UD-002, UD-007 | active |
| Player | Choose engines by stats, adjust placement and orientation | UD-005, UD-008 | active |
| GDD owner | Both GDD 07 §5 clauses ("no part catalogue", "part orientation follows the rocket") now need revision | SF-004, RK-001, RK-002 | active |
| Engine stat SO session | Phase 2 depends on that session's SO type, fields and assets | UD-009, UD-012, RK-004 | active |

## Scope and Non-Goals

### In Scope

| Scope item | Source IDs | Status | Phase | Notes |
| --- | --- | --- | --- | --- |
| Left engine preset panel UI | UD-002, UD-003 | active | 1 | UGUI, prefab-based root and repeated entry prefabs |
| Preset drag → attach to rocket / place on ground | UD-007, UD-010 | active | 1 | Drop target chooses the branch |
| Click an attached engine → move and rotate buttons | UD-004 | active | 1 | Move button behaviour is OI-003 |
| Unrestricted part rotation with thrust following orientation | UD-008, UD-011 | active | 1 | Includes revising the locked rule, test and GDD clause |
| Keep right-drag orbit camera as-is | UD-001, SF-001 | active | 1 | No change |
| Separate UI pointer input from 3D raycasts | AR-002, SF-009 | active | 1 | Otherwise panel clicks leak into the 3D scene |
| Revise GDD 07 §5 and `docs/rocket-simulation.md` | UD-002, UD-008, SF-004 | active | 1 | Two clauses contradict the implementation |
| Hover an entry to show its stats | UD-005, UD-009, UD-012 | active | 2 | Wired after the other session's SO is final |

### Out of Scope / Non-Goals

| Excluded item | Source IDs | Status | Why excluded or deferred |
| --- | --- | --- | --- |
| Engine stat SO fields, preset count, price | UD-009, UD-012, SF-012 | active | Other session owns it; this work only consumes it |
| Thrust magnitude slider, engine ON/OFF timeline | SF-005 | active | GDD 07 §4 items, not part of this request |
| Map, target path, design fit, probability display | SF-016 | active | GDD 11 §7 items, not part of this request |
| Grid/mirror snapping, overlap checks | SF-004 | active | GDD 07 §5 deliberate omissions. Rotation angle snapping left this list in rev 4 (UD-011) |
| Staging, gimbal, drag | SF-004 | active | GDD 07 §5 deliberate omissions |

## Core Experience / Operating Flow

### Primary Flow (final)

1. Entering the design screen shows the 3D rocket plus a **left-hand engine preset panel**.
2. (Phase 2) Hovering an entry shows that engine's stats, read from the other session's SO (UD-009).
3. **Left-dragging** an entry towards the map makes an engine instance follow the cursor.
4. Releasing over the **rocket surface attaches at that point**, over the **ground places it on the ground**,
   and **anywhere else cancels the drop and discards the instance** (UD-010).
5. A ground-placed engine can be picked up with the existing left-drag and attached (SF-002 path unchanged).
6. **Clicking** an attached engine selects it and reveals **move and rotate buttons**.
7. Rotating changes the part's orientation and **thrust follows that orientation** (UD-008); there is no axis or
   angle limit (UD-011).
8. **Right-drag** orbits the camera throughout (SF-001).

### Alternate, Error, and Edge Flows

| Condition | Expected behavior | Related IDs | Status |
| --- | --- | --- | --- |
| Left/right click over the panel | No 3D part drag, no camera orbit | R-006, SF-009 | active |
| Preset drop over the rocket surface | Attach at that point | UD-010, R-013 | active |
| Preset drop over the ground | Place on the ground, do not attach | UD-007, R-004 | active |
| Preset drop over neither | Cancel the drop, discard the instance | UD-010, R-013 | active |
| Click empty space while a part is selected | Deselect, hide buttons | R-005 | active |
| Panel interaction after launch (`rocket.Launched`) | Part drag is already blocked; panel behaviour undefined | SF-002, OI-006 | open |
| Launching with an inverted or heavily tilted engine | Thrust follows the part, so the rocket rotates or crashes — intended | UD-008, UD-011, RK-007 | active |
| Engines pulled out and left unused on the ground | Removal/recovery path undefined | OI-007 | open |
| Hovering a preset before phase 2 | No stat values yet; the hover area exists but is not wired | UD-012, AR-011 | active |

### State, Data, and Lifecycle Notes

- **The thrust model changes.** `Rocket.FixedUpdate` currently uses `transform.up * engine.Thrust`, the rocket's
  up (SF-003). Under UD-008 it becomes `engine.transform.up`, and `Rocket.Attach` stops overwriting rotation.
  `RocketSimulationTests.Attach_KeepsWorldPoint_AndAlignsToRocket` (SF-013) must be updated.
- This accepts an outcome `docs/rocket-simulation.md` records as **deliberately avoided** ("a side engine would
  spin the rocket and the game becomes something else", SF-020). The user chose it in UD-008 and declined any
  limit in UD-011, so an inverted engine driving the rocket into the ground is allowed (RK-007).
- The drop branch extends the existing `_overRocket` decision in `EndDrag` (SF-021, AR-008).
- `Assets/03. Prefabs/Simulation/RocketEngine.prefab` already exists as the instance source (SF-006).
- `Border.Simulation` is `autoReferenced: true`, so `UnityEngine.UI`/TMP are usable without touching the asmdef
  (SF-008).

## Requirements

| ID | Requirement | Type | Source IDs | Priority | Phase | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R-001 | Right-drag orbit and wheel zoom keep their current behaviour | functional | UD-001, SF-001 | must | 1 | active | No regression in existing controls |
| R-002 | A left-hand engine preset panel is displayed | functional | UD-002 | must | 1 | active | Entries visible on the left of the design screen |
| R-003 | Hovering a preset entry shows that engine's stats | functional | UD-005 | must | 2 | active | Values differ per entry |
| R-004 | Dropping a dragged preset on the ground places an engine instance there | functional | UD-007 | must | 1 | active | An engine not pre-placed in the scene appears on the ground |
| R-005 | Clicking an attached engine selects it and shows move/rotate buttons | functional | UD-004 | must | 1 | active | Buttons appear on click, hide on deselect |
| R-006 | Pointer input over UI does not trigger 3D part drag or camera orbit | quality | AR-002, SF-009 | must | 1 | active | Dragging on the panel does not orbit the camera |
| R-007 | The rotate button rotates the part without axis or angle limits and thrust follows that orientation | functional | UD-008, UD-011 | must | 1 | active | A tilted engine rotates the rocket |
| R-008 | Define the relationship between the move button and the existing left-drag | functional | UD-004, OI-003 | should | 1 | blocked | Q-004 never asked; implemented under AR-012 |
| R-009 | Revise `docs/rocket-simulation.md` and GDD 07 §5 in the same commit | process | UD-002, UD-008, SF-004 | must | 1 | active | Docs and implementation do not contradict |
| R-010 | `Rocket.FixedUpdate` applies thrust along `engine.transform.up` | technical | UD-008, SF-003 | must | 1 | active | Part orientation shows up in the trajectory |
| R-011 | Remove the rotation overwrite in `Rocket.Attach` and update `RocketSimulationTests` | technical | UD-008, SF-013 | must | 1 | active | EditMode tests lock the new rule |
| R-012 | Hover stats are read from the engine stat SO finalized in the other session | technical | UD-009, UD-012 | must | 2 | deferred | Revisit when that SO is final |
| R-013 | The drop point branches into attach / ground placement / cancel | functional | UD-010 | must | 1 | active | All three cases behave differently |

## Constraints

| Category | Constraint | Source IDs | Consequence | Status |
| --- | --- | --- | --- | --- |
| policy | GDD 07 §5 lists "part catalogue" as a deliberate omission | SF-004 | The left preset panel requires revising that clause | active |
| policy | GDD 07 §5: "part orientation follows the rocket; do not lay parts along the surface normal" | SF-004, SF-003 | UD-008/UD-011 reverse this — revision required | active |
| policy | GDD 07 §3 lists "thrust direction adjustment" as allowed input | SF-005 | Describing rotation as thrust-direction adjustment stays consistent | active |
| policy | GDD 18: "the player does not fly the rocket directly" | SF-017 | Only design-stage interaction may be extended | active |
| technical | `Attach_KeepsWorldPoint_AndAlignsToRocket` locks the orientation rule | SF-013 | Updated by R-011 | active |
| technical | `activeInputHandler: 1` — legacy `UnityEngine.Input` throws at runtime | SF-018 | Mouse input via Input System or EventSystem only | active |
| technical | `RocketBuilder` calls `Physics.Raycast` directly with no UI guard | SF-009 | Input separation is mandatory once a panel exists | active |
| dependency | SO type, fields and asset paths are settled in the other session; phase 2 does not start before that | UD-009, UD-012 | Phase 1 must be complete without the SO | active |
| process | Production UI must be prefab-based; code-generated UGUI is allowed only for temporary debug screens | Latest GDD 11 and GDD 18 | Main/design UI should bind data to prefab instances | active |
| process | `docs/artemis-2026-gdd/07_로켓_설계.md` has an uncommitted working-tree edit | SF-015 | Preserve the user's change when editing that file | active |

## Success Evidence

| Related requirement IDs | Evidence or acceptance condition | Verification method | Owner | Status |
| --- | --- | --- | --- | --- |
| R-002 | Preset entries visible on the left of the design screen | Play Mode + screenshot | developer | proposed |
| R-004, R-013 | Rocket drop → attach, ground drop → placed, void drop → discarded | Play Mode observation | developer | proposed |
| R-005 | Click attached engine → buttons appear; click empty space → hidden | Play Mode + screenshot | developer | proposed |
| R-006 | Right-dragging over the panel does not orbit the camera | Play Mode observation | developer | proposed |
| R-007, R-010 | Tilting one engine and launching rotates the rocket that way | Play Mode launch | developer | proposed |
| R-011 | Updated `Border.Simulation.EditModeTests` passes against the new rule | EditMode tests | developer | proposed |
| R-001 | Existing orbit, zoom and surface attachment still work | Play Mode + EditMode tests | developer | proposed |
| R-003, R-012 | Hovering shows SO values that differ per entry | Play Mode + screenshot | developer | deferred (phase 2) |

## Decision and Evidence Ledger

| ID | Kind | Statement | Evidence / rationale | Status | Consequence / linked IDs |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | Keep right-drag camera rotation as it is today | initial request (rev 1) | active | R-001 |
| UD-002 | user decision | Engines come from a preset panel on the left of the screen | initial request | active | R-002, RK-001 |
| UD-003 | user decision | Presets are "UI first" | initial request | active | Boundary fixed by UD-007 |
| UD-004 | user decision | Clicking an engine reveals move and rotate buttons | initial request | active | R-005, R-007, R-008 |
| UD-005 | user decision | Hovering a preset entry shows that engine's stats | initial request | active | R-003, UD-009 |
| UD-006 | user decision | Drag an engine from the preset into the map and attach it | initial request | corrected (rev 2) | Replaced by UD-007 / UD-010 |
| UD-007 | user decision | Scope is hover plus drag-to-place; a ground drop places the engine on the ground | Q-001 answer (rev 2) | active | R-004, R-013 |
| UD-008 | user decision | The rotate button rotates the part **and thrust follows that orientation** | Q-002 answer (rev 2) | active | R-007, R-010, R-011, R-009, RK-002, RK-007 |
| UD-009 | user decision | Hover stats use the **engine stat SO being built in another session** | Q-003 answer (rev 2) | active | R-012, RK-004, OI-010 |
| UD-010 | user decision | Rocket-surface drop **attaches immediately**; ground drop places; anything else cancels and discards | Q-006 answer (rev 3) | active | R-013, resolves OI-009 / OI-011 |
| UD-011 | user decision | Rotation is **free — no axis or angle limit**; revised (rev 4) to add a 45° aiming snap (7° tolerance, guide line only while it holds) | Q-007 answer (rev 3), revised by the user (rev 4) | active | R-007, resolves OI-012, strengthens RK-007 |
| UD-012 | user decision | Connect **after** that session's SO is final; phase 1 completes without it | Q-008 answer (rev 3) | active | R-012 deferred, mitigates RK-004 / RK-008 |
| UD-013 | user decision | End the planning interview and begin implementation | "로 하고 구현 시작해줘" (rev 4) | active | Finalization, implementation authorization |
| SF-001 | sourced fact | Right-drag orbit and wheel zoom already exist | `Assets/01. Scripts/Simulation/RocketBuilder.cs:66-80` | active | R-001 |
| SF-002 | sourced fact | Left-drag picks up an **existing** `RocketPart` under the cursor; there is no spawn path | `RocketBuilder.cs:82-127` | active | R-004, AR-003 |
| SF-003 | sourced fact | `Rocket.Attach` overwrites part rotation with the rocket's; thrust is `transform.up * engine.Thrust` | `Rocket.cs:28-33`, `Rocket.cs:63` | active | R-010, R-011, RK-002 |
| SF-004 | sourced fact | GDD 07 §5 lists **part catalogue** as a deliberate omission and states **"part orientation follows the rocket"** | `docs/artemis-2026-gdd/07_로켓_설계.md` §5 | active | RK-001, RK-002, R-009 |
| SF-005 | sourced fact | GDD 07 §3 and GDD 11 §7 both list "thrust direction adjustment" as allowed | `07_로켓_설계.md` §3, `11_UI_UX_화면설계.md` §7 | active | R-009 |
| SF-006 | sourced fact | `RocketEngine.prefab` exists with `thrust=1200`, `fuel=100`, `burnRate=20` | `Assets/03. Prefabs/Simulation/RocketEngine.prefab:4964-4967` | active | AR-003 |
| SF-007 | sourced fact | The previous research prototype built UGUI **in code** and spawned via `[RuntimeInitializeOnLoadMethod]`; latest GDD now supersedes this for production UI | `Assets/01. Scripts/Research/ResearchOperationUIController.cs:43-90`; GDD 11/18 | active | AR-001 superseded |
| SF-008 | sourced fact | `Border.Simulation.asmdef` references only `Border` and `Unity.InputSystem` but is `autoReferenced: true`, so UGUI/TMP are available | `Assets/01. Scripts/Simulation/Border.Simulation.asmdef` | active | AR-001, R-012 |
| SF-009 | sourced fact | `RocketBuilder` calls `Physics.Raycast` directly; no `IsPointerOverGameObject` guard exists anywhere in the project | `RocketBuilder.cs:87,102`, repo-wide grep | active | R-006, RK-003 |
| SF-010 | sourced fact | Engine stats are the four GDD 06/07 values (`FuelCapacity`/`Cooling`/`MaxOutput`/`IgnitionReliability`), range 0-100 | `07_로켓_설계.md` §6 | active | R-003, R-012 |
| SF-011 | sourced fact | `RocketPart` has only `thrust`/`fuel`/`burnRate`; the four stats are not implemented | `Assets/01. Scripts/Simulation/RocketPart.cs:9-12` | active | OI-010 |
| SF-012 | sourced fact | `engine-preset-stats-spec.md` is at rev 1 `active` with Q-001~003 unanswered — the SO type and fields are not settled | that document's Document State | active | R-012, RK-004, OI-010 |
| SF-013 | sourced fact | `Attach_KeepsWorldPoint_AndAlignsToRocket` locks the "orientation follows the rocket" rule | `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs:37-59` | active | R-011 |
| SF-014 | sourced fact | `SimulationTest.unity` **is** tracked by git; the prior spec's "untracked" note is stale | `git ls-files "Assets/00. Scenes"` | active | Scene edits land in commits |
| SF-015 | sourced fact | `07_로켓_설계.md` has a one-line uncommitted edit in §6.1 | `git diff` (rev 1) | active | R-009, RK-005 |
| SF-016 | sourced fact | GDD 11 §7 requires target path, design fit, probabilities and a launch button on the design screen | `11_UI_UX_화면설계.md` §7 | active | Out of scope but affects the final screen |
| SF-017 | sourced fact | GDD 18: "the player does not fly the rocket directly"; outcomes are pre-rolled | `18_확정사항_및_변경금지선.md` | active | Only design-stage interaction may grow |
| SF-018 | sourced fact | `activeInputHandler: 1` (Input System only) makes legacy `UnityEngine.Input` throw | `docs/rocket-simulation.md` | active | Limits input implementation |
| SF-019 | sourced fact | The prior spec's R-018/R-019 (integration into the main game) are `blocked` on its Q-011 | that document §6 | active | RK-006 |
| SF-020 | sourced fact | `docs/rocket-simulation.md` records part-following thrust as a **deliberately rejected** option — "a side engine would spin the rocket and the game becomes something else" | `docs/rocket-simulation.md` | active | RK-007, R-009 |
| SF-021 | sourced fact | `EndDrag` already branches on `_overRocket` between attach and revert — UD-010's three-way branch extends this point | `RocketBuilder.cs:129-138` | active | R-013, AR-008 |
| AR-001 | agent recommendation | Build the preset panel from prefabs and bind the developed preset list at runtime | Latest user correction rejects all-code UI generation for production screens | active | R-002 |
| AR-002 | agent recommendation | Guard `RocketBuilder`'s click handling with `EventSystem.current.IsPointerOverGameObject()` | No UI guard exists today (SF-009) | proposed | R-006, RK-003 |
| AR-003 | agent recommendation | Preset drag instantiates `RocketEngine.prefab` and hands it to the existing drag state | A separate placement path would duplicate raycast and collider handling | proposed | R-004, R-013 |
| AR-004 | agent recommendation | Define rotation as thrust-direction only and keep part orientation fixed | Was the minimal-change option | superseded (rev 2) | UD-008 chose part rotation |
| AR-005 | agent assumption | Show dummy hover stats and swap them once the SO lands | Would unblock UI work | superseded (rev 2) | UD-009/UD-012 chose to wait |
| AR-006 | agent recommendation | The move button overlaps the existing left-drag; entering a surface-drag mode after selection is the smallest addition | A separate move gizmo would duplicate the surface attachment rules | proposed | R-008, OI-003 |
| AR-007 | agent recommendation | Change thrust to `engine.transform.up` and remove only the rotation overwrite in `Rocket.Attach`; keep the attachment-point rule | Smallest diff that unlocks the single rule in question | proposed | R-010, R-011 |
| AR-008 | agent recommendation | Implement the three-way drop by extending the existing `_overRocket` branch (SF-021) | The branch point already exists; no new state machine needed | proposed | R-013 |
| AR-009 | agent recommendation | Keep the coupling surface to the other session narrow — the UI needs only "preset list plus displayable stats" | Two sessions defining the same type would collide at compile time | proposed | R-012, OI-010 |
| AR-010 | agent assumption | Without rotation limits, an inverted engine can drive the rocket into the ground | Thrust follows orientation, so extremes become physics immediately | confirmed (rev 3) | UD-011 chose no limits — intended |
| AR-011 | agent recommendation | Phase 1 must stand alone without the SO; leave the hover area unwired until phase 2 | UD-012 chose to wait for the SO, so phase 1 must not block on it | proposed | R-003, R-012 |
| AR-012 | agent assumption | **Move button** enters a surface-drag mode for the selected part, reusing the existing drag path rather than adding a gizmo | Q-004 was never asked; this is AR-006 applied so implementation can proceed | proposed | R-008, OI-003 — confirm with the user |
| AR-013 | agent assumption | **Target scene** is `SimulationTest.unity`, extended in place; the UI spawns from code so no scene YAML is edited | Q-005 was never asked; the prototype scene is the only design scene that exists | proposed | OI-006, RK-006 — confirm with the user |
| AR-014 | agent assumption | **Removal path** is Delete/Backspace on the selected part | Q-009 was never asked; without any removal path presets accumulate indefinitely (RK-009) | proposed | OI-007, RK-009 — confirm with the user |
| OI-001 | unresolved item | Boundary of "UI first" | conflicting statements in the request | resolved (rev 2) | UD-007 |
| OI-002 | unresolved item | What the rotate button rotates | tied to the locked orientation rule | resolved (rev 2) | UD-008 |
| OI-003 | unresolved item | Move button vs. existing left-drag | two paths make the interaction ambiguous | open | R-008, AR-006, AR-012, Q-004 |
| OI-004 | unresolved item | Source of hover stat values | needed before the UI can display anything | resolved (rev 2) | UD-009 |
| OI-005 | unresolved item | How many preset entries, and on what basis | panel layout undetermined | open | R-002, OI-010 |
| OI-006 | unresolved item | Target scene — extend `SimulationTest.unity` or build a new design screen | integration form is unresolved upstream (SF-019) | open | RK-006, AR-013, Q-005 |
| OI-007 | unresolved item | Engine removal and recovery | presets can be pulled out indefinitely with no way back | open | R-013, AR-014, Q-009 |
| OI-008 | unresolved item | Scope of the GDD 07 §5 revision — both clauses | leaving docs contradicting the build misleads later work | open | R-009, RK-001, RK-002 |
| OI-009 | unresolved item | Drop over the rocket surface | the most natural interaction was undefined | resolved (rev 3) | UD-010 |
| OI-010 | unresolved item | The other session's SO type name, fields, asset path and lookup | a single name mismatch fails compilation | deferred | UD-012 — revisit when that SO is final |
| OI-011 | unresolved item | Drop over neither ground nor rocket | unrecoverable instances | resolved (rev 3) | UD-010 |
| OI-012 | unresolved item | Rotation freedom and limits | extremes become physics results | resolved (rev 3) | UD-011 |

## Question Register

| ID | Decision needed | Why it matters | Related IDs | State | Asked / updated revision | Resolution |
| --- | --- | --- | --- | --- | --- | --- |
| Q-001 | Scope of this change | "UI first" conflicted with "drag and attach" | OI-001, UD-007 | answered | 1 → 2 | UD-007 |
| Q-002 | What the rotate button rotates | Locked rule, test and GDD clause depend on it | OI-002, UD-008 | answered | 1 → 2 | UD-008 |
| Q-003 | Source of hover stat values | The stat SO was unsettled | OI-004, UD-009 | answered | 1 → 2 | UD-009 |
| Q-006 | Drop over the rocket or the void | UD-007 defined only the ground case | OI-009, OI-011, UD-010 | answered | 2 → 3 | UD-010 |
| Q-007 | Rotation freedom and limits | Thrust follows orientation, so extremes are physical | OI-012, UD-011 | answered | 2 → 3 | UD-011 |
| Q-008 | Contract and ordering with the other session's SO | Type mismatch breaks compilation; concurrent edits collide | OI-010, UD-012 | answered | 2 → 3 | UD-012 |
| Q-004 | Move button vs. existing left-drag | Two interaction paths become ambiguous | OI-003, R-008, AR-012 | **open — never asked** | 1 → 4 | Implemented under AR-012 |
| Q-005 | Target scene | Scene-edit scope and integration form (RK-006) | OI-006, AR-013 | **open — never asked** | 1 → 4 | Implemented under AR-013 |
| Q-009 | Removal and recovery path for pulled-out engines | Presets can be pulled out indefinitely | OI-007, AR-014 | **open — never asked** | 3 → 4 | Implemented under AR-014 |

## Corrections and Revision History

| Revision | Trigger | Change | Corrected / superseded IDs | Downstream sections reconciled |
| --- | --- | --- | --- | --- |
| 1 | Initial request plus code, GDD and prior-spec inspection | Initial planning hypothesis | none | All sections; UD-001~006, SF-001~019, AR-001~006, OI-001~008, RK-001~006, R-001~009, Q-001~005 |
| 2 | Q-001/Q-002/Q-003 answers | Scope fixed to hover plus ground placement; thrust model switched to part-following; hover data bound to the other session's SO | UD-006 `corrected`; AR-004, AR-005 `superseded`; OI-001, OI-002, OI-004 `resolved` | Snapshot, Scope, Core flow, R-004 revised, R-007 unblocked, R-010~013 added, RK-002 reclassified, RK-007/008 added, Q-006~008 added |
| 3 | Q-006/Q-007/Q-008 answers | Three-way drop branch; no rotation limits; SO connection split into phase 2 | OI-009, OI-011, OI-012 `resolved`; OI-010 `deferred`; AR-010 `confirmed` | Snapshot (phases), Scope (phase column), Core flow step 4, Requirements, Constraints, Ledger (UD-010~012, SF-021, AR-011), RK-004/007/008 updated, Q-009 added |
| 4 | "로 하고 구현 시작해줘" — explicit finish and implementation authorization | Interview closed; document rewritten as the authoritative English version with a Korean mirror; Q-004/Q-005/Q-009 preserved as open and implemented under labelled assumptions | none — no unresolved item was silently decided | Document State, unresolved banner, AR-012~014 added, Question Register, Finalization and Handoff |

## Risks, Conflicts, and Dependencies

| ID | Kind | Risk, conflict, or dependency | Likelihood / impact | Mitigation, decision, or owner | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | conflict | GDD 07 §5 fixes "part catalogue" as a deliberate omission, yet the left preset panel is exactly that | high / medium | Revised by R-009; scope is OI-008 | SF-004, UD-002, OI-008 | open |
| RK-002 | conflict | The rotate button collides with the fixed-orientation rule (SF-003), the locked test (SF-013) and GDD 07 §5 | high / high | **Decided by UD-008/UD-011** — change the rule. Executed via R-010, R-011, R-009 | UD-008, R-010, R-011 | resolved (rev 2) |
| RK-003 | risk | A UGUI overlay leaks panel clicks and drags into 3D raycasts and camera orbit | high / medium | AR-002 made mandatory as R-006 | SF-009, R-006 | open |
| RK-004 | dependency | Hover stats depend on an SO being built in another session | high / medium | **Mitigated by UD-012** — phase 1 stands alone, phase 2 wires it | UD-009, UD-012, OI-010 | mitigated |
| RK-005 | dependency | `07_로켓_설계.md` has uncommitted changes | low / low | Preserve the user's edit; never overwrite | SF-015 | open |
| RK-006 | dependency | The upstream integration decision (prior spec Q-011) is still open, so the host scene is unsettled | medium / medium | Implemented under AR-013 | SF-019, OI-006 | open |
| RK-007 | risk | Thrust following part orientation **with no limits** changes the assembly game and allows inverted engines that crash the rocket | certain / high | Chosen by the user (UD-008, UD-011); documents revised to match | UD-008, UD-011, SF-020 | accepted |
| RK-008 | risk | Two sessions may edit the same area (`RocketPart`, spec docs, `Border` assembly) | medium / medium | **Mitigated by UD-012** — sequential work; check `git status` before starting | UD-009, UD-012 | mitigated |
| RK-009 | risk | Engines can be pulled from the panel indefinitely with no recovery path, littering the scene | medium / low | Implemented under AR-014 (Delete key) | OI-007, R-013 | open |

## Open, Skipped, and Deferred Items

| ID | Item | State | Consequence | Current position | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-003 | Move button vs. left-drag | open | Ambiguous interaction | AR-012 assumption in force | user | Q-004 |
| OI-005 | Preset entry count and basis | open | Panel layout undetermined | Phase 1 repeats one prefab; phase 2 uses the SO list | user | OI-010 |
| OI-006 | Target scene | open | Scene-edit scope | AR-013 assumption in force | user | Q-005 |
| OI-007 | Engine removal and recovery | open | Litter accumulates | AR-014 assumption in force | user | Q-009 |
| OI-008 | GDD 07 §5 revision scope (two clauses) | open | Docs contradict the build | Revise both clauses together | user | After Q-005 |
| OI-010 | Other session's SO contract | deferred | Name mismatch fails compilation | AR-009 (narrow coupling surface) | user | That session finalizing its SO |

## Coverage and Consistency Check

| Planning area | State | Supporting IDs | Remaining gap |
| --- | --- | --- | --- |
| Outcome | covered | UD-001~005, UD-007~013 | — |
| Users and stakeholders | covered | UD-009, UD-012, SF-012 | — |
| Scope | covered | UD-007, UD-010, UD-012 | Phases fixed |
| Non-goals | covered | SF-004, SF-016, UD-011 | — |
| Core flow | covered | UD-007, UD-008, UD-010, UD-011 | Move-button interaction assumed (OI-003) |
| Constraints | covered | SF-003~005, SF-009, SF-013, SF-018 | GDD 07 §5 revision presumed |
| Success evidence | covered | R-001~R-013 | R-003/R-012 are phase 2 |
| Risks and dependencies | covered | RK-001~009 | RK-007 `accepted`; RK-004/008 `mitigated` |
| Unresolved decisions | open | OI-003, OI-005~008, OI-010 | Q-004, Q-005, Q-009 never asked |
| Handoff and authorization | covered | UD-013 | Phase 1 implementation authorized |

## Finalization and Handoff

- **Authoritative document:** `docs/specs/rocket-design-ui-spec.md` (English, this file).
- **Korean mirror:** `docs/specs/rocket-design-ui-spec.ko.md` — same IDs, statuses, requirements, decisions,
  risks, unresolved items, interview state and next action.
- **Interview state:** `explicitly-finished`. No further planning questions will be asked.
- **Preserved without resolution:** Q-004 / Q-005 / Q-009 and OI-003, OI-005, OI-006, OI-007, OI-008, OI-010.
  None of these was decided on the user's behalf. AR-012, AR-013 and AR-014 are agent assumptions adopted so
  that implementation can proceed; each remains an assumption until the user confirms it.
- **Accepted consequence:** RK-007 — assembly play changes, and an inverted engine can crash the rocket.
- **Next action:** implement Phase 1 (R-001, R-002, R-004, R-005, R-006, R-007, R-009, R-010, R-011, R-013).
  Phase 2 (R-003, R-012) waits on the other session's SO.
- **Resume point if planning reopens:** ask Q-004, Q-005 and Q-009, then reconcile OI-008 (GDD 07 §5 revision
  scope) and OI-005 (preset entry count).

> Approving this plan does not by itself authorize commits, PRs, deployment or external changes.
> Implementation of Phase 1 was authorized separately in the same message (UD-013).
