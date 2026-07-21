# Layered tunnel movement

The Unity settlement slice models resident movement in three logical axes:

- `X` is horizontal movement across a walkable surface;
- `Y` is vertical position;
- `Z` is depth, limited to four cells (`0..3`).

## Movement planes

Walking and climbing use different planes.

- Surface and cave-floor movement happens on the `XZ` plane. `Y` remains constant while a resident walks across a level.
- Shaft movement happens on the `XY` plane. `Z` remains fixed while the resident climbs vertically.
- The demo shaft and its horizontal connector use the nearest depth cell, `Z=0`.
- A route may change `Y` only between adjacent cells that are both marked as shaft cells.

The Unity adapter projects these coordinates directly into world space:

- logical `X` maps to world `X`;
- logical `Y` maps to world vertical position;
- logical `Z` maps to world depth.

Resident and tunnel visuals do not derive their positions from the rotated legacy terrain root. This keeps the resident upright and prevents `Y` and `Z` movement from being visually exchanged.

## Demo topology

`TunnelNavigationVolume.CreateDemo` creates two broad walkable planes.

- The upper surface spans four `Z` cells.
- The lower cave floor spans four `Z` cells and is exactly four `Y` cells below the upper surface.
- The lower cave is at least three cells high and at least four cells wide.
- The vertical shaft occupies one cell at `Z=0`.
- The horizontal connector between the shaft and cave occupies one cell at `Z=0`.
- Demo residents start on the upper surface.

The Unity terrain snapshot combines front World cells with a deep solid-rock projection. Open tunnel cells, explicit depth excavations and completed cave-room volume cells are removed from the collider-free chunk meshes. The old separate deep-rock renderer no longer exists.

## Traversal rules

`TunnelNavigationVolume` owns the current layered topology.

- A resident may move to an orthogonally adjacent open cell on `X`.
- A resident may move to an orthogonally adjacent open cell on `Z`.
- A resident may change `Y` only when both source and destination are marked shaft cells.
- Routes are deterministic and are validated before the resident aggregate is moved.

Before direct movement, `WithSynchronizedFrontLayer` derives new `Z=0` navigation from the authoritative `WorldSnapshot`.

- An excavated cell with solid support directly below becomes a horizontal walkable cell.
- A cell recorded by a vertical tunnel stroke becomes an open shaft cell even when it has no floor support.
- Existing demo and deep-room cells remain in the immutable volume projection.
- Unsupported open air above a room floor is not added, so the room wall does not become a climbable or floating surface.
- Movement handlers are rebuilt only when the synchronized topology actually changes.

This refresh runs after excavation presentation changes and immediately before both single-resident and group movement commands. A newly excavated floor therefore cannot be visible while remaining blocked in the pathfinder.

## Ownership

`AgentState` remains the only owner of logical resident position. It stores `SpatialCellId`, while the existing `CellId Position` projection remains available for older terrain and Job flows.

A legacy two-dimensional move changes `X/Y` while preserving the current `Z` layer. A tunnel movement command validates the full `SpatialCellId` route and then writes the final destination through the aggregate.

Group movement uses one Application command. The handler first resolves every selected resident and validates every route to the shared destination. No resident position is mutated until all routes are valid. If one selected resident cannot reach the destination, the whole group command fails without moving anyone. A one-resident selection keeps the dedicated single-resident command path.

After a direct route validates, the terrain-work adapter releases every active Job owned by the ordered residents. Job, agent, target and tool reservations are released through `ReleaseJobAssignmentHandler`, stale work routes are discarded, and the residents enter persistent direct-movement ownership. Automatic assignment excludes these residents until the player explicitly assigns an excavation group.

Unity owns disposable presentation state only:

- collider-free chunk meshes for the current visual rock volume;
- invisible continuous movement surfaces built from contiguous open-cell runs;
- disabled-by-default exact tunnel/room cell proxies used only by excavation modes;
- non-interactive cave shell and back walls;
- world-space route rendering;
- interpolation through validated `X/Y/Z` route cells;
- a bounded within-cell X offset for the final freeform movement point;
- resident selection visuals and an immutable ordered selection-id snapshot;
- the drag-selection rectangle.

The terrain mesh is never a movement or cell-picking authority. `DigTunnelDemoRenderer` owns invisible movement surfaces and maps their continuous hit point to a hidden navigation cell. `DigCaveRoomFloorRenderer` owns only excavation proxies and renders no per-cell floor geometry.

## Input

1. LMB on a dwarf selects only that dwarf.
2. `Shift + LMB` on a dwarf adds it to the current group or removes it.
3. Press and hold LMB on the world, then drag beyond the selection threshold to select visible dwarfs inside the rectangle.
4. A drag without Shift replaces the current selection; `Shift + drag` adds residents not already selected.
5. A short LMB click anywhere on an invisible walkable tunnel surface sends the current group; the route uses hidden cells while the final visual position keeps the clicked within-cell offset.
6. An ordinary terrain click creates no cell target and issues no movement command.
7. Tunnel, Delete, Depth and cave-room planning temporarily enable the bounded front-cell proxies used by excavation only.
8. RMB in the world cancels active marquee, excavation/cave mode, building preview, route and object selections.
9. `Ctrl + mouse` orbits the side-view camera when depth separation is needed.

The movement resolver checks only renderer-free surfaces from `DigTunnelDemoRenderer`. It does not fall back to `DigWorldRenderer`, chunk meshes, completed-room dig proxies or hidden front-cell proxies. A deferred short click from marquee input follows the same resolver.

The marquee does not start from resident, Job, building or world-item colliders, so normal object clicks are not stolen. RMB over the HUD is shielded from world input. The cancel path is resolved before excavation, cave placement or building placement can consume the pointer event.

Every selected resident receives an independently validated route and route animation. The shared route overlay displays one representative path, while all selected resident visuals move. A player-issued tunnel order suppresses old automatic movement for every group member.

## HUD

The runtime HUD is a small scrollable status and selection panel. Its `-` button collapses it to a single summary row; the `+` button restores the settings and selection panel. The collapsed panel also reduces its pointer-blocking rectangle to one-row height.

For a group selection, the HUD reports the selected resident count and keeps the most recently selected resident as the primary details/inventory view. The complete group remains selected across normal simulation rendering and movement refreshes.

## Scope boundary

All selected residents currently occupy the exact requested destination cell. Formation offsets, collision avoidance and distributing a group across neighbouring free cells are separate movement-policy work.

The authoritative World and Job targeting systems still use the two-dimensional `CellId` projection. The unified terrain mesh now displays `Z=0..3`, but deep material state, damage, Jobs, reservations and persistence still require the full spatial migration tracked by #88.
