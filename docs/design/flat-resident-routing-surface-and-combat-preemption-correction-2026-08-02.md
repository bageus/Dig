# Flat Resident Routing, Surface Edges, and Combat Preemption Correction

Status: APPROVED

Tracking: [#386](https://github.com/bageus/Dig/issues/386), [#508](https://github.com/bageus/Dig/issues/508), [#559](https://github.com/bageus/Dig/issues/559), [#577](https://github.com/bageus/Dig/issues/577)

Latest sight-loss and post-task-pause correction: [`enemy-vision-and-resident-task-transition-pause-2026-08-03.md`](enemy-vision-and-resident-task-transition-pause-2026-08-03.md).

## Authority

This correction refines the approved resident movement, combat spatial execution, enemy combat, and resident schedule/needs specifications. It supersedes any implementation behavior that selects a shorter climb while a supported flat route exists, leaves solid surface-level end caps at the demo world edges, advances work/needs during an active hostile combat intent, or lets autonomous/self-defense combat replace an active direct player command.

The latest confirmed input-priority rule is absolute: a successful direct player command is the highest-priority resident instruction, regardless of the resident's current work, need action, combat stage, self-defense intent, alarm intent, or tactical retreat.

Enemy pursuit persistence is further narrowed by the 2026-08-03 correction: enemy acquisition and continued pursuit both require current sight range and valid LoS. A direct command does not itself cancel enemy combat, but enemy intent ends immediately if sight is lost.

## Resident route priority

Resident route selection is lexicographic:

1. fewer `ShaftGapTraverse` edges;
2. fewer `VerticalClimb` edges;
3. lower authoritative movement cost / fewer remaining steps;
4. stable `CellId` tie-break.

A longer supported `SupportedWalk`/`DepthTraverse` route therefore wins over a shorter route containing a wall climb. Climbing remains legal only when no lower-priority supported route reaches the target.

Every resident route producer must consume the same typed traversal policy. Work, production, supply, needs, pickup, direct movement, and combat adapters may not substitute shortest-cell routing.

## Flat demo surface

The fresh demo surface platform spans `X=0..width-1` on all four depth layers. Both edge cells are open, fully supported, and connected to the same surface navigation plane.

The cells at `X=0` and `X=width-1` on `SurfaceY` are not solid end caps. Presentation geometry must not create side protrusions or colliders that can be classified as a climb, push a resident off support, or display airborne crawling.

## Combat preemption without a direct command

When no direct player command owns the resident, an active incoming hostile combat intent or resident self-defense intent is exclusive with resident work and schedule/needs actions.

Before food bites, targeted need intervals, generic schedule actions, movement-to-work, or job progress advance for the tick:

- autonomous enemy acquisition is synchronized;
- the targeted resident receives or retains a self-defense combat intent;
- active Eat, Sleep, Leisure, Study/Learn, Work, and other actions are interrupted with reason `combat_preempted`;
- already-applied progressive effects remain applied, but no additional interval is granted while combat is active;
- active food meals and Bed/facility reservations are released;
- assigned jobs use their existing typed cancellation/release transactions and all route plans are removed;
- combat execution advances instead of restarting the interrupted action in the same tick.

Passive need decay continues during combat.

## Direct player command priority

A successful direct player command immediately replaces the resident's current behavior and remains authoritative until that command completes, is cancelled, fails terminally, or is replaced by a newer direct player command.

This rule applies to direct movement, excavation, pickup/use, mushroom/barrel actions, building placement/assembly/packing, production commands, and any future resident command routed through the common direct-command preparation boundary.

While a direct player command is active:

- active resident combat intent/execution, including `PlayerOrder`, `Alarm`, self-defense, autonomous combat, and retreat, is cancelled or suppressed for that resident;
- self-defense is not recreated merely because an enemy currently owns an incoming intent;
- Eat, Sleep, Leisure, Study/Learn, Work, and other autonomous actions do not progress in parallel;
- the direct command's own movement/job/action pipeline advances first;
- enemy pursuit and attacks may continue only while the enemy still sees the resident;
- losing sight completes enemy execution/intent with `enemy_target_out_of_sight`;
- after the direct command ends, self-defense may be recreated only from a currently visible threat.

A direct attack order is itself a direct player command and therefore replaces the previous direct command according to the same rule.

After a successful direct movement/job/action completes, the shared transition pause from the 2026-08-03 correction blocks the next ordinary automatic task for `AgentUtilityPolicy.DecisionCooldownTicks`. A newer direct command or critical emergency bypasses that pause.

## Save/load and diagnostics

The correction adds no second combat, navigation, job, need, or reservation owner. Direct-command priority is derived from the existing active manual movement/direct job owners plus the common preparation boundary. Existing authoritative state and save formats remain unchanged by the 2026-08-02 scope.

Combat interruption is observable through existing action/job events with stable `combat_preempted` diagnostics. Direct command replacement uses stable direct-command cancellation/replacement diagnostics and must not be reported as an autonomous combat decision. Sight loss and task-transition state are defined by the linked 2026-08-03 correction.

## Acceptance

- Domain route regression proves a longer supported detour wins over a shorter climb.
- Fresh demo regression proves both surface edges are open, supported, connected, and remain on one Y plane.
- Domain/Application regression proves the autonomy action gate prevents food/action progress while preserving passive needs.
- Direct-command regression proves a resident leaves self-defense/retreat and advances the ordered action.
- Sight-loss regression proves enemy intent ends when range/LoS is lost and self-defense is not recreated without a visible threat.
- Task-transition regression proves successful direct/automatic completion waits the shared cooldown before the next ordinary task.
- Unity source contracts require combat acquisition before food/action execution, typed work cleanup, direct-command priority, sight-owned pursuit and transition-pause wiring.
- Checked-in Play Mode coverage must exercise Work/need combat preemption, direct command priority, enemy sight loss and the post-command Idle pause.
- The authoritative status remains `APPROVED` while PR #575 is open. After merge and green automated checks it becomes `IMPLEMENTED`; it becomes `VERIFIED` only after the checked-in workflow actually executes in a licensed Unity Test Runner.
