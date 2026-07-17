# Layered tunnel movement

The Unity settlement slice now models resident movement in three logical axes:

- `X` is horizontal movement along a level;
- `Y` is vertical level position;
- `Z` is depth, limited to four cells (`0..3`).

## Traversal rules

`TunnelNavigationVolume` owns the immutable tunnel topology used by the demo.

- A resident may move to an orthogonally adjacent open cell on `X`.
- A resident may move to an orthogonally adjacent open cell on `Z`.
- A resident may change `Y` only when both the source and destination are marked vertical-tunnel cells.
- Routes are deterministic and are validated before the resident aggregate is moved.

The generated demo contains three broad horizontal levels across all four depth cells and a connected vertical shaft. Existing demo resident starting positions receive an entrance corridor to the shaft.

## Ownership

`AgentState` remains the only owner of logical resident position. It now stores `SpatialCellId`, while the existing `CellId Position` projection remains available for older terrain and Job flows.

A legacy two-dimensional move changes `X/Y` while preserving the current `Z` layer. A tunnel movement command validates the full `SpatialCellId` route and then writes the final destination through the aggregate.

Unity owns only disposable presentation state:

- selectable tunnel-cell GameObjects;
- route line rendering;
- interpolation through the validated route cells;
- material and depth offsets.

## Input

1. Left-click a dwarf to select it.
2. Orbit with `Ctrl + mouse` when depth separation is needed.
3. Left-click an open tunnel cell.

The route is rendered and the selected dwarf animates through each route cell. A player-issued tunnel order suppresses the old random demo movement for that resident so the order is not overwritten by the next simulation tick.

## Scope boundary

The authoritative terrain and Job targeting systems still use the existing two-dimensional `CellId` projection. This slice introduces the compatible resident/tunnel 3D contract without forcing an unsafe all-at-once migration of world chunks, inventory locations, reservations, buildings, and Job definitions.

A later world-volume migration can replace the projected terrain layer while keeping the `SpatialCellId` movement and presentation contracts.
