# Z0 and layered excavation planning

## Rock volume and walk surfaces

`WorldState` owns all four playable depth layers (`Z=0..3`). `TunnelNavigationVolume` is derived from an immutable `WorldSnapshot` and contains no independently mutable open-cell set.

- The world starts as solid rock below the surface.
- Air is carved above the surface, through the existing shaft and connector, inside the generated lower cave, and inside completed user-planned rooms.
- `DigTerrainRenderSnapshotBuilder` builds combined chunk meshes for authoritative solid cells on every Z layer.
- Designation, damage, material yield and Job targets preserve exact XYZ coordinates.
- A completed room and a manual depth command mutate their exact World cells before navigation and presentation rebuild.

A walkable cell represents empty space above a supporting rock cell. Floor geometry therefore uses the upper boundary of the cell below instead of the center of the empty cell.

- `DigTunnelProjection.WalkSurfaceY` owns the shared floor height.
- Resident feet, route lines, surface platforms, and cave-floor platforms use that height.
- The floor cube is sunk into the supporting block by its own thickness.
- The front `Z=0` floor is rendered by `DigWorldRenderer` only when the empty cell has solid support directly below.
- Initial `Z=0` platforms are not duplicated by `DigTunnelDemoRenderer`.
- Cave shells and room back walls have no collider and are never navigation surfaces.

Combined meshes are used instead of one GameObject per rock cell. The renderer consumes one four-layer `WorldViewModel`; completed rooms and depth excavation need no second cutout owner.

## Direct resident movement

Direct movement accepts every renderer that owns a walkable destination:

- `DigTunnelDemoRenderer` builds invisible continuous surfaces over original layered cells, synchronized Z0 cells, shafts, completed-room floors and manually opened depth cells.
- the continuous pointer hit resolves to a hidden `CellId` plus a bounded presentation-only X offset;
- `DigWorldRenderer` and `DigCaveRoomFloorRenderer` expose exact cells only while an excavation tool is active and never act as ordinary movement targets.
- All routes execute through the same single-resident or atomic group tunnel movement commands.
- Excavation changes World; `TunnelNavigationVolume.FromWorldSnapshot` rebuilds the resulting walk topology.

## Protected rock

`ExcavationBoundaryPolicy` owns the non-excavatable front-layer boundary.

- The left and right world edges are protected.
- The bottom edge is protected.
- The first solid row below the upper surface is protected.
- Protected cells are returned in deterministic coordinate order.
- Designation commands reject protected cells before mutating `WorldState`.
- Protected solid rock uses a darker visual treatment.
- An attempted LMB designation highlights the rejected cell and leaves it undesignated.

## Tunnel, depth, and delete tools

Excavation editing is explicit and can only be enabled while no resident is selected.

### Tunnel

- Holding LMB starts a stroke on `Z=0`.
- The first meaningful pointer movement selects the dominant horizontal or vertical axis.
- The selected axis remains locked until LMB is released.
- Cells crossed by fast pointer movement are filled deterministically.

### Depth

- Depth excavation starts only from an already open horizontal tunnel cell.
- Shaft cells and solid cells cannot be used as the source.
- One LMB command evaluates exactly one target: `(X,Y,Z+1)`.
- When the next layer is already open, the player must select that new layer before continuing.
- `Z=3` is the hard limit, so a fifth depth cell cannot be created.
- Holding LMB is not a depth stroke and cannot open multiple layers.

### Delete

- `Delete` removes front-layer Dig designations one hovered cell at a time.
- Releasing LMB resets only the current stroke; the selected tool remains active.
- The HUD exposes priority in steps of 50 within `0..1000`.

RMB globally cancels the current excavation mode, room preview, placement preview, route, and object selections before any world command is routed.

## Cave-room presets

The room catalog is explicit and deterministic.

| Preset | Base width | Top width | Depth | Height |
| --- | ---: | ---: | ---: | ---: |
| Small | 5 | 3 | 3 | 3 |
| Medium | 8 | 6 | 3 | 3 |
| Large | 12 | 8 | 4 | 5 |
| Tall | 10 | 6 | 4 | 7 |

The selected horizontal-tunnel cell is the deterministic anchor for the complete bottom row. The entire `BaseWidth` row on `Z=0` must already be an open through tunnel; its left and right extremes are the room entrances. The room rises upward and narrows toward the top. Intermediate row widths are linearly interpolated and rounded deterministically.

