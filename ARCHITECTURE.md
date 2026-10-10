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
| **Data** | Base maxima, regen rate/delay/interval, modifier list, timed modifier tracking entries (`TimedModifierEntry`) |
| **Runtime** | Apply/heal/clamp; consume/restore resources; recalculate max from Flat then Percent; regen via coroutines. Timed modifier lifecycle: modifier duration represents absolute game time (`ExpirationTime = now + Duration`), matching UE5 `FTimerManager` world-time semantics; disabling the component does not pause duration. Re-enabling (`OnEnable` / `ReconcileTimedModifiers`) prunes modifiers that expired while disabled and reschedules remaining duration for active ones. Ownership of expiration belongs strictly to `AttributeComponent`. A monotonic generation token prevents stale coroutines from evicting replaced or re-added modifier instances, ensuring expiration occurs exactly once. `RecalculateMaxAttributes` wraps event dispatches in isolated try-catch blocks logging via `SSLog.Error` to guarantee exception safety |
| **Events** | Health/Stamina/Essence changed; damage/heal received; death |
| **Dependencies** | Combat damage payload type |
| **Unity** | `AttributeComponent`, `AttributeTypes`, `DamageInfo` |
| **Notes** | No `Update()`; regen and modifier expiry use coroutines with absolute expiration reconciliation. Dead blocks heal/damage/consumes |

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
| **Notes** | Implements `IDamageable`. Requires `AttributeComponent` and `ProgressionComponent`. `CombatComponent` and `StatusEffectComponent` resolved when present; Equipment still null stub |

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

### Items & Inventory
| | |
|--|--|
| **UE** | `UShadowSlaveInventoryComponent`, `UShadowSlaveItemDefinition`, `FShadowSlaveItemInstance`, `EShadowSlaveItemType`, `EShadowSlaveEquipmentSlot`, `FOnInventoryItemAddedSignature`, `FOnInventoryItemRemovedSignature`, `FOnInventoryChangedSignature` |
| **Purpose** | Generic, data-driven item definitions and runtime inventory component managing slots, stacking, capacity, partial additions with remainder, atomic removals, and query APIs |
| **Ownership** | `ItemDefinition` is an immutable static archetype (`ScriptableObject`) describing item classification, stacking rules, max stack size, weight, and base value. `ItemInstance` is a runtime representation separating immutable static archetype data from mutable instance-specific runtime data (`InstanceId`, `Quantity`, and encapsulated `DynamicProperties` as `IReadOnlyDictionary<string, string>` backed by `ReadOnlyDictionary` to prevent downcast mutations). `InventoryComponent` is a reusable `MonoBehaviour` owning actor inventory slots (`_slots` exposed via read-only view `Slots` / `GetSlots()`) |
| **Stacking & Capacity** | Purely event-driven without tick overhead. Supports configurable slot capacity (`MaxSlots` clamped >= 1). `AddItem` checks `IsStackable`, fills existing partially filled compatible stacks in Phase 1, allocates empty slots up to capacity in Phase 2, and outputs unplaced units via `remainder`. Non-stackable items occupy 1 unit per slot. `AddItemInstance` preserves instance GUID and dynamic properties, matching stacks via `CanStackWith` |
| **Atomic Removal** | `RemoveItem` verifies `GetTotalItemCount(itemDef) >= quantity` upfront. If insufficient, returns false with zero items removed (atomic guarantee). Removes backwards from trailing stacks, creating isolated removed snapshots. `RemoveItemByInstanceId` decrements or removes specific stacks by unique GUID |
| **Safety & Reentrancy** | Transition guard `_isProcessingInventoryTransition` protects against recursive mutations during all state transitions and event callbacks (`OnItemAdded`, `OnItemRemoved`, `OnInventoryChanged`) with `try-finally` safety. Delegate broadcasts wrap in `try-catch` logging to `SSLog.Error(SSLog.CategoryItems, ...)` so subscriber exceptions cannot abort state commit or caller execution |
| **Queries & Persistence** | Query APIs: `HasItem`, `FindItem`, `FindItemByInstanceId`, `HasItemByInstanceId`, `GetTotalItemCount`, `GetItemsByType`, `IsFull`, `UsedSlotCount`, `FreeSlotCount`. Persistence restoration API: `RestoreInventory` replaces slots directly without triggering gameplay acquisition rules |
| **CharacterBase Integration** | Optional `InventoryComponent` property on `CharacterBase` resolved via `GetComponent<InventoryComponent>()` in `Awake()`; null-safe if omitted |
| **Dependencies** | `ShadowSlave.Core` (`SSLog`). Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `ShadowSlaveItemType`, `ShadowSlaveEquipmentSlot`, `ItemInstance`, `ItemDefinition`, `InventoryComponent` |
| **Deferred Systems** | Memories, Echoes, loot drops, crafting, shops, UI, save/load, and gameplay-specific effects deferred to subsequent phases |

