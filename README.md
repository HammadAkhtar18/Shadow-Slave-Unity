# Shadow Slave (Unity)

Native Unity port of the *Shadow Slave* game prototype. The Unreal Engine 5 project remains the behavioural reference; this repository is an idiomatic C# / Unity rewrite, not a mechanical API translation.

## Unity version

**Unity 6000.3.24f1 (Unity 6.3 LTS)**

Recorded in `ProjectSettings/ProjectVersion.txt`. Open this project with that editor version (or compatible 6000.3.x).

## Layout

```
Assets/ShadowSlave/
  Core/           Gameplay tags, logging
  Attributes/     Health / Stamina / Essence
  Characters/     CharacterBase foundation
  Combat/         DamageInfo (foundation)
  Tests/EditMode/ Unity Test Framework Edit Mode tests
  …               Folder stubs for remaining systems
```

## Opening the project

1. Install Unity Hub + Editor **6000.3.24f1**.
2. Add this folder as a project and open it.
3. Allow Unity to regenerate `.meta` files and resolve packages from `Packages/manifest.json`.
4. Open **Window → General → Test Runner** and run Edit Mode tests under `ShadowSlave.Tests`.

## Validation status

Unity Editor is **not** installed in the agent environment that authored this foundation. See `VALIDATION.md` for what was statically checked versus what requires the Editor.

## Branching

Foundation work lives on `unity-foundation`. Do not push unless explicitly requested.
