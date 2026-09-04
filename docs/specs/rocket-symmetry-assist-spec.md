# Rocket Engine Alignment Guides

## Document State

| Field | Value |
|---|---|
| Project root | `C:\myGame\2026NHNAI` |
| Authoritative document (English) | `docs/specs/rocket-symmetry-assist-spec.md` |
| Korean mirror | `docs/specs/rocket-symmetry-assist-spec.ko.md` |
| Interview working language | Korean |
| Revision | 4 (final) |
| Timestamp | 2026-09-04 |
| Interview state | `explicitly-finished` — ended by "이제 구현해줘", which also authorized implementation |
| Unresolved at close | **Q-007 / Q-008 / Q-009 were never asked** (the finish signal arrived first). OI-004, OI-005, OI-007 stay open; RK-007 and RK-009 stay unmitigated. |
| Implementation | R-001 ~ R-007 implemented. R-008 `cancelled` by UD-007. **Nothing was run in Unity** — see §12. |
| Next authorized action | Run the EditMode suite and verify the guides in Play Mode. Committing is not authorized. |

Related documents: `docs/rocket-simulation.md` (current system description, updated by this work),
`docs/specs/rocket-prototype-revision-spec.md` and `docs/specs/rocket-design-ui-spec.md` (prior decisions),
`docs/artemis-2026-gdd/07_로켓_설계.md` (design-stage plan, revised by this work).

> **Two risks are deliberately left open.** RK-007: with three or more engines, "aligned" does not mean
> "balanced" — UD-005 (never move existing parts) and UD-007 (no balance readout) both declined a mitigation.
> RK-009: thrust follows `engine.transform.up` since a parallel change, so mirrored positions with different
> rotations still produce torque. Neither was silently solved.

---

## 1. Goal

When an engine is dropped onto the rocket surface, show a **Figma-style alignment guide** the moment it lines up
with an already-attached engine, and snap the coordinate exactly onto that line. The point is to remove the few
millimetres of hand error in a placement the player *meant* to be symmetric — not to remove the act of aiming for
symmetry (RK-002). It must work with three or more engines, not just a mirrored pair (UD-002).

Background: thrust is applied at the engine's actual attachment point, not at the centre of mass (SF-004), so a
small coordinate error becomes torque directly. Before this change there was no snapping, quantisation or guide
of any kind (SF-001, SF-002).

## 2. Sourced facts

