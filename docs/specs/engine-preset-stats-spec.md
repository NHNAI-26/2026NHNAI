# Engine Preset Stats (ScriptableObject) — Planning Document

> Authoritative English source. Korean mirror: `engine-preset-stats-spec.ko.md`.

## Document State

| Field | Value |
| --- | --- |
| Interview state | `explicitly-finished` |
| Working language | Korean (interview) / English (authoritative) |
| Current revision | 6 |
| Last updated | 2026-09-04 (KST) |
| Project or workspace root | `C:\myGame\2026NHNAI` |
| Base path | `docs/specs/engine-preset-stats-spec.md` |
| Korean mirror path | `docs/specs/engine-preset-stats-spec.ko.md` |
| Explicit finish received | `yes` — "로 하고 구현해줘" (rev 5) |
| Next authorized action | Compile and run `Simulation.EditModeTests` in the Editor (neither has been verified). Document rewrite (R-016) and formula redesign (OI-012) remain unauthorized. |
| Implementation state | Landed, unverified — see **Implementation Record** |

## Current Snapshot

- **Outcome:** Engine presets are authored as ScriptableObjects holding **physical-unit** values. Rocket design and launch consume those values with no conversion layer. Cooling drives a real heat-accumulation model. The GDD's `0~100` stat scale is abandoned.
- **Primary audience:** designers (authoring/balancing), design & launch implementers, players.
- **In scope:** physical-unit SO data model, 10-slot cap, runtime developed-slot filtering, heat model, design-stage consumption, editor test-fill tool.
- **Out of scope:** minigame implementation, runtime preset-editing UI, runtime slot persistence, GDD 06 formula redesign.
- **Material unresolved items:** OI-005, OI-006, OI-007, OI-010, OI-011, OI-012, OI-013, OI-016, OI-017.
- **Active question IDs:** none — interview finished.

## Outcome and Context

### Desired Outcome

Each engine preset carries price, fuel tank capacity, cooling, max output, and ignition reliability, stored **directly in physical units** (UD-007). The `0~100` normalized stat scale of GDD 06 is abandoned and its formulas are to be rewritten (UD-010). Cooling is heat dissipation: temperature accumulates at `heat − cooling` per second and the engine explodes past a shared critical temperature (UD-012).

### Problem and Background

`RocketPart` hardcodes `thrust = 1200f`, `fuel = 100f`, `burnRate = 20f` (SF-005); "engines differ" exists in the GDD but not in data. GDD 06/07 fixed four stats on a `0~100` scale whose quality, penalty, probability, and derived-performance formulas all depend on same-scale comparisons such as `MaxOutput − Cooling` (SF-001, SF-009, SF-017). The user chose to abandon that scale rather than normalize (UD-010). The heat model gives GDD 08 §9's `Overheat` accident an actual simulation behind it (SF-018).

### Planning Boundary

This plan decides the unit system, the heat model, SO structure and constraints, design-stage consumption, the test tool, and the scope of GDD rewrites. It does not authorize commits, GDD edits, or the GDD 06 formula redesign.

## Users and Stakeholders

| Stakeholder | Need or concern | Source IDs | Status |
| --- | --- | --- | --- |
| Designer | Tune preset stats in physical units in the Inspector | UD-004, UD-007, SF-008 | active |
| Test operator | Fill preset values quickly in the test scene | UD-009 | active |
| Design/launch implementer | Read SO values into part physics and the heat model | UD-005, UD-012, SF-005 | active |
| Player | Understand that engine choice and count change the outcome | UD-014, UD-015, SF-002 | active |
| GDD owner | Keep 06 rewrite consistent with 07/08 | UD-010, RK-008 | active |

## Scope and Non-Goals

### In Scope

| Scope item | Source IDs | Status |
| --- | --- | --- |
| Engine preset SO type with 5 fields | UD-003, UD-004 | active |
| Values stored directly in physical units | UD-007 | active |
| Abandon the `0~100` stat scale | UD-010 | active |
| Heat model (`dT/dt = heat − cooling`, shared critical temperature, floor 0) | UD-011, UD-012, UD-017 | active |
| Heat driven by actual applied output, not preset maximum | UD-016 | active |
| Ignition reliability stored as a percentage | UD-013 | active |
| Max 10 preset slots | UD-002, UD-008 | active |
| New games expose one developed preset; later presets are unlocked by new engine development | latest GDD correction | active |
| Unlimited engine count per rocket | UD-014 | active |
| Mixing different presets on one rocket | UD-015 | active |
| Editor tool that fills test preset values | UD-009 | active |
| Design stage reads SO values only | UD-005, SF-002 | active |

### Out of Scope / Non-Goals

| Excluded item | Source IDs | Why |
| --- | --- | --- |
| The four research minigames | UD-005 | User scoped this pass to "SO first" |
| Runtime preset-stat editing UI | UD-005 | Data + read + test tool only |
| Runtime slot persistence | UD-008, OI-010 | Slot model decided; storage medium deferred |
| A fifth management resource | SF-013, UD-006 | GDD 18 §4 forbids it; price is display-only |
| Staging, part catalog, gimbal | SF-002 | GDD 07 §5 "deliberately not built" |
| Mass loss from fuel burn | SF-002 | GDD 07 §5 excludes it |
| GDD 06 formula redesign | OI-012 | Separate design task (RK-008) |

