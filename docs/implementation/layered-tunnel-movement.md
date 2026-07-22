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

`WorldState` owns the open/solid state for every `CellId(X,Y,Z)`. `TunnelNavigationVolume` is a disposable projection rebuilt by `TunnelNavigationVolume.FromWorldSnapshot`; it never owns additional open cells.

- A resident may move to an orthogonally adjacent open cell on `X` or `Z`.
- A resident may change `Y` only when both source and destination satisfy the shaft traversal rule.
- Routes are deterministic and are validated against the current World-derived navigation version before the resident aggregate is moved.
- Excavation mutates World first. Navigation is rebuilt from the resulting immutable snapshot, so a visible open cell and a pathfinding-open cell cannot diverge.
- Unsupported open air above a room floor is excluded by the traversal projection and cannot become a floating walk surface.

## Ownership

`AgentState` is the only owner of logical resident position and stores one exact `CellId(X,Y,Z)`. There is no parallel spatial position or two-dimensional position projection. Every movement command validates a full XYZ route and writes the final destination through the aggregate.

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

Formation offsets, collision avoidance and distributing a group across neighbouring free cells remain later movement-policy work. The coordinate ownership itself is unified: World, Agents, Inventory, Buildings, Jobs, reservations, Navigation and Save/Load use the same bounded `CellId(X,Y,Z)`.
