# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.3.10f1** (URP 17.3) 2D/3D game project. All C# lives under `Assets/`, namespaced `Border.*`.
There is no CI, no build script, and no lint config — Unity itself is the toolchain.

## Standard work gate

Start each task with `git status --short` and inspect the diff for files you will edit. Existing dirty files are
user or parallel-session work; do not overwrite, revert, or tidy them unless explicitly requested. Stage only
current-session changes.

When using Unity MCP, verify the shared HTTP endpoint `http://127.0.0.1:8080/mcp`, connected instance, project
path, Unity version, and Editor readiness before mutating anything. If the Editor is in Play Mode, compiling, or
running tests, wait or stop rather than forcing state changes.

Use Context7 only when current API/package behavior matters: Unity APIs, package setup, version-sensitive
settings, or unfamiliar SDKs. Routine C# edits should not pay that lookup cost.

Apply validation in increasing cost order: compile/Console, new or changed tests, nearby test class, affected
assembly, then full EditMode/PlayMode only for broad changes, release, or merge gates. UI, shader, VFX, camera,
scene, and prefab layout changes require Game View or screenshot verification. Report only verification actually
run; list skipped checks as not tested.

## Commands

Unity is normally driven through the Editor GUI. For headless runs (the Editor must be **closed** —
it holds a project lock).

The Unity executable path is machine-specific — resolve it per machine, do not hardcode it. Hub defaults:

| OS | Path |
|---|---|
| Windows | `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe` |
| macOS | `/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity` |
| Linux | `~/Unity/Hub/Editor/6000.3.10f1/Editor/Unity` |

Run the commands below **from the repository root**, so `-projectPath .` resolves correctly:

```bash
"$UNITY" -batchmode -quit -projectPath . -logFile -
```

Run tests (EditMode or PlayMode):

```bash
"$UNITY" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults "results.xml" -logFile -
```

Run a single test or one assembly — add to the command above:

```bash
-testFilter "Border.Rendering.Tests.UberShaderSuiteTests.VariantCollectionContainsExactlyReviewedWhitelist"
```

```bash
-assemblyNames "Border.Audio.EditModeTests"
```

Notes:
- The Uber shader tests compile shader variants; do **not** pass `-nographics` for `Border.Rendering.EditModeTests`.
- In the Editor, the same suites run from **Window → General → Test Runner**.
- Shader variant collection is maintained from **Tools → Uber Shader → Verify / Rebuild Variant Collection**
  (`UberShaderVariantCollectionGenerator`); the manifest is the source of truth, the `.shadervariants` asset is generated.

## Play Mode is a shared, single-occupancy resource

This section applies **only when the MCP for Unity server is connected**. The Unity-side package ships with
the project (`com.coplaydev.unity-mcp` in `Packages/manifest.json`), but the client side is registered per
machine and the bridge has to be running — see "MCP for Unity setup" below. Without it, `manage_editor` and
the `mcpforunity://` resources do not exist; drive Play Mode from the Editor by hand and ignore the rest of
this section.

On a machine where it is connected, one Unity Editor is shared by the user and every Claude Code session on
that machine. Only one of them can be in Play Mode at a time, so treat it as a lock that must be checked
before taking and released immediately after use.

**Before entering Play Mode** — read the `mcpforunity://editor/state` resource and check `is_playing`
(`activity_phase` is `playmode_transition` while it is changing):

- If it is already playing and **this session did not start it**, someone else owns it. Do **not** call
  `manage_editor` with `stop` or `pause`, and do not run PlayMode tests. Wait and re-check, then report to the
  user that Play Mode is occupied — never seize it, and never "just stop it quickly" to fit work in.
- Only enter Play Mode (`manage_editor` action `play`) when it is idle.

**After entering Play Mode** — always exit it (`manage_editor` action `stop`) as soon as the verification is
finished, *including when the run fails, errors, or is interrupted*, and before reporting results. Do not end a
turn leaving Play Mode running or paused; leaving it entered blocks the user and every other session.

The same applies to PlayMode test runs (`Border.Audio.PlayModeTests`), which occupy the Editor for their whole
duration: check first, release afterwards. Batchmode CLI runs need the Editor fully closed — never close or
kill the user's running Editor to make one possible; ask instead.

### MCP for Unity setup

Per-machine, not carried by the repository:

1. Open the project in Unity once so the `com.coplaydev.unity-mcp` package resolves.
2. In the Editor, **Window > MCP For Unity**, keep "Use HTTP transport" enabled, start the bridge and note the
   port it reports (default `8080`).
