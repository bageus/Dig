# Settlement management menu

## Scope

The Unity HUD exposes a top-left hamburger menu button. A left click toggles a dropdown with
`Dwarfs`, `Items`, and `Buildings`. Selecting an entry closes the dropdown and opens
one centered management overlay. The overlay close button is placed in its upper-left
corner. Menu, dropdown, and overlay rectangles participate in HUD hit testing, so a UI
click cannot reach world controls.

## Dwarfs

The dwarf overlay reads the existing resident roster, Society snapshot, skill set, and
resident inventory layout. It does not own or copy simulation state.

- `Standard`: name, sex, Health, Alertness, Hunger, Mood, age, and schedule state.
- `Production`: total work skill and the seven production skills.
- `Fight`: total combat skill and the five combat skills.
- `Family`: partner, father, mother, and children resolved to resident names.
- `Inventory`: all weapon, main, and cargo slots laid out horizontally per resident.

Age is derived from the authoritative birth tick. The current demo founders enter the
runtime at age 25. Family cells remain `-` until Society contains the corresponding
relationship.

## Items

The item overlay reads one `InventorySnapshot`. "Current visible zone" means the
currently loaded runtime settlement; unloaded worlds or save slots are not included.
Totals include world, resident, equipped, storage, and building locations. Every active
building receives a column, and storage/building quantities are grouped by owner ID.

The tabs are `Materials`, `Weapons`, `Food`, `Potions`, and `Tools`. The Materials tab
always keeps the requested stable rows, including zero quantities, so missing demo
content is explicit. Other items are classified by stable item IDs, content categories,
tool metadata, and inventory-expansion metadata.

## Buildings

The buildings overlay lists every active building with name, definition, lifecycle
status, origin, durability, and construction progress. It reads the existing building
view models and does not change building state.

## Verification

- presentation unit tests cover item grouping, totals, building/storage columns,
  requested zero-value material rows, age, family name resolution, skill totals, and
  resident inventory binding;
- Play Mode coverage verifies menu toggle behavior and opening an overlay;
- the Unity source contract prevents removal of the three entries, ten requested tabs,
  Society projection, or authoritative inventory projection.
