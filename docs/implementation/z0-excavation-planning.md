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
| Medium | 7 | 3 | 4 | 3 |
| Large | 9 | 5 | 4 | 5 |
| Tall | 8 | 4 | 4 | 6 |

The selected horizontal-tunnel cell is included in the bottom, widest row and acts as the entrance and exit. The room rises upward from that row and narrows toward the top. Intermediate row widths are linearly interpolated and rounded deterministically. Even widths use a stable one-cell bias around the selected entrance.

The cross-section is extruded through the preset depth. `CaveRoomPlan.VolumeCells` owns the complete `XYZ` volume, while `FrontExcavationCells` contains only solid `Z=0` cells that create Dig Jobs.

Placement rules:

- No resident may be selected.
- The cursor must point at an excavated `Z=0` horizontal tunnel cell.
- The room may not overlap protected rock or leave world bounds.
- One complete solid row matching the top width must remain above the target room.
- Open cells above the entrance row normally block placement.
- Open cells belonging to a completed room at the same entrance are allowed as an upgrade footprint.
- A larger preset over a completed smaller room creates Dig Jobs only for additional solid cells.
- An unrelated natural cavity or arbitrary open shape cannot impersonate a completed room upgrade.

`DigCaveRoomPreviewRenderer` displays a 12-edge trapezoidal prism. LMB on a valid preview applies all new front designations as one transaction and synchronizes the existing Dig Job flow once.

## Back walls

User-excavated rooms always receive a non-interactive back wall behind their deepest playable layer.

- The wall is presentation shell geometry, not a fifth navigation cell.
- The documented room depth remains fully walkable.
- Upgrading a room replaces the previous wall at that entrance so an old Small wall does not remain inside a Large room.
- The generated natural cave owns an explicit `CaveHasBackWall` option and may be rendered with or without a back wall.

## Cave-room completion

A room remains closed in depth while any new front excavation cell is still solid. Completion is evaluated once per simulation tick.

When every front Dig Job has removed its target cell:

1. deeper cells from `CaveRoomPlan.VolumeCells` are excluded from combined rock meshes;
2. the full bottom row across the preset depth is added to tunnel navigation;
3. clickable `XZ` floor cells are created at `Z=1..depth-1`;
4. the room back wall is created or replaced;
5. direct movement can route through every room depth cell;
6. repeated refreshes remain idempotent.

## Jobs and automatic assignment

The front excavation flow remains:

`World designation -> Dig Job -> candidate assignment -> navigation -> work -> terrain completion`

Each designated solid cell creates one Dig Job with an exact XYZ target and work cell. Automatic candidates are exposed only when the target is a current frontier, the resident is alive, a physical route exists, and the Job is not held by a player-directed group.

## Scope boundary

Room geometry, completed room expansion, optional generated-cave walls, mandatory planned-room walls, bounded one-cell depth opening, deep floor rendering, and deep direct movement are implemented.

Depth excavation now uses an exact XYZ Dig Job, worker/position/designation reservations, terrain completion and World-derived navigation. Support simulation, collapse, material-specific balancing and formation-aware resident occupancy remain later work.
