# Issue 574 — tunnel topology provenance reconciliation

Status: `READY FOR REVIEW` in stacked PR [#587](https://github.com/bageus/Dig/pull/587).

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Dependency: PR [#584](https://github.com/bageus/Dig/pull/584).

## Scope

This note records Slice 2B-2b1 only: the engine-independent Application reconciliation boundary for already-completed authoritative excavation/template provenance.

The implementation does not infer tunnel ownership from arbitrary open terrain. A future runtime adapter must provide stable completed facts from the existing World/excavation provenance owners.

## Implemented

- each completed direction supplies a stable segment id, origin kind, origin cell and exact ordered contiguous horizontal cells;
- topology identity is `origin kind + origin cell + direction`;
- exact repeated provenance is idempotent and produces no state or event version change;
- a new completed direction registers one segment through `TunnelInfrastructureState`;
- an absent direction system-cancels every associated automatic work job, releases Inventory reservations and removes the segment;
- extension or shortening replaces immutable segment geometry through existing Domain APIs while retaining completed wooden-support/door anchors whose cells still belong to the direction;
- completed junction stone trim survives extension of the authoritative vertical-junction direction;
- after geometry reconciliation, only wooden-support jobs whose derived target is obsolete are cancelled;
- stable segment-id drift and segment-id reuse for another topology direction reject before authoritative mutation;
- the complete desired provenance set is validated in a temporary `TunnelInfrastructureState` before cross-owner mutation.

## Ownership

- World/excavation provenance remains the source of completed room exits, junction origins and corridor cells.
- `TunnelInfrastructureState` remains the single owner of registered segments, anchors, decorative targets and derived support targets.
- `JobSystem` remains the owner of automatic work lifecycle and worker/position claims.
- `InventoryState` remains the owner of source identity and reservations.
- `SynchronizeTunnelTopologyHandler` coordinates add/update/remove after complete preflight.

## Regression coverage

- repeated completed provenance is idempotent;
- extending a segment preserves a completed support and derives the newly reachable next target;
- shortening a segment cancels an obsolete support job and releases its exact source reservation;
- removing a direction cancels all associated automatic jobs before removing the segment;
- completed junction trim survives geometry extension;
- stable topology identity drift rejects without mutation.

## Validation

Passed on code head `79aa2d2a3ff9ab239bfd2bd2455c70a85c43dcf3`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency and Domain-boundary checks passed;
- Release build passed with `0` warnings and `0` errors;
- full .NET suite passed: `1422/1422`;
- all six new topology-provenance regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak passed with replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents passed with replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity activation was unavailable. The workflow recorded blocked runtime evidence; actual EditMode/PlayMode execution and runtime evidence validation were skipped. Unity runtime verification is not claimed.

## Deliberately deferred to Slice 2B-2b2

- concrete runtime projection from completed `CaveRoomPlan` / `ExcavationTemplateInstance` and completed horizontal/vertical excavation provenance;
- composition of topology reconciliation followed by automatic support/trim synchronization;
- runtime movement and work-stage execution;
- Unity visual projection and actual Play Mode coverage.

Tunnel-infrastructure save-document persistence remains Slice 3. Player cancellation remains deferred until `Q-TUNNEL-008` is answered.
