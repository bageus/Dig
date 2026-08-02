# Issue 574 — automatic tunnel junction trim lifecycle

Status: `READY FOR REVIEW` in PR [#582](https://github.com/bageus/Dig/pull/582).

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

## Scope

This note records Slice 2B-1 only. It implements confirmed decorative junction-trim behavior without choosing defaults for the open questionnaire decisions.

Implemented:

- `TunnelInfrastructureState` owns pending and completed stone-trim state for vertical-junction cells;
- left/right horizontal chains with the same vertical-junction origin project one target, not two;
- the target owner is the stable lowest segment id and can rebind when one direction disappears;
- removing a segment system-cancels its non-terminal automatic tunnel jobs and releases exact Inventory reservations;
- the last removed junction direction removes pending/completed trim provenance;
- automatic stone-trim work uses ordinary priority `0` and the existing 20-cell XYZ Manhattan completed-building range;
- source selection reuses revealed, reachable, unreserved world-stack ordering by distance, cell and stack id;
- no source leaves one unresolved `Created` job without phantom reservations;
- an appearing stone source resolves the same job and makes it `Available`;
- completed trim removes the pending target and subsequent synchronization cancels stale work;
- `job.tunnel_automatic_work.v1` round-trip coverage now includes `JunctionStoneTrim`.

## Ownership

- `TunnelInfrastructureState`: unique junction target, completion provenance and segment topology mutation.
- `InventoryState`: stone identity, quantity and reservation.
- `JobSystem`: automatic job lifecycle and worker/position claims.
- Application handlers: cross-owner synchronization and system cancellation.

Stone trim remains decorative. It is not a structural anchor, does not reset the rolling wooden-support chain and does not protect against collapse.

## Deliberately deferred

- adapter from completed excavation/template-room provenance into segment registration/removal;
- automatic work execution, exact material consumption and `+0.7` skill commit;
- tunnel-infrastructure save-document section and migration;
- Unity composition, projection and Play Mode workflow;
- player cancellation until `Q-TUNNEL-008` is answered.

## Regression coverage

- split junction creates one stable target;
- completed target is idempotent and snapshot-restorable;
- owner rebind and last-side removal are deterministic;
- completed provenance is discarded when the junction no longer exists;
- no-source and source-appearance lifecycles preserve reservation invariants;
- segment removal cancels work and releases the source;
- trim completion cancels stale work;
- both automatic work kinds round-trip through the save codec.

## Validation

Passed on PR #582 final implementation head:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency and Domain-boundary checks;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1412/1412`;
- headless smoke completed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 exports passed.

Unity workflow recorded blocked runtime evidence: activation was unavailable, therefore actual EditMode/PlayMode execution and runtime evidence validation were skipped. No Unity runtime-verification claim is made for this slice.