## Core Experience / Operating Flow

### Primary Flow

1. A designer creates an `EngineStatsSO` asset and enters price plus four stats in physical units.
2. The editor tool fills test preset values in one action (UD-009).
3. Presets are registered into a slot list capped at 10, but a new game exposes only slot 0 (`Engine01`).
4. The player enters the rocket design stage.
5. The design stage reads only developed slots. `New Engine Development` exposes the next slot in order, up to 10.
6. The player attaches **any number** of engines (UD-014), **mixing presets freely** (UD-015).
7. `RocketPart` uses the SO's physical values directly for thrust, fuel, burn rate, heat, and cooling.
8. On launch, ignition is rolled per engine against its reliability percentage; surviving engines apply thrust and accumulate temperature.
9. Temperature crossing the shared critical value becomes an `Overheat` outcome (SF-018).

### Heat Model (UD-012, UD-016, UD-017)

```text
heatRate      = k * appliedOutput        # linear in the output actually applied; k is a code constant (OI-013)
dT/dt         = heatRate - cooling
temperature   = max(0, temperature + dT/dt * dt)
temperature >= CriticalTemperature  ->  overheat
```

- The critical temperature is one constant shared by every engine (UD-012).
- Temperature accumulates **per engine**; only the threshold is shared (AR-011).
- When cooling exceeds heat — including while an engine is off, where heat is zero — temperature falls by the surplus and is floored at 0 (UD-017). This makes engine on/off timing a real overheat-management input.
- Heat follows the output actually applied, so lowering the force slider trades thrust for thermal headroom (UD-016).

### Alternate, Error, and Edge Flows

| Condition | Expected behavior | Related IDs | Status |
| --- | --- | --- | --- |
| Preset slots empty | Design stage cannot place engines; explicit warning log | R-005 | active |
| More than 10 slots registered | Rejected or clamped at author time | R-002 | active |
| `RocketPart` has no SO assigned | Warn and refuse placement | R-017, OI-007 | active |
| Different presets mixed on one rocket | Allowed; fuel and temperature diverge per engine | R-006, UD-015 | active |
| Cooling ≥ heat | Temperature falls by the surplus, floored at 0 | R-012, UD-017 | active |
| Ignition roll fails | Engine never fires; `IgnitionFailure` per GDD 08 §9 | R-014, SF-018 | active |
| One engine overheats | Whole launch ends as `Overheat` | R-013, AR-012, OI-016 | active (assumption) |
| Negative stat value | Clamped at 0 in the editor | R-003 | active |

### State, Data, and Lifecycle Notes

- SO assets are **authoring data**. Writing SO fields at runtime persists past Play Mode and corrupts balance data (AR-005). Since presets are also described as save slots (UD-008), this pass treats SO as test/authoring data and defers runtime persistence (AR-006, RK-007, OI-010).
- Temperature is runtime-only state and is never written back to an SO.
- GDD 07 §7 `DesignData` already declares an `engineStats` field (SF-011).

## Requirements

| ID | Requirement | Type | Source IDs | Priority | Status | Success evidence |
| --- | --- | --- | --- | --- | --- | --- |
| R-001 | A preset holds price, fuel capacity, cooling, max output, ignition reliability | functional | UD-003 | must | active | Five fields visible in the Inspector |
| R-002 | At most 10 preset slots can be registered | functional | UD-002, UD-008 | must | active | The 11th entry is rejected or clamped |
| R-003 | Stat values are clamped to a valid lower bound (≥ 0) | quality | UD-007 | must | active | Negative input is clamped |
| R-004 | Preset data is authored as a ScriptableObject | technical | UD-004 | must | active | Creatable via a `Simulation/...` CreateAssetMenu |
| R-005 | The design stage only reads SO values, never writes them | functional | UD-005, SF-002 | must | active | No stat-raising path during design |
| R-006 | Multiple engines sum their thrust | functional | SF-002 | should | active | Matches GDD 07 §6.3 |
| R-007 | Preset physical values feed thrust, fuel, and burn rate with no conversion | functional | UD-005, UD-007, SF-005 | must | active | Changing preset changes the trajectory |
| R-008 | Price is a display/balance figure only and deducts no resource | functional | UD-006 | must | active | No code path spends it |
| R-009 | An editor tool fills preset SO values for the test scene | operational | UD-009 | must | active | One action leaves the scene launch-ready |
| R-010 | Minigame rewards use the same physical unit system | functional | UD-007, UD-010 | should | blocked | Blocked on OI-012 |
| R-011 | A baseline preset reproduces the current hardcoded 1200 N / 100 / 20 behavior | quality | SF-005, RK-003 | should | active | Baseline trajectory matches the prototype |
| R-012 | Cooling is heat dissipation; temperature integrates `heat − cooling` per second with a floor of 0 | functional | UD-011, UD-012, UD-017 | must | active | Low-cooling presets reach the threshold sooner; off engines cool down |
| R-013 | Crossing the shared critical temperature triggers overheat failure | functional | UD-012 | must | active | Overheat outcome fires at the threshold |
| R-014 | Ignition reliability is a percentage used in the ignition roll | functional | UD-013 | must | active | 0% always fails, 100% always succeeds |
| R-015 | No cap on the number of engines attached | functional | UD-014 | must | active | Many engines attach without rejection |
| R-016 | Rewrite the GDD 06 `0~100` formulas in physical units | operational | UD-010 | must | blocked | Blocked on OI-012; not authorized in this pass |
| R-017 | Heat is computed from the output actually applied, not the preset maximum | functional | UD-016 | must | active | Lowering applied output slows temperature rise |

