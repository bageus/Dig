# Campfire production regression and room/reinforcement entry points — 2026-08-06

Status: `IMPLEMENTED IN BRANCH`; room/reinforcement runtime implementation pending.

Authoritative corrections:

- [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md);
- [`../design/room-purpose-and-manual-reinforcement-runtime-entrypoints-2026-08-06.md`](../design/room-purpose-and-manual-reinforcement-runtime-entrypoints-2026-08-06.md).

## Campfire regression root cause

The work-time AUTO correction changed `CreateResidentNeedsContext` so `Work` was available only while at least one global Job remained `Available`. Production synchronization assigns a campfire job before the resident utility decision. After assignment the job becomes `Claimed`, no global `Available` job may remain, and the assigned resident was therefore allowed to select `Idle` instead of continuing the authoritative production work.

This split the Job owner from the utility candidate projection and stalled the full campfire route after a successful assignment.

## Campfire correction

`DigTerrainWorkSession.HasAvailableAutomaticJob` now first checks whether the current resident owns a `Claimed` or `InProgress` Job. Owned work keeps the Work intent available independently of later schedule/AUTO transitions. Only acquisition of a new unowned Job remains gated by Work schedule and AUTO ON.

`CampfireProductionAutoplanningRegressionTests` protects the ordering and ownership contract so future item, schedule or UI changes cannot reintroduce the stall by looking only at global `Available` jobs.

## Room and tunnel entry-point audit

The repository contains the tunnel infrastructure aggregate, automatic wooden-support/junction jobs, material consumption, renderer and tests. It does not contain a runtime room-purpose interaction point or the confirmed exact-item `B + LMB` manual reinforcement entry point. Older issue status and references therefore overstated runtime availability.

The new authoritative correction separates:

- ordinary item placement without `B`;
- exact-item manual reinforcement with `B + LMB`;
- existing automatic tunnel reinforcement;
- room-purpose mode opened by a point above an eligible room.

## Verification boundary

The branch requires Quality, Release build/full .NET tests and deterministic smoke/soak. Licensed Unity Play Mode is required before claiming that the campfire route, room point, ordinary placement and both reinforcement ghosts work in the actual game.