| ID | Fact | Source |
|---|---|---|
| SF-001 | The attachment point was the raw surface raycast `hit.point`, passed straight to `rocket.Attach`. No snapping, quantisation or correction existed. | `Assets/01. Scripts/Simulation/RocketBuilder.cs` (pre-change) |
| SF-002 | `Rocket.Attach(part, worldPoint)` reparents and uses the given coordinate verbatim. | `Assets/01. Scripts/Simulation/Rocket.cs:33-38` |
| SF-003 | The EditMode test `Attach_KeepsWorldPoint_AndLeavesPartRotation` asserts the attachment coordinate is unchanged within `1e-4`. Correcting inside `Attach` would break it. | `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs:109-130` |
| SF-004 | Thrust is `AddForceAtPosition(..., engine.transform.position)` — the attachment position is the torque arm. | `Assets/01. Scripts/Simulation/Rocket.cs:85` |
| SF-005 | The rocket body is the child `Body` (Cylinder, localScale `(1,2,1)`, CapsuleCollider radius `0.5`, height `2`) → world radius 0.5 m, axis length 4 m. The root must stay at scale 1. | `Assets/00. Scenes/SimulationTest.unity:223-313`, `docs/rocket-simulation.md` |
| SF-006 | GDD 07 (status **확정**) §5 listed "그리드 스냅과 대칭 스냅" under *things deliberately not built*. | `docs/artemis-2026-gdd/07_로켓_설계.md` §5 (pre-change) |
| SF-007 | The prior spec's `UD-005` (no grid snap) and `UD-009` (no symmetry aid) were `active` when this document opened. | `docs/specs/rocket-prototype-revision-spec.ko.md:167,171` |
| SF-008 | `docs/rocket-simulation.md` recorded the same limitation as intentional, reasoning that "symmetry cancelling torque" *is* the skill test. | `docs/rocket-simulation.md` (pre-change) |
| SF-009 | Fuel is per-part, not a shared tank. Engines with different values run dry at different times, creating late asymmetric torque that placement cannot fix. | `RocketPart.cs`, prior spec `RK-009` |
| SF-010 | There is no overlap or minimum-clearance check. Engines can be stacked on the same spot. | `docs/rocket-simulation.md` |
| SF-011 | GDD 07 §8 already asks for a "무게중심 이탈 경고" and a "예상 경로가 옆으로 휨" warning — the plan already has a place to report balance. | `docs/artemis-2026-gdd/07_로켓_설계.md` §8 |
| SF-012 | The simulation is its own assembly and reads Input System directly. | `RocketBuilder.cs` |
| SF-014 | The project had **no** `LineRenderer` anywhere. Drawing a runtime line needs a new component and material (`Gizmos`/`Debug.DrawLine` are editor-only). | `grep -rl LineRenderer Assets` → no hits |
| SF-015 | A non-Uber material precedent exists: `EngineFlame.mat` uses URP `Particles/Unlit`. Using an Uber shader with a new keyword combination forces `UberShaderVariantManifest` row-count and `UberShaderSuiteTests` constant updates plus `.shadervariants` regeneration. | `EngineFlame.mat:11`, `docs/rocket-simulation.md` |
| SF-016 | **Changed mid-interview by parallel work.** `Rocket.Attach` no longer overwrites part rotation, and thrust is now `engine.transform.up * engine.Output` — not the rocket's up. Part *rotation* is now part of the torque, so mirrored positions alone no longer guarantee cancellation. | `Rocket.cs:85`, `docs/rocket-simulation.md`, `docs/specs/rocket-design-ui-spec.md` UD-008 |
| SF-017 | **Changed mid-interview by parallel work.** The namespace moved `Border.Simulation` → `Simulation`, and `RocketBuilder` gained a preset panel, part selection, and Move / Rotate edit modes. | `Assets/01. Scripts/Simulation/*.cs`, `docs/specs/rocket-design-ui-spec.md` |
| SF-018 | In Move mode the selected part stays parented to the rocket (only its collider is disabled), unlike drag, which detaches it. A shared snap helper must therefore exclude the moving part explicitly. | `RocketBuilder.SetMode`, `RocketBuilder.StartDragging` |

## 3. Scope

**In scope (implemented):**

- Guide lines during drag when the point lines up with an existing engine, and a snap onto that line (UD-002, UD-006)
- Three or more engines, using the same rule — existing parts never move (UD-005)
- No correction outside the tolerance, so deliberate asymmetry stays possible (UD-004)
- The same behaviour on both placement paths: drag-to-attach and Move mode (SF-018)
- Revising GDD 07 §5 and `docs/rocket-simulation.md` (UD-003)

**Out of scope:**

- Torque balance readout (UD-007 excluded it explicitly)
- Automatic redistribution or even spacing of existing parts (UD-005 excluded it)
- Rotation alignment or rotation snapping (never decided; see RK-009)
- Overlap and minimum-clearance checks (SF-010)
- Late asymmetry from differing fuel (SF-009)
- Grid quantisation unrelated to existing parts

## 4. Coordinates and the definition of alignment

In rocket-local space the axis is `up` (local +Y). An attachment point becomes:

- **height** = `y`
- **azimuth** = `atan2(x, z)`
- **radius** = `sqrt(x² + z²)` ≈ 0.5 (body surface, SF-005)

The two are handled **independently**, exactly as Figma treats its x and y guides.

| Alignment | Condition | On screen | Status |
|---|---|---|---|
| Same height | `y` within tolerance of an existing engine's `y` | horizontal **ring** at that height | implemented |
| Same azimuth | azimuth equals an existing engine's (one vertical line) | **vertical line** at that azimuth | implemented |
| Mirrored | azimuth differs by 180° | **vertical line** at that azimuth | implemented |
| Even spacing (N engines) | azimuth at `360°/N` intervals | — | **excluded** (UD-005) |

**Alignment is not balance.** For equal height, radius and thrust, torque cancels when the azimuth unit vectors
sum to zero: 180° for two, 120° for three, 90° for four. If two engines already sit at 0°/180°, **no third
position balances them.** UD-005 forbids moving existing parts and UD-007 forbids a balance readout, so this is
only discovered by launching (RK-007).

