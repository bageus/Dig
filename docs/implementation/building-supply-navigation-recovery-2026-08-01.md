# Building supply navigation and production-demand recovery — 2026-08-01

Status: `IMPLEMENTED` in bugfix branch; licensed Unity Play Mode evidence remains required.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Confirmed behavior

- internal stock accepts only data-defined stock item types and capacities for that workstation;
- ordinary refill uses visible, navigation-connected, unreserved world items;
- every non-terminal production order force-enables delivery for its recipe inputs, without changing stock priority or unrelated toggles;
- missing supported cap/leg may create the existing mushroom extraction dependency;
- a failed source route, blocked acquisition or exhausted supply job releases source reservations, resident slot claims and incoming capacity before later replanning.

## Root cause

The Unity synchronization passed every explored non-solid cell to `BuildingSupplyPlanner` as reachable. That set did not represent navigation connectivity to the workstation. A higher-priority item in a disconnected region could therefore be reserved and assigned. Route planning then retained a failed non-terminal `BuildingSupply` job. Because `HasNonTerminalBuildingSupplyJob` and `ActiveSupplyJobId` are building-wide gates, that one job suppressed every later ordinary refill and extraction dependency, including sources that were actually visible and reachable.

Blocked acquisition had the same persistence problem: `JobSystem.Block` releases job reservations, but building-supply source reservations, resident slot claims and incoming capacity have separate authoritative owners and require the supply cancellation transaction. No production synchronization recovery invoked that transaction.

The production queue also lacked the newly confirmed rule that required recipe inputs force their delivery toggles on.

A follow-up Unity compile regression was caused by `DigBuildingProductionRuntime.cs` referencing the new `Dig.Domain.Production.BuildingSupplyReachability` type without importing `Dig.Domain.Production`. The regular .NET solution build does not compile Unity runtime scripts, and the licensed Unity runner was blocked, so the missing namespace escaped the first CI pass.

## Correction

- `BuildingSupplyReachability` derives the eligible source/worker cells from the navigation region connected to the building work position.
- Production, ordinary supply, deferred supply and mushroom dependency candidates use that same connectivity boundary.
- A failed supply route immediately runs the authoritative cancellation transaction.
- Blocked or failed active supply jobs are reconciled on the next production synchronization and release all external reservation owners before replanning.
- `EnableProductionInputDeliveryCommand` force-enables stock rules referenced by all non-terminal orders; stock priorities are unchanged and unrelated toggles are untouched.
- Hamster remains opt-in on a fresh game, while a queued roasted-hamster order force-enables hamster delivery as a required input.
- `DigBuildingProductionRuntime.cs` imports `Dig.Domain.Production`, and the source-contract regression now requires that compile-safe namespace reference together with the reachability call.

## Regression coverage

- disconnected explored/open cells are excluded from connected supply reachability;
- production input delivery is enabled without changing priorities;
- cancelling a blocked supply releases source quantity, resident slot claims and incoming ledger;
- Unity source contracts reject the former explored/open reachability approximation and require recovery wiring;
- Unity source contracts require `DigBuildingProductionRuntime.cs` to import the namespace that owns `BuildingSupplyReachability`;
- checked-in Play Mode scenarios cover ordinary cap delivery and required-input toggle activation.

Actual Unity EditMode/PlayMode execution remains required before changing the system status to `VERIFIED`.
