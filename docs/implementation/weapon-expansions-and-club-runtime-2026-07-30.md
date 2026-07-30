# Weapon expansions and club runtime — 2026-07-30

Status: `IMPLEMENTED` after merge of the linked PR; licensed Unity Play Mode evidence remains required for `VERIFIED`.

Implementation PR: [#522](https://github.com/bageus/Dig/pull/522).

Authoritative specifications:

- [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md);
- [`../design/content/weapons-and-shields.md`](../design/content/weapons-and-shields.md).

Tracking: #64, #68, #69, #70, #71, #511.

## Implementation map

- `CombatEquipmentContent` registers `weapon.club` as a stable non-stackable Weapon item.
- demo inventory starts with tools in Main and spawns sheath, weapon harness and club as separate world items on the surface.
- shared pickup slot claims keep Weapon-compatible items in the active Weapon compartment before Main/Cargo.
- sheath and harness use existing Main-only tier priority and quantity-safe Weapon spill semantics.
- `DigBasketVisualPolicy` equipment partial owns distinct procedural world/carry/placement visuals for sheath, harness and club; no generic magenta fallback is used for these IDs.

## Regression boundary

Automated coverage includes content identity/category validation, sheath/harness pickup, active-tier switching, club Weapon-slot claims, quantity-safe spill, demo surface registration, procedural visuals and checked-in Unity Play Mode geometry/layout assertions.

Repository CI requires Release build, the complete .NET suite, headless smoke, standard deterministic soak, large-settlement deterministic soak and Stage 2 source exports. Actual licensed Unity Test Runner execution remains owned by #511; a green activation-blocked workflow does not promote this implementation to `VERIFIED`.
