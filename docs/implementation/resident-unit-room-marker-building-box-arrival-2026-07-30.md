# Resident unit inventory, cave-room marker and BuildingBox arrival corrections — 2026-07-30

Status: `IMPLEMENTED`; the original correction was incomplete and this follow-up repairs the remaining runtime ownership/order gaps. Licensed Unity Play Mode evidence remains required for `VERIFIED`.

Authoritative specifications:

- [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md);
- [`../design/runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md);
- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md);
- [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md).

Tracking: [#67](https://github.com/bageus/Dig/issues/67), [#87](https://github.com/bageus/Dig/issues/87), [#118](https://github.com/bageus/Dig/issues/118).

## Resident unit-per-slot inventory

The previous runtime deliberately consolidated compatible resident stacks and counted free quantity inside occupied stacks as pickup/hauling capacity. The confirmed rule now treats every ordinary item/material unit as a separate quantity-one entity in resident inventory.

The correction keeps `InventoryState` authoritative and changes all resident ingress paths:

- normalization splits legacy unowned multi-unit resident stacks into deterministic quantity-one identities;
- occupied slots never provide merge capacity;
- each slot claim has quantity one;
- world pickup, hauling and building supply materialize one quantity-one stack per claimed slot;
- Main free slots remain preferred before Cargo free slots; Weapon keeps its specialized accepted-category priority;
- direct moves into resident slots reject quantities other than one;
- world/building/storage locations may still aggregate quantities according to their own policy.

Reserved or held legacy multi-unit stacks are rejected by normalization rather than being split behind an active action owner.

## Cave-room marker and confirmation

Medium, Large and Tall presets retain the approved dynamic Stonework thresholds `20/40/60`. The demo previously created every resident without skills, which made only Small usable even though the room controls were visible. The first demo resident now starts with the highest approved Stonework threshold so all four room sizes can be exercised in the representative runtime without removing the progression rule.

After successful confirmation:

- interactive room preview is closed;
- the authoritative plan is applied;
- designation synchronization is explicitly invalidated and refreshed;
- persistent Dig markers are rendered from World designations rather than from the temporary preview.

The checked-in room regression applies Small, Medium, Large and Tall plans and requires at least one authoritative `CellDesignation.Dig` target for every confirmed plan.

## BuildingBox site arrival

The actual resident layout stores a carried box as `ItemLocation.InResidentSlot(...)`. Assembly and relocation policies still compared that value to the legacy unslotted `ItemLocation.InAgent(worker)`, so a worker could reach the work cell while the lifecycle reported that the box was not carried.

All BuildingBox execution, commit and route-state checks now use the shared resident ownership contract. On arrival at an assembly work cell the same authoritative box:

1. moves from the resident slot to `ItemLocation.InBuilding(buildingId)`;
2. disappears from the resident inventory projection;
3. removes the planned building ghost;
4. exposes the physical `ReadyToBuild` assembly visual at `0/3`;
5. advances through `1/3`, `2/3`, `3/3` and completed state on subsequent ticks.

No duplicate building identity or second roster row is introduced.

## Regression coverage

- resident normalization split and no-merge capacity tests;
- world pickup and hauling one-unit-per-slot transit tests;
- spill, diagnostics, presenter and expansion projection tests using physical unit stacks;
- BuildingBox Application policies and Play Mode arrival assertions for `AtSite`, site inventory and initial assembly visual;
- all cave-room presets leave authoritative Dig designations after confirmation;
- source contracts protect resident capacity, room refresh and BuildingBox slotted ownership.

## Follow-up root-cause correction — 2026-07-30

Runtime review after the first merged correction found three paths that its tests did not execute:

- the legacy selected-resident HUD loaded `ResidentInventoryViewModel` without running the same unit normalization used by the gameplay slot layout, so an old or compatibility quantity could still appear as one `×N` row;
- cave confirmation refreshed World and Jobs through the simulation driver, while temporary preview shutdown belonged to `DigWorldInteraction`; the persistent designation overlay was therefore not guaranteed to refresh before the preview disappeared;
- the BuildingBox Play Mode regression teleported a resident to the work cell. It did not execute placement work-position selection, Navigation, resident movement and the arrival transition in their real tick order. Placement could also substitute an unreachable side position whenever any legacy configured position was reachable.

The follow-up keeps the existing owners and removes those bypasses:

- both resident inventory projections call one shared `LoadNormalizedResidentInventory` path and neither HUD renders an aggregate quantity badge;
- `DigWorldInteraction` synchronously invalidates and projects World-owned cave designations before clearing the temporary room preview; the simulation driver no longer duplicates cursor ownership;
- building placement selects only an actually reachable configured/side work position;
- assembly arrival is advanced immediately after resident movement, before unrelated work systems can defer the transition;
- the new Play Mode scenario follows the natural route instead of teleporting the resident and asserts `Reserved -> AtSite`, physical `0/3`, site inventory ownership and removal from resident slots.

## Verification boundary

Normal Quality CI validates Release compilation, .NET behavior, source contracts, smoke and deterministic soak. The checked-in Unity Play Mode tests must execute on a licensed Unity runner before the visual/runtime workflows can be marked `VERIFIED`.
