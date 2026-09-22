# Shadow Slave — Unity Architecture

## Relationship to Unreal

| Repository | Role |
|------------|------|
| `/workspace/ShadowSlave` (UE5) | **Behavioural reference only** — read, do not modify |
| `Shadow-Slave-Unity` | **Native Unity rewrite** preserving architecture and behaviour, not Unreal APIs |

This is not a line-by-line C++ → C# transliteration. UE types map to idiomatic Unity equivalents; ownership and event flow stay recognisable.

## Port order

1. **Core** — gameplay tags, logging  
2. **Attributes** — health / stamina / essence  
3. **Characters** — `CharacterBase` foundation  
4. Tags (covered in Core)  
5. Later: Combat → Interaction → Items/Equipment → StatusEffects → Abilities/Aspects → AI → Progression → Echoes/Memories → Dialogue/Story/Quests → Nightmares → Save → UI → World/Content  

## Cross-cutting UE → Unity mappings

| Unreal | Unity |
|--------|-------|
| `AActor` | `GameObject` + `MonoBehaviour` |
| `UActorComponent` | `MonoBehaviour` on the same GameObject |
| `UDataAsset` / primary data assets | `ScriptableObject` |
| `UObject` (lightweight) | Plain C# class or `ScriptableObject` |
| `UWorld` | `Scene` (+ optional bootstrap managers) |
| `UUserWidget` (UMG) | uGUI (`UnityEngine.UI` / UI Toolkit later) |
| Enhanced Input | Input System (`com.unity.inputsystem`) |
| Dynamic multicast delegates | C# `event` / `Action<>` (prefer over `UnityEvent` for code) |
| Gameplay Tags (`FGameplayTag`) | Custom `GameplayTag` + `GameplayTagContainer` |
| Game Instance subsystems | Scene or app-lifetime managers/services (singleton or injected) |
| `FTimerHandle` | Coroutines or cancellable async (no per-frame tick when avoidable) |
| `UPROPERTY` replication | Out of scope for foundation |

## Major systems (from UE `Source/ShadowSlave`)

### Core
| | |
|--|--|
| **UE** | `ShadowSlaveGameplayTags`, log channels, `AShadowSlaveGameModeBase`, player controller / camera manager |
| **Purpose** | Shared tags, logging, session bootstrap |
| **Data** | Native tag declarations (State / Event / Ability / Combat / Interaction) |
| **Runtime** | Tag queries; game mode sets default pawn/controller/HUD |
| **Events** | N/A (tags); game mode lifecycle |
| **Dependencies** | Engine gameplay tags module |
| **Unity** | `GameplayTag`, `GameplayTagContainer`, `ShadowSlaveTags`, `SSLog` |
| **Notes** | Custom tag system — do not depend on a third-party GAS port for foundation |

### Attributes
| | |
|--|--|
| **UE** | `UShadowSlaveAttributeComponent`, `FAttributeModifier`, `FAttributeInitConfig`, damage delegates |
| **Purpose** | Health, stamina, soul essence; modifiers; stamina regen without component tick |
| **Data** | Base maxima, regen rate/delay/interval, modifier list |
| **Runtime** | Apply/heal/clamp; consume/restore resources; recalculate max from Flat then Percent; regen via timers |
| **Events** | Health/Stamina/Essence changed; damage/heal received; death |
| **Dependencies** | Combat damage payload type |
| **Unity** | `AttributeComponent`, `AttributeTypes`, `DamageInfo` |
| **Notes** | No `Update()`; regen and modifier expiry use coroutines. Dead blocks heal/damage/consumes |

### Characters
| | |
|--|--|
| **UE** | `AShadowSlaveCharacterBase`, `EShadowSlaveGait`, player character |
| **Purpose** | Shared pawn: attributes, gait, movement suppression, damage routing hooks |
| **Data** | Character id, walk/sprint speeds, suppression source set |
| **Runtime** | Wire attribute events; sprint gated by alive/move/stamina; death suppresses movement |
| **Events** | Health changed, damaged, died, gait changed |
| **Dependencies** | Attributes; later Combat / Equipment / StatusEffects |
| **Unity** | `CharacterBase`, `ShadowSlaveGait` |
| **Notes** | Combat/Equipment/StatusEffect accessors are null-safe stubs until those systems are ported |

### Combat
| | |
|--|--|
| **UE** | `UShadowSlaveCombatComponent`, `FShadowSlaveDamageInfo`, damageable interface, hit-window notifies |
| **Purpose** | Combat state, melee traces, dodge, damage routing |
| **Unity (foundation)** | `DamageInfo` only |
| **Notes** | Full combat component deferred; attributes already accept `DamageInfo` |

### Interaction
| | |
|--|--|
| **UE** | `UShadowSlaveInteractionComponent`, interactable actors/interfaces, pickups |
| **Purpose** | Trace/focus interactables; doors, switches, NPCs, containers |
| **Unity** | Folder stub — port after Characters |

### Items / Equipment / Memories / Echoes
| | |
|--|--|
| **UE** | Inventory + item definitions; equipment slots; memory/echo components + content definitions |
| **Purpose** | Instance inventories, equip modifiers into Attributes, collectible Memories/Echoes |
| **Unity** | Folder stubs — definitions as `ScriptableObject` later |

### StatusEffects / Abilities / Aspects
| | |
|--|--|
| **UE** | Status effect component + definitions; Aspect component/abilities/flaws (Abilities folder empty in UE) |
| **Purpose** | Timed conditions; Aspect identity and abilities |
| **Unity** | Folder stubs |

### AI
| | |
|--|--|
| **UE** | `AShadowSlaveAIController`, enemy character base, AI state types, anim instance |
| **Purpose** | Idle → Investigate → Chase → Attack → Recover flow for Nightmare creatures |
| **Unity** | Folder stub — prefer Unity AI Navigation / custom state machine |

### Progression / Nightmares / Story / Dialogue / Quests (Gameplay)
| | |
|--|--|
| **UE** | Progression component; Nightmare subsystem + objectives; Story/Dialogue subsystems; Quest subsystem |
| **Purpose** | Rank/soul cores; Nightmare session lifecycle; narrative and quest state |
| **Unity** | Folder stubs — subsystems → managers/services |

### Save / UI / World / Content
| | |
|--|--|
| **UE** | SaveGame + save subsystem; UMG widgets + UI manager; world state component; content registry subsystem |
| **Purpose** | Persistence; presentation; world flags; static content lookup |
| **Unity** | Folder stubs — Save as `JsonUtility`/`ScriptableObject` snapshots; UI as uGUI; Content as SO registry |

## Foundation assemblies

- `ShadowSlave.Runtime` — gameplay code (`Assets/ShadowSlave`, excluding Tests)  
- `ShadowSlave.Tests` — Edit Mode tests referencing Runtime + NUnit / Test Framework  

## Design principles (carried from UE)

1. Prefer **events/timers/coroutines** over per-frame polling.  
2. Keep **content definitions** immutable and separate from runtime instances.  
3. Avoid hard-coded canon names in technical systems.  
4. Components stay reusable across player, NPC, and enemy.
