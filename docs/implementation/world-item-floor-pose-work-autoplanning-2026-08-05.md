# World-item floor pose and work-time autoplanning — 2026-08-05

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md`](../design/world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md).  
Tracking issues: [#648](https://github.com/bageus/Dig/issues/648), [#650](https://github.com/bageus/Dig/issues/650).

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

## Verification boundary

Repository and CI evidence must be appended after execution. Until licensed Unity Play Mode actually runs the visible scenarios, the correction is not `VERIFIED`.
