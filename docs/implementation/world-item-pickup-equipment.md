# World item pickup and resident equipment

This slice closes the resident item loop without adding Unity-owned inventory state.

- Ordinary `left click` on an available generic/material/tool/weapon/food world item creates a typed pickup Job for the selected living resident. BuildingBox alone uses `Alt + left click` for pickup; food uses `Alt + left click` for pickup-then-use.
- The Job reserves the exact source quantity, item key, source position and resident through the shared Inventory and Jobs owners.
- Completion moves the reserved quantity into `ItemLocation.InAgent`; cancellation releases both reservation systems.
- Classification comes from `ItemDefinition.ItemInteractionProfile`; Unity does not list item ids or infer behavior from id prefixes. BuildingBox keeps its profile-defined selection/placement/pickup behavior.
- Resident inventory presentation includes carried and equipped stacks. Equipped tools are marked separately and can be put down into the resident cell.
- World item and resident inventory projections are rebuilt from authoritative Inventory snapshots.

The stable save codec type for generic pickup Jobs is `job.world_item_pickup.v1`.
