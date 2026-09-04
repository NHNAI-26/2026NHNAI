# Rocket Launch Prototype Revision — Plan

## Document State

| Field | Value |
|---|---|
| Project root | `C:\myGame\2026NHNAI` |
| Authoritative document (English) | `docs/specs/rocket-prototype-revision-spec.md` |
| Korean mirror | `docs/specs/rocket-prototype-revision-spec.ko.md` |
| Working language during interview | Korean |
| Revision | 6 (final, second round applied) |
| Timestamp | 2026-09-04 |
| Interview state | `explicitly-finished` — round 1 ended by "이제 그만하고 구현해줘"; round 2 (§14) planned and implemented under a separate approval |
| Unresolved | **Q-011 / OI-011 / RK-001** — the main-game integration conflict is still unresolved after both rounds |
| Implementation | R-001~R-017 (round 1) and R-020~R-029 (round 2, see §14) implemented and verified. R-018 partially unblocked by UD-016 (prefab promotion only); its remainder and R-019 stay blocked. |
| Next action | Answer Q-011, then plan main-game integration (R-018 remainder, R-019). Nothing else is authorized. |

Related documents: `docs/rocket-simulation.md` (as-is system description, updated by this work),
`docs/artemis-2026-gdd/08_로켓_발사.md`, `docs/artemis-2026-gdd/18_확정사항_및_변경금지선.md`.

> **Open conflict at finish.** UD-013 ("all of these features go into the main game") contradicts SF-010
> (GDD 18 change-freeze line: "the player does not directly control", results decided by probability
> beforehand; GDD 08 §1: "manual control is not implemented"). The user ended the interview before
> choosing a resolution. This was **not** silently decided. Q-011 remains open and R-018 / R-019 stay
> `blocked`. The prototype-scope requirements (R-001~R-017) are unaffected and were implemented.

---

## 1. Outcome

Extend the existing rocket assembly/launch prototype into a physics sandbox where the player can
**attach parts anywhere on the rocket surface, orbit the view by dragging, and watch the rocket fall
under gravity once its engines run out of fuel.**

Before this work: (a) attachment was limited to two fixed bottom slots, (b) the camera could not rotate,
(c) there was no fuel, so thrust was infinite and the rocket rose forever.

Per UD-013 these features are intended for the main game, but the integration form is unresolved (OI-011).

## 2. Verified facts (as inspected)

| ID | Fact | Source |
|---|---|---|
| SF-001 | Attachment slots were two empty child Transforms at local `(-0.8, -1.6, 0)` (Slot_L) and `(0.8, -1.6, 0)` (Slot_R) — **both at the bottom**. This was the direct cause of "parts only attach at the bottom". | `Assets/00. Scenes/SimulationTest.unity:132,147,859,874` |
| SF-002 | Attachment used `Rocket.FindFreeSlot`, picking the nearest **empty** slot within `snapRadius` (1.0 m); occupancy was `slot.childCount > 0`. | `Assets/01. Scripts/Simulation/Rocket.cs:29-47` (pre-change) |
| SF-003 | **Gravity was already enabled.** Physics `m_Gravity: {x:0, y:-9.81, z:0}`; rocket Rigidbody `m_UseGravity: 1`, `m_Mass: 100`, `m_AngularDamping: 4`. | `ProjectSettings/DynamicsManager.asset:7`, scene `:309,311,323` |
| SF-004 | Thrust was unlimited: `FixedUpdate` applied `AddForceAtPosition` every frame and `RocketPart` had only a `thrust` field. This — not missing gravity — was why the rocket never fell. | `Rocket.cs:71-78`, `RocketPart.cs:10-12` (pre-change) |
| SF-005 | The camera had no rotation input; after launch `LateUpdate` only followed the rocket at a fixed offset. Initial position `(0, 4, -14)`. | `RocketBuilder.cs:34-38`, scene `:478` (pre-change) |
| SF-006 | Part dragging occupied the **left mouse button**. Right and middle buttons were unbound. | `RocketBuilder.cs:28-31` (pre-change) |
| SF-007 | The drag plane passed through the part's starting position facing the camera, so free camera rotation would rotate that reference plane too. | `RocketBuilder.cs:50-58` (pre-change) |
| SF-008 | `RocketSlotTests.FindFreeSlot_SkipsOccupied_AndRespectsRadius` locked the slot model; changing the attachment model required replacing it. | `Assets/Tests/EditMode/Simulation/RocketSlotTests.cs:8-25` (deleted) |
| SF-009 | `Border.Simulation.asmdef` references `Border` and `Unity.InputSystem`; legacy `UnityEngine.Input` is unusable because `activeInputHandler: 1`. | `Border.Simulation.asmdef`, `docs/rocket-simulation.md` |
| SF-010 | GDD 18 fixes as **change-freeze lines**: "the player does not directly control", results are probabilistic and decided once before the scene, the 3D scene only presents the decided result. GDD 08 §1: "multi-stage separation and manual control are not implemented"; §10 lists no drag, no real trajectory computation. | `docs/artemis-2026-gdd/18_확정사항_및_변경금지선.md`, `docs/artemis-2026-gdd/08_로켓_발사.md` |
| SF-011 | `docs/rocket-simulation.md` described the scope as "two engines attached, vertical launch" and excluded fuel explicitly; it had to be updated with this change. | `docs/rocket-simulation.md:6-7` (pre-change) |
| SF-012 | The rocket's collider is the **CapsuleCollider on the child `Body`** (`!u!136`), `Body` scale `(1, 2, 1)`. The root `Rocket` has no collider. Engines use BoxColliders (`!u!65`), the ground a MeshCollider (`!u!64`). Surface raycasting therefore targets that capsule and identification needs `GetComponentInParent<Rocket>()`. | scene `:236,566,678,805,182` |
| SF-013 | Existing code already exposes tuning values as `[SerializeField] private` (`thrust = 1200`, `snapRadius = 1`), so exposing fuel the same way matches project conventions. | `RocketPart.cs:10`, `Rocket.cs:11-12` (pre-change) |