### Equipment
| | |
|--|--|
| **UE** | `UShadowSlaveEquipmentComponent`, `EShadowSlaveEquipmentSourceType`, `FShadowSlaveEquippedItem`, `FOnEquipmentItemEquippedSignature`, `FOnEquipmentItemUnequippedSignature`, `FOnEquipmentSlotChangedSignature`, `FOnEquipmentChangedSignature` |
| **Purpose** | Reusable actor component managing equipped items and Memories across equipment slots; coordinates between `InventoryComponent`, `AttributeComponent`, and future `MemoryComponent` purely event-driven without tick overhead |
| **Ownership** | `EquipmentComponent` owns equipped-slot runtime assignments (`_equippedSlots` mapped from `ShadowSlaveEquipmentSlot` to `EquippedItem`, exposed as `IReadOnlyDictionary<ShadowSlaveEquipmentSlot, EquippedItem>` backed by `ReadOnlyDictionary` to prevent downcast mutations). `InventoryComponent` remains authoritative for item ownership, quantities, and lifetime. `AttributeComponent` remains authoritative for attribute calculations and modifier lifecycle. `EquippedItem` is a lightweight descriptor referencing authoritative instance GUID, slot, source type, and definition ID without duplicating mutable state |
| **Modifier Lifecycle & Isolation** | Equipment-granted modifiers from `ItemDefinition.GrantedModifiers` are applied via `ApplyModifiersForSource` with generated composite IDs (`$"{baseName}_{sourceId:N}_{index}"`) and `SourceId = sourceId`. Modifiers are removed via `AttributeComponent.RemoveModifiersFromSourceId`, guaranteeing strict source isolation so modifiers belonging to Aspects, Status Effects, or other equipment instances are untouched. Modifier application is atomic: failure rolls back applied modifiers and leaves slot occupant unchanged |
| **Inventory Lifecycle Integration** | Subscribes dynamically to `InventoryComponent.OnItemRemoved` and `InventoryComponent.OnInventoryChanged`. When an equipped item instance is depleted from inventory or the inventory is cleared, `EquipmentComponent` automatically unequips the affected slot and strips its modifiers. Partial stack removal is explicitly handled: if remaining stack `Quantity > 0`, the item remains equipped |
| **Safety & Reentrancy** | Reentrancy guard `_isProcessingEquipmentTransition` protects all equip and unequip state transitions and event dispatches (`OnEquipmentItemEquipped`, `OnEquipmentItemUnequipped`, `OnEquipmentSlotChanged`, `OnEquipmentChanged`) with `try-finally` safety. Reentrant inventory callbacks during active transitions are deferred via `_hasPendingInventoryReconciliation` and executed immediately post-transition, preventing dropped inventory reconciliations. Delegate invocations are wrapped in `try-catch` logging to `SSLog.Error(SSLog.CategoryEquipment, ...)` so subscriber exceptions cannot corrupt internal state or abort caller execution |
| **CharacterBase Integration** | Optional `EquipmentComponent` property on `CharacterBase` resolved via `GetComponent<EquipmentComponent>()` in `Awake()`; null-safe if omitted |
| **Dependencies** | `ShadowSlave.Core` (`SSLog`), `ShadowSlave.Items`, `ShadowSlave.Attributes`. Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `EquipmentSourceType`, `EquippedItem`, `EquipmentComponent`, `EquipmentTests` |
| **Deliberate boundaries & Deferred Systems** | Memory integration (`EquipmentSourceType.Memory`), MemoryComponent integration, Echoes, equipment UI, durability/repair systems, crafting, shops, loot drops, save/load serialization, and novel-specific equipment assets are deferred to subsequent phases |