## Constraints

| Category | Constraint | Source IDs | Consequence | Status |
| --- | --- | --- | --- | --- |
| policy | GDD 07 §3 forbids buying resources in the design stage | SF-003 | Satisfied by UD-006 | active |
| policy | GDD 18 §4 forbids a fifth management resource | SF-013 | Satisfied by UD-006 | active |
| policy | GDD 08 §9 allows only one major accident per launch | SF-018 | Ignition and overheat must not both resolve | active |
| policy | The four stat display names stay; only the value scale changes | SF-001 | Naming preserved | active |
| technical | `Simulation` assembly for engine code; SOs use a `Simulation/...` menu path (UD-019) | SF-008 | New SOs follow it | active |
| technical | Editor-only code lives in `Border.Editor` (`Assets/06. Packages/Editor`) | SF-016 | Tool placement | active |
| technical | `Rocket` applies thrust in newtons directly | SF-005, SF-006 | Physical storage needs no conversion layer | active |
| technical | The engine list is frozen at launch (`ponytail:` note) | SF-014 | No mid-flight preset swap | active |
| technical | No mass loss from fuel consumption | SF-002 | Fuel mass is static weight only — implemented as `Rocket.tankMassPerFuel`, summed into `Rigidbody.mass` at launch | active |
| process | `docs/artemis-2026-gdd/07_로켓_설계.md` is modified in the working tree | SF-015 | Preserve the user's edit | active |

## Success Evidence

| Related IDs | Acceptance condition | Method | Owner | Status |
| --- | --- | --- | --- | --- |
| R-001, R-003, R-004 | Five fields author and clamp correctly | Editor inspection | Designer | proposed |
| R-002 | The 11th slot is refused | Inspection | Implementer | proposed |
| R-007, R-011 | Baseline matches the prototype; other presets reach different altitudes | Play Mode | Implementer | proposed |
| R-012, R-013, R-017 | A low-cooling preset overheats; throttling down or switching off cools it | Play Mode + `Log.D` | Implementer | proposed |
| R-014 | 0% never ignites, 100% always ignites | Play Mode | Implementer | proposed |
| R-006, R-015 | Many mixed engines sum thrust and diverge in fuel/temperature | Play Mode | Implementer | proposed |
| R-005, R-008 | No SO write path and no price deduction exist | Code review / grep | Reviewer | proposed |
| R-009 | One tool action makes the test scene launch-ready | Inspection | Test operator | proposed |
| R-016 | No `0~100` residue remains in GDD 06 | Doc review | GDD owner | blocked |

## Decision and Evidence Ledger

