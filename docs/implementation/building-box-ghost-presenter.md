# BuildingBox ghost presenter

## Purpose

The BuildingBox ghost is a Presentation projection of the same authoritative placement rules used by plan confirmation. Unity must not decide whether a footprint is valid from collider colors or duplicate geometry checks.

`BuildingBoxPlacementPresenter` consumes:

- the selected `ItemStackSnapshot` and its `ItemDefinition`;
- the box-enabled `BuildingDefinition`;
- origin and orientation;
- immutable `WorldSnapshot`;
- occupied building cells;
- reachable logical cells.

It returns one immutable `BuildingBoxGhostViewModel` containing the rotated footprint, selected work position, validity and a stable reason code.

## Source validation

Before terrain placement is evaluated, the presenter verifies:

- the definition has a `BuildingBoxPolicy`;
- the source stack and item definition exist;
- item ids match the policy;
- maximum stack size and current quantity are exactly one;
- the box has one available quantity and is not already reserved.

Source failures use typed reason codes such as:

- `building_box.preview.source_missing`;
- `building_box.preview.item_mismatch`;
- `building_box.preview.box_not_single`;
- `building_box.preview.box_unavailable`.

## Placement validation

Terrain, exploration, bounds, occupied footprint and reachable work-position checks are delegated to `BuildingPlacementValidator`. Invalid ghosts preserve its Domain error code, including `buildings.placement.occupied` and `buildings.placement.no_reachable_work_position`.

The preview remains non-authoritative. `ConfirmBuildingBoxPlacementHandler` repeats the same validation when the player confirms, preventing stale previews from creating a plan after the world changes.

## Rotation and confirmation

`BuildingBoxPlacementModeState` stores only the source stack, definition id and orientation. Clockwise and counter-clockwise rotation are deterministic four-state transitions and never mutate Domain state.

A valid preview can produce a typed `BuildingBoxPlacementConfirmationDraft` with source stack, definition, origin, orientation and work position. Invalid previews return a failure and cannot be converted into a confirm command.

## Unity boundary

The next Unity slice maps the ghost footprint to pooled preview visuals:

- `Valid` and `Invalid` are style tokens, not hard-coded simulation colors;
- the HUD localizes `ReasonCode`;
- rotation updates only the local mode state;
- LMB confirmation supplies generated building and Job ids to the Application handler;
- RMB cancellation discards the preview without releasing inventory because no reservation exists before confirmation.

## Validation

Tests cover:

- north/east footprint and work-position rotation;
- four-step rotation identity and reverse rotation;
- item mismatch and already-reserved box diagnostics;
- occupied and unreachable authoritative reasons;
- valid-only confirmation drafts.