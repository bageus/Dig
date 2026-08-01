# Internal stock, refill, route cost and production progress correction

Статус: `IMPLEMENTED` pending CI and licensed Unity Play Mode evidence.

Authoritative sources:

- [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md), issue #433;
- [`../design/resident-route-cost-and-run-correction-2026-07-31.md`](../design/resident-route-cost-and-run-correction-2026-07-31.md), issue #386;
- contextual input issue #390.

## Root causes

1. Supply planning was explicitly suppressed while any production order was active, so enabled stock stopped before capacity.
2. Internal stock was rendered as decorative proxy primitives without exact `StackId`; hover ignored those objects and click re-selected the first matching `ItemId` stack.
3. Search cost counted shaft gaps but not ordinary vertical climbs, so a shorter climbing route could beat a faster flat detour.
4. Resident action projection had no run state for ordinary unburdened movement.
5. Product icons projected queue and shortage state but no material-step progress.

## Implementation

- Refill remains enabled during production and uses `AvailableQuantity` to exclude production reservations.
- Internal stock projects actual stack/unit identity through ordinary `DigWorldItemVisual`; available units use normal hover/pickup cursor and exact-stack LMB, reserved units remain visible with disabled interaction.
- Hover and click share exact `StackId`; `ItemLocation.InBuilding` remains excluded from automatic supply sources.
- Navigation cost adds `VerticalClimb` count after shaft-gap count.
- Ordinary unburdened supported/depth movement projects `Run`; carrying/tired/climbing modes remain unchanged.
- Product view model and HUD project one progress segment per recipe material step.

## Deliberately deferred staged package runtime

The user-confirmed box/material workflow is recorded in the authoritative production design, but unfinished-package pickup, cancel/failure, save/load and occupied-output behavior remain Q-PROD-021..024. Runtime must not invent those observable rules.

## Verification target

Domain/application regressions cover continuous refill, route priority and progress. Source contracts and Play Mode fixtures cover exact internal-stack identity, shared item visuals, reserved-unit shielding and segmented HUD. Actual runtime status remains below `VERIFIED` until licensed Unity tests execute.
