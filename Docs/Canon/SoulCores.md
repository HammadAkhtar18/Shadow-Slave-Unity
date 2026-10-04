# Soul Cores

## Canon Findings

* **Soul Center**: A Soul Core is the metaphysical energy center located within the Soul Sea of Awakened beings and Nightmare Creatures, functioning as the vessel that accumulates and produces Soul Essence.
* **Orthogonal Progression Axes**: Power in *Shadow Slave* is measured along two distinct axes: **Rank** (spiritual quality/tier of the soul: Dormant through Divine) and **Class** (the physical quantity of soul cores: Beast [1] through Titan [7]).
* **Mundane Humans**: Non-Awakened humans have zero (0) soul cores and cannot store or manipulate soul essence, nor can they activate Memories.
* **Standard Awakened**: Normal humans cultivate and possess exactly one (1) soul core throughout all ranks. Their maximum core capacity is 1.
* **Multi-Core Entities**: Only Nightmare Creatures and rare humans possessing a Divine Aspect (Sunless, Nephilm, Mordret) can form multiple soul cores, up to a maximum of seven (7), corresponding to the Titan class.
* **Core Saturation and Birth**: Forming an additional core requires saturating the existing soul core with essence/fragments, followed by the painful emergence of a new core.
* **Essence Capacity**: Each soul core acts as a distinct essence reservoir; multiple cores multiply total essence capacity and regeneration throughput proportionally.
* **Destruction and Loss**:
  * Destruction of a soul core causes extreme soul trauma.
  * For single-core humans, core destruction shatters the soul, resulting in death or rendering the victim a "Hollow" (a living body devoid of a soul).
  * Multi-core entities can survive the loss, detonation, or sacrifice of a core (e.g., Nephis in the Third Nightmare, Mordret shattering cores), though it inflicts devastating soul damage and reduces class/essence capacity.
* **Core Restoration**: Multi-core beings can reconstruct lost cores by harvesting soul/shadow fragments. Conversely, single-core souls destroyed into Hollows cannot be restored through standard means.
* **Universal Ceiling**: Seven (7) soul cores (Titan class) is the established upper bound in canon.

## Chapter References

* **Chapters 15–16 ("First Nightmare Conclusion")**: The Soul Sea and initial soul core formation upon surviving the First Nightmare; Awakening status.
* **Chapter 95 ("Soul Sea")**: Examination of Sunny's Soul Sea, dormant core, and shadow fragments.
* **Chapter 352 ("Monster Core")**: Sunny completes saturation of his first core and forms a second core, advancing in class to Monster.
* **Chapter 526 ("Demon")**: Advancement to three cores (Demon class) during the Chained Isles arc.
* **Chapter 702 ("The Devil You Know")**: Emergence of Sunny's fourth core, attaining Devil class.
* **Chapter 863 ("Fractured")**: Inspection of Master Jet's fractured soul core, demonstrating the catastrophic mechanics of cracked/damaged core integrity.
* **Chapter 1090 ("Tyrant")**: Sunny reaches five soul cores during the Antarctica campaign, attaining Tyrant class.
* **Chapter 1270 ("Terror")**: Attainment of six cores (Terror class) in East Antarctica.
* **Chapter 1517 ("Supernova")**: Nephis detonates an active core against Soul Stealer's puppets and forges her seventh core, reaching Titan class; demonstrates core sacrifice and restoration mechanics.
* **Chapters 1585+ ("Verge / Tomb of Ariel")**: Sunny achieves his seventh core (Titan class), confirming the universal 7-core ceiling for Divine Aspect holders.

## Confirmed Mechanics

* `[CANON]` Core count directly denotes creature Class: 1 = Beast, 2 = Monster, 3 = Demon, 4 = Devil, 5 = Tyrant, 6 = Terror, 7 = Titan.
* `[CANON]` Standard human Awakened are capped at 1 soul core across all ranks.
* `[CANON]` Divine Aspect holders and Nightmare Creatures can form up to 7 soul cores.
* `[CANON]` Mundane humans have 0 soul cores; Hollows have destroyed/vacant soul cores.
* `[CANON]` Character Rank and Soul Core count (Class) operate as completely independent progression parameters.
* `[CANON]` Soul essence capacity scales directly with soul core count.
* `[CANON]` Cores can be destroyed, sacrificed, or lost, with multi-core entities surviving reduced core counts.

