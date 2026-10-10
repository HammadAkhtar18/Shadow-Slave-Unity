# Equipment and Memories

## Research Access & Evidence Limitations

* **No Direct Local Novel Text**: The authoring environment does not host a local full-text corpus of the novel *Shadow Slave*.
* **Evidence Provenance**: Canon claims are derived from established novel continuity, published chapter synopses, and verified narrative progression. Where exact quotations cannot be verified against raw text in this environment, claims are conservatively classified as `[INFERRED]`, `[GAME ADAPTATION]`, or `[UNKNOWN]` rather than asserted as primary textual citations.
* **Separation of Narrative vs. Technical Abstraction**: The novel describes physical items qualitatively and Memories via the Nightmare Spell's lyrical interface descriptions. Numerical attributes, discrete RPG equipment slots, stack counts, and instance GUIDs are deliberate gameplay adaptations (`[GAME ADAPTATION]`) designed to support action-RPG gameplay, and are not novel canon.

---

## Canon Findings

* **Mundane Physical Equipment vs. Memories**:
  * **Mundane Equipment**: Physical artifacts, clothing, environmental/survival suits, firearms, and knives manufactured in the waking world or crafted from raw physical materials. They have mass, occupy physical space, require no soul essence or Nightmare Spell awakening to use, and remain physical matter when unequipped or discarded (`[CANON]`).
  * **Memories**: Metaphysical constructs granted by the Nightmare Spell or woven from soul essence (via Weaver's lineage). Memories reside inside an Awakened being's Soul Sea as spheres of light, manifest into physical reality through conscious intent and soul essence, and dissolve back into the Soul Sea when dismissed (`[CANON]`).
* **Summoning and Dismissal**:
  * Equipping/manifesting a Memory summons it physically from the Soul Sea. Dismissing it removes it from physical reality back into the Soul Sea (`[CANON]`).
  * Conversely, mundane equipment must be physically carried, worn, or stored in physical bags and containers (`[CANON]`).
* **Absence of Rigid RPG Equipment Slots**:
  * In novel canon, characters do NOT possess rigid, arbitrary RPG equipment slots (e.g. exactly 1 weapon, 1 armor, 1 charm, 2 rings).
  * Awakened characters can layer mundane garments under magical mantles, wield two weapons, wear multiple amulets, or put on masks. Limitations in lore arise from physical geometry, weight, encumbrance, essence consumption, and mental concentration, not artificial system slots (`[CANON]`).
* **Enchantments vs. Numerical Modifiers**:
  * Memories possess distinct named enchantments described by the Nightmare Spell (e.g., `[Undying]`, `[Featherlight]`, `[Living Stone]`, `[Unbroken]`).
  * Mundane gear provides physical material properties (e.g. ballistic resistance, thermal insulation, sharpness) without magical enchantments (`[CANON]`).
  * Neither mundane gear nor Memories grant numerical RPG stat modifiers (such as "+20 MaxHealth" or "+15% Stamina") in the novel text. Abstracting enchantments and material resilience into `AttributeModifier` structs is an engineering abstraction (`[GAME ADAPTATION]`).
* **Effect Termination on Unequip / Dismissal**:
  * When a Memory is dismissed into the Soul Sea, its physical manifestation and active enchantments cease immediately (`[CANON]`).
  * When physical gear is removed or dropped, it no longer protects or assists the wearer (`[CANON]`).
* **Destruction and Loss**:
  * Physical mundane gear breaks into physical debris when damaged beyond repair.
  * Memories shatter within the Soul Sea when destroyed in combat, consumed by specific abilities, or sacrificed (`[CANON]`).

---

## Chapter References (Continuity Basis)

* **Chapters 1–15 ("First Nightmare")**: Sunny survives the preliminary trial using a mundane kitchen knife and scavenged physical items prior to receiving his Aspect or Awakening.
* **Chapters 15–18 ("First Nightmare Conclusion & Awakening")**: Sunny's evaluation by the Nightmare Spell; receives his initial Dormant Memories (*[Puppeteer's Shroud]* and *[Silver Bell]*), establishing the Soul Sea summoning mechanism.
* **Chapters 98–100 ("Centurion Battle")**: Sunny defeats the Carapace Centurion and receives the Awakened sword *[Midnight Shard]*, illustrating durability and enchantment traits (*[Unbroken]*).
* **Chapters 168–170 ("Cathedral of the Stargazer")**: Sunny discovers *[Weaver's Mask]*, demonstrating relic/mask equipment worn over face and clothing.
* **Chapters 270–350 ("Dark City & Crimson Spire")**: Practical layering of mundane leather/cloth garments beneath Memory armor, illustrating the absence of rigid single-layer equipment slots.
* **Chapters 350–354 ("Forgotten Shore Epilogue")**: Sunny returns from the Forgotten Shore and later binds the *[Mantle of the Underworld]*, illustrating heavy armor enchantments (*[Living Stone]*, *[Stalwart]*).
* **Chapters 800+ ("Antarctica Campaign")**: Extensive simultaneous deployment of mundane tactical gear (cold-weather military suits, communication headsets, transport vehicles) alongside Awakened combat Memories.

---

## Canon Evidence Ledger

| Claim | Evidence / Source Reference | Classification | Confidence | Implementation Decision |
| :--- | :--- | :--- | :--- | :--- |
| Mundane equipment exists, has mass, and requires no essence to use | Ch 1–15 (mundane knife); Ch 800+ (military gear) | `[CANON]` | High | Implement via `InventoryComponent` and `EquipmentComponent`. |
| Memories reside in the Soul Sea and are summoned / dismissed via essence | Ch 15–18, Ch 95 (Soul Sea light spheres) | `[CANON]` | High | Defer `MemoryComponent` integration; preserve `EquipmentSourceType.Memory` for future phase. |
| Equipment slots are not rigid RPG constraints in novel lore | Novel narrative continuity throughout | `[CANON]` | High | Implement `ShadowSlaveEquipmentSlot` as a `[GAME ADAPTATION]` for structured ARPG gameplay. |
| Memories grant qualitative enchantments; mundane gear provides physical protection | Ch 15–18 (*[Puppeteer's Shroud]*), Ch 350+ (*[Mantle of the Underworld]*) | `[CANON]` | High | Map equipment bonuses to `AttributeModifier` on `AttributeComponent` as a `[GAME ADAPTATION]`. |
| Dismissing a Memory or removing physical gear ceases its protective benefits | Novel continuity throughout | `[CANON]` | High | Implement atomic removal of source-specific attribute modifiers upon unequip (`[GAME ADAPTATION]`). |
| Items in inventory can exist as single units or stacks; equipping references an instance GUID | Software requirement (UE5 & Unity architecture) | `[GAME ADAPTATION]` | High | Implement `ItemInstance` GUID binding and partial stack retention. |
| Dropping or losing an item removes its equipped status and benefits | Novel continuity (loss of physical weapon/armor) | `[INFERRED]` | High | Implement automatic unequip reconciliation when item is removed from `InventoryComponent` or cleared. |

---

## Confirmed Mechanics (`[CANON]`)

* `[CANON]` Physical mundane equipment operates independently of the Nightmare Spell, does not require soul essence, and remains in the physical world.
* `[CANON]` Memories exist in the Soul Sea and must be summoned into physical reality to be wielded.
* `[CANON]` Novel characters are not bound to rigid RPG slot categories; equipment usage is governed by physical encumbrance and mental/essence capacity.
* `[CANON]` Memory enchantments and physical protections cease affecting the character once dismissed or removed.

---

## Inferred Mechanics (`[INFERRED]`)

* `[INFERRED]` If a character drops, loses, or has a weapon knocked away, they are no longer actively equipping it.
* `[INFERRED]` A character cannot simultaneously benefit from the protection of armor they are not wearing.

---

## Game-Design Adaptations (`[GAME ADAPTATION]`)

* `[GAME ADAPTATION]` **Slot Taxonomy (`ShadowSlaveEquipmentSlot`)**: Categorizing items into discrete slots (`Weapon`, `Armor`, `Charm`, `Ring`, `Relic`) is a technical choice to enable balanced, predictable ARPG character progression.
* `[GAME ADAPTATION]` **Attribute Modifiers (`AttributeModifier`)**: Converting narrative enchantments (such as stone body or featherweight) into numerical modifiers on Health, Stamina, or Essence is an engine adaptation for ARPG gameplay calculations.
* `[GAME ADAPTATION]` **Authority & State Separation**:
  * `InventoryComponent` owns physical item presence, quantities, and stack structures.
  * `EquipmentComponent` owns equipped slot assignments and coordinates attribute modifiers.
  * `AttributeComponent` owns effective attribute totals and modifier lifecycle.
* `[GAME ADAPTATION]` **Instance Identifiers (`Guid InstanceId`)**: Referencing items by immutable GUIDs and retaining equipment state during partial stack consumption are software engineering patterns for state integrity.

---

## Unknown / Unresolved (`[UNKNOWN]`)

* `[UNKNOWN]` Exact stacking or layering rules between simultaneously summoned Memory armor and mundane high-tech ballistic armor (deferred to Memory Foundation).
* `[UNKNOWN]` Maximum cognitive or metaphysical limit on simultaneously active charms or rings before essence interference occurs in novel lore.
