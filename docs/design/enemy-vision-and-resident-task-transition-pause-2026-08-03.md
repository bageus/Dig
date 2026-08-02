# Enemy Vision Loss and Resident Task Transition Pause

Status: `APPROVED`

Tracking: [#577](https://github.com/bageus/Dig/issues/577), [#508](https://github.com/bageus/Dig/issues/508), [#559](https://github.com/bageus/Dig/issues/559), [#159](https://github.com/bageus/Dig/issues/159), [#113](https://github.com/bageus/Dig/issues/113)

## Authority

This correction records the latest confirmed rules for enemy pursuit and resident task transitions. It supersedes every older clause that lets `enemy.vuker` track a living resident after sight or World/Tunnel line of sight is lost, including persistent-current-cell pursuit described in `combat-spatial-execution.md`, `enemy-combat-and-cave-encounters.md`, and the 2026-08-02 combat-preemption correction.

It also extends the existing data-driven `AgentUtilityPolicy.DecisionCooldownTicks` contract from active-intent anti-oscillation to the observable pause after successful task completion. No second combat detector, task scheduler, or presentation timer is introduced.

## Enemy vision and pursuit

- Enemy target acquisition requires the species sight range and valid World/Tunnel line of sight.
- `enemy.vuker` keeps an autonomous attack intent only while its current target remains visible under the same range and LoS rules.
- When the current target leaves sight range or terrain blocks LoS, the enemy stops pursuit immediately.
- The active enemy execution and intent complete with typed reason `enemy_target_out_of_sight`.
- The enemy does not follow the target's current authoritative cell and does not travel to a last-known cell after sight loss.
- Patrol/idle becomes eligible again after combat ownership ends.
- A later visible hostile may create a new autonomous intent through the ordinary detector on a later combat evaluation.
- Target death/removal continues to use the existing terminal/retarget rules. A replacement target must itself be currently visible.

The enemy may continue approaching or attacking while the resident obeys a direct command only while the enemy still has valid sight. A direct command never cancels enemy intent by itself, but moving out of sight does.

## Direct-command priority

The existing absolute priority remains authoritative:

- a successful direct player command suppresses resident self-defense, Alarm, combat execution and Retreat while that command owns movement/job/action state;
- enemy attacks may still resolve while the enemy sees the resident;
- if the direct command moves the resident out of sight, the enemy intent ends with `enemy_target_out_of_sight`;
- after command completion, resident self-defense is recreated only if a currently visible hostile threat is detected again.

## Resident task-transition pause

A resident enters a short task-transition pause after a successfully completed task. The pause applies to:

- manual/direct movement reaching its destination;
- a direct or automatic assigned job reaching `Completed`;
- generic Work/Rest/Idle-policy actions reaching completion;
- targeted Eat/Sleep/Leisure actions reaching completion;
- a bite-based meal reaching its final bite.

The pause does not start for rejection, interruption, cancellation, blocked retry, death, or a task that never acquired authoritative ownership.

### Duration

The duration is `AgentUtilityPolicy.DecisionCooldownTicks`. The current default is `2` simulation ticks. With the authoritative demo cadence of `2.0` real seconds per tick, this is approximately `4` real seconds at normal playback. Speed controls change real-time presentation only; simulation duration remains two ticks.

### Priority during the pause

- Ordinary automatic Work, Eat, Sleep, Leisure/Rest and automatic job assignment do not start.
- The resident remains in `Idle` and may continue passive needs decay.
- A new successful direct player command bypasses the pause immediately.
- Critical survival/emergency and a newly detected combat threat bypass the pause.
- The pause ends deterministically when the configured cooldown ticks have elapsed.

The pause is a transition between completed tasks, not a minimum duration added to the task itself. It does not delay the terminal commit, item transfer, job completion, reservation release, or event publication.

## State ownership and events

- `AgentState` owns the last successful task-completion tick.
- The existing utility policy owns the duration.
- Job, movement, food and targeted-action owners report successful completion to `AgentState` exactly once.
- Automatic planners query the resident transition-pause fact before assigning new work.
- Presentation may display Idle/pause diagnostics but does not run or mutate the timer.

A typed task-transition event records resident id, completion tick and reason. Repeated reconciliation of the same completed job must not extend the pause or publish duplicate events.

## Save/load

The runtime snapshot exposes the last successful task-completion tick. Full active-needs/task persistence remains tracked by #101; when that save section is completed, the transition tick must round-trip so loading cannot remove or duplicate the remaining pause.

No current save migration is claimed by this correction unless the corresponding agent runtime state is already serialized by the owning save section.

## Diagnostics

Diagnostics expose:

- enemy sight range, LoS result and terminal `enemy_target_out_of_sight` reason;
- resident last task-completion tick;
- configured pause duration and remaining ticks;
- whether an ordinary automatic candidate/job was rejected by task-transition pause;
- whether direct order or critical/emergency behavior bypassed the pause.

## Acceptance

- Enemy acquisition and continued pursuit use the same sight-range and LoS condition.
- Losing range or LoS immediately completes enemy intent/execution with `enemy_target_out_of_sight`.
- Enemy does not pursue current or last-known target cells after sight loss and can resume patrol.
- Direct movement out of sight leaves enemy combat ended and does not recreate resident self-defense without a new visible threat.
- Generic, targeted and meal completion record one task-completion fact.
- Manual movement and assigned-job completion record one task-completion fact.
- Ordinary Utility AI candidates and automatic assignment wait `DecisionCooldownTicks`.
- Direct player orders and critical/emergency behavior bypass the pause.
- Rejected/interrupted/cancelled work does not create a successful-completion pause.
- Domain, Application, source-contract, deterministic and checked-in Play Mode regressions cover both workflows.
- Status becomes `IMPLEMENTED` after merge and green automated checks, and `VERIFIED` only after licensed Unity runtime execution.