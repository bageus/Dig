# Agent Session Authoritative Tick Regression

- Status: IMPLEMENTED
- Date: 2026-08-05
- Tracking issue: [#652](https://github.com/bageus/Dig/issues/652)

## Authoritative specifications

- [Unified Game Time And Action Cadence](../design/unified-game-time-and-action-cadence.md)
- [Game Time Scale Runtime Synchronization Correction](../design/game-time-scale-runtime-synchronization-correction-2026-08-03.md)
- [Runtime Needs, Supply, Sleep And Food Recovery](../design/runtime-needs-supply-sleep-food-recovery.md)

## Runtime failure

Unity runtime could throw `agents.tick_not_increasing` from `AgentAutonomySystem` when resident runtime state had already committed needs for a restored simulation tick.

## Root cause

`AgentState` correctly persists `LastNeedsTick` and rejects repeated or decreasing needs commits. `DigAgentSession`, however, owned a second private `_tick` counter that always started at zero and did not advance `SimulationState.Clock`. After reconstruction or load, the session counter could therefore be behind both the authoritative clock and resident runtime state.

## Implementation

- `DigAgentSession.Tick` now projects `SimulationState.Clock.Tick`.
- `DigAgentSession.Advance` advances the authoritative clock once and passes that tick to tunnel traffic and `AgentAutonomySystem`.
- The independent `_tick` field was removed.
- The strict `AgentState.AdvanceNeeds` guard remains unchanged.

## Regression coverage

- `ResidentNeedsUnityRuntimeContractTests` prevents reintroducing a private session tick.
- `ResidentNeedsRestoredTickPlayModeTests` restores the clock and resident needs to tick 10, executes the next session tick, and verifies continuation at tick 11 without an exception.

## Verification status

The code, source contract, and Unity Play Mode regression are committed on the fix branch. Automated test execution and representative-scene console validation are pending GitHub Actions or a Unity test run; this document does not mark the runtime behavior VERIFIED until those checks pass.