## 3. Users and stakeholders

- **Primary user**: the developer, verifying part placement and physics tuning by eye.
- **End users**: `ARTEMIS: 2026` players — per UD-013 these controls reach the shipping game, so the
  integration form (Q-011) determines what they actually get.

## 4. Scope

### In scope (implemented)
- Slot model removed; surface raycast free attachment (UD-005)
- Right-drag orbit camera (UD-006)
- Per-engine finite fuel (amount + burn rate), inspector-exposed, fixed mass (UD-007, UD-010, UD-011)
- Fall under gravity after burnout; ground contact is plain physics collision (UD-004, UD-008)
- Remaining fuel reported through `Log.D` only (UD-012)
- `docs/rocket-simulation.md` updated; `RocketSlotTests` replaced (SF-008, SF-011)

### Blocked (not implemented — awaiting Q-011)
- Structural work for main-game integration: input/physics separation, prefab promotion, wiring to the
  pre-decided probabilistic result, GDD amendments (R-018, R-019, OI-011)
- Whether the main game needs a part catalogue / assembly UI (OI-012)

### Out of scope (declined)
- Grid or angle snapping (UD-005)
- Surface-normal alignment, mirrored placement, minimum-spacing check (UD-009)
- Reset key, destruction verdict, explosion VFX on ground contact (UD-008)
- On-screen fuel gauge (UD-012)
- Stage separation, drag, altitude-dependent atmosphere, gimbal control (AR-005)
- Mass loss from fuel consumption (UD-007)

## 5. Core flow (as built)

1. **Assembly** — the rocket is kinematic. **Right-drag** orbits the camera around it; the wheel zooms.
2. **Left-drag** picks up a part in the scene; already-attached parts can be picked up and moved again.
3. While dragging, a ray from the cursor hits the rocket surface (`Body` capsule, SF-012) and the part is
   placed at that point, keeping the rocket's orientation (UD-009).
4. Releasing over the surface attaches it there; releasing elsewhere returns the part to its origin.
5. **Launch** — Space. Kinematic off, engine list collected, fuel refilled, `Log.D` records the engine count.
6. **Ascent** — thrust is applied at each engine's position, so asymmetry becomes torque (unchanged behaviour).
7. **Per-engine burnout** — an engine that runs dry stops producing thrust and logs one line. Torque appears
   toward the remaining engines. Identical parts run dry simultaneously, so this only shows when values differ (AR-010).
8. **All engines dry** — no thrust; gravity alone decelerates the rocket through apex and back down.
9. **Ground contact** — plain physics collision with the ground MeshCollider. No reset, explosion or verdict
   (UD-008). Repeat runs require restarting Play Mode (AR-011).

### Alternate / error flows
- Failed attachment returns the part to its drag origin.
- After launch, part dragging is disabled; camera orbit stays live (AR-004).
- Launching with zero engines or zero fuel produces no thrust and no special handling (AR-012).

## 6. Requirements

