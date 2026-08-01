# Resident HUD uninitialized simulation guard — 2026-07-28

Status: `IMPLEMENTED` in [PR #498](https://github.com/bageus/Dig/pull/498); runtime verification pending licensed Unity Play Mode execution.

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

## Follow-up startup-shell correction — PR #546

`InitializeStartup` creates the visual HUD shell and its `DigClockHoverDriver` before the world, residents and simulation driver are bound. If an earlier bootstrap stage fails, the hover driver remains active and used to call `RefreshClockInteractionFrame`, which immediately dereferenced `_simulation` and `_agentRenderer` through `RefreshClock`.

PR #546 makes the interaction-frame callback a no-op while `DigGameHudCanvas._initialized` is false. This preserves the startup status UI and original failure message without producing a secondary clock exception. The fully initialized clock path is unchanged. A checked-in Play Mode regression builds only the startup HUD and invokes the real clock interaction callback; the .NET source contract locks the initialization guard when licensed Unity execution is unavailable.

## Regression coverage

- `ResidentHudInitializationGuardContractTests` locks the null guards, deterministic defaults, typed mutation rejection, startup clock guard and executable Play Mode scenarios.
- `ResidentHudInitializationGuardPlayModeTests` creates the real Unity simulation driver without initialization and invokes the actual HUD bridge. It verifies schedule/planning queries, society/roster reads, mutation failures, readiness and tick projection without an exception.
- `DemoStartupRegressionPlayModeTests.Startup_hud_clock_hover_is_idle_before_runtime_binding` verifies the real startup shell remains exception-free before runtime binding.

## Validation evidence

PR head before this documentation-only commit: `6036b1fcce782b8b4e1d4573e84b1f8da20505c3`.

- Quality workflow `30403859585`: success;
- architecture, file-size, C# compatibility and all Unity source/presentation contracts: success;
- Release build: success with zero warnings;
- .NET tests: `1096` passed, `0` failed;
- headless smoke: success;
- standard deterministic soak: success;
- large-settlement deterministic soak: success;
- Stage 2 v2 export `30403859433`: success;
- Stage 2 v3 export `30403859516`: success.

Unity workflow `30403859438` completed successfully, but its licensed `Run Play Mode tests` step was skipped by the activation gate. The executable regression is checked in; no `VERIFIED` claim is made.

Follow-up PR #546 carries its own current validation evidence in the PR body. Until licensed Unity Test Runner evidence exists, the follow-up remains `IMPLEMENTED`, not `VERIFIED`.

## Verification boundary

Source contracts, build and .NET tests prove the checked-in boundary. The system remains `IMPLEMENTED`, not `VERIFIED`, until a licensed Unity Test Runner executes the Play Mode regression and publishes result artifacts.
