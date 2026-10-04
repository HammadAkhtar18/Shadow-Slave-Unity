# Validation report — Unity foundation + combat + interaction + progression (rank + soul cores)

## Environment

| Item | Status |
|------|--------|
| Unity Editor 6000.3.24f1 | **Not installed** on authoring machine |
| `dotnet` / `csc` / `mcs` | **Not available** |
| Static file + syntax checks | **Run** via `tools/static-validate.sh` |
| Unity Test Framework (Edit Mode) | **Authored**; **not executed** |
| Play Mode / Player build | **Not run** |

## What was validated

1. Repository structure: `Assets/`, `Packages/`, `ProjectSettings/` present.
2. `ProjectSettings/ProjectVersion.txt` records `m_EditorVersion: 6000.3.24f1`.
3. Required `Assets/ShadowSlave/*` folders and assembly definitions exist.
4. Foundation C# sources exist for Core, Attributes, Characters, Combat primitives, Interaction foundation, Progression foundation (Character Rank and Soul Cores).
5. Edit Mode test sources exist for GameplayTag, AttributeComponent, CharacterBase, Combat, Interaction, Progression (Rank and Soul Core specifications).
6. Canon documentation exists at `Docs/Canon/SoulCores.md`.
7. Brace / parenthesis balance and basic C# token checks via Python (no Unity compile).
8. UE5 reference at `/workspace/ShadowSlave` was read-only; not modified.
9. ProgressionComponent stores serialized `SoulCoreState` struct directly with overflow-safe additions (`AddSoulCore` with `int.MaxValue`).

## What could not be validated

1. Unity package resolve / import (Library generation).
2. Script compilation against `UnityEngine` / Input System / Test Framework.
3. Edit Mode or Play Mode test execution.
4. MonoBehaviour lifecycle (Awake/Start/coroutines) under the Player loop.
5. Stamina regen coroutine timing in Play Mode.
6. Scene / prefab wiring (none authored yet).

## How to validate locally

```bash
# From repo root (optional static re-check without Unity)
./tools/static-validate.sh

# In Unity 6000.3.24f1
# Test Runner → EditMode → Run All (ShadowSlave.Tests)
```

## Verdict

Foundation sources and project scaffolding are in place for Editor open + compile. **Unity compile was NOT run** in this environment.
