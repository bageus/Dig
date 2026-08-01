# Campfire internal-supply dependency recovery — 2026-08-01

Status: `IMPLEMENTED` in bugfix branch; licensed Unity Play Mode evidence remains required.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md), [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md).
Tracking issues: [#433](https://github.com/bageus/Dig/issues/433), [#459](https://github.com/bageus/Dig/issues/459).

## Root cause

Two runtime gates could leave one source-unresolved `BuildingSupply` job permanently blocking a campfire:

- deferred resolution selected only the nearest available resident; an inventory-capacity failure was retried against the same resident every synchronization and no later candidate was attempted;
- mushroom extraction was tied to the next queued grilled-mushroom recipe, required zero current caps and was suppressed by any active production order. It therefore did not own continuous refill for enabled missing cap/leg stock.

A stale `Created` supply job made `HasNonTerminalBuildingSupplyJob` true, suppressing both ordinary world-source delivery and creation of a replacement extraction/dependency pair.

## Correction

- `BuildingSupplyDependencyPlanner` selects one highest-priority enabled missing cap/leg only when no revealed, reachable, unreserved world source already exists.
- Campfire dependency creation runs independently of queued recipe and active production, while retaining one pair per building.
- A `Large` mushroom chop creates one requested-unit deferred supply; additional cap/leg drops remain ordinary world sources for following batches.
- Deferred resolution tries all available residents in deterministic distance/id order.
- If a completed dependency has no remaining requested world quantity, the stale deferred supply is cancelled without incoming/source/slot reservations, allowing the next synchronization to replan.

## Regression coverage

- planner chooses missing cap first, falls through to leg when an eligible cap exists and respects disabled toggles;
- completed dependency without requested world output is classified stale;
- application retry can fail for a full first resident and resolve the same job with a later resident;
- Unity source contract requires candidate iteration, stale-output cancellation and removal of the active-production gate.