| ID | Kind | Statement | Evidence / rationale | Status | Linked IDs |
| --- | --- | --- | --- | --- | --- |
| UD-001 | user decision | Engines differ per preset; engines are saved as presets | Initial request (rev 1) | active | R-001, R-004 |
| UD-002 | user decision | At most 10 presets | Initial request | active | R-002 |
| UD-003 | user decision | Fields: price, fuel capacity, cooling, max output, ignition reliability | Initial request | active | R-001, R-008 |
| UD-004 | user decision | Preset stats are authored as ScriptableObjects | Initial request | active | R-004, AR-001 |
| UD-005 | user decision | This pass: build the SO first; the design stage just reads and runs on those values | Initial request | active | R-005, R-007 |
| UD-006 | user decision | Price is display/balance only and deducts nothing | Q-001 (rev 2) | active | R-008 |
| UD-007 | user decision | The SO stores physical units directly; minigame rewards follow the same system | Q-002 (rev 2) | active | R-007, R-010 |
| UD-008 | user decision | Presets are a 10-slot save model | Q-003 (rev 2) | active | R-002, OI-010 |
| UD-009 | user decision | Provide a test-only tool/button that fills SO values for the test scene | Q-003 (rev 2) | active | R-009, OI-011 |
| UD-010 | user decision | Abandon the `0~100` scale and rewrite the GDD 06 formulas | Q-005 (rev 3) | active | R-016, OI-012, RK-008 |
| UD-011 | user decision | Cooling is the engine's heat dissipation | Q-006 (rev 3) | active | R-012 |
| UD-012 | user decision | Heat is linear in engine output; temperature accumulates `heat − cooling` per second; exceeding a critical temperature shared by all engines causes an explosion | Q-006 (rev 3) | active | R-012, R-013, OI-013, OI-016 |
| UD-013 | user decision | Ignition reliability is a percentage | Q-006 (rev 3) | active | R-014 |
| UD-014 | user decision | Any number of engines may be attached | Q-004 (rev 3) | active | R-015 |
| UD-015 | user decision | Different presets may be mixed on one rocket | Q-007 (rev 4) | active | R-006, OI-004 resolved |
| UD-016 | user decision | Heat is based on the output actually applied, not the preset maximum | Q-008 (rev 4) | active | R-017, OI-014 resolved, RK-010 resolved |
| UD-017 | user decision | Temperature falls by the surplus when cooling exceeds heat, floored at 0; an engine that is off produces no heat | Q-009 (rev 4) | active | R-012, OI-015 resolved |
| UD-018 | user decision | Proceed with these decisions and implement | "로 하고 구현해줘" (rev 5) | active | Finalization + implementation authorization |
| SF-001 | sourced fact | Stats `FuelCapacity`/`Cooling`/`MaxOutput`/`IgnitionReliability`, initial 20, range 0~100 | `06_엔진_연구.md` §2.2 | active | Superseded as a value scale by UD-010 |
| SF-002 | sourced fact | The design stage only reads stats; multiple engines sum thrust with burn time set by the shortest; no mass loss from fuel | `07_로켓_설계.md` §5, §6, §6.3 | active | R-005, R-006 |
| SF-003 | sourced fact | The design stage forbids "buying new resources" | `07_로켓_설계.md` §3 | active | UD-006 |
| SF-004 | sourced fact | The GDD models one engine per session (`EngineState`); no preset concept | `06_엔진_연구.md` §22 | active | RK-001, OI-005 |
| SF-005 | sourced fact | `RocketPart` hardcodes `thrust=1200f`, `fuel=100f`, `burnRate=20f` | `Assets/01. Scripts/Simulation/RocketPart.cs:9-12` | active | R-007, R-011 |
| SF-006 | sourced fact | Thrust enters physics as `AddForceAtPosition(transform.up * engine.Thrust, ...)` | `Assets/01. Scripts/Simulation/Rocket.cs:63` | active | Consistent with UD-007 |
| SF-007 | sourced fact | `RocketBuilder` drags pre-existing `RocketPart` objects; it does not spawn from data | `Assets/01. Scripts/Simulation/RocketBuilder.cs:88-95` | active | R-015, R-017 |
| SF-008 | sourced fact | Pre-existing SO convention: `[CreateAssetMenu(menuName = "Border/...")]` in the `Border` assembly | `Assets/01. Scripts/Audio/SoundDatabaseSO.cs:90` | active | Superseded for new code by UD-019; existing assets keep their menu paths |
| SF-009 | sourced fact | GDD 06 §23 derives performance from the four stats, all on the 0~100 assumption | `06_엔진_연구.md` §23 | active | OI-012 |
| SF-010 | sourced fact | `docs/specs` already uses the base `.md` + `.ko.md` pair convention | `docs/specs/rocket-prototype-revision-spec{,.ko}.md` | active | Finalization paths |
| SF-011 | sourced fact | `DesignData` already declares `engineStats` | `07_로켓_설계.md` §7 | active | Design data shape |
| SF-012 | sourced fact | `ResearchPrototypeModel` tracks only per-stage `Progress`; the four stats are unimplemented | `Assets/01. Scripts/Research/ResearchPrototypeModel.cs:30-38` | active | Presets may be the only stat source |
| SF-013 | sourced fact | GDD 18 §4 forbids a fifth management resource | `18_확정사항_및_변경금지선.md` §4 | active | UD-006 |
| SF-014 | sourced fact | The engine list is frozen at `Launch()` (`ponytail:` note) | `Assets/01. Scripts/Simulation/Rocket.cs:52-53` | active | No mid-flight swap |
| SF-015 | sourced fact | `07_로켓_설계.md` has an uncommitted one-line edit | `git diff` (rev 1) | active | RK-004 |
| SF-016 | sourced fact | Editor-only code belongs to `Border.Editor` (`Assets/06. Packages/Editor`), which references `Border` | `CLAUDE.md`; `Border.Editor.asmdef` | active | R-009 |
| SF-017 | sourced fact | GDD 06 §5 grants `+10~+26` stat points from a 0~100 minigame score | `06_엔진_연구.md` §5 | active | R-010, OI-012 |
| SF-018 | sourced fact | GDD 08 §8~9: cooling drives the temperature indicator and overheat accidents; `Overheat` is "temperature warning, then thrust oscillation or explosion"; only one major accident per launch | `08_로켓_발사.md` §8, §9 | active | R-013, RK-009 |
| SF-019 | sourced fact | `Assets/00. Scenes/SimulationTest.unity` is the existing simulation test scene | `find Assets -name "*.unity"` (rev 5) | active | R-009 target |
| UD-019 | user decision | Code created from here on must not carry the `Border.` prefix. The Simulation assembly was renamed `Border.Simulation` → `Simulation` (asmdef name, rootNamespace, namespace declarations, `CreateAssetMenu` paths) | "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" (rev 6) | active | SF-020, SF-025, RK-012 |
| SF-020 | sourced fact | Runtime code lives in three assemblies, not one: `Border` (`Assets/01. Scripts`), `Border.Input`, and — after UD-019 — `Simulation` (`Assets/01. Scripts/Simulation`, references `Border`). The `Border` name comes from the vendored upstream package `com.borderjung.unity-modules`, whose author handle became the assembly name | `Assets/01. Scripts/Simulation/Simulation.asmdef`; `Packages/manifest.json:3` | active | Engine preset types are `Simulation.*` |
| SF-021 | sourced fact | Unity assembly references are not transitive: `Border.Editor` and `Simulation.EditModeTests` each needed an explicit reference added | `Border.Editor.asmdef`; `Simulation.EditModeTests.asmdef` (rev 6) | active | Editor tool and tests would not compile otherwise |
| SF-022 | sourced fact | `Rocket.Attach` no longer forces the part's rotation to the rocket's, and its comment claims thrust follows the part's up — but `Rocket.FixedUpdate` still applies `transform.up` (the rocket's) and `RocketBuilder.Drag` still snaps the dragged part to the rocket's rotation | `Rocket.cs:29-37`, `Rocket.cs:84`; `RocketBuilder.cs:140` (rev 6, changed outside this session) | active | RK-011, OI-017 |
| SF-023 | sourced fact | Compilation and tests could not be verified: three Unity processes hold the project lock (so batchmode is impossible) and the MCP for Unity bridge is disconnected | `Get-Process Unity`; `Temp/UnityLockfile`; session MCP status (rev 6) | active | Every success-evidence row stays `proposed` |
| SF-024 | sourced fact | `RocketPart.ApplyPreset(EngineStatsSO)` was added outside this session as a scene-instance setter that explicitly does not touch the preset asset | `RocketPart.cs:25-29` (rev 6) | active | Consistent with R-005 and AR-005 |
| SF-025 | sourced fact | `Log` and `DeterministicRng` still live in the `Border` assembly (`Assets/01. Scripts/Core`), so `Simulation` code keeps `using Border.Core;` | `Assets/01. Scripts/Core/Log.cs`, `DeterministicRng.cs` | active | UD-019 does not reach shared code |
| SF-026 | sourced fact | `CLAUDE.md` states all C# is namespaced `Border.*` and lists only `Border`, `Border.Input`, `Border.Editor` plus three test assemblies — it omits `Simulation`/`Border.Simulation` and `Border.Research.EditModeTests` | `CLAUDE.md`, Assembly layout | active | RK-012 |
| AR-001 | agent recommendation | One preset per `EngineStatsSO` asset; a single library SO owns the slot list and clamps to 10 in `OnValidate` | The cap needs one enforcement point | proposed | R-002, OI-010 |
| AR-002 | agent recommendation | (withdrawn) Store 0~100 and convert in code | Superseded by UD-007 | superseded | — |
| AR-003 | agent recommendation | Add an `EngineStatsSO` reference to `RocketPart` and derive the existing fields from it | Smallest diff; a new component would force changes in `Rocket`'s collection logic | proposed | R-007, SF-005 |
| AR-004 | agent assumption | Price is display/balance only | — | resolved | Adopted as UD-006 |
| AR-005 | agent recommendation | Never write SO fields at runtime; copy into a separate state object | Editor writes persist past Play Mode | proposed | R-005, RK-007 |
| AR-006 | agent recommendation | Treat SO as test/authoring data this pass; defer runtime 10-slot persistence to `SaveLoad` | Resolves the UD-004 / UD-008 tension by staging | proposed | OI-010, RK-007 |
| AR-007 | agent recommendation | (withdrawn) Cooling as "sustainable thrust in N" | Superseded by UD-011 | superseded | — |
| AR-008 | agent recommendation | Derive burn rate from max output rather than adding a sixth field | The user fixed the field count at five; higher thrust burning fuel faster is natural | proposed | R-007, OI-006 |
| AR-009 | agent recommendation | (withdrawn) Physical storage plus a 0~100 normalized projection | Superseded by UD-010 | superseded | — |
| AR-010 | agent recommendation | Store heat rate and cooling in the same unit (degrees per second) and keep a single output→heat coefficient as a code constant | `heat − cooling` requires matching units | proposed | R-012, OI-013 |
| AR-011 | agent assumption | Temperature accumulates per engine; only the threshold is shared | "the temperature is the same for all engines" reads as a shared threshold, and per-engine output/cooling make per-engine temperature the only meaningful reading. Reinforced by UD-015 (mixed presets) | proposed | R-013, OI-016 |
| AR-012 | agent recommendation | An overheat ends the whole launch rather than destroying one engine | GDD 08 §9 allows only one major accident per launch | proposed | R-013, OI-016 |
| OI-001 | unresolved item | Meaning and spend point of engine price | — | resolved | UD-006 |
| OI-002 | unresolved item | SO storage units | — | resolved | UD-007 |
| OI-003 | unresolved item | Where the presets come from | — | resolved | UD-008, UD-009 |
| OI-004 | unresolved item | Whether different presets may be mixed | — | resolved | UD-015 |
| OI-005 | unresolved item | Scope of GDD 06/07/08 updates | Leaving docs and code divergent misleads later work | open | UD-010, RK-008 |
| OI-006 | unresolved item | Burn-rate coefficient and baseline preset numbers | No measured balance basis | open | R-007, R-011, AR-008 |
| OI-007 | unresolved item | Behavior when a `RocketPart` has no SO: fallback or refuse | A silent zero-thrust part is hard to debug | open | R-017; implementation adopts "warn and refuse" |
| OI-008 | unresolved item | Handling of the 0~100 formulas | — | resolved | UD-010 |
| OI-009 | unresolved item | Physical units for cooling and ignition reliability | — | resolved | UD-011, UD-013 |
| OI-010 | unresolved item | Storage medium for the runtime 10 slots: SO assets or `SaveLoad` JSON | Runtime SO writes corrupt editor data | open | UD-008, RK-007, AR-006 |
| OI-011 | unresolved item | Test tool form: menu item, inspector button, or context menu | Determines the hosting assembly | open | R-009; implementation adopts a `Border.Editor` menu item |
| OI-012 | unresolved item | The actual content of the rewritten GDD 06 formulas (quality, imbalance penalty, test probability, failure weighting, derived performance, reward magnitudes) | Values in different units cannot be averaged or subtracted | open | UD-010, R-010, R-016, RK-008 |
| OI-013 | unresolved item | Output→heat coefficient and the shared critical temperature | No balance basis yet | open | R-012, R-013, AR-010 |
| OI-014 | unresolved item | Which output drives heat | — | resolved | UD-016 |
| OI-015 | unresolved item | Cooling recovery and off-engine handling | — | resolved | UD-017 |
| OI-016 | unresolved item | Whether an overheat destroys one engine or fails the launch | GDD 08 §9 permits only one major accident | open | R-013, AR-011, AR-012 |
| OI-017 | unresolved item | Does thrust follow the rocket's up or each part's own up | GDD 07 §5 fixes it to the rocket's up, and the heat model assumes applied output along that axis. The code is currently half-changed (SF-022), so placement and force direction can disagree | open | R-007, SF-022, RK-011 |