3. Register the server with your client from that same window's **Configure** button, or by hand at user scope:
   `claude mcp add --scope user --transport http UnityMCP http://127.0.0.1:8080/mcp` — substitute the port from
   step 2. Verify with `claude mcp list`.
4. Approve the server when Claude Code prompts for it on first use.

Personal MCP servers and response-style preferences belong in your own `~/.claude/CLAUDE.md` and
`~/.claude.json`, never in this file — API keys must never be committed.

## Assembly layout

`.asmdef` boundaries matter more than folders here:

| Assembly | Location | Notes |
|---|---|---|
| `Border` | `Assets/01. Scripts` | All runtime code (Core, Events, Audio, Settings, SaveLoad, Localization, UI, Rendering) in one assembly |
| `Border.Input` | `Assets/01. Scripts/Input` | Separate, gated by `BORDER_INPUTSYSTEM` version define on `com.unity.inputsystem` |
| `Border.Editor` | `Assets/06. Packages/Editor` | Editor-only inspectors/drawers for `Border` types |
| `UberShader.Editor` | `Assets/05. Arts/Shader/Editor` | Shader GUI + variant manifest; depends on `LWGUI` |
| `Border.Audio.EditModeTests` / `Border.Audio.PlayModeTests` / `Border.Rendering.EditModeTests` | `Assets/Tests` | `autoReferenced: false`, `TestAssemblies` |

## Architecture

### `Assets/01. Scripts` is a vendored copy of an upstream package

`com.borderjung.unity-modules` (pinned `#v2.0.0` in `Packages/manifest.json`) is a **source-delivery**
package: its code lives in `Plugins~/`, so it does *not* compile from `Packages/`. The sources were imported
into `Assets/01. Scripts` and are now project-owned and editable. `Core`, `Events`, `SaveLoad`, `Localization`,
`Settings`, `UI`, `Input` are still byte-identical to upstream; `Audio/` and `Rendering/` are project-original.

Consequences when editing:
- Re-importing the package sample would overwrite the vendored folders — never import into a second location,
  or the `Border` assembly gets defined twice.
- Changes to upstream-mirrored files are local forks; keep them deliberate.

Module layers (all under namespace `Border.*`):
- **Core** — `Log` (every method `[Conditional("UNITY_EDITOR")]`, so logs are stripped from builds — use it
  instead of `UnityEngine.Debug`), `DeterministicRng` (GC-free xorshift32), `ScreenshotManager`.
- **Events** — ScriptableObject event channels (`Void/Bool/Int/Float/Vector2/String`, `FadeChannelSO`) wired in
  the Inspector; the standard decoupling mechanism between systems.
- **Settings / SaveLoad / Localization** — SO-backed config with `ISettingsRepository` injection, JSON save
  files, SO localization tables plus a `[LocalizeKey]` drawer.

### Audio (`Border.Audio`)

`SoundManager` is a `DontDestroyOnLoad` singleton (`SoundManager.Instance`) composing four serialized parts:
`SoundDatabaseSO` (id → `BgmEntry`/`SfxEntry`, category-local and **case-sensitive** lookup, invalid rows
skipped, first valid duplicate wins), `BgmPlayer` (two-source crossfade), `SfxPool` (pooled voices), and
`AudioMixerVolumeController`. SFX playback returns a `SoundHandle` — a generation-checked struct over a
`PooledSfxVoice`, so a stale handle silently no-ops instead of controlling a recycled voice. Spatial entries
require `PlaySfxAt`/`PlaySfxAttached` and route through the Steam Audio spatializer (`Assets/Plugins/SteamAudio`).
Scene wiring lives in `Assets/03. Prefabs/Systems/SoundManager.prefab` + `Assets/02. ScriptableObjects/Audio/SoundDatabase.asset`.

### Uber shader family (`Assets/05. Arts/Shader`)

Five sibling URP shaders — `Shader/Uber/{3D Object, 2D Sprite, UI, Particle, Post Processing}` — each a
`.shader` + `.hlsl` pair, sharing only `UberCommon.hlsl`. The contract stated in `UberCommon.hlsl`: it holds
**only stateless helpers used unchanged by ≥2 families**; textures, cbuffers, surface structs, keywords, and
effect ordering stay surface-owned. Features are toggled by `_*_ON` shader keywords (dissolve, light sweep,
outline/glow, color adjust, UV fade, post filters…).

