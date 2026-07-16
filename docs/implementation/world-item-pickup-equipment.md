# World item pickup and resident equipment

This slice closes the resident item loop without adding Unity-owned inventory state.

- `Alt + left click` on an available generic world item creates a typed pickup Job for the selected living resident.
- The Job reserves the exact source quantity, item key, source position and resident through the shared Inventory and Jobs owners.
- Completion moves the reserved quantity into `ItemLocation.InAgent`; cancellation releases both reservation systems.
- BuildingBox world items keep their dedicated placement and pickup behavior.
- Resident inventory presentation includes carried and equipped stacks. Equipped tools are marked separately and can be put down into the resident cell.
- World item and resident inventory projections are rebuilt from authoritative Inventory snapshots.

The stable save codec type for generic pickup Jobs is `job.world_item_pickup.v1`.