## Implementation Record (rev 7)

Heat balance was normalized back to the documented value: `EngineStatsSO.HeatPerNewton = 0.05`.
The runtime research scaling, per-engine temperature accumulation, shared `300 °C` critical threshold,
and whole-launch overheat failure behavior are unchanged. A regression test covers the `56 / 98 / 87 / 41`
research-stat case so high cooling no longer explodes before fuel depletion.

## Implementation Record (rev 6)

Landed under UD-018. Everything below is written but **neither compiled nor tested** — the Editor holds
the project lock and the MCP bridge is disconnected, so no verification ran (SF-023).

| Requirement | Where | Note |
| --- | --- | --- |
| R-001, R-003, R-004 | `Assets/01. Scripts/Simulation/EngineStatsSO.cs` | Five fields; shared constants `CriticalTemperature = 300`, `HeatPerNewton = 0.05`, `FuelPerNewton = 20/1200`. Burn rate and heat derive from output (AR-008), so the field count stays at five |
| R-002 | `Assets/01. Scripts/Simulation/EnginePresetLibrarySO.cs` | `MaxSlots = 10`, trimmed in `OnValidate` (AR-001) |
| R-005, R-007, R-012, R-014, R-017 | `Assets/01. Scripts/Simulation/RocketPart.cs` | SO reference replaces the hardcoded fields; `throttle` gives the applied output; `Prepare(rng)` rolls ignition; `Tick(dt)` burns fuel and integrates temperature with a floor of 0 |
| R-006, R-013, R-015 | `Assets/01. Scripts/Simulation/Rocket.cs` | Seeded ignition roll at launch; an overheat ends the launch immediately (AR-012, GDD 08 §9) |
| OI-007 | `Assets/01. Scripts/Simulation/RocketBuilder.cs` | A part with no SO cannot be picked up; warns instead |
| R-009 | `Assets/06. Packages/Editor/Simulation/EnginePresetTestFiller.cs` | `Tools/Engine Preset/Fill Test Presets` writes ten presets plus the library; a separate menu item assigns the baseline to scene engines, because that one edits the scene |
| R-011 | Same file, `Baseline` preset | 1200 N / 100 kg / cooling 60 reproduces the old hardcoded behavior; heat equals cooling, so it never overheats |
| — | `Assets/Tests/EditMode/Simulation/RocketSimulationTests.cs` | Existing tests moved to the new API; added throttle, temperature rise to overheat, cooldown to the floor, ignition at 0%/100%, and the slot cap |

