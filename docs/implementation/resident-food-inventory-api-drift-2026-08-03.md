# Resident food inventory API drift fix — 2026-08-03

Status: `IMPLEMENTED IN BRANCH`; merge and Unity editor recompile pending.

Tracking: [#459](https://github.com/bageus/Dig/issues/459).

Authoritative design: [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md).

## Root cause

`DigTerrainWorkSession.ResidentFood.cs` retained names from an older inventory snapshot contract after the canonical inventory model had moved to:

- `ItemStackSnapshot.StackId` instead of `ItemStackSnapshot.Id`;
- `ItemLocationKind.AgentInventory` instead of `ItemLocationKind.ResidentInventory`.

Unity compiles runtime partials directly, while the hosted .NET solution does not compile this Unity-only file. The stale names therefore produced `CS1061` and `CS0117` in the Unity editor despite the ordinary Quality build being green.

## Correction

The automatic resident-food planner now uses the canonical inventory contract for carried-food selection, deterministic ordering, world-source identity and deduplication. No food, inventory, scheduling or observable gameplay rule changed.

`CampfireFoodUnityRuntimeContractTests` now reads the authoritative inventory declarations and rejects reintroduction of the stale aliases.

## Verification boundary

Required evidence for merge:

- architecture and Unity source-contract gates;
- Release build and full .NET suite;
- headless smoke and deterministic soaks;
- Unity editor recompile without the four reported errors.

Actual Play Mode food workflow evidence remains governed by the authoritative food specification and issue #459.
