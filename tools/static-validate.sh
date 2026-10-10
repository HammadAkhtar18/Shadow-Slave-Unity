#!/usr/bin/env bash
# Static validation for Shadow Slave Unity foundation (no Unity Editor required).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
FAIL=0

echo "== Shadow Slave Unity — static validate =="
echo "Root: $ROOT"

require() {
  if [[ ! -e "$1" ]]; then
    echo "MISSING: $1"
    FAIL=1
  else
    echo "OK: $1"
  fi
}

require "ProjectSettings/ProjectVersion.txt"
require "Packages/manifest.json"
require "Assets/ShadowSlave/ShadowSlave.Runtime.asmdef"
require "Assets/ShadowSlave/Tests/ShadowSlave.Tests.asmdef"
require "Assets/ShadowSlave/Core/GameplayTag.cs"
require "Assets/ShadowSlave/Core/GameplayTagContainer.cs"
require "Assets/ShadowSlave/Core/ShadowSlaveTags.cs"
require "Assets/ShadowSlave/Core/SSLog.cs"
require "Assets/ShadowSlave/Attributes/AttributeTypes.cs"
require "Assets/ShadowSlave/Attributes/AttributeComponent.cs"
require "Assets/ShadowSlave/Combat/DamageInfo.cs"
require "Assets/ShadowSlave/Combat/CombatTypes.cs"
require "Assets/ShadowSlave/Combat/IDamageable.cs"
require "Assets/ShadowSlave/Combat/CombatComponent.cs"
require "Assets/ShadowSlave/Combat/DamageCalculator.cs"
require "Assets/ShadowSlave/Combat/CombatDummy.cs"
require "Assets/ShadowSlave/Characters/CharacterTypes.cs"
require "Assets/ShadowSlave/Characters/CharacterBase.cs"
require "Assets/ShadowSlave/Tests/EditMode/GameplayTagTests.cs"
require "Assets/ShadowSlave/Tests/EditMode/AttributeComponentTests.cs"
require "Assets/ShadowSlave/Tests/EditMode/CharacterBaseTests.cs"
require "Assets/ShadowSlave/Tests/EditMode/CombatTests.cs"
require "Assets/ShadowSlave/Interaction/IInteractable.cs"
require "Assets/ShadowSlave/Interaction/InteractionResult.cs"
require "Assets/ShadowSlave/Interaction/InteractionComponent.cs"
require "Assets/ShadowSlave/Interaction/InteractableBehaviour.cs"
require "Assets/ShadowSlave/Tests/EditMode/InteractionTests.cs"
require "Assets/ShadowSlave/Progression/ProgressionTypes.cs"
require "Assets/ShadowSlave/Progression/ProgressionComponent.cs"
require "Assets/ShadowSlave/Tests/EditMode/ProgressionTests.cs"
require "Assets/ShadowSlave/Aspects/AspectTypes.cs"
require "Assets/ShadowSlave/Aspects/FlawDefinition.cs"
require "Assets/ShadowSlave/Aspects/AspectAbilityDefinition.cs"
require "Assets/ShadowSlave/Aspects/AspectDefinition.cs"
require "Assets/ShadowSlave/Aspects/AspectComponent.cs"
require "Assets/ShadowSlave/Tests/EditMode/AspectTests.cs"
require "Assets/ShadowSlave/StatusEffects/StatusEffectTypes.cs"
require "Assets/ShadowSlave/StatusEffects/StatusEffectDefinition.cs"
require "Assets/ShadowSlave/StatusEffects/StatusEffectComponent.cs"
require "Assets/ShadowSlave/Tests/EditMode/StatusEffectTests.cs"
require "Assets/ShadowSlave/Items/ItemTypes.cs"
require "Assets/ShadowSlave/Items/ItemDefinition.cs"
require "Assets/ShadowSlave/Items/InventoryComponent.cs"
require "Assets/ShadowSlave/Tests/EditMode/InventoryTests.cs"
require "ARCHITECTURE.md"
require "README.md"
require "VALIDATION.md"
require "Docs/Canon/SoulCores.md"

if grep -q "m_EditorVersion: 6000.3.24f1" ProjectSettings/ProjectVersion.txt; then
  echo "OK: Unity version 6000.3.24f1"
else
  echo "FAIL: ProjectVersion.txt does not record 6000.3.24f1"
  FAIL=1
fi

echo
echo "== C# brace / paren balance =="
python3 - <<'PY'
import pathlib, sys
root = pathlib.Path(".")
fail = 0
for path in sorted(root.glob("Assets/ShadowSlave/**/*.cs")):
    text = path.read_text(encoding="utf-8")
    # strip strings roughly
    cleaned = []
    i = 0
    while i < len(text):
        c = text[i]
        if c == '"' or c == "'":
            q = c
            i += 1
            while i < len(text) and text[i] != q:
                if text[i] == '\\':
                    i += 2
                    continue
                i += 1
            i += 1
            continue
        if c == '/' and i + 1 < len(text) and text[i+1] == '/':
            while i < len(text) and text[i] != '\n':
                i += 1
            continue
        if c == '/' and i + 1 < len(text) and text[i+1] == '*':
            i += 2
            while i + 1 < len(text) and not (text[i] == '*' and text[i+1] == '/'):
                i += 1
            i += 2
            continue
        cleaned.append(c)
        i += 1
    s = ''.join(cleaned)
    for open_c, close_c in [('(',')'), ('{','}'), ('[',']')]:
        bal = s.count(open_c) - s.count(close_c)
        if bal != 0:
            print(f"FAIL balance {open_c}{close_c}={bal}: {path}")
            fail = 1
if fail:
    sys.exit(1)
print("OK: brace/paren balance on all foundation .cs files")
PY

echo
if [[ "$FAIL" -ne 0 ]]; then
  echo "STATIC VALIDATION FAILED"
  exit 1
fi
echo "STATIC VALIDATION PASSED"
echo "NOTE: Unity compile and Test Runner were NOT executed."
