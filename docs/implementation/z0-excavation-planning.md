# Z0 excavation planning

## Rock volume and walk surfaces

The Unity slice keeps the existing authoritative `WorldState` as the front excavation layer at `Z=0`.

- The world starts as solid rock below the surface.
- Air is carved only above the surface, through the existing front shaft and connector, and inside the initial lower cave.
- `DigRockVolumeRenderer` builds combined meshes for solid rock at `Z=1..3`.
- Individual designation, damage, and Job targets remain authoritative on `Z=0`.
- A completed cave-room plan derives a bounded deeper excavation volume from its immutable preset and removes those cells from the combined meshes as one activation step.

A walkable cell represents empty space above a supporting rock cell. Floor geometry therefore uses the upper boundary of the cell below instead of the center of the empty cell.

- `DigTunnelProjection.WalkSurfaceY` owns the shared floor height.
- resident feet, route lines, surface platforms, and cave-floor platforms use that same height;
- the floor cube is sunk into the supporting block by its own thickness;
- the front `Z=0` floor is rendered by `DigWorldRenderer` only when the empty cell has solid support directly below;
- newly completed Dig Jobs automatically expose the resulting supported cell as a horizontal `XZ` floor plane;
- initial `Z=0` platforms are not duplicated by `DigTunnelDemoRenderer`;
- the cave shell has no collider and is never a navigation surface.

Combined meshes are used instead of one GameObject per rock cell to keep Unity startup and rendering bounded. Meshes rebuild only when the set of completed room-volume cells changes.

## Direct resident movement

Direct movement accepts every renderer that owns a walkable destination:

- `DigTunnelDemoRenderer` supplies `SpatialCellId` values for original layered cells and shaft cells;
- `DigWorldRenderer.TryGetWalkSurface` supplies `SpatialCellId(X,Y,0)` for supported front-floor cells;
- `DigCaveRoomFloorRenderer` supplies deep base-row cells created after a room is completed;
- all routes execute through the same `MoveResidentThroughTunnel` application command;
- completed room base cells are added through `TunnelNavigationVolume.WithAdditionalOpenCells` without mutating the original volume.

## Protected rock

`ExcavationBoundaryPolicy` owns the non-excavatable demo boundary.

- the left and right world edges are protected;
- the bottom edge is protected;
- the first solid row below the upper surface is protected;
- protected cells are returned in deterministic coordinate order;
- designation commands reject protected cells before mutating `WorldState`;
- protected solid rock uses a darker visual treatment;
- an attempted LMB designation highlights the rejected cell red and leaves it undesignated.

## Tunnel and delete tools

Excavation editing is explicit and can only be enabled while no resident is selected.

- `Tunnel` is the only drawing tool.
- Holding LMB starts a stroke on `Z=0`.
- The first meaningful pointer movement selects the dominant horizontal or vertical axis.
- The selected axis remains locked until LMB is released.
- Cells crossed by a fast pointer movement are filled deterministically along the locked axis.
- `Delete` removes Dig designations with LMB, one hovered cell at a time.
- Releasing LMB resets only the current stroke; the selected tool remains active.
- The HUD exposes priority in steps of 50 within `0..1000`.

The previous implicit right-click excavation route remains disabled. Building placement retains its own right-click cancellation behavior.

## Cave-room presets

The cave-room catalog is explicit and deterministic.

| Preset | Base width | Top width | Depth | Height |
| --- | ---: | ---: | ---: | ---: |
| Small | 5 | 3 | 3 | 3 |
| Medium | 7 | 3 | 4 | 3 |
| Large | 9 | 5 | 4 | 5 |
| Tall | 8 | 4 | 4 | 6 |

The selected horizontal-tunnel cell is included in the bottom, widest row and acts as the room entrance and exit. The room rises upward from that row and narrows toward the top. Intermediate row widths are linearly interpolated and rounded deterministically. Even widths use a stable one-cell bias around the selected entrance.

The cross-section is extruded through the preset depth without changing shape. `CaveRoomPlan.VolumeCells` owns the complete `XYZ` room volume, while `FrontExcavationCells` contains the solid `Z=0` cells that create authoritative Dig Jobs.

Placement rules:

- no resident may be selected;
- the cursor must point at an already excavated `Z=0` cell;
- at least one open left or right neighbor must make that cell part of a horizontal tunnel;
- existing open cells are allowed only in the bottom entrance row;
- all higher room cells must still be solid rock;
- the room may not overlap protected rock or leave world bounds;
- one complete solid row matching the top width must remain above the room.

`DigCaveRoomPreviewRenderer` displays a 12-edge trapezoidal prism. A valid outline is green; an invalid outline is red. LMB on a valid preview applies all front designations as one transaction, then synchronizes the existing Dig Job and automatic candidate flow once.

## Cave-room completion

A room remains closed in depth while any front excavation cell is still solid. Completion is evaluated once per simulation tick.

When every front Dig Job has removed its target cell:

1. all deeper cells from `CaveRoomPlan.VolumeCells` are excluded from the combined rock meshes;
2. the full bottom row across the preset depth is added to the immutable tunnel navigation projection;
3. `DigCaveRoomFloorRenderer` creates clickable `XZ` floor cells at `Z=1..depth-1`;
4. direct movement can route from the entrance through every depth cell on that floor;
5. repeated frames are idempotent and do not duplicate floors or navigation cells.

The completion key is the stable entrance coordinate plus preset kind. Rock meshes compare the complete excavated-cell set before rebuilding.

## Jobs and automatic assignment

The existing authoritative flow is reused:

`World designation -> Dig Job -> candidate assignment -> navigation -> work -> terrain completion`

Each designated solid cell creates one Dig Job with the drawing priority. Automatic candidates are exposed only when:

- the cell is a current excavation frontier adjacent to open air;
- the resident is alive;
- the resident is on `Z=0`;
- the Job is not held by a player-directed excavation group.

This prevents workers from claiming buried cells before a route can exist. Room placement uses the same designations and priorities, so a free resident can claim the nearest reachable room-front Job without a parallel room-specific worker system.

## Player-directed excavation

Selecting a resident disables drawing and room-preview modes. Left-clicking a designated cell or its Job marker:

1. moves the resident to the nearest front depth `Z=0` when necessary;
2. releases the persistent manual tunnel-order before assigning work;
3. finds connected designated cells within Manhattan radius four;
4. releases that resident's previous active Job;
5. releases any previous automatic worker from the selected target group;
6. assigns the nearest reachable frontier Job to the selected resident;
7. holds the remaining group Jobs outside automatic assignment;
8. assigns the next reachable group Job to the same resident after each completed cell.

Releasing the manual tunnel-order is required because manual movement intentionally suppresses simulation movement targets. The explicit Dig assignment becomes the new movement owner immediately after that release.

Assignment changes use the existing Job reservation ledger. Releasing an assignment returns the Job to `Available`, clears its execution stage, and releases Job, Agent, target, and Tool reservations.

## Scope boundary

Room dimensions, full `XYZ` outlines, entrance validation, roof clearance, Z0 room Dig Jobs, completed deep meshes, deep floor cells, and deep direct movement are implemented.

The deeper volume is currently activated atomically when the authoritative front section is complete. It does not yet own independent per-`SpatialCellId` damage, work stages, material yields, reservations, or partial visual progress. Migrating `WorldState`, Job targets, inventory locations, and buildings to `SpatialCellId` remains necessary for cell-by-cell deep mining.

Support rules beyond the mandatory top rock row, collapse simulation, and material-specific mining remain later world-volume work.
