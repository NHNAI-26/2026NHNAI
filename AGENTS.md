# Repository Guidelines

## Project Structure & Module Organization

Unity `6000.3.10f1` source and assets live under `Assets/`:

- `Assets/00. Scenes/`: scenes.
- `Assets/01. Scripts/`: runtime C# and asmdefs.
- `Assets/02. ScriptableObjects/`, `Assets/03. Prefabs/`, `Assets/04. Audios/`, `Assets/05. Arts/`: project data and content.
- `Assets/06. Packages/`, `Assets/Plugins/`: bundled or third-party code.
- `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`: Unity Test Framework tests.

Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or `.omx/`.

## Standard Work Gate

Start with `git status --short` and target diffs. Treat dirty files as parallel work; never overwrite or revert them unless requested.

Unity paths are machine-specific; resolve them as `$UNITY` instead of hardcoding one path.

When using Unity MCP, first confirm `http://127.0.0.1:8080/mcp`, instance, project path, Unity version, and Editor readiness. Treat Play Mode as a shared lock: if this session did not start it, do not stop, pause, or seize it. If Unity is compiling or testing, wait.

Use Context7 only for current API/package behavior: Unity APIs, package setup, version-sensitive settings, or unfamiliar SDKs.

## Documentation

Plans, specs, and feature descriptions belong in `docs/` as Markdown, one subject per file
(`docs/<subject>.md`, lowercase kebab-case). Search `docs/` and update the document that already covers the area
before creating a new one; rewrite stale content instead of appending to it. Update the document in the same
commit as the change it describes. Keep `CLAUDE.md` and `AGENTS.md` limited to agent work rules.

## Build, Test, and Development Commands

```powershell
dotnet build .\NHNAI2026.slnx -nologo
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults .\Logs\editmode-results.xml -quit
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults .\Logs\playmode-results.xml -quit
```

Use `dotnet build` for C# compile feedback; use batchmode for Unity behavior.

Do not run `dotnet build`, Unity compilation, or Unity batchmode tests for trivial edits, small documentation changes, formatting-only work, or read-only investigation. When compilation or tests are needed, batch related changes first and run the fewest meaningful verification commands so the user can keep testing in Unity without repeated interruptions.

## Coding Style & Naming Conventions

Use four-space indentation. Use PascalCase for types, methods, and public properties; camelCase for locals and parameters; `_camelCase` for private serialized fields when matching nearby code. Keep pure C# separate from Unity scene/component behavior.

Preserve asmdef boundaries, `Border.*` namespaces, `.meta` files, serialized references, and GUID-sensitive assets. Avoid hand-editing Unity YAML.

Prefer Prefab, ScriptableObject, or code changes before scene edits. Edit scenes only for scene-local placement, transforms, lighting, camera setup, or unavoidable scene references.

## Testing Guidelines

Test in stages: new/changed test, nearby class, affected assembly, then full EditMode/PlayMode for broad changes, release, or merge gates. Rendering, audio, input, shared contracts, and state changes need more checks.

For UI, shader, VFX, camera, scene, or prefab changes, verify in Game View or screenshots. Delete screenshot files once the check is done; never commit them or leave them in the working tree. Report only tests run; list skipped checks as not tested.

## Commit & Pull Request Guidelines

Follow `README.md`: `TAG(#issue) : Title`, using `feat`, `fix`, `docs`, `art`, `sound`, `merge`, or `chore`. Branch names use `TAG/summary/#issue`, for example `feat/player/#99`.

Before committing, run `git update-index -q --refresh` to clear stat-only `NHNAI2026.slnx` noise. Do not hide meaningful solution changes with ignore or skip-worktree. Stage only current-session changes. PRs need a summary, verification notes, linked issues, and screenshots/clips for visual changes.