## Inferred Mechanics

* `[INFERRED]` A character with `CurrentSoulCores = 0` accurately models a non-awakened mundane human or a hollow entity.
* `[INFERRED]` Maximum soul core capacity is a property of the soul's structural capacity (1 for standard humans, up to 7 for divine aspects or nightmare beasts).
* `[INFERRED]` `CurrentSoulCores` is bounded by `0 <= CurrentSoulCores <= MaximumSoulCores`.

## Unknown / Unresolved

* `[UNKNOWN]` Whether any hypothetical entity could ever exceed 7 cores beyond the Titan class (no canon example exists).
* `[UNKNOWN]` Whether non-divine humans can ever temporarily host a secondary core via external relics or sorcery.

## UE5 Comparison

* **State Model**: UE5 defines `FShadowSlaveSoulCoreState` with `CurrentSoulCores` (default: 1) and `MaximumSoulCores` (default: 1).
* **Clamping Policies**:
  * `SetSoulCoreCount`: Clamps to `[0, MaximumSoulCores]`.
  * `SetMaxSoulCores`: Clamps to `[1, infinity)`. Clamping down automatically clamps down `CurrentSoulCores`.
  * `AddSoulCore`: Rejects `Count <= 0`; adds to current and clamps to `MaximumSoulCores`.
  * `RemoveSoulCore`: Rejects `Count <= 0`; subtracts from current and clamps to 0.
* **Event Semantics**:
  * `OnSoulCoreCountChanged(NewCount, OldCount)`: Fires on actual core count changes; ignores same-value mutations.
  * `OnMaxSoulCoresChanged(NewMax, OldMax)`: Fires on maximum changes; ignores same-value mutations.
  * When `SetMaxSoulCores` reduces the maximum below current cores, `OnMaxSoulCoresChanged` fires first, followed immediately by `OnSoulCoreCountChanged`.
* **Component Ownership**: Co-located in `UShadowSlaveProgressionComponent` alongside `CharacterRank`.
* **Resource Decoupling**: Soul core count in UE5 does not directly alter AttributeComponent pools; attribute pools remain strictly owned by `UShadowSlaveAttributeComponent`.

## Unity Design Decisions

* **Unified Progression Ownership**: `ProgressionComponent` retains ownership of both `CharacterRank` and `SoulCoreState`, mirroring UE5 and preserving the canonical relationship between Rank (quality) and Class/Cores (quantity).
* **Attribute Decoupling**: In accordance with UE5 architecture and canon boundaries, `ProgressionComponent` does not duplicate or alter `AttributeComponent` resource pools. Future essence multipliers can read `GetSoulCoreCount()` without tight coupling.
* **API Completeness**: Unity exposes both standard property accessors and UE5-matching methods:
  * `GetSoulCoreCount()`, `CurrentSoulCores`
  * `GetMaximumSoulCores()`, `GetMaxSoulCores()`, `MaximumSoulCores`
  * `SetSoulCoreCount(int)`
  * `SetMaximumSoulCores(int)`, `SetMaxSoulCores(int)`
  * `AddSoulCores(int)`, `AddSoulCore(int)`
  * `RemoveSoulCores(int)`, `RemoveSoulCore(int)`
  * `HasMultipleCores()`, `IsMaxCoresReached()`
  * `GetSoulCoreState()` returning `SoulCoreState` struct
* **Event Matching**: Unity fires `OnSoulCoreCountChanged(newCount, oldCount)` and `OnMaxSoulCoresChanged(newMax, oldMax)` matching UE5 ordering and zero-event same-value suppression.
* **Invariants**:
  * `0 <= CurrentSoulCores <= MaximumSoulCores`
  * `1 <= MaximumSoulCores`