| ID | Requirement | Provenance | Status |
|---|---|---|---|
| R-001 | Parts can be attached somewhere other than the rocket bottom. | UD-001, SF-001 | `implemented` |
| R-002 | The attachment point is the surface point hit by the cursor ray. Fixed slots, `FindFreeSlot` and `snapRadius` are removed. | UD-005, SF-012 | `implemented` |
| R-003 | The camera can be rotated around the rocket by dragging, to view it from any direction. | UD-002, SF-005 | `implemented` |
| R-004 | Camera rotation is bound to **right-drag** and never coincides with left-drag part dragging. | UD-006, SF-006 | `implemented` |
| R-005 | Part dragging is anchored to the surface raycast, not a screen plane, so any camera angle attaches where the cursor points. | UD-005, UD-006, SF-007 | `implemented` |
| R-006 | Each engine part holds finite fuel and a burn rate, consumed while producing thrust. | UD-003, UD-007 | `implemented` |
| R-007 | Fuel is stored **per engine part**; rocket mass stays constant regardless of fuel consumed. | UD-007, RK-005 | `implemented` |
| R-008 | An engine at zero fuel produces no thrust; when all engines are dry the rocket decelerates and falls under gravity alone. | UD-004, SF-003, SF-004 | `implemented` |
| R-009 | Remaining fuel is reported only through `Border.Core.Log.D` (launch, per-engine burnout, all dry). No screen UI. | UD-012 | `implemented` |
| R-010 | `docs/rocket-simulation.md` is updated in the same change. | SF-011, project CLAUDE.md | `implemented` |
| R-011 | `RocketSlotTests` is replaced for the surface model, and fuel depletion keeps one EditMode check. | SF-008, UD-005, UD-007 | `implemented` |
| R-012 | The dragged part's own collider must not block the surface raycast. | UD-005, SF-012 | `implemented` |
| R-013 | Attached parts keep the rocket's orientation (thrust = rocket up). No normal alignment, mirroring or spacing check. | UD-009, AR-008 | `implemented` |
| R-014 | Fuel is stored as amount (`fuel`) and rate (`burnRate`), defaulting to `100` / `20` — a 5-second burn. | UD-010, UD-011 | `implemented` |
| R-015 | Ground contact gets no special handling; the physics result stands. | UD-008 | `implemented` |
| R-016 | `fuel` and `burnRate` are `[SerializeField] private` with read-only properties, tunable per part in the Inspector without code changes. | UD-011, SF-013 | `implemented` |
| R-017 | Orbit camera sensitivity, pitch limits and distance limits are inspector-exposed as well. | UD-011, AR-009 | `implemented` |
| R-018 | Define the structural separation (input vs. physics), prefab promotion and wiring to the pre-decided probabilistic result required for main-game integration. | UD-013, OI-011 | `blocked` (Q-011) |
| R-019 | Amend the relevant GDD 08 / GDD 18 clauses, or reshape the integration to fit them. Documentation and implementation must not stay contradictory. | UD-013, SF-010 | `blocked` (Q-011) |

## 7. Constraints

- Unity 6000.3.10f1 / URP 17.3. Input System only; legacy `UnityEngine.Input` throws (SF-009).
- The `Rocket` root must stay at scale 1.
- Surface identification requires `GetComponentInParent<Rocket>()` — the collider sits on the child `Body` (SF-012).
- Scene edits are a last resort; `SimulationTest.unity` is still untracked in git.
- Serialized fields are `[SerializeField] private` with public read-only properties (project CLAUDE.md, SF-013).

## 8. Success evidence

| ID | Evidence | Result |
|---|---|---|
| SE-1 | A part attached somewhere other than the bottom. | **Met** — both engines attached at local `y = +0.6` (mid-body side), parented to `Rocket`. |
| SE-2 | Camera orbits to view and attach on the far side. | **Partially met** — orbit math verified in Play Mode (yaw 0 → offset `(0, 2, -14)`; yaw 90 → `(-14, 2, 0)`; distance 14.1 preserved; camera aim error 0.0°). Actual mouse dragging was not exercised. |
| SE-3 | Ascent → burnout → apex → fall in Play Mode. | **Met** — t=7.6 s: y=328.4, vy=+46.0, both engines at fuel 0; t=15.6 s: y=381.3, vy=−32.6, tilt 0.0° (symmetric placement cancels torque). Mass held at 100 throughout. |
| SE-4 | EditMode tests pass. | **Met** — `Border.Simulation.EditModeTests`: 2 passed, 0 failed. |
| SE-5 | Changing `burnRate` in the Inspector changes burn length. | **Not tested** — the field is exposed and used, but no run varied it. |

## 9. Risks, conflicts, dependencies

