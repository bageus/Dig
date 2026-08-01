# Hamsters and grubs living-material ecology

Status: `IMPLEMENTED`; licensed Unity execution remains required for `VERIFIED`.

Authoritative specification: [`../design/hamsters-and-grubs-ecology.md`](../design/hamsters-and-grubs-ecology.md).
Tracking issue: [#524](https://github.com/bageus/Dig/issues/524).
Original implementation PR: [#529](https://github.com/bageus/Dig/pull/529).
Fresh-population correction PR: [#543](https://github.com/bageus/Dig/pull/543).

## Ownership

- `InventoryState` is the only authoritative owner of quantity-one item identity, location, reservations, fresh-demo seed units and pickup/drop/building transfers.
- `LivingMaterialEcologyState` owns the linked creature lifecycle, species, flat-plane anchor/current cell, deterministic direction/activity cadence, reproduction counters and cooldowns.
- Navigation supplies supported same-`Y/Z` cells and traversal edges; Application derives connected flat-plane components for seed, movement and reproduction.
- Presentation consumes immutable Ecology and Inventory projections. Animation, tether transforms and creature meshes do not mutate gameplay state.

## Original implemented vertical slice

PR #529 added:

- canonical `creature.hamster` and `creature.grub` unit-item content;
- inventory-first free/stored reconciliation and ordinary pickup/drop integration;
- 96-step ecology day with four deterministic substeps per simulation tick;
- hamster `0.8x` cadence, radius 6, resident steering, search/sleep and one-step post-drop dormancy;
- grub `0.65x` continuous crawl, radius 4 and resident overlap tolerance;
- stable-lowest hamster parent, self-reproducing grub, two successful cycles per individual and atomic cap 10 per species/plane;
- save format v12 with terrain-deposit `v10 -> v11` followed by living-material `v11 -> v12`;
- creature activity projection, scale `0.25/0.20`, ordinary pickup collider proxy and two stable campfire tether slots;
- Domain, Application, save, source-contract and checked-in Unity Play Mode fixtures.

## Fresh-world visibility defect

### Symptom

The Unity runtime initialized Ecology and invoked `DigCreatureRenderer`, but a fresh session displayed no hamster or grub.

### Root cause

`CreateDemoResidentInventory` registered `LivingMaterialContent.CreateItems()` in the item catalog but created no corresponding Inventory stacks. `InitializeLivingMaterials` only reconciled existing stacks, so the authoritative Ecology repository remained empty and Presentation correctly received an empty snapshot.

The original tests covered lifecycle/projection in isolation but did not execute the complete fresh bootstrap path.

## Fresh population correction

### Application planner

`LivingMaterialInitialPopulationPlanner` reads `NavigationSnapshot` and a stable set of cells unavailable for initial placement.

- it resolves the same connected `SupportedWalk` planes used by Ecology;
- plane and cell order is stable;
- occupied world-item cells are excluded;
- the Unity bootstrap also contributes all current cells of living residents to the unavailable set;
- two hamster are assigned distinct cells of one eligible plane;
- grub prefers another eligible plane;
- with only one suitable plane, grub uses a third distinct free cell;
- an incomplete `2 hamster + 1 grub` plan returns typed `ecology.initial_population.no_suitable_plane`.

### Unity bootstrap adapter

`DigTerrainWorkSession.InitializeLivingMaterials` now:

1. receives the current resident snapshot from `DigUnityBootstrap`;
2. creates the Ecology repository/handler;
3. checks whether Inventory already contains any canonical or legacy living-material unit;
4. obtains the current Navigation snapshot;
5. combines world-item cells with living-resident cells;
6. plans the initial population outside that occupied set;
7. prevalidates all three stable entity IDs and catalog entries;
8. adds two `creature.hamster` units and one `creature.grub` unit at authoritative world locations;
9. saves Inventory and publishes its events;
10. runs normal Ecology reconciliation;
11. supplies the resulting snapshots to the existing initial `DigCreatureRenderer` call.

Repeated initialization is a no-op because the Ecology repository is already present. Reconstructed sessions with any existing living-material Inventory unit skip seeding, so save/load or a population reduced by gameplay does not recreate missing individuals.

## Resident-overlap regression — 2026-08-01

After the first fresh-population correction, a user observed hamster presentation immediately at a dwarf/inventory position. The seed itself still committed `ItemLocation.InWorld`, but its candidate filter knew only about world-item stacks. It could therefore choose the exact current cell of a living resident, making the new creature overlap resident/inventory presentation at startup.

The follow-up correction passes the initial resident list into `InitializeLivingMaterials` and excludes every living resident cell before the first Inventory commit. It does not invent a post-spawn move or transfer: the creature is born in a different world cell, and only an explicit later pickup can change it to `AgentInventory`.

## Regression coverage

- `LivingMaterialInitialPopulationPlannerTests`:
  - hamster pair remains on one plane;
  - grub uses a different plane when available;
  - one-plane fallback uses a third distinct cell;
  - unavailable cells are excluded;
  - repeated planning is deterministic.
- `LivingMaterialUnityRuntimeContractTests` requires resident-aware bootstrap wiring, seed planner, stable IDs and Inventory world commit.
- `LivingMaterialEcologyPlayModeTests.FreshDemoSeedsTwoHamstersAndOneGrubAwayFromResidentsExactlyOnce` builds the real demo world/agents/terrain session, initializes living materials twice, requires the same three creature IDs, asserts no creature shares a living-resident cell and asserts no resident inventory slot contains hamster/grub.
- Existing movement, dormancy, vertical rejection, scale/activity and campfire tether scenarios remain in place.

## Save chain

The ecology section still enters the document through `save.v11_to_v12.living_materials`. Terrain output persistence subsequently advances the current document through `save.v12_to_v13.terrain_output_contract`; both authoritative sections remain preserved.

Fresh seed is bootstrap content, not a save migration. A restored Inventory/Ecology population is authoritative and is never supplemented during load.

## Evidence boundary

Original PR #529 Quality evidence passed architecture, file-size/C# compatibility, Unity source contracts, .NET build/tests, headless smoke and deterministic soaks. Licensed Unity EditMode/PlayMode execution was skipped by the activation gate.

The current PR #543 head must pass the same repository Quality pipeline. Until a licensed runner actually executes the checked-in fresh-demo, inventory quick-drop/placement and existing Ecology Play Mode scenarios, the system remains `IMPLEMENTED`, not `VERIFIED`.