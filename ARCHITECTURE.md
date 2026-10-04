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
5. **Combat** (primitives) → **Interaction** (foundation) → later: Items/Equipment → StatusEffects → Abilities/Aspects → AI → Progression → Echoes/Memories → Dialogue/Story/Quests → Nightmares → Save → UI → World/Content  

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
| **Dependencies** | Attributes, Progression; later Combat / Equipment / StatusEffects |
| **Unity** | `CharacterBase`, `ShadowSlaveGait` |
| **Notes** | Implements `IDamageable`. Requires `AttributeComponent` and `ProgressionComponent`. `CombatComponent` resolved when present; Equipment/StatusEffect still null stubs |

### Combat
| | |
|--|--|
| **UE** | `UShadowSlaveCombatComponent`, `FShadowSlaveDamageInfo` / `FShadowSlaveAttackData`, `IShadowSlaveDamageableInterface`, `AShadowSlaveCombatDummy`, hit-window anim notify |
| **Purpose** | Combat state machine, attack lifecycle, hit tracking, damage routing (melee traces / dodge deferred on Unity) |
| **Data** | `ECombatState`, `EAttackType`, `AttackData`, `DamageInfo` (UE has no damage-type field; none added) |
| **Runtime** | Start/cancel attack → Attacking → hit window → Recovering → Neutral; `TryApplyHit` applies damage via `IDamageable` or `AttributeComponent` |
| **Events** | `OnCombatStateChanged`, `OnAttackExecuted`/`Started`/`Ended`, `OnTargetHit`/`OnHitLanded`, `OnDamageDealt`, `OnDamageReceived` |
| **Dependencies** | Attributes (health reduction); Characters implement `IDamageable` |
| **Unity** | `CombatTypes`, `DamageInfo`, `IDamageable`, `CombatComponent`, `DamageCalculator`, `CombatDummy` |
| **Notes** | See behaviour notes below. Sphere sweeps, anim hit-window notifies, and full dodge movement are deferred |

#### Combat behaviour notes (from UE5 inspection)

- **Damage flow:** `CombatComponent` hit path → resolve `IDamageable.TakeDamage` on target (else `AttributeComponent.ApplyDamage`) → health clamp/death on attributes. `CharacterBase.TakeDamage` also notifies owner `CombatComponent.NotifyDamageReceived`.
- **Damage types:** UE `FShadowSlaveDamageInfo` has **no** damage-type field. Unity `DamageInfo` matches that parity — no type id or enum until a later system needs it.
- **Damage sources/causers:** `Attacker` and `DamageCauser` (`GameObject`, UE `TWeakObjectPtr<AActor>`). Attack instance id ties hits to one `ExecuteAttack` call.
- **Combat events/delegates:** State changed; attack executed/started/ended; target hit; damage dealt; damage received. Dodge started/ended/rejected exist in UE — Unity dodge execution deferred (types retained).
- **Attack state (`ECombatState`):** Neutral, Attacking, Recovering, Dodging, Stunned, Dead. Dead is terminal; Stunned interruptible into from any living state; attacks only from Neutral.
- **Hit handling / hit windows:** UE opens window via `UAnimNotifyState_ShadowSlaveHitWindow` or timer fallback, then sphere-sweeps (`PerformMeleeTrace`). **Unity deferral:** no physics sweeps / anim notifies yet; `OpenHitWindow`/`CloseHitWindow` + coroutine fallback duration; external systems call `TryApplyHit` / `RegisterHit`.
- **`IDamageable`:** Mirrors `IShadowSlaveDamageableInterface` (`TakeDamage`, `IsAlive`). Implemented by `CharacterBase` and `CombatDummy`.
- **`CombatComponent` responsibilities:** State transitions (re-entrancy guard), light/heavy `AttackData`, attack instance ids, `MaxHitsPerTarget` hit map, damage routing, owner death → Dead. Does **not** require `CharacterBase` specifically.
- **Cooldown/timing:** `HitWindowDuration` then `RecoveryDuration` (UE timers → Unity coroutines). No separate global attack cooldown beyond recovery/state gate.
- **Relationship with `AttributeComponent`:** Combat deals damage amounts; attributes own health reduction, clamp, death. Owner alive checks use `IDamageable` or attributes. Stamina for dodge is UE-side; dodge spend deferred here.
- **`CombatDummy` purpose:** Minimal damageable + attributes actor for prototype/hit validation without AI (UE `AShadowSlaveCombatDummy`).

#### UE5 → Unity combat mapping

