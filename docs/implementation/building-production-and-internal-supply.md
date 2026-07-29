# Generic building production and internal supply implementation

Статус: revised spatial workflow is `IMPLEMENTED` in draft PR #515; actual licensed Unity Play Mode evidence remains required before `VERIFIED`.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Реализованные владельцы

- Production владеет data-driven workstation recipes, очередями, active material step и progressive consumption.
- BuildingSupplyState владеет internal input capacities, delivery toggles, incoming reservations и protected-source policy.
- InventoryState владеет physical item entities, reservations, resident transit cargo, `ItemLocation.InBuilding` и world outputs.
- JobSystem владеет supply/production worker lifecycle, worker claims, blocked/retry/cancel.
- Buildings владеет footprint/work position; zone anchors derived from footprint and are not saved.
- Skills выдаёт exactly-once grants после завершения одного production order.
- Presentation строит product icons, counters, left/right zone trays, stock units и post-work camera-facing pose.

## Building spatial zones — PR #515

### Left internal-storage zone

`DigBuildingInternalStockRenderer` derives the zone from `min(footprint.X) - 1` and renders it even when empty. Stock units use the same left anchor and remain `DigBuildingInternalStockVisual` trigger targets. The tray has disabled colliders and cannot block navigation.

`DigWorldItemPickupSession.TryResolveBuildingInternalStockPickup` resolves a direct quantity-one pickup to the same left-zone logical cell. The selected stack must be `ItemLocation.InBuilding(buildingId)` and have `AvailableQuantity > 0`; production reservations cannot be stolen.

Automatic supply behavior is unchanged and remains protected: planner candidates are revealed/reachable/unreserved world stacks only. Internal building stock is not a source for another delivery job, and resident inventory is valid only as reserved transit cargo of its owning supply job.

### Right finished-output zone

`ProductionOutputPlacement` now creates candidates only to the right of the footprint:

```text
right edge + 1 -> right edge + 2 -> ...
```

For multi-row footprints, row order is stable by distance to building origin, then Y/Z. Building orientation does not rotate this screen/world-X contract. Candidate validation requires world bounds, explored open cell, explored solid support, no building footprint and no existing world item. No front/left/rear fallback remains.

`DigBuildingProductionZones` keeps the assigned worker authoritative through Finalize. When the job enters `JobStageKind.Finalize`, movement target changes from workstation work position to the current deterministic right-zone output cell. Completion is allowed only when the same worker reaches that cell. The output is committed as ordinary `ItemLocation.InWorld(outputCell)`, so existing world selection/pickup/save behavior applies.

If no right-zone cell is available, completion is deferred with the existing typed `production.output_space_unavailable` state; inputs, output IDs and skill grants are not committed.

### Counter and post-work pose

Queue projection already counts non-terminal orders. A successful completion terminalizes exactly one order, so the building counter decreases by one on the next presentation refresh.

After successful output commit, `LoadProductionWaitOffsets` exposes a small `0.28` presentation-only outward offset. `DigAgentVisual` keeps the worker on the authoritative output cell, shifts the model slightly farther right and faces the active camera while idle. Any active job or manual movement clears this derived pose. The pose is not serialized.

## Demo campfire placement — PR #515

`DigTerrainWorkSession.Buildings` no longer searches the lower cave for the completed campfire. It reads `TunnelDemoLayout` and requires exact origin:

```text
ShaftX - 2 / SurfaceY / ShaftZ
```

Bootstrap validates open footprint, solid support, no overlap and a valid work position. It throws a diagnostic initialization failure instead of silently selecting a different cell. The separate packed campfire box remains in the lower cave.

Authoritative demo design: [`../design/demo-starting-scenario.md`](../design/demo-starting-scenario.md), issue [#389](https://github.com/bageus/Dig/issues/389).

## Production icon input correction — PR #501

- LMB on product icon enqueues one order.
- RMB on the same icon cancels one order while projected count is positive.
- Newest queued is cancelled before active.
- A separate minus/decrement button is not created.
- Tooltip, shortage tint and counter remain on the same icon.

## Supply workflow retained

- Supply batch is planned/reserved before movement.
- Worker follows `workstation check -> reserved world sources -> workstation deposit`.
- Deposit creates/merges `ItemLocation.InBuilding(buildingId)` stacks.
- Direct internal pickup may recreate replacement demand when delivery remains enabled.
- Mixed partial batches, cancellation cleanup and save/load ownership remain unchanged.

## Runtime deposit correction retained — PR #504

Demo mushroom cap/leg definitions use stack size `100`, matching authoritative content. This prevents `InventoryErrors.StackSizeExceeded` when a resident reaches `DepositItem` with a multi-unit reserved stack. `InternalsVisibleTo("Dig.Unity.PlayModeTests")` preserves the intended executable test boundary.

## Save/load

Save format v7 stores queue, active order/material step, consumed inputs, toggles, incoming supply batches and production/supply jobs. World outputs persist through ordinary item locations. Left/right tray geometry and post-work visual offsets are derived after load and are not serialized. The loader does not replay committed material consumption, outputs or skill grants.

## Regression coverage in PR #515

Domain/Application:

- deterministic right-only output candidates;
- orientation-independent screen-right placement;
- occupied/unsupported candidates skip forward;
- fully occupied right zone returns `OutputSpaceUnavailable` without side fallback;
- existing direct internal pickup/replacement-demand and protected-source tests remain active;
- demo layout anchor is open, two cells left of shaft and outside vertical topology.

Unity source contracts:

- Finalize moves toward output cell and commits `ItemLocation.InWorld` there;
- renderer contains separate left input/right output zone projection;
- direct pickup uses `min(footprint.X) - 1`;
- post-production offset synchronization and camera-facing pose are wired into the simulation loop;
- bootstrap uses exact surface campfire anchor without lower-cave fallback.

Checked-in Unity Play Mode:

- internal stock units render left of building;
- both empty-capable zone trays exist;
- finished-output tray renders right of building;
- stock colliders are triggers and tray colliders are disabled;
- product-icon RMB remains exactly-once.

## CI evidence

PR #515 head validation is recorded after GitHub Actions completes. Workflows requested on the initial draft head:

- Quality;
- Export Stage 2 v2 Source;
- Export Stage 2 v3 Source;
- Unity Play Mode.

Only checks that actually complete successfully will be reported as passed. The system remains `IMPLEMENTED`, not `VERIFIED`, until the runtime Play Mode scenario executes with licensed Unity evidence.
