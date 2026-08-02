# Vuker PlayMode lifecycle projection API drift — 2026-08-03

Status: `IMPLEMENTED IN BRANCH`.

Tracking issue: [#569](https://github.com/bageus/Dig/issues/569).

Authoritative specification: [`../design/vuker-reproduction-questionnaire.md`](../design/vuker-reproduction-questionnaire.md).

## Reported failure

Unity compilation stopped in `VukerReproductionPlayModeTests.cs` with CS1061 at the child visual lifecycle assertion.

## Root cause

`CreatureVisualSnapshot` exposes the authoritative presentation property as `LifecycleStage`. The checked-in PlayMode fixture still referenced the removed compatibility name `Lifecycle`, so the Unity test assembly could not compile even though the runtime implementation and the non-Unity source-contract suite were green.

## Correction

- changed the PlayMode assertion to `childVisual.LifecycleStage`;
- retained the existing expected value `CreatureLifecycleVisualStage.Child` and all birth, no-combat, kidnapping, movement and maturity assertions;
- strengthened `VukerReproductionUnityRuntimeContractTests` so the fixture and `CreatureVisualSnapshot` declaration must agree on `LifecycleStage`.

No ecology, combat, input, navigation, save/load or observable gameplay rule changed.

## Verification boundary

Required automated evidence:

- Release build;
- full .NET suite including the new source contract;
- architecture and Unity source-contract gates;
- headless smoke and deterministic soaks.

The actual Unity EditMode/PlayMode Test Runner remains required before changing the system status to `VERIFIED`. A hosted workflow that records blocked evidence because Unity activation is unavailable is not runtime execution.