## 5. Candidate generation

Each frame, while the cursor is over the rocket:

1. Convert the surface hit to rocket-local `(height, azimuth, radius)`.
2. Build candidates from every **other** attached engine — the part being moved is excluded (AR-013, SF-018).
   - candidate heights = each engine's `y`
   - candidate azimuths = each engine's `θ` and `θ + 180°`
3. On each axis, if the nearest candidate is within tolerance, replace the value; otherwise keep it.
4. Enable the guide for each replaced axis and disable the other.
5. Convert back to world space and use that as the preview position.

With no engines attached there are no candidates, so the first engine attaches freely (AR-014). If nothing snaps,
the original coordinate is returned unchanged rather than reconstructed, so no floating-point drift is introduced.

## 6. Recommendations and assumptions

| ID | Recommendation / assumption | Status |
|---|---|---|
| AR-001 | Mirror-partner snap (pairs only, no guides). | `superseded` (UD-002) |
| AR-002 | Angle and height quantisation. | `superseded` (UD-002) |
| AR-003 | Physics torque compensation. | `superseded` (UD-002) — visible layout and actual force would diverge, negating SF-008 |
| AR-004 | Auto-align button that redistributes everything evenly. | `cancelled` (UD-005) |
| AR-005 | Put the correction in `RocketBuilder`, not `Rocket.Attach`, so the "coordinate is used verbatim" contract and its test survive, and correction stays an input-stage concern. | `implemented` |
| AR-006 | Apply the correction to the drag preview so nothing jumps on release. | `implemented` |
| AR-007 | Initial tolerances: azimuth ±20°, height ±0.25 m. At radius 0.5 m that is an arc of about 0.17 m. Exposed as `[SerializeField] private` for in-play tuning. | `implemented`, values unverified (OI-005) |
| AR-008 | Keep feedback minimal; the snap itself is the feedback. | `superseded` (UD-006) |
| AR-009 | Guides do not remove SF-009 (fuel differences) or SF-010 (overlap), so "I lined it up and it still tips" remains reachable. | `active` |
| AR-010 | Draw the guides with two `LineRenderer`s — a 32-segment `loop` ring and a 2-point line. Shortest runtime path given SF-014. | `implemented` |
| AR-011 | Use URP `Unlit` rather than an Uber shader, avoiding the variant-manifest cost (SF-015). | `implemented`, but via `Shader.Find` at runtime instead of a new `.mat` asset — one fewer asset and `.meta` to maintain. Assign `guideMaterial` before shipping a build, since `Shader.Find` can be stripped. |
| AR-012 | Create the guide objects in code, not in the scene, to avoid growing the scene YAML diff. | `implemented` |
| AR-013 | Exclude the part currently being moved from the candidate list, or it snaps to its own position. | `implemented` — required for Move mode (SF-018); drag already detaches the part |
| AR-014 | With no engines attached the first part places freely. | `implemented` |

## 7. Requirements

| ID | Requirement | Basis | Status |
|---|---|---|---|
| R-001 | While dragging, if the height or azimuth is within tolerance of a candidate derived from an existing engine (same height / same azimuth / opposite azimuth), snap that axis exactly. Existing parts do not move. | UD-001, UD-002, UD-004, UD-005 | `implemented` |
| R-002 | A snapped height shows a horizontal ring; a snapped azimuth shows a vertical line; leaving the tolerance hides them. | UD-002, UD-006 | `implemented` |
| R-003 | Outside the tolerance nothing is corrected — deliberate asymmetry remains possible. | UD-004, RK-002 | `implemented` |
| R-004 | `Rocket.Attach` keeps its verbatim-coordinate contract and the existing EditMode tests still pass. | SF-003, AR-005 | `implemented`, **not run** (§12) |
| R-005 | Add an EditMode test for candidate generation and snapping: inside tolerance → exact candidate; outside → unchanged; three or more engines; existing coordinates untouched. | SF-003, UD-002, UD-005 | `implemented`, **not run** (§12) |
| R-006 | Revise GDD 07 §5 and `docs/rocket-simulation.md`: remove the alignment prohibition, and keep grid snapping, even spacing, balance display and rotation snapping listed as not built. | UD-003, UD-005, SF-006, SF-008 | `implemented` |
| R-007 | Three or more engines use the same rule — candidates come from every attached engine and nothing is redistributed. | UD-002, UD-005 | `implemented` |
| R-008 | Show the current torque balance. | SF-011, RK-007 | `cancelled` (UD-007) |

