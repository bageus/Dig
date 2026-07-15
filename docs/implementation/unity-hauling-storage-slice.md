# Unity hauling and storage slice

## Ownership

The Unity host composes the existing systems without copying their state:

- Inventory owns world stacks, stored stacks and quantity reservations.
- Storage owns stockpile filters, capacity and incoming-capacity reservations.
- Buildings/Production/Construction own typed material demands.
- World/Exploration owns explored/revealed state used by hauling eligibility.
- Jobs owns hauling stages, worker assignment and worker reservations.
- Navigation owns paths to the source stack and destination.
- Unity owns rebuildable route, stockpile and HUD visuals only.

The terrain completion handler and hauling handlers use the same Inventory and Job repositories.

## Current implemented slice

The currently implemented demo planner:

1. finds any unreserved world stack;
2. reserves source quantity and stockpile capacity;
3. creates a hauling job to the demo stockpile;
4. completes the existing `AcquireItem -> TravelToDestination -> DepositItem` stages.

This behavior proved quantity/capacity reservations and the end-to-end cycle, but it is **not** the final game rule.

## Target game rule

The generic «collect every unreserved stack» planner must be replaced/guarded by demand and visibility eligibility.

A world stack is a candidate only when:

1. an active building/production/construction demand requires its ItemId/category; or
2. a stockpile has an enabled collection filter for that ItemId/category;
3. the source is explored and not hidden by fog of war at planning time;
4. source quantity, destination capacity and path are available;
5. the source is not a protected inventory such as a sentry post or resident inventory.

A mined output with no demand/filter remains on the ground. The miner does not automatically place it in personal inventory.

Detailed design: `docs/design/material-demand-and-hauling.md`. Implementation issue: #110.

## Target fixed-tick workflow

1. Read active typed demands and enabled storage filters.
2. Query revealed, reachable, unreserved world stacks matching those requests.
3. Select source/destination deterministically by priority, path cost and stable IDs.
4. Atomically reserve source quantity, demand quantity and destination capacity.
5. Create an available hauling job.
6. Assignment claims a free resident.
7. Navigation moves through `AcquireItem` and `TravelToDestination`.
8. `DepositItem` moves quantity into the destination and releases all reservations.
9. Cancellation/block/failure releases demand, source, capacity and worker reservations.

After a job is claimed, visual hiding alone does not cancel it if source and route remain valid. Missing source, invalid reservation or path loss uses the normal blocked/retry flow.

## Presentation

- hauling paths are rebuildable visuals;
- storage shows stored and incoming reserved quantities;
- source diagnostics show demand/filter and visibility eligibility;
- fog rendering is not the source of truth for eligibility;
- item/stockpile visuals have no colliders that intercept cell selection.

## Diagnostics

Required reason codes include:

- `no_active_demand`;
- `storage_filter_disabled`;
- `hidden_by_fog`;
- `source_reserved`;
- `destination_full`;
- `path_unavailable`;
- `demand_cancelled`.

## Validation

Existing tests continue to cover reservations, assignment, stages, quantity conservation and cleanup.

New tests from #110 must cover:

- no hauling without demand/filter;
- building demand for a revealed mined item;
- enabled/disabled storage filter;
- hidden source at planning time;
- visual hiding after claim;
- competing demands;
- protected sources;
- Save/Load of demands/jobs/reservations;
- deterministic source selection.

Unity Play Mode remains a local validation step because CI does not launch the editor.