### StatusEffects
| | |
|--|--|
| **UE** | `UShadowSlaveStatusEffectComponent`, `UShadowSlaveStatusEffectDefinition`, `FShadowSlaveStatusEffectInstance`, `FShadowSlaveStatusEffectSource`, `EStatusEffectDurationPolicy`, `EStatusEffectStackingPolicy`, `EStatusEffectPolarity` |
| **Purpose** | Generic runtime condition and status effect foundation; manages active effects, stacking, duration, polarity, attribution, and lifecycle |
| **Ownership** | `StatusEffectDefinition` is an immutable static archetype (`ScriptableObject`) with duration policy, duration, stacking policy, max stacks, polarity, and persistence flags. `StatusEffectComponent` owns all active runtime `StatusEffectInstance` objects on the actor. Strictly decoupled from `AttributeComponent` (does NOT duplicate attributes or calculate health/essence directly) and `CombatComponent` |
| **Policies** | Duration policies: `Instant` (immediate notification, no persistence), `Timed` (authoritative game-time expiration), `Persistent` (persists until manually removed). Stacking policies: `IgnoreNew` (retains existing instance), `RefreshDuration` (resets duration to full), `AddStacks` (increments stacks up to `MaxStacks` and refreshes duration), `Replace` (removes old and applies new) |
| **Runtime & Lifecycle** | Purely event-driven and timer-driven; NO `Update()` polling loops. Timed effects track authoritative `ExpirationTime` and monotonic generation tokens to prevent stale coroutine callbacks. Disabling component halts coroutines while preserving state; `OnEnable` / `ReconcileTimedEffects` prunes expired effects and reschedules active ones for exact remaining duration. Protected by reentrancy guard (`_isProcessingEffectTransition`) rejecting recursive mutations during callbacks. Exception-safe event dispatch via `SSLog.Error` |
| **Events** | `OnStatusEffectApplied`, `OnStatusEffectRemoved`, `OnStatusEffectExpired`, `OnStatusEffectStackChanged`, `OnStatusEffectCollectionChanged` |
| **Dependencies** | `ShadowSlave.Core` (`SSLog`); optional integration on `CharacterBase` |
| **Unity** | `StatusEffectComponent`, `StatusEffectDefinition`, `StatusEffectTypes` (`StatusEffectInstance`, `StatusEffectSource`, policies) |
| **Deliberate boundaries** | Generic technical foundation with zero canon mechanics (no hardcoded poison/stun/DOT/CC gameplay logic, no VFX, no UI, no global registry). Canon distinctions: conditions, enchantments, and corruptions exist in canon [CANON], but numeric duration policies and stacking rules are game-design adaptations [GAME ADAPTATION] |

