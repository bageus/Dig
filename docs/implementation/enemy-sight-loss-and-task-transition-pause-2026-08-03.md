# Enemy Sight Loss and Resident Task Transition Pause — 2026-08-03

Status: `APPROVED`

Authoritative design: [`../design/enemy-vision-and-resident-task-transition-pause-2026-08-03.md`](../design/enemy-vision-and-resident-task-transition-pause-2026-08-03.md)

Tracking: #577, #508, #559, #113. Implementation PR: #575.

## Reported behavior

- `enemy.vuker` retained a persistent autonomous attack intent after the resident left sight range or World/Tunnel line of sight, so it continued following the resident's current cell;
- after direct movement, pickup/use, or an automatically completed job, the resident could immediately acquire the next ordinary task without an observable transition pause.

## Root causes

1. `CombatSpatialExecutionHandler.Reevaluate` had an explicit persistent-aggro branch that updated the enemy execution to the target's current authoritative cell even when `IsVisible` was false.
2. Enemy acquisition used sight range and LoS, but continued pursuit did not consume the same condition at every execution stage.
3. Utility cooldown was derived only from `LastActionSwitchTick`; successful command/job completion had no authoritative transition fact.
4. Manual movement and the different job execution pipelines committed terminal state through their own owners, but none reported successful completion to `AgentState`.
5. Applying the pause to survival actions or every internal generic Work/Rest cycle incorrectly throttled settlement recovery/cadence. Those actions are not completed assigned jobs.

## Implementation

### Enemy sight ownership

- non-player combat acquisition and retargeting require current sight range and World/Tunnel LoS;
- a common pre-stage guard completes non-player execution and intent with `enemy_target_out_of_sight` before another Approach, wind-up, attack, recovery, or stage wait advances;
- autonomous enemies no longer follow the target's current cell or a last-known cell after sight loss;
- explicit player attack orders retain their existing last-known pursuit contract;
- patrol becomes eligible after the enemy intent is terminal.

### Resident task-transition ownership

- `AgentState.LastTaskCompletionTick` is the authoritative transition fact;
- `AgentTaskTransitionPauseStarted` records the stable reason and tick;
- `AgentSnapshot` exposes the completion tick to pure Utility AI decisions;
- `AgentDecisionSystem` blocks ordinary candidates during the next complete tick, while `Idle`, `PlayerOrder`, and critical candidates remain eligible;
- manual movement reports completion when its route reaches the destination;
- the Unity simulation driver captures assigned jobs before advancement and records residents whose same job reaches `Completed`;
- direct `PlayerOrder` action completion reports the same transition fact;
- Eat/Sleep/Leisure, final meal bites, and generic Work/Rest/Idle decision cycles do not report command/job completion;
- automatic job assignment reads the same `AgentState` pause through `DigAgentSession`, so no second timer is created in Terrain or Presentation.

The current `DecisionCooldownTicks = 2` boundary produces one full observable Idle tick: completion at `T`, ordinary task blocked at `T+1`, eligible at `T+2`. With the normal two-second simulation tick, the pause is approximately two real seconds.

## Priority and failure behavior

- a newer direct player command bypasses the pause;
- critical survival/emergency and newly detected combat bypass the pause;
- rejection, interruption, cancellation, retry blocking, and death do not create a successful-completion pause;
- terminal item transfer, job commit, reservation release, and event publication happen before the pause and are not delayed;
- repeated same-tick reconciliation is idempotent and does not extend the pause.

## Regression coverage

- `CombatSpatialExecutionTests.Enemy_aggro_ends_immediately_when_target_leaves_sight`;
- `EnemySightLossStageTests.Autonomous_approach_does_not_take_another_step_after_sight_loss`;
- existing player-order last-known pursuit regression remains unchanged;
- `AgentTaskTransitionPauseTests` covers ordinary Work rejection for one full tick, direct-order bypass, critical bypass, and same-tick idempotency;
- `CombatPreemptionUnityRuntimeContractTests` guards sight-loss reason, removal of persistent target tracking, and Unity task-transition wiring;
- `ResidentCombatPreemptionPlayModeTests` contains the full checked-in workflow for direct movement completion, Idle transition, movement out of sight, enemy disengagement, and no resident self-defense without a visible threat;
- headless soak protects survival/recovery cadence from accidental pause expansion.

## Verification boundary

The implementation remains `APPROVED` while PR #575 is open. It may become `IMPLEMENTED` after merge and successful Quality/build/test/smoke/soak evidence.

The checked-in Unity scenario is not `VERIFIED` until an actual licensed Unity Test Runner executes it. A green workflow with skipped EditMode/PlayMode steps is blocked evidence, not runtime verification.