`CaveRoomPlanner.ResolveRowProfile` owns exact doubled boundaries and physical terrain masks for planning, roof validation, preview, completed trim, side protection and room floors. Every row shares the geometric center of the base. When row/base parity differs, the silhouette uses two boundary half-cells rather than shifting a whole cell: Small `5,4,3` keeps full cells `X-2..X+2` at the base, digs the right half of `X-2`, full `X-1..X+1`, and the left half of `X+2` on the middle row, then uses full `X-1..X+1` at the top. Existing four-quarter state is sufficient.

The cross-section is extruded through the preset depth. `CaveRoomPlan.VolumeCells` owns the complete `XYZ` volume, while `FrontExcavationCells` contains only solid `Z=0` cells that create Dig Jobs.

Placement rules:

- No resident may be selected.
- The pointer may be on the open base tunnel or on rock inside the intended front silhouette; runtime resolves the matching base row below it.
- The complete base row must be an open horizontal through tunnel.
- The room may not overlap protected or unmineable rock or leave world bounds.
- One complete solid row matching the top width must remain above the target room.
- Open cells above the base row block placement.
- A completed room at the same anchor is immutable and cannot be silently resized.

`DigCaveRoomPreviewRenderer` displays the full trapezoidal prism even for an invalid placement, while invalid cells receive diagnostics. LMB on a valid preview applies all new front designations as one transaction and synchronizes the existing Dig Job flow once. Medium uses the same preview and completed-trim pipeline as every other preset.

## Back walls and completed trim

Completed template provenance creates non-interactive trim, including entrance outline, internal depth arches, side walls and a back-wall surface behind the deepest playable layer.

- Trim is Presentation geometry, not an additional navigation cell.
- The documented room depth remains fully walkable.
- Trim rows use the same authoritative row bounds as the excavation mask.
- The trim root preserves world-space identity under the rotated bootstrap object; it does not inherit the side-view rotation a second time and cannot create a floating duplicate above the scene.
- The generated natural cave remains separate from user-planned template provenance.

## Cave-room completion

A room remains incomplete while any excavation target has not reached its required quarter mask. Full targets require `4/4` and become air. Boundary half-cell targets require their exact `2/4`, clear their designation/job, remain solid shell cells and create no output or navigation opening. Completion is evaluated once per simulation tick.

When every room target has completed its required mask:

1. deeper cells from `CaveRoomPlan.VolumeCells` are represented as open authoritative World cells;
2. the full bottom row across the preset depth is added to tunnel navigation;
3. clickable `XZ` floor cells are created at `Z=1..depth-1`;
4. template trim is rebuilt from the completed plan;
5. direct movement can route through every room depth cell;
6. repeated refreshes remain idempotent.

## Quarter excavation and unsupported work

The authoritative cell progress remains four quarters. Vertical front-slice targets use horizontal rows: `UpperLeft|UpperRight`, then `LowerLeft|LowerRight`.

The side-view root rotates logical vertical onto Unity local Z. Quarter visuals therefore split local X/Z and keep local Y as full depth. After any quarter is committed in the cell directly below a resident, that cell is no longer full standing support. Any mining direction without full support uses stationary climbing stance immediately, including side excavation from a vertical shaft. The posture is derived from World support even after the job leaves `PerformWork`; it remains active until the resident starts another traversal or reaches a supported cell. If no new job/direct order is assigned, the ordinary movement planner sends the resident to the nearest reachable full-support walk cell.

## Jobs and automatic assignment

The front excavation flow remains:

`World designation -> Dig Job -> candidate assignment -> navigation -> work -> terrain completion`

Each designated solid cell creates one Dig Job with an exact XYZ target and work cell. Automatic candidates are exposed only when the target is a current frontier, the resident is alive, a physical route exists, and the Job is not held by a player-directed group.

## Scope boundary

Room geometry, centered half-cell boundaries, Small depth 3, completed-room world-space projection, bounded one-cell depth opening, deep floor rendering, deep direct movement, horizontal quarter presentation, terminal unsupported climbing and support recovery are implemented.

Depth excavation uses an exact XYZ Dig Job, worker/position/designation reservations, terrain completion and World-derived navigation. Collapse, material-specific balancing and formation-aware resident occupancy remain later work.