Engineering defaults adopted for still-open items — all remain open for design confirmation:
OI-006 (`FuelPerNewton` back-solved from 1200 N → 20 kg/s), OI-007 (warn and refuse),
OI-011 (`Border.Editor` menu item), OI-013 (`HeatPerNewton = 0.05`, critical 300 °C, set so the
baseline preset holds a steady state), OI-016 (AR-012, the whole launch fails).

## Question Register

| ID | Decision needed | Related IDs | State | Revision | Resolution |
| --- | --- | --- | --- | --- | --- |
| Q-001 | Meaning and spend point of engine price | OI-001, R-008 | answered | 1 | UD-006 |
| Q-002 | Units stored in the SO | OI-002, R-007 | answered | 1 | UD-007 |
| Q-003 | Origin of the 10 presets | OI-003, RK-001 | answered | 1 | UD-008, UD-009 |
| Q-004 | Preset selection granularity | OI-004, R-006 | answered (partial) | 2 | UD-014 (count only); mixing deferred to Q-007 |
| Q-005 | Fate of the 0~100 formulas after the unit change | OI-008, RK-006 | answered | 2 | UD-010 |
| Q-006 | Physical units for cooling and ignition reliability | OI-009, R-001 | answered | 2 | UD-011, UD-012, UD-013 |
| Q-007 | Mixing different presets on one rocket | OI-004, R-006 | answered | 3 | UD-015 (allowed) |
| Q-008 | Which output drives heat | OI-014, R-012 | answered | 3 | UD-016 (actual applied output) |
| Q-009 | Temperature when cooling exceeds heat | OI-015, R-012 | answered | 3 | UD-017 (falls by the surplus, floor 0) |

