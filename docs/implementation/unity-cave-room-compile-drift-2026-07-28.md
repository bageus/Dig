# Unity cave-room compile drift — 2026-07-28

Status: `IMPLEMENTED` pending Unity Editor confirmation.

Tracking: [#388](https://github.com/bageus/Dig/issues/388), parent [#87](https://github.com/bageus/Dig/issues/87).

## Symptom

After PR #489 was merged, Unity Safe Mode reported:

- `CS0246` in `DigTerrainWorkExcavationQuarters.cs`: `CaveRoomExcavationTarget` was used without importing its authoritative `Dig.Application.World` namespace;
- `CS1739` in `DigTerrainWorkSession.PartialCompletion.cs`: the call to `PublishTerrainCompletionEffects` used a stale named argument `producedOutput`, while the current private runtime contract accepts the fourth `bool` positionally.

The .NET solution build did not compile Unity runtime assemblies, and the licensed Unity Play Mode step was skipped, so these two cross-assembly/source-level errors were not detected before merge.

## Fix

- `DigTerrainWorkExcavationQuarters.cs` imports `Dig.Application.World` and therefore resolves the existing authoritative `CaveRoomExcavationTarget` type without adding a duplicate model;
- partial cave-room completion calls `PublishTerrainCompletionEffects(jobId, cell, tick, false)` using the current runtime signature;
- `UnitySafeModeApiDriftContractTests` now locks the namespace import, target type, effect-call shape and absence of the stale named argument.

No gameplay rule, save contract, output policy or navigation behavior changed.

## Verification

Required before promotion beyond `IMPLEMENTED`:

- Quality and full .NET tests;
- Unity source-contract validators;
- Unity Editor script compilation with zero errors;
- licensed Play Mode execution when activation credentials are available.
