# Rocket Launch Prototype Revision

> Revised: 2026-09-06
>
> Status: main-game direction decided

## Goal

Integrate the prototype's free assembly and physical flight into the main launch flow. The player submits a design before launch; the rocket then flies from that design.

## Confirmed Behavior

- Free attachment on the rocket surface
- Local-axis translation and rotation gizmos
- Force and torque from each engine's position and orientation
- Per-engine fuel, heat, and ignition state
- Right-drag orbit camera
- Continuous collision for ground contact
- Mission-specific telemetry collection
- Mission evaluation from runtime physical state

## Main-Game Boundary

```text
DesignData
-> construct rocket and engines
-> run physics
-> collect telemetry
-> evaluate mission
-> LaunchResult
```

`DesignData` contains engine presets, transforms, operating windows, and cost. The result is created only after simulation.

## Requirements

- An engine contributes mass and thrust only while attached to the rocket.
- Thrust is applied at the engine's actual position along its `up` direction.
- Asymmetric designs create physical torque.
- Fuel depletion, overheating, ignition failure, and collisions produce structured termination reasons.
- Mission evaluators read runtime state, not display text.
- A launch result is applied once.
- Visibility does not affect physics or mission evaluation.

## Excluded

- In-flight thrust controls
- Staging
- Gimbals and automatic attitude control
- Full orbital mechanics
- Presentation paths that override flight
- Any aggregate modifier that replaces the submitted physical design

## Verification

- The same design and initial state produce the same trajectory.
- Changing engine position, orientation, or count produces the expected physical difference.
- Ground and water contact arise from the actual collision state.
- The mission condition and result report agree.
- Temporary objects and input locks are released after simulation.