| ID | Item | Response | Status |
|---|---|---|---|
| RK-001 | **Direct conflict with the shipping design.** UD-013 puts these manual controls in the main game, while GDD 18 forbids direct player control and fixes probabilistic pre-decided results, and GDD 08 §1 forbids manual control (SF-010). | Q-011. R-018 / R-019 blocked until answered. | **`conflict` — open at finish** |
| RK-002 | Free surface placement replaces the slot model and its test outright. | Done deliberately (R-002, R-011). | `resolved` |
| RK-003 | Sharing the left button between rotation and attachment causes misinput. | Right-drag separation (UD-006). | `resolved` |
| RK-004 | Free camera rotation plus screen-plane dragging makes far-side depth unreadable. | The surface raycast lets the surface decide depth (R-005). | `resolved` |
| RK-005 | Reducing mass with fuel would spike late acceleration and invalidate the tuning table. | Mass held constant (UD-007). | `resolved` |
| RK-006 | The scene file is untracked, so there is no rollback baseline. | Committing the prototype before further work is still recommended (AR-007). | `active` |
| RK-007 | Removing slots would force a scene edit. | **Deviation from plan**: the two empty `Slot_L` / `Slot_R` Transforms were left in the scene instead of hand-editing scene YAML. Nothing references them; deleting them in the Editor is harmless. | `accepted` |
| RK-008 | Free placement allows overlapping parts, stacking thrust at one point. | Deliberately accepted (UD-009); revisit if it bites (OI-009). | `accepted` |
| RK-009 | Differing per-engine burnout times create strong late asymmetric torque. | Identical parts run dry together (AR-010); tune `angularDamping` when values differ. | `active` |
| RK-010 | No reset means Play Mode restart per trial. | Accepted (UD-008). | `accepted` |
| RK-011 | **If main-game integration is confirmed, prototype compromises become debt** — primitive placeholder assets, scene-placed parts, overlap allowed (RK-008), no reset (RK-010), log-only feedback (UD-012) were all accepted on "it is only an experiment" grounds. | Re-examine after Q-011; any decision to reverse turns its UD `corrected`. | `active` |

## 10. Decision ledger

### User decisions (UD)
| ID | Content | Status |
|---|---|---|
| UD-001 | "Parts should attach where I want, but right now they only attach at the bottom." | `active` |
| UD-002 | "I want to rotate the view by dragging to see the position I want." | `active` |
| UD-003 | "The fuel amount is fixed." | `active` |
| UD-004 | "Gravity exists and it should fall when the fuel runs out." | `active` |
| UD-005 | **Q-001 = 2.** Surface raycast free attachment; slots and `FindFreeSlot` removed; no grid snapping. | `active` |
| UD-006 | **Q-002 = 1.** Right-drag orbit rotation; left-drag stays part dragging. | `active` |
| UD-007 | **Q-003 = 1.** Fuel per engine part; rocket mass fixed. | `active` |
| UD-008 | **Q-004 = 1.** Ground contact is plain physics collision — no reset key, verdict or explosion. | `active` |
| UD-009 | **Q-006 = 1.** Attached parts keep the rocket's orientation; no normal alignment, no symmetry aid, no spacing check. | `active` |
| UD-010 | **Q-007 = 4.** Store fuel as amount plus burn rate rather than seconds. | `active` |
| UD-011 | **Q-008 = 1 plus "make it inspector-adjustable".** Defaults `fuel = 100`, `burnRate = 20` (5-second burn with two engines); fuel and camera parameters tunable in the Inspector. | `active` |
| UD-012 | **Q-010 = 1.** Remaining fuel via `Log.D` only; no screen UI. | `active` |
| UD-013 | **Q-005, free-text: "all of these features are going into the main game."** | `active` — conflicts with SF-010 (RK-001) |
| UD-014 | "이제 그만하고 구현해줘" — end the planning interview and implement. Read as an explicit finish signal plus explicit implementation authorization. | `active` |