### Aspects
| | |
|--|--|
| **UE** | `UShadowSlaveAspectComponent`, `UShadowSlaveAspectDefinition`, `UShadowSlaveAspectAbilityDefinition`, `UShadowSlaveFlawDefinition`, `EShadowSlaveAspectRank`, `FShadowSlaveAspectAbilityInstance` |
| **Purpose** | Static Aspect archetype content definitions, abilities, bound flaws, runtime ability instances, and runtime component binding |
| **Ownership** | `AspectDefinition`, `AspectAbilityDefinition`, and `FlawDefinition` are static serialized content assets (`ScriptableObject`) authored through Unity's asset workflow. They are read-only at runtime without general mutation setters (retaining only explicit identity compatibility setters `SetAspectId`, `SetAbilityId`, `SetFlawId`). `AspectComponent` owns the character's active Aspect binding, active Flaw binding (`activeFlawDefinition`), and all runtime `AspectAbilityInstance` objects (`IsUnlocked`, `IsActive`, `DynamicProperties`). Dynamic properties belong exclusively to runtime `AspectAbilityInstance` objects, not static definitions |
| **Classification** | `AspectRank` (`Unknown = 0`, `Dormant = 1`, `Awakened = 2`, `Ascended = 3`, `Transcendent = 4`, `Supreme = 5`, `Sacred = 6`, `Divine = 7`). **Completely independent from Character Rank** (e.g. Divine Aspect on a Dormant sleeper) |
| **Data (Static)** | `AspectDefinition` (`AspectId`, `DisplayName`, `Description`, `AspectRank`, `AbilityDefinitions`, `FlawDefinition`, `Metadata`, `CanonProvenance`, `HasDuplicateAbilityIds()`); `AspectAbilityDefinition` (`AbilityId`, `DisplayName`, `Description`, `RequiredCharacterRank`, `BaseEssenceCost`, `Metadata`, `CanonProvenance`); `FlawDefinition` (`FlawId`, `DisplayName`, `Description`, `Metadata`, `CanonProvenance`) |
| **Runtime** | `AspectComponent`: `Awake()` / `ReconcileRuntimeState()` (deterministic reconciliation for Inspector-assigned/serialized Aspect definitions without polling or per-frame overhead, initializing instances locked and inactive with clean dynamic properties without consuming Essence or activating abilities; discards and rebuilds any serialized instance list containing stale runtime state such as unlocked, active, or non-empty dynamic properties), `SetAspectDefinition(newDef)` (validates duplicate IDs, rejects ambiguous definitions, rebuilds runtime `AspectAbilityInstance` collection atomically, deactivating active instances, commits structural state atomically before event dispatch, binds new Aspect's `FlawDefinition` to `activeFlawDefinition`, fires `OnAbilityDeactivated` for deactivated instances, `OnAspectChanged`, and `OnFlawChanged` if Flaw changed; protects all event dispatches with try-catch logging via `SSLog.Error`), `GetAspectDefinition()`, `HasAspect()`, `GetAspectRank()`, `GetFlawDefinition()` (pure query returning currently bound static `FlawDefinition`), `SetFlawDefinition(newFlawDef)` (sets or overrides active Flaw directly, fires `OnFlawChanged` on reference change, suppresses duplicates, standalone without ability transition guard), `HasFlaw()` (pure query `activeFlawDefinition != null`), `GetAbilityInstances()` (public read-only view encapsulated via `ReadOnlyCollection<AspectAbilityInstance>` to prevent external list mutations), `IsAbilityUnlocked(abilityId)`, `IsAbilityActive(abilityId)`, `FindAbilityInstance(abilityId)`, `UnlockAbility(abilityId)` (mutates `IsUnlocked` to true on transition, returns true when state actually changes), `DeactivateAbility(abilityId)` (clears `IsActive`, safely succeeds if ability exists and already inactive, guarded against reentrancy), `GetAttributeComponent()`, `CanActivateAbility(abilityId)` (pure eligibility prerequisite gate; verifies valid ID, unlocked instance, valid definition, non-negative finite cost, Character Rank prerequisite via owner's `ProgressionComponent`, and sufficient Essence via owner's `AttributeComponent` if cost > 0; completely side-effect free, does not consume Essence, does not activate ability), `ActivateAbility(abilityId)` (runtime activation state transition: verifies eligibility via `CanActivateAbility`, consumes configured `BaseEssenceCost` via `AttributeComponent.ConsumeEssence`, transitions `IsActive` to true, and broadcasts `OnAbilityActivated`; catches and logs subscriber exceptions via `SSLog.Error` so notification failures cannot abort state commit or cause successful operations to report false failures; idempotent safe success if already active without duplicate cost or event), `IsProcessingAbilityTransition` (guards against reentrant delegate callbacks during activation, deactivation, and aspect replacement transitions with try-finally safety), `SetAbilityDynamicProperty(abilityId, key, value)` (stores runtime dynamic property on ability instance; rejects invalid ID/key/instance; ordinal key matching; normalizes null value to empty string matching UE5 FString semantics), `GetAbilityDynamicProperty(abilityId, key, out value)` (pure side-effect-free retrieval of dynamic property; safely initializes out value to null; ordinal key matching). Suppresses duplicate assignment; purely event-driven, no tick/polling |
| **Events** | `OnAspectChanged(newAspectDef, oldAspectDef)`, `OnFlawChanged(newFlawDef, oldFlawDef)`, `OnAbilityUnlocked(instance)`, `OnAbilityActivated(instance)`, `OnAbilityDeactivated(instance)`. Dispatched only when corresponding state transitions actually occur. Dispatched strictly after atomic state commit; subscriber exceptions are caught and logged with `SSLog.Error` so listeners cannot prevent state commits or crash callers |
| **Deliberate boundaries** | Static definitions are strictly serialized content data, not runtime gameplay state. No general-purpose mutation setters exist on `AspectDefinition`, `AspectAbilityDefinition`, or `FlawDefinition`. All public collection properties on static definitions (`AbilityDefinitions`, `Metadata`) and runtime types (`GetAbilityInstances()`, `DynamicProperties`) are encapsulated with `ReadOnlyCollection` and `ReadOnlyDictionary` wrappers with source caching, preventing callers from recovering mutable backing collections via downcasting. `AspectDefinition.HasDuplicateAbilityIds()` detects duplicate IDs via exact ordinal comparison, and `SetAspectDefinition()` / `ReconcileRuntimeState()` reject invalid definitions rather than arbitrarily binding duplicates. Aspect replacement commits all structural state atomically prior to event dispatch; if any event subscriber throws, state is already fully committed to the new Aspect, the exception is logged via `SSLog.Error`, and the transition guard clears via `finally`. In `ActivateAbility()`, resource deduction via `AttributeComponent.ConsumeEssence()` precedes setting `instance.IsActive = true;`. Event subscriber exceptions across `ConsumeEssence()` (`OnEssenceChanged`) and `ActivateAbility()` (`OnAbilityActivated`) are caught and logged via `SSLog.Error` rather than crashing the caller or aborting state commit; state is committed independently of notification exceptions, and `ActivateAbility()` never reports failure or throws after successfully consuming Essence and activating the ability. In `ReconcileRuntimeState()`, if any serialized instance has stale unlocked, active, or dynamic property state, it is discarded and rebuilt to guarantee the clean initial contract (`IsUnlocked = false`, `IsActive = false`, clean properties). Reentrant callback mutations are deterministic: recursive calls to `ActivateAbility`, `DeactivateAbility`, or `SetAspectDefinition` are blocked by `_isProcessingAbilityTransition`, whereas `SetFlawDefinition` overrides from callbacks (such as `OnFlawChanged`) are permitted, and callbacks during Aspect replacement cannot corrupt old runtime instances because `abilityInstances` has already been atomically switched. `AspectComponent` owns runtime ability instances and manages transitions without mutating definition assets. When an Aspect is replaced or cleared, old runtime instances are discarded and new instances start locked and inactive. Aspect Rank is separate from Character Rank. `CanActivateAbility()` evaluates eligibility without activating abilities, consuming Essence, or mutating any state; strictly distinct from actual runtime activation. `ActivateAbility()` implements only generic runtime activation state transitions and atomic Essence consumption; it does NOT introduce combat actions, damage, hitboxes, VFX, audio, montages, animations, timers, cooldowns, or Flaw mechanics. Dynamic properties are runtime ability-instance state encapsulated as a read-only view `IReadOnlyDictionary<string, string>` on `AspectAbilityInstance.DynamicProperties`, backed by private storage with deterministic ordinal key matching, matching UE5's `FShadowSlaveAspectAbilityInstance::DynamicProperties` (`TMap<FName, FString>`). External callers cannot directly mutate dynamic properties; mutations must go through `AspectComponent.SetAbilityDynamicProperty` and queries through `AspectComponent.GetAbilityDynamicProperty`. They are strictly runtime state, not static definition data. When an Aspect is replaced or cleared, old runtime instances and their dynamic properties are discarded, and newly populated instances begin with clean state. Dynamic properties have no automatic gameplay meaning and do NOT mutate progression, attributes, or static definitions. `FlawDefinition` is static content data; `AspectComponent.activeFlawDefinition` is runtime binding state. An Aspect binds its Flaw definition automatically during `SetAspectDefinition()`, and the Flaw can also be directly set or overridden through `SetFlawDefinition()`. `HasFlaw()` and `GetFlawDefinition()` are pure queries. `OnFlawChanged` fires only when the active Flaw reference actually changes, and fires after `OnAspectChanged` when an Aspect change alters the Flaw. No Flaw gameplay mechanics or effects (penalties, triggers, conditions, status effects) are implemented in this phase. `ProgressionComponent` owns Character Rank, Soul Cores, and Progression Metadata; `AttributeComponent` owns Health, Stamina, and Essence; `AspectComponent` owns runtime Aspect binding, Flaw binding, and ability instance states |
| **Dependencies** | Reads `ShadowSlaveCharacterRank` in `AspectAbilityDefinition` for rank prerequisite data parameter. Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `AspectRank`, `AspectMetadataEntry`, `AspectAbilityInstance`, `AspectDefinition`, `AspectAbilityDefinition`, `FlawDefinition`, `AspectComponent` |

### AI
| | |
|--|--|
| **UE** | `AShadowSlaveAIController`, enemy character base, AI state types, anim instance |
| **Purpose** | Idle → Investigate → Chase → Attack → Recover flow for Nightmare creatures |
| **Unity** | Folder stub — prefer Unity AI Navigation / custom state machine |

### Progression
| | |
|--|--|
| **UE** | `UShadowSlaveProgressionComponent` (Rank, SoulCores, Metadata, `GetAttributeComponent`), `EShadowSlaveCharacterRank`, `FShadowSlaveSoulCoreState`, `TMap<FName, FString> ProgressionMetadata` |
| **Purpose** | Character progression rank, soul core capacity ownership, arbitrary metadata tracking for quest/story hooks, and non-owning attribute lookup |
| **Ownership** | Character Rank, Soul Core state, and Progression Metadata are owned by `ProgressionComponent`. `CharacterBase` requires and hosts `ProgressionComponent` and `AttributeComponent` on the same GameObject via `[RequireComponent]`. Resource pools (Health, Stamina, Essence) remain solely owned by `AttributeComponent` |
| **Data** | `ShadowSlaveCharacterRank` (`Unknown = 0`, defaults to `Unknown`); `SoulCoreState soulCoreState` (`currentSoulCores = 1`, `maximumSoulCores = 1`, implementing `ISerializationCallbackReceiver` and `Normalize()`); `List<ProgressionMetadataEntry> progressionMetadata`. Serialized on component as a list of entries because Unity does not natively serialize `Dictionary<string, string>` in the Inspector/Prefab/Scene workflow |
| **Runtime** | Rank API (`GetCharacterRank`, `HasKnownRank`, `SetCharacterRank`, `CanAdvanceRank`, `AdvanceRank`); Soul Core API (`GetSoulCoreCount`, `SetSoulCoreCount`, `GetMaxSoulCores`, `SetMaxSoulCores`, `AddSoulCore`, `RemoveSoulCore`, `GetSoulCoreState`); Metadata API (`SetProgressionMetadata`, `GetProgressionMetadata`, `RemoveProgressionMetadata`); Attribute integration helper (`GetAttributeComponent()`). Unity keys must be non-null, non-empty strings; matching is exact, case-sensitive ordinal. Null values are rejected (no-op) rather than converted to empty strings; setting an existing key with null does not overwrite. Setting an existing key with valid value overwrites its value; getting a missing key returns false (out null); removing a missing key returns false. `GetAttributeComponent()` is a [GAME ADAPTATION] lookup helper inherited from the UE5 reference architecture that locates the `AttributeComponent` on the same GameObject or returns null; it does not cache, create, or mutate attribute state. No metadata events. No `Update()` / no tick |
| **Events** | `OnCharacterRankChanged(newRank, oldRank)`, `OnSoulCoreCountChanged(newCount, oldCount)`, `OnMaxSoulCoresChanged(newMax, oldMax)`. Fires on actual changes; suppressed on same-value mutations. When decreasing maximum below current count, `OnMaxSoulCoresChanged` fires first, followed by `OnSoulCoreCountChanged`. Metadata and attribute lookup have no events |
| **Deliberate boundaries** | Character Rank (quality) and Soul Cores (quantity/class) are orthogonal progression axes. `SoulCoreState` serialized invariants (`currentSoulCores >= 0`, `maximumSoulCores >= 1`, `currentSoulCores <= maximumSoulCores`) are maintained across Unity serialization via `ISerializationCallbackReceiver`, `Normalize()`, and defensive property clamping on both `SoulCoreState` and `ProgressionComponent`. Metadata is an extensibility and state storage mechanism for quest/story hooks and does not itself establish canon. Decoupled from `AttributeComponent`: resource pools (Health, Stamina, Essence) remain strictly owned by `AttributeComponent`; Progression does not duplicate or own attribute state, and `GetAttributeComponent()` does not mutate attributes. Character Rank is independent of future Aspect Rank. Save, UI, and Aspect integrations are deferred |
| **Dependencies** | Resolves `AttributeComponent` on owning GameObject for lookup helper; no dependencies on Combat or Interaction. Same `ShadowSlave.Runtime` asmdef |
| **Unity** | `ShadowSlaveCharacterRank`, `SoulCoreState`, `ProgressionMetadataEntry`, `ProgressionComponent` |

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
