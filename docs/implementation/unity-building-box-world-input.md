# Unity BuildingBox world input

This slice connects a packed BuildingBox world stack to context input without making Unity the owner of item, job, building, or placement state.

## Input behavior

After UI shielding, the existing `ContextInputRouter` remains authoritative for pointer priority:

- normal left click on an available world BuildingBox starts local placement mode;
- `Alt + left click` with a selected living resident creates one pickup command;
- while placement is active, left click on valid ground confirms the plan;
- invalid ground keeps the preview and reports the typed reason code;
- right click cancels placement without reserving or moving the box.

One pointer event still produces at most one Application command.

## World item identity

`WorldItemViewModel` carries a typed `WorldItemInteractionKind`. Only the dedicated BuildingBox inventory projection emits `BuildingBox`; terrain resources retain `None` and stay on Unity's ignore-raycast layer.

`DigWorldItemVisual` stores the immutable model used by raycast routing. Unity does not infer a box from its display name, material, primitive type, or item-id prefix.

## Pickup ownership and lifecycle

`BuildingBoxPickupJobDefinition` uses the common Job lifecycle:

1. create and make the job available;
2. reserve the exact Inventory quantity;
3. claim the selected resident and reserve Job, Agent, Item, and Position keys;
4. navigate to the authoritative source cell;
5. advance through `TravelToTarget` and `AcquireItem`;
6. move the reserved quantity-one stack to `ItemLocation.InAgent`;
7. complete the Job and release all common reservations.

Cancellation releases both Inventory and Job reservations and leaves the box in its original world location. A competing pickup cannot reserve the same box.

The stable save codec type is `job.building_box_pickup.v1` and stores the stack id plus source cell.

## Placement ownership

Placement mode stores only `BuildingBoxPlacementModeState` and the latest immutable `BuildingBoxGhostViewModel` in Presentation. Preview delegates to `BuildingBoxPlacementPresenter`, which validates the source box and calls the existing `BuildingPlacementValidator` over World, Buildings, and reachable-cell snapshots.

Confirmation delegates to `ConfirmBuildingBoxPlacementHandler`. The handler repeats validation, reserves the exact source box, creates the authoritative plan and assembly Job, and publishes owner events. The world item remains visible but becomes unavailable while reserved.

## Accessibility and rebuildability

The ghost is not color-only:

- valid footprint cells are low and flat;
- invalid cells are taller and rotated;
- the HUD reports `valid` or the exact reason code;
- the work position uses a distinct narrow marker.

Deleting and rebuilding item or ghost GameObjects does not change Inventory, Jobs, Buildings, or World.

## Remaining work

The new authoritative placement creates a BuildingBox assembly Job, but this slice does not yet execute the complete pickup/delivery/assembly sequence in the Unity simulation adapter. Resident inventory UI also does not yet expose its carried BuildingBox as a clickable slot, although the shared placement session accepts a source stack in `AgentInventory`.