### Agent recommendations / assumptions (AR)
| ID | Content | Status |
|---|---|---|
| AR-001 | "I want gravity" was assumed to mean the effect was invisible under infinite thrust, not that gravity was off; the fuel work resolves it (SF-003, SF-004). | `proposed` — supported by the Play Mode run (SE-3) |
| AR-002 | Orbit rotation around the rocket suits assembly better than a free-fly camera. | `resolved` (UD-006) |
| AR-003 | Minimal fuel feedback is enough; a real HUD belongs to the main game. | `resolved` (UD-012) |
| AR-004 | Keep camera rotation after launch, following the rocket as the orbit centre. | `implemented` |
| AR-005 | Stage separation and drag stay out of scope, matching GDD 08 §10. | `proposed` |
| AR-006 | No part catalogue UI; scene-placed parts are dragged. | `proposed` — revisit for main-game integration (OI-012) |
| AR-007 | Commit the prototype before further work to create a rollback baseline. | `proposed` — not done; still recommended |
| AR-008 | Thrust is fixed to `transform.up`, so normal alignment would divorce the visual from the force. | `resolved` (UD-009) |
| AR-009 | The orbit camera needs a pitch clamp and wheel zoom or it goes under the ground. | `implemented` as R-017 (`minPitch = -20`, `maxPitch = 80`) |
| AR-010 | Identical parts share `fuel`/`burnRate` and run dry together; staggered burnout only appears when the Inspector values differ. | `proposed` — consistent with SE-3 (both dry together, tilt 0°) |
| AR-011 | Without a reset, repeat trials mean restarting Play Mode. | `proposed` |
| AR-012 | Launching with no engines or no fuel is not blocked; it simply produces no thrust. Closes OI-003. | `proposed` |
| AR-013 | Smallest reading that satisfies UD-013 while honouring GDD 18: assembly is manual **before** launch, flight results stay probabilistic and pre-decided. Basis for Q-011 option 1. | `proposed` — undecided |

### Unresolved items (OI)
| ID | Content | Status |
|---|---|---|
| OI-001 | Ground-contact handling. | `resolved` (UD-008) |
| OI-002 | Final home of the prototype. | `resolved` (UD-013) |
| OI-003 | Behaviour when launching with zero fuel or zero engines. | `resolved` (AR-012 default) |
| OI-004 | Attachment point model. | `resolved` (UD-005) |
| OI-005 | Fuel storage unit and mass behaviour. | `resolved` (UD-007) |
| OI-006 | Camera rotation binding. | `resolved` (UD-006) |
| OI-007 | Part orientation and symmetry aids. | `resolved` (UD-009) |
| OI-008 | Default fuel numbers. | `resolved` (UD-011) |
| OI-009 | Whether allowing overlapping parts causes real problems. | `deferred` — re-evaluate after hands-on use |
| OI-010 | Fuel display method. | `resolved` (UD-012) |
| OI-011 | **How to resolve UD-013 against SF-010.** Manual assembly and manual camera in the main game cannot coexist with "the player does not directly control", "manual control is not implemented", and the pre-decided-result/auto-playback premise. | **`open`** — blocks R-018, R-019 |
| OI-012 | Whether main-game integration needs a part catalogue / assembly UI. | `open` — follows Q-011 |

## 11. Question register

| ID | Title | Status |
|---|---|---|
| Q-001 | Attachment point model | `answered` (2 — surface free attachment) → UD-005 |
| Q-002 | Camera rotation binding | `answered` (1 — right-drag) → UD-006 |
| Q-003 | Fuel model | `answered` (1 — per-part fuel) → UD-007 |
| Q-004 | Ground-contact handling | `answered` (1 — plain physics) → UD-008 |
| Q-005 | Final home of the prototype | `answered` (free text — all features go to the main game) → UD-013 |
| Q-006 | Part orientation and symmetry aids | `answered` (1 — rocket-aligned, no aids) → UD-009 |
| Q-007 | Fuel value basis | `answered` (4 — amount plus rate) → UD-010 |
| Q-008 | Default fuel numbers | `answered` (1 plus inspector exposure) → UD-011 |
| Q-010 | Fuel display method | `answered` (1 — log only) → UD-012 |
| Q-011 | **Main-game integration vs. the GDD change-freeze line** | **`open` — asked, never answered.** Options presented: (1) manual assembly before launch, automatic probabilistic flight — smallest GDD amendment; (2) let assembly feed the result probability — amends GDD 18 §2/§7 and cascades into the economy/balance chapters; (3) reuse only the physics, keep the controls as a dev tool — no GDD amendment but narrows UD-013; (4) rewrite the GDD 18 change-freeze line entirely. |

## 12. Revision history

| Revision | Change |
|---|---|
| 1 | Initial hypothesis. UD-001~004 recorded from the request; SF-001~011 verified from code, scene, physics settings and the GDD; Q-001~003 registered. |
| 2 | Q-001=2, Q-002=1, Q-003=1 recorded as UD-005~007. OI-004/005/006 and RK-002~005 resolved. SF-012 added. RK-007~009, OI-007/008, AR-008/009 and Q-006/007 registered. |
| 3 | Q-004=1, Q-006=1, Q-007=4 recorded as UD-008~010. OI-001/007 resolved, R-015 added; default numbers split out into Q-008 because the answer carried no target burn time. RK-008 accepted, RK-010 added, AR-010~012 added. |
| 4 | Q-008 and Q-010 recorded as UD-011/012, resolving OI-008/010 and fixing R-014; R-016/R-017 and SF-013 added. **Q-005's free-text answer recorded as UD-013 and its conflict with SF-010 registered as OI-011 / RK-011, turning RK-001 into `conflict`.** R-018/R-019 added and blocked. AR-013 added. |
| 5 | Finish signal received (UD-014). Interview closed with Q-011 unresolved and R-018/R-019 still blocked — the conflict was preserved, not decided. R-001~R-017 marked `implemented`; success evidence filled in with actual verification results (SE-1/3/4 met, SE-2 partial, SE-5 not tested). RK-007 recorded as an accepted deviation (slot GameObjects left in the scene). Document rewritten in English with a Korean mirror. |