## Corrections and Revision History

| Revision | Trigger | Change | Superseded IDs | Sections reconciled |
| --- | --- | --- | --- | --- |
| 1 | Initial request + GDD 06/07/18 and `Assets/01. Scripts/{Simulation,Research}` inspection | Initial hypothesis | none | All |
| 2 | Q-001/Q-002/Q-003 | Price display-only, physical units, 10 save slots + test tool | AR-002 superseded; AR-004, OI-001~003 resolved | Snapshot, scope, flow, R-003/008~011, SF-016/017, AR-006~009, OI-008~011, RK-006/007, Q-004~006 |
| 3 | Q-004/Q-005/Q-006 + `08_로켓_발사.md` §8~9 | 0~100 abandoned, heat model added, ignition as %, unlimited engine count | AR-007/AR-009 superseded; OI-008/009 resolved; RK-006 resolved | Snapshot, outcome, scope, heat model, flow, R-012~016, SF-018, AR-010~012, OI-012~016, RK-008~010, Q-007~009 |
| 4 | Q-007/Q-008/Q-009 | Mixed presets allowed, heat from applied output, temperature falls to a floor of 0 | OI-004/014/015 resolved; RK-010 resolved | Snapshot, heat model, flow, R-012/R-017, UD-015~017 |
| 5 | "로 하고 구현해줘" | Explicit finish and implementation authorization; finalized to English base + Korean mirror | none | Document State, snapshot, ledger (UD-018, SF-019), finalization |
| 6 | Implementation landed + "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" | Recorded what was built and that nothing was verified; renamed `Border.Simulation` → `Simulation` per UD-019; recorded the half-applied `Attach` rotation change and the stale `CLAUDE.md` assembly table | none | Document State, Implementation Record (new), ledger (UD-019, SF-020~026), OI-017, RK-011/012, checkpoint |

## Risks, Conflicts, and Dependencies

| ID | Kind | Item | Likelihood / impact | Response | Related IDs | Status |
| --- | --- | --- | --- | --- | --- | --- |
| RK-001 | conflict | GDD 06/07 model one developed engine; 10 slots contradict it | high / high | UD-008 decided; doc scope is OI-005 | SF-004, OI-005 | open |
| RK-002 | conflict | Deducting price in the design stage would violate GDD 07 §3 | — | Resolved by UD-006 | SF-003 | resolved |
| RK-003 | risk | Arbitrary physical values would break the prototype's launch feel | medium / medium | Build the baseline preset first (R-011) | SF-005, OI-006 | open |
| RK-004 | dependency | `07_로켓_설계.md` has uncommitted edits | low / low | Preserve the user's change | SF-015 | open |
| RK-005 | risk | If presets become the only stat source, minigame rewards have no target | medium / medium | UD-007/UD-010 put rewards in physical units; magnitudes are OI-012 | SF-012, SF-017 | open |
| RK-006 | conflict | The unit change invalidates every 0~100 formula | — | Resolved by UD-010; rewrite content is OI-012 | SF-009 | resolved |
| RK-007 | conflict | UD-008 (runtime slots) vs UD-004 (SO authoring data): runtime SO writes corrupt editor assets | medium / high | AR-006 staging | UD-004, UD-008, OI-010 | open |
| RK-008 | risk | The GDD 06 rewrite spans §2.2, §5, §8, §9, §11, §12, §13, §15, §16, §20, §21, §23 and needs rebalancing | high / high | Split OI-012 into a separate design task | UD-010, R-016 | open |
| RK-009 | dependency | The heat model must line up with GDD 08 §8~9 `Overheat` staging | medium / medium | SF-018 confirmed consistent | SF-018, R-013 | open |
| RK-010 | risk | Heat from preset maximum would make the force slider meaningless | — | Resolved by UD-016 | OI-014 | resolved |
| RK-011 | conflict | `Rocket.Attach` was changed to leave the part's rotation alone and its comment says thrust follows the part's up, but `FixedUpdate` still uses the rocket's up and `RocketBuilder.Drag` still snaps to the rocket. Placement and force direction can therefore disagree, and GDD 07 §5 fixes thrust to the rocket's up | medium / high | Decide OI-017, then make comment, `Attach`, `Drag` and `FixedUpdate` agree | SF-022, OI-017 | open |
| RK-012 | risk | `CLAUDE.md` still says everything is namespaced `Border.*` and omits the `Simulation` assembly, so it now contradicts UD-019 and misleads later work | medium / medium | Update the Assembly layout table; not authorized in this pass | UD-019, SF-026 | open |

