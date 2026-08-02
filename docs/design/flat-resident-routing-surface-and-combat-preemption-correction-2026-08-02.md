# Flat Resident Routing, Surface Edges, and Combat Preemption Correction

Status: IMPLEMENTED

Tracking: [#386](https://github.com/bageus/Dig/issues/386), [#508](https://github.com/bageus/Dig/issues/508), [#559](https://github.com/bageus/Dig/issues/559)

## Authority

This correction refines the approved resident movement, combat spatial execution, enemy combat, and resident schedule/needs specifications. It supersedes any implementation behavior that selects a shorter climb while a supported flat route exists, leaves solid surface-level end caps at the demo world edges, or advances work/needs during an active hostile combat intent.

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

## Combat preemption

An active incoming hostile combat intent or resident self-defense intent is exclusive with resident work and schedule/needs actions.

Before food bites, targeted need intervals, generic schedule actions, movement-to-work, or job progress advance for the tick:

- autonomous enemy acquisition is synchronized;
- the targeted resident receives or retains a self-defense combat intent;
- active Eat, Sleep, Leisure, Study/Learn, Work, and other actions are interrupted with reason `combat_preempted`;
- already-applied progressive effects remain applied, but no additional interval is granted while combat is active;
- active food meals and Bed/facility reservations are released;
- assigned jobs use their existing typed cancellation/release transactions and all route plans are removed;
- combat execution advances instead of restarting the interrupted action in the same tick.

Passive need decay continues during combat. Explicit direct player disengage remains separate and is not invoked by combat preemption.

## Save/load and diagnostics

The correction adds no second combat, navigation, job, need, or reservation owner. Existing authoritative state and save formats remain unchanged. Interruption is observable through the existing action/job events with stable `combat_preempted` diagnostics.

## Acceptance

- Domain route regression proves a longer supported detour wins over a shorter climb.
- Fresh demo regression proves both surface edges are open, supported, connected, and remain on one Y plane.
- Domain/Application regression proves the autonomy action gate prevents food/action progress while preserving passive needs.
- Unity source contracts require combat acquisition before food/action execution and typed work cleanup.
- Checked-in Play Mode coverage must exercise a resident entering combat from Work and a need action, then prove no concurrent action/job progress.
- Status remains `IMPLEMENTED`, not `VERIFIED`, until the Play Mode scenario executes in a licensed Unity Test Runner.
