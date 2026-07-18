# Z0 excavation planning

## Rock volume and walk surfaces

The Unity slice keeps the existing authoritative `WorldState` as the front excavation layer at `Z=0`.

- The world starts as solid rock below the surface.
- Air is carved only above the surface, through the existing front shaft and connector, and inside the initial lower cave.
- `DigRockVolumeRenderer` builds combined meshes for solid rock at `Z=1..3`.
- The deeper meshes are presentation-only in this slice. Designation and destruction remain authoritative on `Z=0`.

A walkable cell represents empty space above a supporting rock cell. Floor geometry therefore uses the upper boundary of the cell below instead of the center of the empty cell.

- `DigTunnelProjection.WalkSurfaceY` owns the shared floor height.
- resident feet, route lines, surface platforms, and cave-floor platforms use that same height;
- the floor cube is sunk into the supporting block by its own thickness;
- the front `Z=0` floor is rendered by `DigWorldRenderer` only when the empty cell has solid support directly below;
- newly completed Dig Jobs automatically expose the resulting supported cell as a horizontal `XZ` floor plane;
- initial `Z=0` platforms are not duplicated by `DigTunnelDemoRenderer`;
- the cave shell has no collider and is never a navigation surface.

Combined meshes are used instead of one GameObject per rock cell to keep Unity startup and rendering bounded.

## Direct resident movement

Direct movement accepts both renderer types that own walkable destinations:

- `DigTunnelDemoRenderer` supplies `SpatialCellId` values for layered cells at `Z=1..3` and shaft cells;
- `DigWorldRenderer.TryGetWalkSurface` supplies `SpatialCellId(X,Y,0)` for supported front-floor cells;
- both routes execute through the same `MoveResidentThroughTunnel` application command;
- moving a floor from the layered renderer into the authoritative front renderer must not remove click-to-move support.

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

## Jobs and automatic assignment

The existing authoritative flow is reused:

`World designation -> Dig Job -> candidate assignment -> navigation -> work -> terrain completion`

Each designated solid cell creates one Dig Job with the drawing priority. Automatic candidates are exposed only when:

- the cell is a current excavation frontier adjacent to open air;
- the resident is alive;
- the resident is on `Z=0`;
- the Job is not held by a player-directed excavation group.

This prevents workers from claiming buried cells before a route can exist.

## Player-directed excavation

Selecting a resident disables drawing mode. Left-clicking a designated cell or its Job marker:

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

## Cave-room specification boundary

No repository or File Library document currently defines the cell dimensions for the requested small, medium, large, and high cave presets. The original-game reference confirms four cave sizes but does not provide grid dimensions. Room buttons, preview footprints, and bulk room designations must therefore wait for an explicit width/height catalog rather than embedding guessed dimensions.

The next room-planning slice also requires a stable definition of where the tunnel entrance attaches to each footprint.

## Scope boundary

This slice does not yet migrate `WorldState`, buildings, inventory locations, reservations, and Job targets to full `SpatialCellId` ownership. Digging into `Z=1..3`, room-volume brushes, support rules, collapse simulation, and material-specific mining remain later world-volume work.