## 8. User decisions

| ID | Decision | Status |
|---|---|---|
| UD-001 | "로켓에 엔진 부착하는 부분이 약간 대칭이나, 이런거 자동 보정 되도록 하고 싶은데" — near-symmetric placement should be corrected automatically. | `active` |
| UD-002 | **Q-001 = free text.** "엔진이 3개 이상일수도 있는데 피그마나 레이아웃 둘 때 선 뜨는것처럼 그런식으로 처리하면 좋을 거 같다" — use a Figma-style alignment-guide model rather than a mirror-pair snap, and support three or more engines. | `active` |
| UD-003 | **Q-002 = 1.** Revise GDD 07 §5; remove alignment aids from the "not built" list. | `active` |
| UD-004 | **Q-003 = 1.** Correct only within a tolerance, and reflect it in the drag preview so it is visible before release. | `active` |
| UD-005 | **Q-004 = 1.** Align against existing engines only — same height / same azimuth / opposite azimuth. **Existing parts never move.** No even-spacing candidates, no redistribution. | `active` |
| UD-006 | **Q-005 = 1.** Draw exactly two guide kinds: the height ring and the azimuth line. No ghost preview, no always-on display, no mirror-specific highlight. | `active` |
| UD-007 | **Q-006 = 3.** Do **not** display torque balance. Keep the current prototype behaviour of finding out by launching. | `active` |
| UD-008 | "이제 구현해줘" — end the interview and implement the plan. Committing was not included. | `active` |

## 9. Risks, conflicts and dependencies

| ID | Item | Response | Status |
|---|---|---|---|
| RK-001 | GDD 07 (확정) and the prior spec's UD-005 / UD-009 forbade symmetry snapping. | UD-003 chose revision; R-006 carried it out. | `resolved` |
| RK-002 | Strong correction would erase "assemble it properly and it flies straight" (SF-008). | UD-004 (tolerance only), UD-005 (existing parts fixed), R-003 (deliberate asymmetry possible). | `active` |
| RK-003 | Symmetric placement does not guarantee symmetric flight — differing fuel creates late torque (SF-009). | Declared out of scope; UD-007 removed the mitigation. | `active` |
| RK-004 | Correcting inside `Attach` would break the existing test (SF-003). | AR-005 put the correction in `RocketBuilder`. | `resolved` |
| RK-005 | The rule for three or more engines was undefined. | UD-005 fixed it. | `resolved` |
| RK-006 | With no clearance check (SF-010), a point whose height *and* azimuth both snap lands exactly on top of an existing engine. | Out of scope. Not mitigated. | `active` |
| RK-007 | **Alignment ≠ balance.** With two engines at 0°/180°, no third position balances them. The guide says "aligned" while the rocket still tips. | UD-005 and UD-007 both declined a mitigation. **No response.** | `open` (OI-007) |
| RK-008 | The project had no way to draw a runtime line (SF-014). | AR-010 ~ AR-012. | `resolved` |
| RK-009 | **Rotation is not aligned.** Since SF-016, thrust follows `engine.transform.up`, so two engines at mirrored positions with different rotations do not cancel. Position alignment alone no longer guarantees straight flight even for two engines. | Discovered after UD-005 / UD-006 were taken; no decision was requested before the finish signal. **Not mitigated.** | `open` |

## 10. Unresolved items

