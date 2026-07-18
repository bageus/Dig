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

The Unity terrain renderer applies a projected cutaway mask for the shaft and cave interior. This keeps the authoritative two-dimensional terrain available to older systems while preventing the background terrain wall from visually filling the cave.

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

Unity owns only disposable presentation state:

- horizontal surface and cave-floor slabs;
- a non-interactive cave shell;
- selectable shaft and connector cells;
- world-space route rendering;
- interpolation through validated `X/Y/Z` route cells;
- resident selection visuals and an immutable ordered selection-id snapshot;
- the drag-selection rectangle;
- terrain cutaway visibility.

## Input

1. LMB on a dwarf selects only that dwarf.
2. `Shift + LMB` on a dwarf adds it to the current group or removes it from that group.
3. Press and hold LMB on the world, then drag beyond the selection threshold to select every visible dwarf whose projected center lies inside the rectangle.
4. A drag without Shift replaces the current resident selection; `Shift + drag` adds residents that are not already selected.
5. A short LMB click without a drag keeps its normal action. On a walkable destination it sends the current group; on a terrain cell it keeps terrain selection behavior.
6. LMB on any reachable surface, connector, shaft, or cave-floor cell sends every selected dwarf to the same destination cell.
7. RMB in the world is the global cancel action. It clears the active marquee, excavation or cave-room mode, building preview, route, selected residents, selected Job, selected building, and selected terrain cell.
8. `Ctrl + mouse` orbits the side-view camera when depth separation is needed.

The marquee does not start from resident, Job, building, or world-item colliders, so normal object clicks are not stolen. RMB over the HUD is shielded from world input. The cancel path is resolved before excavation, cave placement, or building placement can consume the pointer event.

Every selected resident receives its own validated route and route animation. The shared route overlay displays one representative path, while all selected resident visuals move. A player-issued tunnel order suppresses old random demo movement for each member of the group, so the command is not overwritten by the next simulation tick.

## HUD

The runtime HUD is a small scrollable status and selection panel. Its `-` button collapses it to a single summary row; the `+` button restores the settings and selection panel. The collapsed panel also reduces its pointer-blocking rectangle to one-row height.

For a group selection, the HUD reports the selected resident count and keeps the most recently selected resident as the primary details/inventory view. The complete group remains selected across normal simulation rendering and movement refreshes.

## Scope boundary

All selected residents currently occupy the exact requested destination cell, matching the direct command semantics. Formation offsets, collision avoidance, and distributing a group across neighboring free cells are separate movement-policy work.

The authoritative terrain and Job targeting systems still use the existing two-dimensional `CellId` projection. This slice keeps the compatible resident/tunnel 3D contract without forcing an unsafe all-at-once migration of world chunks, inventory locations, reservations, buildings, and Job definitions.
