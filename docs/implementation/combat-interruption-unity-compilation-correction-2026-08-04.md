# Combat interruption Unity compilation correction — 2026-08-04

Status: `IMPLEMENTED`; local Unity Editor compilation confirmation pending.

Authoritative specification: [`../design/combat-spatial-execution.md`](../design/combat-spatial-execution.md).  
Related production specification: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).  
Tracking issue: [#622](https://github.com/bageus/Dig/issues/622).  
Related issues: [#508](https://github.com/bageus/Dig/issues/508), [#612](https://github.com/bageus/Dig/issues/612).

## Report

Unity reported `CS7036` in `DigTerrainWorkSession.CombatInterruption.cs:68`. The combat path still used the old three-argument `InterruptProductionForDirectCommand` call after production interruption gained a required resident identity for recovery-cell resolution.

## Root cause

PR #616 corrected direct-command cancellation so carried production and supply materials recover to the resident cell. That changed the shared partial-class helper signature, but `InterruptResidentForCombat` was not updated. Repository Quality did not catch the mismatch because the normal .NET build does not compile Unity runtime sources and the existing source contract checked only the presence of the interruption method.

The same combat switch also lacked the specialized `BuildingSupplyJobDefinition` cancellation branch, so supply work would otherwise have fallen through to generic job release instead of restoring carried materials and clearing incoming/reservations through the authoritative supply handler.

## Correction

- production interruption now passes `resident.Id` before `tick`;
- building-supply interruption uses `CancelBuildingSupplyForDirectCommand(job, resident.Id, tick)`;
- the existing production/supply handlers remain the only owners of material recovery, incoming quantities and reservation cleanup;
- no combat priority, damage, targeting, production recipe, inventory capacity or save format rule changes.

## Regression

`CombatPreemptionUnityRuntimeContractTests` now verifies both helper declarations and both combat call shapes. This makes future signature drift fail the repository test suite even when Unity activation is unavailable.

## Verification boundary

Required before merge:

- architecture/file-size/C# compatibility and Unity source contracts;
- Release build and full test suite;
- headless smoke and deterministic soaks;
- Stage 2 source exports.

Actual Unity Editor compilation remains the final confirmation for the reported `CS7036` because the hosted Unity workflow may record blocked evidence when activation is unavailable.
