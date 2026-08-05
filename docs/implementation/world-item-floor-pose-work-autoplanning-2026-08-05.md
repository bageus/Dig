# World-item floor pose and work-time autoplanning — 2026-08-05

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md`](../design/world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md).  
Tracking issues: [#648](https://github.com/bageus/Dig/issues/648), [#650](https://github.com/bageus/Dig/issues/650).  
Implementation PR: [#655](https://github.com/bageus/Dig/pull/655).

## Root causes

- `DigWorldOverlayRenderer.RenderDynamic` still projected a `Building Footprint` overlay for every non-completed building lifecycle state.
- Loose world-item instances applied only stack yaw/lean and therefore authored tall tools and props remained upright.
- `AgentViewModel.IsAvailableForAutomaticPlanning` ignored the resident schedule, while Utility AI allowed Sleep during Work and could expose Work outside the work window.
- Unity needs context reported `workAvailable: true` even when no available Job existed.

## Correction

- the obsolete building-footprint overlay semantic, style, pool, root and render loop are removed;
- ordinary loose world items use one policy-owned 90-degree floor-rest rotation before geometry-derived grounding and collider calculation;
- BuildingBox remains an upright container, while internal stock reuses the same world-item projection;
- the existing clock mapping is fixed by contract: orange sectors are Work and blue sectors are rest/free;
- automatic Eat, Sleep and Rest are unavailable during Work;
- new Work intent and Unity automatic candidate production require both Work schedule and AUTO ON;
- outside Work no new automatic Job candidate is exposed;
- current Work remains eligible so an already-owned job is not cancelled by AUTO/schedule changes;
- Unity work context reports availability from the authoritative Job repository instead of a constant true value.

## Regression coverage

- `WorkTimeAutomaticPlanningDecisionTests` covers Work AUTO ON/OFF, no-job Idle, free-time gating and current-work continuity;
- `AgentDecisionSystemTests` covers work-time needs suppression and schedule transition from automatic Eat;
- `AgentAutomaticPlanningPresentationTests` covers schedule-gated automatic candidate projection;
- `WorldItemAutomaticGroundingPlayModeTests` covers flat ordinary tool pose, upright BuildingBox, floor contact and collider dimensions;
- `ClockScheduleMeaningPlayModeTests` covers orange Work and blue rest/free sectors;
- `Issue14OverlayPlayModeTests` covers a non-selectable building without a footprint root/marker;
- source contracts reject obsolete footprint ownership and enforce the shared item/schedule policies.

## Executed validation

Tested head `a057153d3597e219b821704320e498d7c2b97253`, which includes the synchronized authoritative system index and world-item specification:

- Quality run `31054660017` — success;
- architecture, file-size, C# compatibility, dependency and Unity source-contract gates — success;
- Release build — success;
- .NET tests — `1527/1527` passed;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Export Stage 2 v2 run `31054659976` — success;
- Export Stage 2 v3 run `31054660004` — success.

Unity workflow `31054659983` completed through the blocked-evidence path. Licensed activation was unavailable, so actual EditMode/PlayMode Test Runner and executed runtime-evidence validation were skipped. The checked-in Play Mode regressions therefore have not executed in licensed Unity.

This evidence report is the only change after tested head `a057153d3597e219b821704320e498d7c2b97253`; production code, tests and authoritative specifications are unchanged.

## Verification boundary

The implementation is `IMPLEMENTED IN BRANCH`, not `VERIFIED`. Licensed Unity Play Mode must still execute the visible loose-item pose/collider, clock colors, building-platform absence and repeated Work/AUTO schedule transitions.
