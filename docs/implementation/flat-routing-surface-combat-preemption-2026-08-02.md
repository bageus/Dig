# Flat Routing, Surface Edges, Combat Preemption, and Direct Command Priority — 2026-08-02

Status: APPROVED

Authoritative corrections:

- `docs/design/flat-resident-routing-surface-and-combat-preemption-correction-2026-08-02.md`;
- `docs/design/enemy-vision-and-resident-task-transition-pause-2026-08-03.md`.

Latest implementation note: `docs/implementation/enemy-sight-loss-and-task-transition-pause-2026-08-03.md`.

Tracking: #386, #508, #559, #577.

## Reported behavior

- residents still appeared to choose wall-climbing or airborne crawling routes instead of walking around on a supported plane;
- solid protrusions at the left and right ends of the fresh demo surface created misleading climb/collision geometry;
- a resident could receive a hostile combat intent while Work, Eat, Sleep, Leisure or Study/Learn continued to progress;
- self-defense could be recreated after a direct command cancelled resident combat;
- enemy autonomous combat could continue following a resident after sight range or World/Tunnel LoS was lost;
- after successful direct or automatic world-task completion, a resident could immediately start the next ordinary task.

## Root causes

1. Typed path cost already preferred flat routes, but the demo surface itself stopped at `X=1` and `X=width-2`. The remaining surface-level boundary cells were solid end caps, so the world exposed climbable side geometry that contradicted the intended flat platform.
2. Autonomous enemy acquisition ran inside the later combat movement loop. Resident autonomy had already advanced food/need actions for the tick, and terrain job systems could advance after combat intent creation because there was no shared combat interruption boundary.
3. `AgentState` exposed targeted blocking but no general interruption operation for untargeted Work/Leisure/Study actions.
4. The initial preemption gate treated every incoming enemy intent as a reason to recreate resident self-defense. After `PrepareResidentsForDirectCommand` cancelled resident combat, the next autonomy evaluation could therefore take control back from the player's order.
5. Enemy acquisition required sight, but continued non-player pursuit explicitly tracked the target's current authoritative cell without sight.
6. Utility cooldown read only active-action switch time. Successful manual movement, job, targeted action, and meal completion had no shared authoritative transition fact.

## Implementation

- `TunnelNavigationVolume.CreateDemo` opens/supports the surface through `X=0..width-1` on every depth layer.
- Existing lexicographic path cost remains authoritative: shaft-gap count, vertical-climb count, movement cost/step count, deterministic tie-break.
- `IAgentActionExecutionGate` lets an execution adapter gate food and schedule action progress while passive need decay continues.
- `DigResidentNeedsRuntime` synchronizes enemy acquisition before autonomous action execution and checks direct-command priority before both autonomy and combat preemption.
- `AgentState.InterruptActiveAction` interrupts targeted or untargeted actions with a stable reason and existing `AgentActionBlocked` event.
- `DigTerrainWorkSession.InterruptResidentForCombat` releases facilities, interrupts meals/actions, invokes existing typed job cleanup transactions, removes routes, and saves authoritative repositories.
- `DigAgentSession.DirectCommandPriority` records the common direct-command preparation boundary, cancels resident combat intent/execution including Alarm/self-defense/retreat, and derives continued ownership from active manual movement or assigned direct work.
- a rejected command receives no invented grace interval: direct priority exists only while an authoritative manual movement or assigned job owner exists;
- while direct-command priority is active, generic needs/schedule actions are gated and resident self-defense cannot replace the command;
- enemy pursuit and attacks continue only while the enemy has current sight range and valid LoS;
- losing sight completes non-player execution and intent with `enemy_target_out_of_sight` before another Approach, wind-up, resolve, recovery, or waiting stage advances;
- `AgentState.LastTaskCompletionTick` and `AgentTaskTransitionPauseStarted` own the post-task transition fact;
- manual movement, completed assigned jobs, direct PlayerOrder action, targeted actions, and final meal bites report completion exactly once;
- ordinary Utility AI and automatic assignment wait one complete following tick; direct orders, critical survival/emergency, and combat bypass;
- generic Work/Rest/Idle decision cycles are cadence rather than completed world tasks and do not create artificial repeated pauses.

## Regression coverage

- `DemoSurfaceNavigationTests`: edge support/connectivity and longer flat detour over shorter climb.
- `AgentCombatActionGateTests`: general action interruption and closed action gate.
- `CombatSpatialExecutionTests`: player last-known pursuit remains explicit; autonomous sight loss ends intent.
- `EnemySightLossStageTests`: autonomous `Approach` cannot take another step after sight loss.
- `AgentTaskTransitionPauseTests`: one complete Idle tick, direct-order bypass, critical bypass, same-tick idempotency.
- `CombatPreemptionUnityRuntimeContractTests`: early threat synchronization, typed cleanup, direct priority, sight-loss and task-pause wiring.
- `ResidentCombatPreemptionPlayModeTests`: Work/Sleep preemption, direct movement priority, post-command Idle pause, movement out of sight, enemy disengagement, and no self-defense without a visible threat.

## Verification boundary

The authoritative status remains `APPROVED` while PR #575 is open. After merge and successful Quality/build/test/smoke/soak evidence, the implemented parts may become `IMPLEMENTED`.

Do not mark the workflow `VERIFIED` until the checked-in direct-command, sight-loss, combat-preemption, and post-task-pause scenarios actually execute in a licensed Unity Test Runner. A green workflow with skipped Unity tests is blocked evidence, not runtime verification.