## 13. Handoff

**Implemented in this session (separately authorized by UD-014):**

- `Assets/01. Scripts/Simulation/RocketPart.cs` — `fuel`, `burnRate` serialized fields; `Refill()`, `TryBurn(dt)`,
  `Remaining`, `HasFuel`.
- `Assets/01. Scripts/Simulation/Rocket.cs` — slots, `snapRadius`, `FindFreeSlot` and `TryAttach` removed;
  `Attach(part, worldPoint)` added; `FixedUpdate` burns fuel per engine and logs burnout transitions.
- `Assets/01. Scripts/Simulation/RocketBuilder.cs` — orbit camera (yaw/pitch/distance derived in `Start`,
  right-drag rotation, wheel zoom, pitch clamp) and surface-raycast part dragging with the dragged part's
  collider temporarily disabled.
- `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs` — replaces `RocketSlotTests.cs` (deleted).
- `docs/rocket-simulation.md` — rewritten for the surface attachment model, orbit camera, fuel and new numbers.

**Verification actually run**: script compilation (no console errors), `Border.Simulation.EditModeTests`
2/2 passed, and one Play Mode flight (attachment at mid-body, ascent, burnout, apex, descent, orbit math).
**Not run**: mouse-driven drag and orbit by hand, Game View screenshots, `burnRate` variation (SE-5),
ground-impact behaviour after touchdown, and the full EditMode suite for other assemblies.

**Not done, and why**: R-018 and R-019 (main-game integration structure and GDD amendments) are blocked on
Q-011. Nothing was committed. The two obsolete `Slot_L` / `Slot_R` GameObjects were left in the scene
rather than hand-editing scene YAML (RK-007).

**Next authorized action**: answer Q-011. Everything downstream of it — code structure changes for the main
game, GDD amendments, commits — needs its own authorization.

---

# 14. Round 2 — ground contact, engine prefab, flame effect

Requested after the user ran the round-1 build: "발사 하고 나면 땅이 없어지는 거야? 떨어지면 땅에
부딪히도록 해야해. 그리고 부착하는 엔진 모듈 같은 부품을 프리팹으로 만들고 싶어. 이 엔진에서
제트팩처럼 불 나오는 이펙트 그냥 간단하게 넣고 싶어." Round 1 had explicitly listed post-touchdown
behaviour as **not tested** (§8, SE-3), and this is what that gap was hiding.

## 14.1 Verified facts

| ID | Fact | Source |
|---|---|---|
| SF-014 | `Ground` was a Plane primitive with a non-convex MeshCollider at localScale `(2,1,2)` → **20 m × 20 m, zero thickness**, no Rigidbody. | `SimulationTest.unity:678-718` |
| SF-015 | The rocket Rigidbody shipped `m_CollisionDetection: 0` (**Discrete**), `m_LinearDamping: 0`, `m_Interpolate: 0`; fixed timestep `0.02`. At the ~92 m/s impact speed that is **1.85 m of travel per physics step**, while a tilted capsule's vertical half-extent is only 0.5 m. | scene `:310,325,327`, `TimeManager.asset:6` |
| SF-016 | `Engine_A` / `Engine_B` were plain scene GameObjects (`m_PrefabInstance: {fileID: 0}`), Cube mesh, BoxCollider `(1,1,1)`, localScale `(0.5,0.8,0.5)`, URP `Lit.mat`. | scene `:484-608, 723-847` |
| SF-017 | The only prefab outside packages was `Assets/03. Prefabs/Systems/SoundManager.prefab`; the convention is `03. Prefabs/<Category>/PascalCase.prefab`. | `Assets/03. Prefabs/` |
| SF-018 | No first-party ParticleSystem or particle material existed anywhere. VFX Graph 17.3.0 is installed but unused. | `Packages/manifest.json:22,33` |
| SF-019 | Using the project's own `Shader/Uber/Particle` with a **new keyword combination** would force the hardcoded row count 97→98 in `UberShaderVariantManifest.cs` and `UberShaderSuiteTests.cs:1730` plus a `.shadervariants` rebuild. Staying off Uber entirely costs nothing. | `UberShaderVariantManifest.cs`, `UberShaderSuiteTests.cs:1730` |
| SF-020 | `RocketPart` exposed no "currently thrusting" signal; `Rocket.FixedUpdate` computed `TryBurn`'s bool and discarded it. Thrust is always `rocket.transform.up`. | `Rocket.cs`, `RocketPart.cs` |
| SF-021 | `m_AutoSyncTransforms: 0` — transform writes reach PhysX only at the next FixedUpdate, so the attach raycast reads one-frame-stale collider poses. Noted, not acted on. | `DynamicsManager.asset:22` |

