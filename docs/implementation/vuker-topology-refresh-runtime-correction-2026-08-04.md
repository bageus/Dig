# Vuker topology refresh runtime correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specifications:

- [`../design/vuker-reproduction-questionnaire.md`](../design/vuker-reproduction-questionnaire.md);
- [`../design/enemy-combat-and-cave-encounters.md`](../design/enemy-combat-and-cave-encounters.md);
- [`../design/ecology-creatures-and-special-drops.md`](../design/ecology-creatures-and-special-drops.md);
- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).

Tracking issue: [#638](https://github.com/bageus/Dig/issues/638).

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

## Local validation

Passed:

- `tools/quality/check_quality.py`;
- `tools/quality/check_unity_source_contracts.py`;
- `tools/quality/check_unity_resident_visual_contracts.py`.

The local container did not provide the `dotnet` executable. Full Release build, .NET suite, smoke and deterministic soak evidence must come from exact-head GitHub CI. Actual licensed Unity Test Runner execution remains required before `VERIFIED`.