| UE5 | Unity |
|-----|-------|
| `ECombatState` / `EAttackType` | Same enum names in `CombatTypes` |
| `FShadowSlaveDamageInfo` | `DamageInfo` |
| `FShadowSlaveAttackData` | `AttackData` (`AnimationClip?` instead of `UAnimMontage`) |
| `IShadowSlaveDamageableInterface` | `IDamageable` (C# interface) |
| `UShadowSlaveCombatComponent` | `CombatComponent` |
| `AShadowSlaveCombatDummy` | `CombatDummy` |
| `PerformMeleeTrace` sphere sweep | Deferred — use `TryApplyHit` |
| `UAnimNotifyState_ShadowSlaveHitWindow` | Deferred |
| `FTimerHandle` hit/recovery | Coroutines |
| Dynamic multicast delegates | C# `event` / `Action<>` |
| Full dodge (stamina, launch, montages) | Types only; execution deferred |

#### Deliberate differences

1. No montage / anim-notify hit window yet — timer/coroutine fallback opens the window; hits injected via `TryApplyHit`.
2. No sphere sweep / physics traces in this phase.
3. C# `IDamageable` instead of UE `UInterface`.
4. `OnHitLanded` alias alongside `OnTargetHit` for clearer naming; both fire on successful apply.
5. No damage-type field on `DamageInfo` — matches UE (deferred until a consumer exists).
6. Friendly-fire tag check uses string `tag` equality to avoid `CompareTag` failures when TagManager lacks Player/Enemy.

### Interaction
| | |
|--|--|
| **UE** | `UShadowSlaveInteractionComponent`, `IShadowSlaveInteractableInterface`, `FShadowSlaveInteractionResult`, `AShadowSlaveInteractableActor`, concrete door/switch/NPC/container/pickup |
| **Purpose** | Focus a candidate interactable and execute polymorphic Interact; prompt/priority for UI and future ranking |
| **Ownership** | `InteractionComponent` on the interactor owns current target + primary event bus. `InteractableBehaviour` is an optional target-side helper implementing `IInteractable` (no second OnInteracted bus) |
| **Data** | `InteractionResult` (Success, FailureReason, InteractionId, optional `Dictionary<string,string>` Metadata) |
| **Runtime / execution contract** | Explicit `SetCurrentInteractable` / `ClearCurrentInteractable` (detection deferred). `TryInteract`: (1) no target → false, no event; (2) invalid/lost target → clear, false, no event; (3) `!CanInteract(owner)` → false, no event, Interact not called; (4) `Interact(owner)`; (5) `OnInteracted` always after Interact; (6) return `result.Success`. Owner is this component's `gameObject` |
| **Events** | `OnInteractionTargetChanged(new, old)` only on actual GameObject reference change (null→null / A→A no fire). `OnInteracted(owner, targetGo, result)` only after Interact is called |
| **Detection** | Deferred — optional `SetInteractionDetectionEnabled` / `IsInteractionDetectionEnabled` flag only; **no** Physics, traces, or Update polling |
| **Dependencies** | None on Combat / Attributes / Characters. Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `IInteractable`, `InteractionResult`, `InteractionComponent`, `InteractableBehaviour` |
| **Deferred concretes** | Door / Switch / NPC / Container / Pickup / prompt UI / physics focus / priority ranking among overlaps |

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

### Progression
| | |
|--|--|
| **UE** | `UShadowSlaveProgressionComponent`, `EShadowSlaveCharacterRank`, `FShadowSlaveSoulCoreState` |
| **Purpose** | Character progression rank and soul core capacity ownership, advancement gates, and soul core tracking |
| **Ownership** | Character Rank and Soul Core state are owned by `ProgressionComponent`. `CharacterBase` requires and hosts `ProgressionComponent` on the same GameObject via `[RequireComponent(typeof(ProgressionComponent))]` |
| **Data** | `ShadowSlaveCharacterRank` (`Unknown = 0`, `Dormant = 1`, `Awakened = 2`, `Ascended = 3`, `Transcendent = 4`, `Supreme = 5`, `Sacred = 6`, `Divine = 7`, defaults to `Unknown`); `SoulCoreState soulCoreState` (`currentSoulCores = 1`, `maximumSoulCores = 1`). Serialized on component as stored state |
| **Runtime** | Rank API (`GetCharacterRank`, `HasKnownRank`, `SetCharacterRank`, `CanAdvanceRank`, `AdvanceRank`); Soul Core API (`GetSoulCoreCount`, `SetSoulCoreCount`, `GetMaxSoulCores`, `SetMaxSoulCores`, `AddSoulCore`, `RemoveSoulCore`, `GetSoulCoreState`). Helpers on `SoulCoreState` (`HasMultipleCores`, `IsMaxCoresReached`). Invariants: `0 <= CurrentSoulCores <= MaximumSoulCores`, `1 <= MaximumSoulCores`. Overflow-safe addition and subtraction. No `Update()` / no tick |
| **Events** | `OnCharacterRankChanged(newRank, oldRank)`, `OnSoulCoreCountChanged(newCount, oldCount)`, `OnMaxSoulCoresChanged(newMax, oldMax)`. Fires on actual changes; suppressed on same-value mutations. When decreasing maximum below current count, `OnMaxSoulCoresChanged` fires first, followed by `OnSoulCoreCountChanged` |
| **Deliberate boundaries** | Character Rank (quality) and Soul Cores (quantity/class) are orthogonal progression axes. Decoupled from `AttributeComponent`: resource pools (Health, Stamina, Essence) remain strictly owned by `AttributeComponent`. Character Rank is independent of future Aspect Rank. Progression has no automatic advancement. Save, UI, and Aspect integrations are deferred |
| **Dependencies** | None on Combat, Interaction, or Attributes. Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `ShadowSlaveCharacterRank`, `SoulCoreState`, `ProgressionComponent` |

### Nightmares / Story / Dialogue / Quests (Gameplay)
| | |
|--|--|
| **UE** | Nightmare subsystem + objectives; Story/Dialogue subsystems; Quest subsystem |
| **Purpose** | Nightmare session lifecycle; narrative and quest state |
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
