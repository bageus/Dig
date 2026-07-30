# Weapon expansions and club runtime — 2026-07-30

Status: `IMPLEMENTED` after merge of the linked PR; licensed Unity Play Mode evidence remains required for `VERIFIED`.

Authoritative specifications:

- [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md);
- [`../design/content/weapons-and-shields.md`](../design/content/weapons-and-shields.md).

Tracking: #64, #68, #69, #70, #71, #511.

## Implementation map

- `CombatEquipmentContent` registers `weapon.club` as a stable non-stackable Weapon item.
- demo inventory starts with tools in Main and spawns sheath, weapon harness and club as separate world items on the surface.
- shared pickup slot claims keep Weapon-compatible items in the active Weapon compartment before Main/Cargo.
- sheath and harness use existing Main-only tier priority and quantity-safe Weapon spill semantics.
- `DigBasketVisualPolicy` equipment partial owns distinct procedural world/carry visuals for sheath, harness and club; no generic magenta fallback is used for these IDs.

## Regression boundary

Automated coverage includes content identity/category validation, sheath/harness pickup, active-tier switching, club Weapon-slot claims, quantity-safe spill, demo surface registration, procedural visuals and checked-in Unity Play Mode geometry/layout assertions. Actual licensed Unity execution remains owned by #511.
