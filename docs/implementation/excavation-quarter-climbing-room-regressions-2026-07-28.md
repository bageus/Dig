# Excavation quarter, climbing stance and cave-room regressions — 2026-07-28

Статус: `IMPLEMENTED`; фактический Unity Play Mode запуск обязателен для `VERIFIED`.

Authoritative specifications and tracking:

- [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md), issue [#388](https://github.com/bageus/Dig/issues/388);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md), issue [#386](https://github.com/bageus/Dig/issues/386);
- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), issue [#87](https://github.com/bageus/Dig/issues/87).

## Runtime symptoms

- partial excavation of a vertical target appeared as left/right columns instead of horizontal near/top and far/bottom bands;
- a resident could keep a standing work pose after the first quarter below their feet was committed;
- unsupported side excavation from a shaft could use an ordinary standing dig pose;
- even-width cave-room rows could drift between planner, preview, completed trim and floor projection;
- the Medium room lacked a dedicated runtime regression proving that its preview and completed trim are produced.

## Root causes

`DigUnityBootstrap` rotates the side-view root by 90 degrees. `DigCellVisual` built quarter children on local X/Y, but local Y is world depth under that rotation. Logical vertical belongs to local Z. Upper and lower pieces therefore overlapped on screen and split in depth.

Work-facing presentation additionally required either vertical-tunnel provenance or the exact target-below special case before entering climbing stance. Full support is already authoritative in World; provenance is not required to know that a mining worker has no floor.

Cave-room row centering was duplicated in planner, preview, trim and floor code. The formulas currently agreed, but no single owner or regression prevented drift, and no Medium runtime fixture proved the visible geometry path.

## Correction

- quarter children split local X/Z and retain full local-Y depth;
- `UpperLeft|UpperRight` is one world-horizontal upper band under the rotated root;
- any mining work without full support uses stationary climbing stance, independent of shaft/template provenance;
- climbing work pose is applied immediately even while the final movement interpolation is finishing;
- `CaveRoomPlanner.ResolveRowMinX` is the single row-bounds owner;
- an even row uses the deterministic right-biased tie-break: Small `5,4,3` at anchor X has its second row at `X-1..X+2`;
- planner, roof, preview, completed trim and room floor use that same function;
- Medium `8,7,6` has planning, trim and Play Mode preview regressions.

The approved four-quarter cell model remains unchanged. Increasing a cell to eight parts is unnecessary for these fixes and would add save/migration and balancing work without resolving the actual presentation-axis bug.

## Regression coverage

`ExcavationRuntimeRegressionContractTests` covers Small and Medium row masks, completed trim projection, quarter-axis source contracts and unsupported-work posture ownership.

`ExcavationQuarterRoomAndClimbingPlayModeTests` covers:

- upper-row quarter geometry under the actual 90-degree side-view root;
- full-depth remaining quarter bounds;
- unsupported mining posture policy and immediate climbing work state;
- visible Medium preview mesh with the expected width and height.

A licensed Unity Test Runner execution is still required to validate the complete input -> job -> quarter commit -> same-tick climbing presentation workflow in the Editor.