Supporting pieces:
- **LWGUI** (`Assets/06. Packages/com.jasonma.lwgui`) drives the material inspectors via property-drawer
  attributes (`[Main]`, `[Sub]`, `[KWEnum]`, `[Title]`) declared in the `Properties` block; `UberShaderGUI.cs`
  adds project-specific drawers (`UberMinMaxVector`, `UberVector2`, `UberGradient`, …).
- **Variant control** — `UberShaderVariantManifest` lists the reviewed (shader, pass, keywords) rows;
  `UberShaderVariantCollectionGenerator` verifies/rebuilds `UberShaderVariants.shadervariants` from it and is
  preloaded via Graphics Settings. Adding a keyword combination means adding a manifest row.
- **Runtime binders** feed per-instance data without breaking batching or touching shared materials:
  `UberSpritePropertyBinder` (MaterialPropertyBlock, sprite atlas UV rects, secondary layer) and
  `UberUIMaterialBinder` (`IMaterialModifier`; outline/glow padding is deliberately restricted to Simple
  Images with Sprite Mesh off so other Graphic geometry is never distorted).
- Post processing is a URP **Full Screen Pass Renderer Feature** ("Uber Post Processing") on `PC_Renderer` /
  `Mobile_Renderer` in `Assets/Settings`, using `Assets/05. Arts/Material/UberPostProcessing.mat`.

### The shader test suite is a spec lock, not a smoke test

`Assets/Tests/EditMode/Rendering/UberShaderSuiteTests.*.cs` (~8.8k lines, one partial class per family) asserts
review decisions, not just behavior: exact asset paths **and GUIDs**, keyword pragma policy (local vs. the
global allow-list), cbuffer layout, cross-family pass parity, LWGUI group/foldout structure, inspector hot-path
allocation, WebGL/GLES3 variant compilation, and byte-level GPU matrices. Renaming or moving a shader, HLSL,
material, font, or editor file — or adding a keyword — will fail these tests by design. Update the constants in
`UberShaderSuiteTests.cs` / `UberShaderVariantManifest.cs` alongside the change, and preserve `.meta` GUIDs.

Audio tests reach into private serialized fields via reflection (`SetField`) and build objects in-memory rather
than loading project assets; follow that pattern and clean up in `TearDown`/`UnityTearDown`.

## Scenes are the conflict hot spot — prefer prefabs

Scene files (`.unity`) are the one asset everyone on the team touches at once, and their YAML merges badly.
**Default to changing a prefab, not a scene.** Before editing a scene, ask whether the same result can be
reached by editing a prefab, a ScriptableObject, or code.

Prefer, in this order:

1. **Prefab** — edit the prefab asset (or a prefab variant) so the change reaches every scene that instances it.
   Wiring, component values, child hierarchy and event-channel references all belong here.
2. **ScriptableObject** — data and configuration (`Assets/02. ScriptableObjects`), including the event channels
   in `Border.Events` that already decouple systems from each other.
3. **Code** — behaviour that does not need to be authored per instance.
4. **Scene** — last resort.

A scene edit is justified only when the change is genuinely scene-local: placing or removing an instance in the
level, scene-specific transforms, lighting/camera setup, or wiring a reference that can only exist between two
objects already in that scene. When one is unavoidable:

- Say so before making it, and keep it to the smallest possible diff — do not reorder, re-save, or "tidy"
  unrelated parts of the scene.
- Never apply a prefab-instance override in a scene when the value belongs on the prefab itself; push it to the
  prefab so other scenes get it too.
- Do not open or re-save scenes that the task does not require; simply entering Play Mode or opening a scene can
  dirty it, so never save one that has no intended change.
- `.gitattributes` sets `merge=unityyamlmerge` — never hand-merge a scene or prefab.

## Conventions

- Commit messages, branches, and PR titles follow `README.md` (Korean): `TAG(#issue) : 제목`, tags
  `feat|fix|docs|art|sound|merge|chore`; branch `TAG/주요내용/#issue`; PR title `FEAT/35 (#40)`.
- Every asset needs its `.meta` committed; `.gitattributes` sets `merge=unityyamlmerge` for Unity YAML — do not
  hand-merge scenes/prefabs.
- Serialized fields are `[SerializeField] private` with public read-only properties; components that must run in
  edit mode are `[ExecuteAlways] [DisallowMultipleComponent]`.
- Cache `Shader.PropertyToID` in `static readonly int` fields rather than passing property-name strings.