**Two independent causes, both needing a fix.** (A) The ground was 20 m across while the rocket reaches
apex ~434 m with zero linear damping, so any tilt carried it past the edge into an endless fall.
(B) Discrete detection against a zero-thickness plane tunnels at 1.85 m per step. Fixing either alone
still fails.

## 14.2 User decisions

| ID | Content | Status |
|---|---|---|
| UD-015 | "떨어지면 땅에 부딪히도록 해야해" — landing must actually happen. **Q-012 = enlarge the ground AND use continuous detection.** | `active` |
| UD-016 | "부착하는 엔진 모듈 같은 부품을 프리팹으로 만들고 싶어" — **Q-013 = one engine prefab**, scene engines replaced by instances. No rocket prefab, no spawner. | `active` |
| UD-017 | "제트팩처럼 불 나오는 이펙트 그냥 간단하게" — **Q-014 = ParticleSystem + URP's stock particle shader, additive.** | `active` |

UD-015 does **not** conflict with UD-008 (ground contact = plain physics, no reset/verdict/explosion):
UD-008 governs what happens after the collision, UD-015 requires the collision to occur at all.
UD-016 explicitly unblocks the **prefab-promotion** part of R-018; the rest of R-018 and all of R-019
stay blocked on Q-011.

## 14.3 Requirements

| ID | Requirement | Provenance | Status |
|---|---|---|---|
| R-020 | A falling rocket must actually collide with the ground — no tunnelling, no falling past its edge. | UD-015, SF-014, SF-015 | `implemented` |
| R-021 | The ground must cover the rocket's reachable horizontal range. | UD-015, SF-014 | `implemented` |
| R-022 | No tunnelling at the ~92 m/s impact speed. | UD-015, SF-015 | `implemented` |
| R-023 | Engine parts become a prefab asset; scene `Engine_A`/`Engine_B` become instances of it. | UD-016, SF-016, SF-017 | `implemented` |
| R-024 | The engine prefab carries a flame that plays only while thrust is actually produced. | UD-017, SF-020 | `implemented` |
| R-025 | The flame points along the rocket's **−up**, not the part's own axes. | UD-017, SF-020 | `implemented` |
| R-026 | A dry engine's flame turns off. | UD-017, SF-020 | `implemented` |
| R-027 | Do not increase the shader variant manifest row count. | SF-019, RK-013 | `implemented` |
| R-028 | Update `docs/rocket-simulation.md` and this spec in the same change. | project CLAUDE.md | `implemented` |
| R-029 | One EditMode check for the flame toggle. | project CLAUDE.md | `implemented` |

## 14.4 What was built

- **`Rocket.cs`** — `Launch()` now sets `collisionDetectionMode = CollisionDetectionMode.Continuous`
  right after clearing `isKinematic`. `Continuous` rather than `ContinuousDynamic` because the ground
  is a static collider and the scene holds exactly one Rigidbody; set in `Launch` rather than `Awake`
  because `Awake` makes the body kinematic, where continuous detection is meaningless.
- **`RocketPart.cs`** — serialized `ParticleSystem flame`; `TryBurn` calls a private `SetFlame(bool)`
  on both paths, so burnout turns the flame off at the same place it stops thrust. `Rocket.cs` needed
  no change for the flame. Guarded by `isEmitting` so `Play()` is not re-issued every frame, and
  null-safe so parts without a flame still work.
- **Scene** — `Ground` localScale `(2,1,2)` → `(40,1,40)` = 400 m × 400 m. `Engine_A`/`Engine_B`
  replaced by prefab instances. The obsolete `Slot_L`/`Slot_R` are still left in place (RK-007).
