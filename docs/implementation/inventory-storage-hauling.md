# Inventory, storage and hauling

## State ownership

`InventoryState` is the only authoritative owner of item stacks, quantities, quantity reservations and current locations. A stack has exactly one `ItemLocation`: world cell, resident inventory, building inventory, storage zone or equipped slot.

`StorageState` owns only storage definitions, acceptance filters, priorities and reserved incoming capacity. It does not copy the current contents of a zone. Occupied quantity is derived from an `InventorySnapshot` whenever capacity is evaluated.

`JobSystem` owns the hauling lifecycle and worker claim. A `HaulJobDefinition` stores the source stack, item id, exact quantity and typed destination, but it never mutates Inventory or Storage directly.

The automatic hauling planner is stateless. It reads immutable/current snapshots, chooses work deterministically and delegates creation to the existing hauling command handler. It never becomes a second owner of stacks, capacity or jobs.

## Item catalog and locations

The immutable `ItemCatalog` contains stable `ItemId` definitions with display name, maximum stack size, categories and tool classification. Storage filters may accept all items, explicit ids or categories.

Locations are typed values rather than strings:

- world cell;
- resident inventory;
- building inventory;
- storage zone;
- equipped by a resident.

Changing location is transactional. A full move updates the existing stack. A partial move splits a new stack id and decrements the source by the same quantity. The total item count therefore remains constant.

## Quantity reservations

Inventory reservations are per hauling job and per quantity. Several jobs may reserve different portions of one stack, but their combined reservation cannot exceed the stack quantity. Available-item queries expose only the unreserved remainder.

A reserved move consumes only the reservation owned by that job. Cancellation releases all remaining quantities owned by the job. Two workers cannot claim the same unit because the second hauling job cannot reserve unavailable quantity.

## Storage filters and capacity

A storage zone defines:

- a stable entity id;
- priority;
- capacity in item units;
- an acceptance filter.

Storage validates its own filter. Incoming capacity is reserved before item quantity, so a hauling job cannot be created for a full or rejecting destination. If any later step fails, the application handler releases both destination and item reservations.

Destination queries sort valid zones by descending priority and then stable id. Actual occupancy comes from Inventory; only future incoming quantities are stored by Storage.

## Automatic planning

`PlanHaulingHandler` scans available world stacks in stable location and stack-id order. For each stack it:

1. re-reads the current available quantity;
2. asks Storage for destinations accepting at least one unit;
3. selects the highest-priority destination with the stable id tie-break;
4. limits quantity by both unreserved stack quantity and available destination capacity;
5. creates the normal typed hauling job through `CreateHaulingJobHandler`.

A planning pass has an explicit maximum job count. Repeated passes see the reservations created by earlier passes, so they can plan only the remaining unreserved quantity and cannot assign one unit twice.

## Typed hauling lifecycle

Hauling uses the same authoritative `JobSystem` as digging, with the stages:

```text
AcquireItem -> TravelToDestination -> DepositItem -> Completed
```

Creation performs one application-level transaction:

1. validate the source stack and item definition;
2. reserve destination capacity in Storage;
3. reserve the exact item quantity in Inventory;
4. add and publish a typed hauling job.

Failure at any stage releases earlier reservations. Job claim still reserves the worker through the common Jobs reservation ledger, enforcing one active job per resident.

Finalization is permitted only at `DepositItem`. It moves the reserved quantity to the storage location, completes the job, releases worker reservations and releases incoming capacity. Cancellation releases item, destination and job reservations without moving quantity.

## Retry and reconciliation

A temporary block releases only the internal Jobs worker/position reservations. Inventory quantity and Storage incoming capacity remain attached to the hauling job, so retry does not reserve them a second time.

When the retry policy is exhausted, the job becomes failed and the hauling application handler releases all external reservations. The reconciliation handler also checks active hauling jobs against their source quantity and destination reservations. A mismatch fails the damaged job with a diagnostic reason and removes any remaining reservations.

Terminal jobs and reservation records whose job no longer exists are cleaned as orphans. Reconciliation is deterministic and returns an immutable report describing every failure or cleanup action.

## Tools and diagnostics

A tool is an item definition marked `IsTool`. Equipping requires a single unreserved item and changes its location to `Equipped` for the resident.

Application queries expose available stacks and valid storage destinations. Planner and reconciliation reports expose created jobs, skipped stacks, failed mismatches and orphan cleanup actions. Immutable snapshots expose stack quantity, reserved quantity, available quantity, location, zone capacity, incoming reservations and priorities for Unity inspectors and debug overlays.

## Validation

Tests cover:

- partial quantity reservation and overbooking prevention;
- repeated stack splits with total quantity conservation;
- transactional reserved moves;
- storage filter ownership and priority ordering;
- rollback when item reservation fails;
- cancellation releasing item and destination reservations;
- automatic planning priority, capacity and pass limits;
- repeated planning without duplicate quantity reservation;
- retry preserving external reservations;
- retry exhaustion releasing every reservation;
- reconciliation of damaged and orphaned reservation links;
- complete hauling preserving total quantity;
- tool equipment;
- the headless automatic multi-stack, multi-worker hauling scenario.
