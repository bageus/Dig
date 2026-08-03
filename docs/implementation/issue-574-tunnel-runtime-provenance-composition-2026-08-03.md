# Issue 574 — runtime composition completed tunnel provenance

Status: `READY FOR REVIEW` in PR [#590](https://github.com/bageus/Dig/pull/590).

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

## Scope

This note records Slice 2B-2b2a: runtime projection of already-completed authoritative tunnel provenance, ordinary automatic-job assignment, worker movement and final support/trim commit.

The branch also restores the reviewed Slice 2B-2a and Slice 2B-2b1 commits from nested PRs #584 and #587 so they can be reviewed against current `main` in one mergeable PR.

No player-cancel behavior is introduced before `Q-TUNNEL-008` is answered. Arbitrary open terrain is never treated as infrastructure provenance.

## Runtime provenance projection

`TunnelRuntimeTopologyProjector` receives only:

- authoritative `WorldSnapshot` completed/open cells;
- completed `CaveRoomPlan` instances;
- `DigWorldSession.PlannedTunnelCells`;
- `DigWorldSession.PlannedVerticalTunnelCells`.

Projection rules:

- planned cells contribute only after authoritative World completion;
- completed room exits trace away from the room from the left/right base-tunnel boundary cells;
- a completed cell present in both horizontal and vertical provenance becomes a vertical-junction reset origin when it has a horizontal neighbour;
- reset origins partition corridors and deterministic ownership prevents reverse duplicate segments;
- stable segment identity is derived from `origin kind + origin XYZ + direction`, not input order or current corridor length;
- reordered provenance therefore preserves identity, while a solid gap truncates only completed geometry;
- arbitrary open cells absent from the planned/template provenance sets are ignored.

## Runtime composition

The existing `DigTerrainWorkSession` remains the runtime coordinator:

1. ordinary designation synchronization reconciles completed topology through `SynchronizeTunnelTopologyHandler`;
2. completed building footprints, revealed World cells and current tunnel-navigation cells feed the existing 20-cell range/source planners;
3. support and junction-trim jobs are synchronized before the ordinary `AssignAvailableJobsCommand` pass;
4. automatic jobs use the existing candidate provider and JobSystem assignment owner;
5. movement routes the assigned worker to the exact reserved source cell, then to the exact infrastructure target;
6. an unavailable route releases only the worker assignment through `ReleaseJobAssignmentHandler`; the job-owned material reservation remains available for reassignment;
7. JobSystem stages advance through the existing runtime loop;
8. Finalize invokes `CompleteTunnelAutomaticWorkHandler`, preserving exact material consumption, structural/decorative completion and exactly-once `+0.7` skill grant;
9. after authoritative excavation commits, topology reconciliation runs before world-item settlement and later pickup/reservation planning.

The existing Job overlay now projects support/trim target X/Y/Z. It does not create a second infrastructure state owner.

## Ownership

- `WorldState` and excavation/template runtime facts own completed terrain provenance.
- `TunnelInfrastructureState` owns registered segments, structural anchors, decorative targets and derived next-support targets.
- `JobSystem` owns automatic work lifecycle, stage and worker/position claims.
- `InventoryState` owns exact material identity, quantity and reservations.
- skill progression owns recipient validation and exactly-once grant identity.
- Unity runtime coordinates commands and projects read models only.

## Regression coverage

- completed vertical junction projects independent left/right segments;
- room exit owns the corridor to a junction without a reverse duplicate;
- solid planned gap truncates geometry and reordered input preserves stable segment id;
- runtime ordering protects `topology reconciliation -> job synchronization -> ordinary assignment`;
- post-excavation ordering protects `topology reconciliation -> item settlement`;
- movement and final completion remain wired to existing authoritative handlers;
- automatic tunnel work projects its exact XYZ target in the Job overlay.

## Unity Safe Mode namespace regression — 2026-08-03

A local Unity import exposed three `CS0246` errors in `DigTerrainTunnelInfrastructure.cs` for `TerrainWorkRoutePlan`, `AdvanceJobCommand` and `ReleaseJobAssignmentCommand`.

Root cause: the Unity runtime partial used three authoritative contracts owned by `Dig.Application.Jobs`, but did not import that namespace. The normal .NET solution build does not compile the Unity runtime assembly, so the missing import was not detected by the previous validation run.

Correction:

- import `Dig.Application.Jobs` in `DigTerrainTunnelInfrastructure.cs`;
- retain the existing route, stage-advance and assignment-release owners without changing observable behavior;
- extend `TunnelInfrastructureUnityRuntimeContractTests` to require the authoritative namespace and all three symbol usages.

This is a compile-contract correction only. Tunnel target selection, material reservation, movement, stage advancement, interruption and completion behavior are unchanged.

## Validation

Passed on code head `d430b065baf6ff5ba4fc86958f62cb4faf47bbae`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency and Domain-boundary checks;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1435/1435`;
- topology projector, reconciliation, execution and Unity-composition regressions;
- headless smoke completed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity activation was unavailable. `Run Unity EditMode and PlayMode tests` and executed-runtime-evidence validation were skipped; the workflow recorded blocked evidence only. Runtime `VERIFIED` is not claimed.

The 2026-08-03 namespace correction has source-contract coverage on branch `agent/fix-tunnel-infrastructure-unity-imports`; repository CI and a local Unity recompile remain required before claiming the screenshot errors are cleared in the Editor.

## Deliberately remaining

- completed wooden-support and junction-stone-trim world visual renderer/projection;
- tunnel-infrastructure save-document section, migration and runtime automatic-job sequence restoration in Slice 3;
- actual licensed Unity Play Mode end-to-end evidence;
- player cancellation until `Q-TUNNEL-008` is answered.
