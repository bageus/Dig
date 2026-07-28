# Unity cave-room compile drift — 2026-07-28

Status: `IMPLEMENTED` pending Unity Editor confirmation.

Tracking: [#388](https://github.com/bageus/Dig/issues/388), parent [#87](https://github.com/bageus/Dig/issues/87).

## Symptoms

After PR #489 was merged, Unity Safe Mode reported:

- `CS0246` in `DigTerrainWorkExcavationQuarters.cs`: `CaveRoomExcavationTarget` was used without importing its authoritative `Dig.Application.World` namespace;
- `CS1739` in `DigTerrainWorkSession.PartialCompletion.cs`: the call to `PublishTerrainCompletionEffects` used a stale named argument `producedOutput`, while the current private runtime contract accepts the fourth `bool` positionally;
- `CS0122` in `CaveRoomRuntimeRecoveryPlayModeTests.cs`: the separate Play Mode assembly directly referenced the internal `DigCaveTemplateTrimRenderer` type and its internal `InstanceCount` property.

The .NET solution build does not compile Unity runtime or Play Mode assemblies, and the licensed Unity Test Runner step may be skipped, so cross-assembly source errors require explicit source-contract coverage.

## Fixes

- `DigTerrainWorkExcavationQuarters.cs` imports `Dig.Application.World` and therefore resolves the existing authoritative `CaveRoomExcavationTarget` type without adding a duplicate model;
- partial cave-room completion calls `PublishTerrainCompletionEffects(jobId, cell, tick, false)` using the current runtime signature;
- the cave-room Play Mode fixture resolves `DigCaveTemplateTrimRenderer` from the runtime assembly, creates it through `AddComponent(Type)`, and invokes internal members through the existing reflection-test boundary;
- production visibility is unchanged: the renderer and diagnostic counters remain internal;
- `UnitySafeModeApiDriftContractTests` and `PlayModeRuntimeVisibilityContractTests` lock these API and assembly boundaries.

No gameplay rule, save contract, output policy, navigation behavior or production API visibility changed.

## Verification

Required before promotion beyond `IMPLEMENTED`:

- Quality and full .NET tests;
- Unity source-contract validators;
- Unity Editor script compilation with zero errors;
- licensed Play Mode execution when activation credentials are available.
