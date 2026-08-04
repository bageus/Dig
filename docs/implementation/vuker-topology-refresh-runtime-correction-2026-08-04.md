# Vuker topology refresh runtime correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/vuker-reproduction-questionnaire.md`](../design/vuker-reproduction-questionnaire.md);
- [`../design/enemy-combat-and-cave-encounters.md`](../design/enemy-combat-and-cave-encounters.md);
- [`../design/ecology-creatures-and-special-drops.md`](../design/ecology-creatures-and-special-drops.md);
- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).

Tracking issue: [#638](https://github.com/bageus/Dig/issues/638).  
Correction PR: [#639](https://github.com/bageus/Dig/pull/639).

## Runtime report

Unity Console emitted:

`InvalidOperationException: A Vuker actor must occupy a supported connected cave cell.`

The exception originated in `DigAgentSession.ResolveVukerRegion` during the ecology tick after tunnel topology had changed.

## Root cause

`DigAgentSession.SynchronizeNavigation` replaced the authoritative `TunnelNavigationVolume` after excavation and rebuilt resident route planners. The Vuker connected-region resolver, birth planner and combat spatial execution handler still referenced the previous immutable volume.

A Vuker could therefore move legally into a newly excavated supported cell through the current movement volume. On the next tick, ecology resolved the actor position against the stale region map and threw before the rest of simulation advanced. Combat path planning had the same stale-volume ownership defect even when it did not immediately throw.

## Correction

- topology synchronization compares sorted open, vertical and supported cell sets;
- when topology changes, `VukerCaveRegionResolver` and `VukerBirthPlanner` are rebuilt from the new volume;
- the existing `CombatSpatialExecutionHandler` receives the same new volume through an explicit topology update method; repositories, intents, executions, equipment provider, event journal and policy remain authoritative and unchanged;
- repeated pointer/renderer synchronization with identical topology does not rebuild ecology or combat planning;
- existing `VukerEcologyState.SynchronizeActor` remains responsible for region-change pair break/re-pair behavior.

No gameplay or balance rule changed.

## Regression coverage

- `VukerTopologyRefreshRuntimeContractTests` guards the shared topology refresh boundary and checked-in runtime scenario;
- `VukerTopologyRefreshPlayModeTests.TopologyRefreshAllowsVukerInNewlyExcavatedSupportedCell` performs excavation, rebuilds navigation, places an existing Vuker in the new supported cell and advances the next ecology tick without an exception;
- the existing birth, child combat exclusion, kidnapping, taming, direct movement and maturity scenario remains unchanged.

## Validation

Local source validation passed:

- `tools/quality/check_quality.py`;
- `tools/quality/check_unity_source_contracts.py`;
- `tools/quality/check_unity_resident_visual_contracts.py`.

Exact code head `5a0d531dbe9634a31a3645fa80eb784e5c3cab6b` passed:

- Quality run `30946209884`;
- architecture, file-size, C# compatibility and all Unity source/presentation contracts;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1506/1506` passed;
- headless smoke completed at tick `20`;
- standard deterministic replay matched: `B26EA859F3F9668DF85CA1BA2842D8C733B09C51B596F4300549AEE7465D5292`;
- large deterministic replay matched: `7FD411B4725F7DADC5D355FEC5FB5159D59314CB25921394D9D8B27669EC51C9`;
- Stage 2 v2/v3 exports passed.

Unity workflow `30946210578` recorded blocked evidence: actual EditMode/PlayMode execution and executed-evidence validation were skipped because licensed activation was unavailable. The correction is `IMPLEMENTED`, not `VERIFIED`; actual licensed Unity Test Runner execution remains required.
