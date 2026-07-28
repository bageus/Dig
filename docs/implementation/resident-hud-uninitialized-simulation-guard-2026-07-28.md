# Resident HUD uninitialized simulation guard — 2026-07-28

Status: `IMPLEMENTED` in branch `fix/resident-work-window-null-guard`; runtime verification pending licensed Unity Play Mode execution.

Authoritative system:

- [`../architecture/systems-core.md`](../architecture/systems-core.md#4-жители);
- [`agents-utility-ai.md`](agents-utility-ai.md#resident-hud-read-path).

Tracking: [#497](https://github.com/bageus/Dig/issues/497), parent system [#5](https://github.com/bageus/Dig/issues/5).

## Runtime symptom

The game clock refreshed a selected resident schedule through
`DigAgentSimulationDriverBase.TryGetResidentWorkWindow`. When the Unity driver existed but its `AgentSession` binding was unavailable, the HUD bridge dereferenced `AgentSession!` and emitted a repeated `NullReferenceException` from `DigAgentSimulationDriverBase.Hud.cs`.

The same bridge used the same unsafe assumption for automatic-planning queries, schedule/planning mutations, resident roster reads and society reads. Guarding only the first visible stack frame would therefore move the failure to the next HUD projection.

## Root cause

The Unity HUD adapter treated a non-null `DigAgentSimulationDriver` component as proof that all runtime sessions were already bound. Unity lifecycle transitions, partial startup and editor/runtime reload boundaries can temporarily leave the component alive while its non-serialized session fields are unavailable.

Presentation reads are not authoritative mutations and must represent this state as an unavailable projection instead of throwing.

## Correction

`DigAgentSimulationDriverBase.Hud.cs` now:

- exposes `IsHudReady` from the actual `AgentSession` and `TerrainSession` bindings;
- returns an empty society/roster projection while the bridge is unavailable;
- returns `false` with deterministic `24 / 0 / 12` schedule defaults;
- returns `false` with automatic planning defaulted to enabled;
- returns the existing typed `unity.agent_simulation.not_initialized` failure for schedule and planning mutations;
- preserves the existing initialized code path without changing schedule, Utility AI or player-order behavior.

## Regression coverage

- `ResidentHudInitializationGuardContractTests` locks the null guards, deterministic defaults, typed mutation rejection and executable Play Mode scenario.
- `ResidentHudInitializationGuardPlayModeTests` creates the real Unity simulation driver without initialization and invokes the actual HUD bridge. It verifies schedule/planning queries, society/roster reads, mutation failures, readiness and tick projection without an exception.

## Verification boundary

Source contracts, build and .NET tests prove the checked-in boundary. The system remains `IMPLEMENTED`, not `VERIFIED`, until a licensed Unity Test Runner executes the Play Mode regression and publishes result artifacts.
