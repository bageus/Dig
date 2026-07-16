# Resident inventory actions

The selected-resident HUD reads the shared building inventory snapshot and exposes actions from typed slot capabilities.

- A free BuildingBox can start placement or be put down at the resident cell.
- A free tool can be equipped or put down.
- A generic free stack can be put down.
- Any stack with an active quantity reservation remains visible but cannot be moved or used.

The HUD routes all actions through `ContextInputRouter` on the `ResidentInventory` surface. The router emits `UseInventoryItem` or `DropInventoryStack` commands with the resident, stack, and resident cell facts.

Application handlers re-read authoritative inventory state. They reject stale ownership and reservations before moving the full stack to `ItemLocation.InWorld` or equipping a tool through `InventoryState.EquipTool`.

World item projection scopes BuildingBox interaction to the BuildingBox item id. A tool placed in the world remains a generic non-interactive item rather than inheriting BuildingBox input behavior.