| ID | Item | Consequence |
|---|---|---|
| OI-001 | GDD 07 §5 handling. | `resolved` (UD-003) |
| OI-002 | Whether to correct azimuth only or height too. | `resolved` (UD-002 / UD-006 — both, independently) |
| OI-003 | The rule for three or more engines. | `resolved` (UD-005) |
| OI-004 | Whether this applies to the prototype scene only or to the main design scene. | `open` — implemented in the prototype scene's `RocketBuilder`. The main-game question is still tied to the prior spec's Q-011. |
| OI-005 | The tolerance values. | `open` — ±20° / ±0.25 m are shipped as inspector defaults and have not been play-tested. Too large means unwanted snapping; too small reads as "it won't attach". |
| OI-006 | Guide line width, colour and duration. | `resolved` — inspector values (`guideWidth`, `guideColor`); no separate decision needed. |
| OI-007 | Whether RK-007 is accepted as intended difficulty or deferred to another round. | `open` — never asked (Q-009 was cut off by the finish signal). |
| OI-008 | Whether rotation should also be aligned (RK-009). | `open` — raised by SF-016 after the relevant decisions were made. |

## 11. Question register

| ID | Question | Status |
|---|---|---|
| Q-001 | Correction mechanism | `answered` (free text — Figma-style guides) → UD-002 |
| Q-002 | GDD 07 §5 handling | `answered` (1) → UD-003 |
| Q-003 | Correction strength and visibility | `answered` (1) → UD-004 |
| Q-004 | Rule for three or more engines | `answered` (1) → UD-005 |
| Q-005 | Which guides to draw | `answered` (1) → UD-006 |
| Q-006 | Torque balance display | `answered` (3) → UD-007, R-008 `cancelled` |
| Q-007 | Scope: prototype scene vs. main design scene | `skipped` — registered but never asked; the finish signal arrived first. OI-004 stands. |
| Q-008 | Tolerance values | `skipped` — same. Defaults shipped as inspector values; OI-005 stands. |
| Q-009 | Disposition of RK-007 | `skipped` — same. OI-007 stands. |

## 12. Implementation and verification

Changed files:

- `Assets/01. Scripts/Simulation/RocketBuilder.cs` — `Align` (static, pure), the `Alignment` struct, `SnapToGuides`,
  `ShowGuides` / `HideGuides` / `CreateGuide`, the guide tolerance and appearance fields, and the calls from
  `Drag`, `EndDrag`, `SetMode` and `EditSelected`.
- `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs` — `Align_SnapsHeightAndAzimuthIndependently_OnlyWithinTolerance`.
- `docs/artemis-2026-gdd/07_로켓_설계.md` §5 and `docs/rocket-simulation.md` (R-006).

**Verification actually run: none.** The Unity Editor holds the project lock and a parallel session was editing
the same files during this work, so a batchmode test run was not possible, and the MCP for Unity bridge was not
connected this session. Not tested: compilation, the EditMode suite, and the on-screen appearance of the guides
in Play Mode. Every implementation status above means "written", not "verified".

Known cost of the parallel edits: an earlier copy of this change was overwritten when `RocketBuilder.cs` was
rewritten mid-session, and was re-applied onto the current file.

## 13. Revision history

| Revision | Change |
|---|---|
| 1 | Initial draft. UD-001, SF-001 ~ SF-013, RK-001, Q-001 ~ Q-003. |
| 2 | UD-002 / UD-003 / UD-004. AR-001 ~ AR-003, AR-008 `superseded`; AR-010 ~ AR-012 added. SF-014 / SF-015 added. Rewrote the model as alignment guides. RK-007 / RK-008 added. Q-004 ~ Q-006 opened. |
| 3 | UD-005 / UD-006 / UD-007. R-008 `cancelled`; AR-004 `cancelled`; AR-013 / AR-014 added. RK-007 escalated to an unmitigated conflict. Q-007 ~ Q-009 opened. |
| 4 | Finish signal (UD-008). Implemented R-001 ~ R-007 and R-006's document revisions. SF-016 ~ SF-018 added from parallel changes; RK-009 and OI-008 opened as a consequence. Q-007 ~ Q-009 marked `skipped`. Rewritten in English with a Korean mirror. |

## 14. Handoff

Implemented but **unverified**. The next actions, each separately authorized:

1. Compile and run `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs`.
2. Check the guides in Play Mode — ring, line, and that the tolerances feel right (OI-005).
3. Decide RK-009 / OI-008: whether rotation should be aligned too, now that thrust follows the part's own up.
4. Decide RK-007 / OI-007: whether "aligned but unbalanced" with three or more engines is accepted as intended.

Committing was not requested and has not been done.