- **`Assets/03. Prefabs/Simulation/RocketEngine.prefab`** — Cube mesh + BoxCollider + `RocketPart` +
  child `Flame` ParticleSystem: local position `(0,-0.5,0)`, rotation `(90,0,0)` so the cone's +Z maps
  to the part's −Y, local scale `(2,1.25,2)` to cancel the parent's non-uniform `(0.5,0.8,0.5)`.
  Lifetime 0.15 s, speed 6, rate 60/s, cone 10°, `simulationSpace = World` (the rocket moves at
  70+ m/s, so Local would glue the plume to the hull), `playOnAwake = false`.
- **`Assets/05. Arts/Material/EngineFlame.mat`** — `Universal Render Pipeline/Particles/Unlit`,
  transparent + additive, orange. Deliberately **not** the project's Uber particle shader (SF-019).
- **`RocketSimulationTests.cs`** — third test `Flame_TurnsOnWhileBurning_AndOffWhenDry`.

## 14.5 Risks

| ID | Item | Status |
|---|---|---|
| RK-012 | Prefab promotion forces scene edits. Kept to the two engine objects plus the single `Ground` scale value. | `accepted` |
| RK-013 | A new Uber keyword combination would cascade into the variant manifest and its locked tests. Avoided by using URP's stock particle shader. | `resolved` |
| RK-014 | The 400 m ground changes the visual scale of the scene. The placeholder white Lit material has no texture, so there is no tiling artefact. Confirmed in the screenshot. | `resolved` |
| RK-015 | Q-011 is still open, so the prefab's location and structure were chosen on prototype terms. Main-game integration may move it. | `active` |
| RK-016 | The flame is a small untextured additive quad plume — legible but plain. Upgrading it means an Uber particle material and a manifest row (SF-019). | `accepted` |

## 14.6 Unresolved

| ID | Content | Status |
|---|---|---|
| OI-013 | Which of (A) off-the-edge and (B) tunnelling fired first was never isolated. | `resolved` — both fixed; the single-engine run lands at x ≈ 53 m, proving (A) was reachable |
| OI-014 | Whether a part catalogue / spawner is needed once parts exceed two kinds. | `open` (with OI-012) |
| OI-011 | **UD-013 vs SF-010** — main-game integration conflict. | **`open`** (Q-011) |

## 14.7 Question register (round 2)

| ID | Title | Status |
|---|---|---|
| Q-012 | How to make ground contact happen | `answered` (enlarge + continuous) → UD-015 |
| Q-013 | Engine prefab scope | `answered` (one engine prefab) → UD-016 |
| Q-014 | Flame implementation | `answered` (ParticleSystem + URP stock shader) → UD-017 |
| Q-011 | Main-game integration vs the GDD change-freeze line | **`open`, unchanged** |

## 14.8 Verification actually run

| Check | Result |
|---|---|
| Compile | No console errors. One intermediate error was hit and fixed: `ParticleSystem.Stop`'s single-argument overload takes `bool withChildren`, not a `ParticleSystemStopBehavior`. |
| EditMode | `Border.Simulation.EditModeTests` **3/3 passed**. `ParticleSystem.Play()` / `isEmitting` behave correctly in EditMode, so no mirrored state field was needed. |
| Landing, symmetric (2 engines) | t=22.2 s: position `(0.00, 2.00, 0.00)`, velocity `(0,0,0)`, `IsSleeping() == true`, tilt 0.0°. Standing upright on the ground; predicted ~21.6 s. |
| Landing, asymmetric (1 engine) | Tilts to 90°, drifts to **x ≈ 53 m**, comes to rest at y = 0.5 on the ground — well outside the old 20 m plane. |
| Collision mode | `collisionDetectionMode == Continuous` after `Launch()`. |
| Flame direction | `flame.transform.forward == (0,-1,0)` while `rocket.transform.up == (0,1,0)` — exactly opposed. |
| Flame toggle at runtime | `emitting == true` immediately after a `TryBurn` call, `false` once fuel reaches 0; ~9 live particles at steady state (rate 60 × lifetime 0.15). |
| Visual | Game View screenshot `Assets/Screenshots/flame_check.png` — the ground now reaches the horizon and an orange plume is visible below the side-mounted engine. |

**Not verified**: hand-driven mouse dragging and right-drag orbit, how the flame reads in motion at
flight speed, and the full EditMode suite for other assemblies. Nothing was committed.

## 14.9 Revision history addition

| Revision | Change |
|---|---|
| 6 | Round 2 planned and implemented under a separate approval. SF-014~021, UD-015~017, R-020~R-029, RK-012~016, OI-013/014, Q-012~014 added. R-018 partially unblocked by UD-016 (prefab promotion only). **Q-011 / OI-011 / RK-001 preserved as open** — the second round did not touch that conflict. |
