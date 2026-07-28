# Mushroom finalization and barrel surface-route regression

Статус: implementation hotfix, Unity Test Runner verification pending.

Authoritative specifications:

- [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md), tracking [#423](https://github.com/bageus/Dig/issues/423);
- [`../design/destructible-barrels.md`](../design/destructible-barrels.md), tracking [#443](https://github.com/bageus/Dig/issues/443);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md), tracking [#386](https://github.com/bageus/Dig/issues/386).

## Mushroom final-swing root cause

`CompleteMushroomSwingCommandHandler` already advances the authoritative chopping job from `PerformWork` to `Finalize` when the last required swing completes. The Unity adapter continued using its stale pre-swing `JobSnapshot` and called the generic `AdvanceJobHandler` a second time. That moved the job from `Finalize` to terminal `Completed` before `CompleteMushroomChopCommand` could atomically transition the site to `AbsentRegrowing`, create cap/leg units and apply the exactly-once Woodworking grant.

`AdvanceMushroomJob` now reloads the authoritative job after the final swing and commits `CompleteMushroomChopCommand` immediately when the reloaded stage is `Finalize`. The generic advance remains only for legacy/restored state where all swings are already stored while the job is still at `PerformWork`.

`MushroomFinalSwingPlayModeTests` drives the actual Unity adapter through arrival and every swing, then requires a completed job, an `AbsentRegrowing` site and exactly two `material.mushroom_cap` plus one `material.mushroom_leg` unit for a Large mushroom.

## Barrel supported-route root cause

Barrel work-position selection and movement replanning accepted any successful `NavigationPathfinder` result to an adjacent walkable cell. Walkability alone includes shaft gaps, vertical/depth traversal and cells whose floor has been partially excavated, so a resident could visually approach through air and hit without a full standing plane.

Barrel attack routing now requires:

- every path cell to have full authoritative actor support;
- the final adjacent work position to have full support;
- every transition in the attack route to be `TunnelTraversalKind.SupportedWalk`.

`VerticalClimb`, `ShaftGapTraverse`, `DepthTraverse`, unsupported cells and partially excavated support are rejected both during hover/command resolution and during later movement replanning. `BarrelAttackSurfacePlayModeTests` checks the supported/airborne path policy and verifies that a resolved demo attack position is adjacent to the barrel and stands over a full support cell.

## Verification

Repository quality/source-contract checks were run locally before publication. GitHub Quality and the hosted Unity workflow must be evaluated on the final PR head. A green Unity workflow does not count as runtime evidence when its licensed `Run Play Mode tests` step is skipped by the activation gate.
