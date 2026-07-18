# Z0 excavation planning

## Rock volume

The Unity slice keeps the existing authoritative `WorldState` as the front excavation layer at `Z=0`.

- The world starts as solid rock below the surface.
- Air is carved only above the surface, through the existing front shaft and connector, and inside the initial lower cave.
- `DigRockVolumeRenderer` builds combined meshes for solid rock at `Z=1..3`.
- Open surface and cave-floor cells remain walkable `XZ` planes.
- The deeper meshes are presentation-only in this slice. Designation and destruction remain authoritative on `Z=0`.

Combined meshes are used instead of one GameObject per rock cell to keep Unity startup and rendering bounded.

## Drawing mode

Excavation drawing is explicit and can only be enabled while no resident is selected.

- `Horizontal` keeps the first selected `Y` and changes `X` cell by cell.
- `Vertical` keeps the first selected `X` and changes `Y` cell by cell.
- Left click adds a Dig designation.
- Right click removes a Dig designation.
- Both modes target `Z=0`.
- The HUD exposes priority in steps of 50 within `0..1000`.

The previous implicit right-click excavation route is disabled. Building placement retains its own right-click cancellation behavior.

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
2. finds connected designated cells within Manhattan radius four;
3. releases that resident's previous active Job;
4. releases any previous automatic worker from the selected target group;
5. assigns the nearest reachable frontier Job to the selected resident;
6. holds the remaining group Jobs outside automatic assignment;
7. assigns the next reachable group Job to the same resident after each completed cell.

Assignment changes use the existing Job reservation ledger. Releasing an assignment returns the Job to `Available`, clears its execution stage, and releases Job, Agent, target, and Tool reservations.

## Scope boundary

This slice does not yet migrate `WorldState`, buildings, inventory locations, reservations, and Job targets to full `SpatialCellId` ownership. Digging into `Z=1..3`, room-volume brushes, support rules, collapse simulation, and material-specific mining remain later world-volume work.
