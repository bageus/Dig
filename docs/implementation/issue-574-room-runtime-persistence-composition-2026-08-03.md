# Issue 574 — room runtime persistence and Unity composition

Date: 2026-08-03.  
Status: `READY FOR REVIEW`.  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).

## Scope

This slice implements the confirmed persistence and runtime-composition part of room upgrades without choosing defaults for `Q-ROOM-003` or `Q-ROOM-007`.

Included:

- save format v16 for `RoomInfrastructureState`;
- completed-room provenance persistence;
- active room job, Inventory reservation and JobSystem reservation integrity;
- deterministic room runtime job/transit-stack sequence;
- legacy v15 migration without phantom room identities;
- Unity completed-room synchronization, temporary stock planning, ordinary hauling assignment, movement and staged work execution;
- runtime capture/restore and post-terrain synchronization.

Not included:

- purpose bonuses or compact placement profiles;
- room world marker and HUD menu;
- partial upgrade visual projection;
- actual licensed Play Mode evidence.

## Authoritative save ownership

`RoomInfrastructureState` remains the only owner of:

- room upgrade order and lifecycle;
- requested/active purpose;
- required/delivered/consumed/released material ledgers;
- exact completed material-unit identities;
- temporary stock cell and active room job ids.

`InventoryState` remains the owner of physical stacks, locations and quantities. `JobSystem` remains the owner of job stages, worker/position claims and retry state. Save data records these owners together only for cross-owner integrity validation; it does not create a second gameplay state.

## Save v16

The save document now contains a room infrastructure section with:

- aggregate and per-room versions;
- stable room infrastructure and template-instance identities;
- template kind and lifecycle state;
- requested/active purpose;
- exact temporary-stock XYZ;
- ordered material ledgers;
- exact committed material units;
- active room job ids;
- completed-room provenance with exact ordered room cells;
- next deterministic room runtime sequence.

Load reconstructs the Domain aggregate first and then validates:

- provenance identity/template/kind parity;
- all room cells are inside the saved world and do not overlap another room provenance;
- active jobs exist, are non-terminal and belong to the room;
- work job room/cell matches the aggregate;
- work stock reservations equal `delivered - consumed` for every material;
- hauling jobs target the exact temporary stock and own the expected reservation;
- no orphan non-terminal room runtime job exists;
- next runtime sequence is above every persisted room job/transit-stack id.

Any mismatch rejects the load before the restored room runtime is published.

## Migration

`save.v15_to_v16.room_infrastructure` creates an empty room section for legacy saves.

The migration:

- never derives room identity from arbitrary open terrain;
- never invents orders, ledgers, purpose or stock;
- advances the next sequence above existing parseable room job/transit-stack ids;
- is idempotent through the existing migration pipeline.

## Unity runtime composition

`DigTerrainWorkSession` now:

- projects only completed template instances;
- rejects restored/current provenance drift;
- synchronizes room identities, temporary stock and jobs before ordinary assignment;
- reuses existing `InventoryState`, `JobSystem`, generic `HaulJobDefinition`, candidate assignment and skill-grant services;
- routes delivery jobs from exact source to exact temporary-stock cell;
- routes work jobs to the exact authoritative work cell;
- commits the next exact material unit and finalizes through Application handlers;
- releases worker/position and resident-slot claims on route failure while retaining job-owned material reservation;
- resynchronizes room infrastructure after completed terrain commits;
- captures/restores aggregate, provenance and deterministic sequence, then recomposes Application handlers.

## Regression coverage

Added coverage for:

- active aggregate/job/Inventory/JobSystem reservation round trip;
- exact sequence preservation;
- legacy v15 migration without phantom rooms;
- stale provenance and stale sequence atomic rejection;
- byte-stable builder → JSON → loader → builder round trip;
- Unity synchronization order, routing, stage execution and capture/restore source contracts;
- all existing migration pipelines reaching v16 idempotently.

## Validation

Code head: `9d55c5778c0d7667b21ed39507174b8efbb3f2c5`.

Quality run `30821792583`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency and Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1460/1460`;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity workflow `30821792579` recorded blocked evidence. Actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped because activation was unavailable. No runtime-verification claim is made.

## Remaining

Slice 4B-2b still requires:

- room world marker and input shielding;
- central HUD menu with upgrade count `0|1`;
- requested/active purpose and progress read model;
- partial improvement visual projection;
- actual Play Mode end-to-end workflow.
