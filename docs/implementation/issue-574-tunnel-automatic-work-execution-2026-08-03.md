# Issue 574 — automatic tunnel work execution

Status: `READY FOR REVIEW` in stacked PR [#584](https://github.com/bageus/Dig/pull/584).

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Dependency: PR [#582](https://github.com/bageus/Dig/pull/582).

## Scope

This note records Slice 2B-2a only. It completes confirmed automatic support/trim jobs without choosing defaults for open questionnaire decisions.

Implemented:

- final commit accepts only `TunnelAutomaticWorkJobDefinition` in `InProgress/Finalize` with an authoritative assigned worker;
- the source must remain resolved, in its original world cell, with the expected item identity and one exact Inventory reservation owned by the job;
- the target is revalidated against current `TunnelInfrastructureState` immediately before mutation;
- wooden support consumes one `material.mushroom_leg`, commits a completed structural anchor and advances the rolling target from that cell;
- junction trim consumes one `material.stone`, commits decorative completion and does not become structural protection or an anchor;
- the final job stage completes and JobSystem claims are released;
- the worker receives `70` fixed-point units (`+0.7`) in Woodworking for support or Stonework for trim;
- skill idempotency uses automatic job identity;
- stale target, missing reservation or changed source rejects before material, infrastructure or skill mutation;
- terminal replay cannot consume material or grant skill a second time.

## Ownership

- `InventoryState` owns exact source identity, reservation and material consumption.
- `TunnelInfrastructureState` owns support-anchor or junction-trim completion.
- `JobSystem` owns worker/position claims and terminal job state.
- Skills owns worker validation, capacity redistribution and idempotency.
- `CompleteTunnelAutomaticWorkHandler` coordinates the cross-owner commit after complete preflight validation.

## Deliberately deferred

- topology reconciliation from completed excavation/template-room provenance;
- runtime movement and per-stage execution composition;
- Unity visual projection and actual Play Mode workflow;
- tunnel-infrastructure save-document section and migration;
- player cancellation until `Q-TUNNEL-008` is answered.

## Regression coverage

- support completion consumes one reserved leg, registers cell 10 as anchor, derives cell 20 as next target and grants Woodworking once;
- junction completion consumes one reserved stone, removes the pending decorative target and grants Stonework once;
- stale support target rejects without material or skill mutation;
- changed Inventory reservation rejects without infrastructure commit;
- replay after completion cannot consume or grant again.

## Validation

Passed on code head `a7b4c479c3aee8647412057371568fb9efa0a5c8`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency and Domain-boundary checks passed;
- Release build passed with `0` warnings and `0` errors;
- full .NET suite passed: `1416/1416`;
- all four automatic-work execution regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity activation was unavailable. The workflow recorded blocked runtime evidence, while actual EditMode/PlayMode execution and runtime evidence validation were skipped. Unity runtime verification is not claimed by this implementation note.