## Open, Skipped, and Deferred Items

| ID | Item | State | Consequence | Recommendation | Owner | Revisit trigger |
| --- | --- | --- | --- | --- | --- | --- |
| OI-005 | GDD 06/07/08 update scope | open | Docs and code stay divergent | Update together once OI-012 lands | User | OI-012 resolved |
| OI-006 | Burn-rate coefficient and baseline numbers | open | No measured basis | Reproduce 1200 N / 100 / 20 as the baseline | Implementer | Balance pass |
| OI-007 | Missing-SO behavior | open | Silent zero thrust is hard to debug | Warn and refuse placement | Implementer | — |
| OI-010 | Runtime slot storage medium | open | Runtime SO writes corrupt editor data | Keep SO as test data; persist via `SaveLoad` later | User | Runtime slot work |
| OI-011 | Test tool form | open | Determines hosting assembly | `Border.Editor` menu item | Implementer | — |
| OI-012 | Content of the rewritten GDD 06 formulas | open | Mixed units cannot be averaged or subtracted | Separate design task | User | After this pass |
| OI-013 | Output→heat coefficient and critical temperature | open | No balance basis | Set so the baseline preset completes a normal burn | Implementer | Balance pass |
| OI-016 | Overheat blast radius | open | GDD 08 §9 allows one major accident | Fail the whole launch (AR-012) | User | Launch-stage work |

## Coverage and Consistency Check

| Area | State | Supporting IDs | Note |
| --- | --- | --- | --- |
| Outcome | covered | UD-001, UD-007, UD-010 | — |
| Users and stakeholders | covered | UD-004, UD-009 | — |
| Scope | covered | UD-005~018 | — |
| Non-goals | covered | SF-002, SF-013, OI-012 | — |
| Core flow | covered | UD-012, UD-014~017 | — |
| Constraints | covered | SF-003, SF-008, SF-013, SF-016, SF-018 | — |
| Success evidence | partial | R-001~R-017 | R-010, R-016 blocked on OI-012 |
| Risks and dependencies | covered | RK-001~010 | RK-008 is the largest open risk |
| Unresolved decisions | open | OI-005~007, OI-010~013, OI-016, OI-017 | Preserved, not silently resolved |
| Handoff and authorization | covered | UD-018 | Implementation authorized; doc rewrite is not |

## Interview Checkpoint

- **Latest user message incorporated:** "앞으로 생성하는거나 시뮬레이션 폴더에 border. 으로 안하면 안돼?" — drop the `Border.` prefix (rev 6).
- **Latest sourced evidence incorporated:** SF-020~026 (assembly layout and rename, non-transitive assembly references, the half-applied `Attach` change, verification blocked, `ApplyPreset`, stale `CLAUDE.md`).
- **Ledger transitions applied:** UD-019 added; SF-020~026 added; OI-017, RK-011, RK-012 opened.
- **Contradictory active items check:** passed. R-010 and R-016 stay `blocked` on OI-012; RK-011 is recorded as an open conflict rather than silently resolved.
- **Traceability check:** passed — R-001~R-017 all trace to UD/SF/OI.
- **Verification status:** nothing compiled or tested (SF-023). Every success-evidence row remains `proposed`.
- **Resume point if planning reopens:** OI-017 (thrust axis, blocks RK-011), then OI-012 (formula rewrite), OI-005 (doc scope), OI-016 (overheat scope), OI-010 (persistence).

## Finalization and Handoff

- **Final interview state:** `explicitly-finished`
- **Authoritative English source:** `docs/specs/engine-preset-stats-spec.md`
- **Korean mirror:** `docs/specs/engine-preset-stats-spec.ko.md`
- **Synchronization check:** both files carry identical stable IDs, statuses, requirements, decisions, risks, unresolved items, and next authorized action.
- **Remaining gaps:** OI-005, OI-006, OI-007, OI-010, OI-011, OI-012, OI-013, OI-016, OI-017; risks RK-001, RK-003, RK-004, RK-005, RK-007, RK-008, RK-009.
- **Assumptions still requiring confirmation:** AR-001, AR-003, AR-005, AR-006, AR-008, AR-010, AR-011, AR-012.
- **Next authorized action:** implement R-001~R-015 and R-017. Implementation adopts AR-001, AR-003, AR-008, AR-010, AR-011, AR-012 and the recommendations recorded for OI-006, OI-007, OI-011, OI-013 as engineering defaults; those items stay open for design confirmation.
- **Not authorized:** GDD 06/07/08 edits (R-016), the formula redesign (OI-012), runtime slot persistence (OI-010), commits.

> Approving this plan does not by itself authorize commits, deployment, publishing, or external-system changes.
