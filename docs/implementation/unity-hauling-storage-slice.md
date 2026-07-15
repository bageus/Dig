# Unity hauling and storage slice

## Ownership

The Unity host composes the existing systems without copying their state:

- Inventory owns world stacks, stored stacks and quantity reservations.
- Storage owns the stockpile definition and incoming-capacity reservations.
- Jobs owns hauling stages, worker assignment and worker reservations.
- Navigation owns paths to the source stack and stockpile cell.
- Unity owns rebuildable route, stockpile and HUD visuals only.

The terrain completion handler and hauling handlers use the same in-memory Inventory and Job repositories.

## Fixed-tick workflow

1. The hauling planner finds unreserved world stacks.
2. It reserves source quantity and stockpile capacity.
3. It creates an available hauling job.
4. The existing assignment handler claims a free resident.
5. Navigation moves the resident to the source during `AcquireItem`.
6. Navigation moves the resident to the stockpile during `TravelToDestination`.
7. At `DepositItem`, the existing completion handler moves the reserved quantity into Storage and releases Inventory, Storage and Job reservations.

Reserved quantities are unavailable to later planner passes. Stored stacks are not world-stack candidates.

## Presentation

- cyan lines show current hauling paths;
- the green base marks the stockpile cell;
- grey cubes represent stored quantity;
- an amber plate represents incoming reserved capacity;
- the HUD reports stored quantity, total capacity and incoming quantity;
- stockpile visuals have no colliders and cannot intercept selection.

## Validation

An engine-independent integration test covers planning, source and capacity reservations, worker assignment, all hauling stages, final stored quantity and complete reservation cleanup. The normal workflow also runs architecture checks, Unity C# 9 and module gates, Release build, all tests, headless smoke and standard/large deterministic soak profiles.

Unity Play Mode remains a local validation step because CI does not launch the editor.
