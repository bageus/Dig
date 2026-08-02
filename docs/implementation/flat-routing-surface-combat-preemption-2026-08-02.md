# Flat Routing, Surface Edges, and Combat Preemption — 2026-08-02

Status: IMPLEMENTED

Authoritative correction: `docs/design/flat-resident-routing-surface-and-combat-preemption-correction-2026-08-02.md`

Tracking: #386, #508, #559

## Reported behavior

- residents still appeared to choose wall-climbing or airborne crawling routes instead of walking around on a supported plane;
- solid protrusions at the left and right ends of the fresh demo surface created misleading climb/collision geometry;
- a resident could receive a hostile combat intent while work, Eat, Sleep, Leisure or Study/Learn continued to progress.

## Root causes

1. Typed path cost already preferred flat routes, but the demo surface itself stopped at `X=1` and `X=width-2`. The remaining surface-level boundary cells were solid end caps, so the world exposed climbable side geometry that contradicted the intended flat platform.
2. Autonomous enemy acquisition ran inside the later combat movement loop. Resident autonomy had already advanced food/need actions for the tick, and terrain job systems could advance after combat intent creation because there was no shared combat interruption boundary.
3. `AgentState` exposed targeted blocking but no general interruption operation for untargeted Work/Leisure/Study actions.

## Implementation

- `TunnelNavigationVolume.CreateDemo` now opens/supports the surface through `X=0..width-1` on every depth layer.
- Existing lexicographic path cost remains authoritative: shaft-gap count, vertical-climb count, movement cost/step count, deterministic tie-break.
- `IAgentActionExecutionGate` lets an execution adapter gate food and schedule action progress while passive need decay continues.
- `DigResidentNeedsRuntime` synchronizes enemy acquisition before action execution, detects active/incoming resident combat, and invokes the terrain interruption boundary.
- `AgentState.InterruptActiveAction` interrupts targeted or untargeted actions with a stable reason and existing `AgentActionBlocked` event.
- `DigTerrainWorkSession.InterruptResidentForCombat` releases facilities, interrupts meals/actions, invokes existing typed job cleanup transactions, removes routes, and saves the authoritative repositories.
- Combat preemption does not call direct-order disengage and therefore retains the resident self-defense intent.

## Regression coverage

- `DemoSurfaceNavigationTests`: edge support/connectivity and longer flat detour over shorter climb.
- `AgentCombatActionGateTests`: general action interruption and closed action gate.
- `CombatPreemptionUnityRuntimeContractTests`: early threat synchronization, action/meal interruption, and typed job cleanup wiring.

## Verification boundary

The branch contains source-level and executable .NET regressions. Repository CI and licensed Unity EditMode/PlayMode execution have not yet been observed for this head. Do not mark the correction `VERIFIED` until the checked-in runtime scenario executes successfully.
