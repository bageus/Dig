# Layered tunnel movement

The Unity settlement slice models resident movement in three logical axes:

- `X` is horizontal movement across a walkable surface;
- `Y` is vertical position;
- `Z` is depth, limited to four cells (`0..3`).

## Demo topology

`TunnelNavigationVolume.CreateDemo` now creates two broad walkable planes instead of duplicating every tunnel across all depth layers.

- The upper surface spans four `Z` cells.
- The lower cave floor spans four `Z` cells and is exactly four `Y` cells below the upper surface.
- The lower cave is at least three cells high and at least four cells wide.
- The vertical shaft occupies one `Z` cell.
- The horizontal connector between the shaft and cave occupies one `Z` cell.
- Demo residents start on the upper surface rather than receiving arbitrary corridors from unrelated terrain positions.

The Unity terrain renderer applies a projected cutaway mask for the shaft and cave interior. This keeps the authoritative two-dimensional terrain available to older systems while preventing the background terrain wall from visually filling the cave.

## Traversal rules

`TunnelNavigationVolume` owns the immutable topology used by the demo.

- A resident may move to an orthogonally adjacent open cell on `X`.
- A resident may move to an orthogonally adjacent open cell on `Z`.
- A resident may change `Y` only when both source and destination are marked shaft cells.
- Routes are deterministic and are validated before the resident aggregate is moved.

## Ownership

`AgentState` remains the only owner of logical resident position. It stores `SpatialCellId`, while the existing `CellId Position` projection remains available for older terrain and Job flows.

A legacy two-dimensional move changes `X/Y` while preserving the current `Z` layer. A tunnel movement command validates the full `SpatialCellId` route and then writes the final destination through the aggregate.

Unity owns only disposable presentation state:

- horizontal surface and cave-floor slabs;
- a non-interactive cave shell;
- selectable shaft and connector cells;
- route line rendering;
- interpolation through validated route cells;
- terrain cutaway visibility.

## Input

1. Left-click a dwarf to select it.
2. Left-click any reachable surface, connector, shaft, or cave-floor cell to issue a move order.
3. Right-click in the world to clear the resident selection and route.
4. Orbit with `Ctrl + mouse` when depth separation is needed.

Building placement keeps its own right-click cancel behavior and takes priority over resident deselection.

The route is rendered and the selected dwarf animates through each route cell. A player-issued tunnel order suppresses the old random demo movement for that resident so the order is not overwritten by the next simulation tick.

## HUD

The always-visible help block was removed. The runtime HUD is a small scrollable status and selection panel, so it no longer covers most of the Game view.

## Scope boundary

The authoritative terrain and Job targeting systems still use the existing two-dimensional `CellId` projection. This slice keeps the compatible resident/tunnel 3D contract without forcing an unsafe all-at-once migration of world chunks, inventory locations, reservations, buildings, and Job definitions.
