# Equipment and Memories

## Canon Findings

* **Mundane Physical Equipment vs. Memories**: In *Shadow Slave*, there is a fundamental ontological division between mundane physical items and magical Memories:
  * **Mundane Equipment**: Physical artifacts, clothing, environmental suits, firearms, and knives manufactured in the waking world or crafted by mundane means. They occupy physical space, have mass, do not require soul essence or the Nightmare Spell to use, and exist permanently in physical reality until destroyed or dropped.
  * **Memories**: Metaphysical constructs granted by the Nightmare Spell or woven from soul essence (Weaver's lineage). Memories reside inside an Awakened being's Soul Sea as spheres of light, are summoned into physical reality through conscious intent and soul essence, and dissolve back into the Soul Sea when dismissed.
* **Summoning and Dismissal**: Equipping a Memory manifests it physically from the Soul Sea. Dismissing it removes it from physical reality back into the spiritual core. Conversely, mundane equipment must be manually worn, carried, or stored in bags and physical inventories.
* **Absence of Rigid RPG Slots**: In novel canon, characters do not have arbitrary rigid UI "slots" (e.g. Weapon, Armor, Charm, Ring, Relic). An Awakened character can wear mundane clothes beneath a Memory mantle, hold a sword in each hand, wear a mask, or carry amulets. The novel restricts usage primarily through active essence maintenance, cognitive load, and physical practicality.
* **Passive Enchantments and Attributes**: Both high-tech mundane gear and magical Memories grant defensive resilience, offensive sharpness, or metaphysical enhancements. In novel terms, these are described as enchantments or structural qualities (e.g., `[Undying]`, `[Featherlight]`, `[Living Stone]`, `[Unbroken]`).
* **Modifier Lifetime**: When an item or Memory is unequipped, dropped, broken, or dismissed, its protective and enhancing effects cease immediately.
* **Destruction and Depletion**: When mundane equipment breaks, it remains physical debris. When a Memory breaks or is fed/consumed, it shatters in the Soul Sea. In both cases, any equipped status and attribute benefits are terminated.

## Chapter References

* **Chapters 1–15 ("First Nightmare")**: Sunny survives using a mundane kitchen knife and scavenged physical items before awakening.
* **Chapter 20 ("First Nightmare Conclusion")**: Sunny receives his first Dormant Memories: *[Puppeteer's Shroud]* (Armor) and *[Silver Bell]* (Tool/Charm), demonstrating summoning from the Soul Sea.
* **Chapters 98–100 ("Centurion")**: Sunny acquires the *[Midnight Shard]* (Awakened sword) from the Carapace Centurion; demonstrates enchantment effects ([Unbroken]) and durability.
* **Chapters 168–170 ("Cathedral of the Stargazer")**: Sunny finds *[Weaver's Mask]* (Divine tool/relic), demonstrating head/face equipment that overlays existing attire.
* **Chapters 270–350 ("Dark City & Spire")**: Sunny alternates between mundane leather/cloth garments and the Puppeteer's Shroud; demonstrates physical layering and weapon switches.
* **Chapter 352 ("Return from Forgotten Shore")**: Sunny acquires *[Mantle of the Underworld]*, illustrating high-tier armor that grants substantial defensive and structural attributes (*[Living Stone]*, *[Stalwart]*).
* **Chapters 800+ ("Antarctica Campaign")**: Extensive interaction between mundane military equipment (cold-weather tactical gear, comms headsets, convoy vehicles) and Awakened Memories, confirming that mundane physical equipment remains relevant alongside magical Memories.

## Canon Evidence Ledger

| Claim | Evidence/Source | Classification | Confidence | Implementation Decision |
| :--- | :--- | :--- | :--- | :--- |
| Mundane equipment exists, has physical weight, and requires no essence to use | Ch 1–15, Ch 800+ (tactical military gear, knives) | [CANON] | High | Implement via `InventoryComponent` and `EquipmentComponent`. |
| Memories reside in the Soul Sea and are summoned/dismissed at will | Ch 15, Ch 20, Ch 95 (Soul Sea light spheres) | [CANON] | High | Defer `MemoryComponent` integration; preserve `EquipmentSourceType.Memory` for future phase. |
| Equipment slots are not rigid RPG constraints in lore | Novel text throughout | [INFERRED] | High | Adopt `ShadowSlaveEquipmentSlot` as a [GAME ADAPTATION] for structured ARPG gameplay. |
| Equipment grants passive defensive and capability benefits | Ch 20 (*[Puppeteer's Shroud]*), Ch 352 (*[Mantle of the Underworld]*) | [CANON] | High | Map equipment bonuses to `AttributeModifier` on `AttributeComponent`. |
| Unequipping, dropping, or unequipping gear immediately removes its bonuses | Novel text throughout | [CANON] | High | Implement atomic removal of source-specific attribute modifiers upon unequip. |
| Items in inventory can exist as single units or stacks; equipping references an instance | Ch 1–15, Ch 800+ | [INFERRED] | Medium | Implement partial stack retention: instance ID binds to slot; remaining quantity stays in inventory. |
| If an equipped item is discarded, consumed, or cleared from inventory, it ceases being equipped | Ch 350, general novel continuity | [CANON] | High | Implement automatic unequip reconciliation when item is removed or inventory cleared. |

## Confirmed Mechanics

* `[CANON]` Physical mundane equipment exists independently of the Nightmare Spell and Memories.
* `[CANON]` Equipment grants passive defensive, structural, and capability enhancements while actively worn or held.
* `[CANON]` Equipment bonuses terminate immediately when the item is removed, dropped, or destroyed.
* `[CANON]` Destruction or loss of an item invalidates its equipped status.

## Inferred Mechanics

* `[INFERRED]` An equipped item descriptor must reference its authoritative inventory entry by a unique stable identifier rather than duplicating state.
* `[INFERRED]` If a stackable item is equipped, the equipped reference represents a specific instance; removing other units from the same stack does not unequip the item as long as the instance remains valid.

## Game-Design Adaptations

* `[GAME ADAPTATION]` `ShadowSlaveEquipmentSlot` (`Weapon`, `Armor`, `Charm`, `Ring`, `Relic`): Adapted from the UE5 reference and standard action-RPG architecture to provide discrete equip slots for player and enemy characters.
* `[GAME ADAPTATION]` Attribute modifiers: Novel enchantments and physical qualities are represented as `AttributeModifier` instances (`Flat` or `Percent`) applied to `AttributeComponent`.
* `[GAME ADAPTATION]` Equipment ownership hierarchy: `ItemDefinition` / `ItemInstance` → `InventoryComponent` → `EquipmentComponent` → `AttributeComponent`. `InventoryComponent` owns item quantity and lifecycle; `EquipmentComponent` owns equipped slot assignments; `AttributeComponent` owns effective attribute values.

## Unknown / Unresolved

* `[UNKNOWN]` Exact maximum limit on concurrent mundane rings, charms, or layered garments in novel canon (bounded by physical geometry rather than arbitrary system limits).
* `[UNKNOWN]` Future interaction between simultaneously equipped mundane armor and summoned Memory armor (deferred to Memory Foundation).

## UE5 Architecture Comparison

* The UE5 reference implementation (`UShadowSlaveEquipmentComponent`) supports both `EShadowSlaveEquipmentSourceType::Item` and `EShadowSlaveEquipmentSourceType::Memory`.
* For Unity Phase 1, we implement the complete inventory-backed item equipment pipeline (`EquipmentSourceType.Item`), slot queries, companion component event binding (`InventoryComponent`, `AttributeComponent`), and attribute modifier lifecycle.
* Memory integration (`EquipmentSourceType.Memory`) is cleanly deferred to the upcoming Memory Foundation phase, preserving the enum and architectural extensibility without premature coupling.
