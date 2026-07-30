# Hamsters and grubs living-material ecology

Status: `IMPLEMENTED`.

Authoritative specification: [`../design/hamsters-and-grubs-ecology.md`](../design/hamsters-and-grubs-ecology.md).
Tracking issue: [#524](https://github.com/bageus/Dig/issues/524).
Implementation PR: [#529](https://github.com/bageus/Dig/pull/529).

## Ownership

- `LivingMaterialEcologyState` owns stable creature identity, species, flat-plane anchor/current cell, deterministic direction/activity cadence, reproduction counters and cooldowns.
- `InventoryState` remains the only authoritative owner of item location, quantity-one identity, reservations and pickup/drop/building transfers.
- Navigation supplies supported same-`Y/Z` cells and traversal edges; Ecology derives connected flat-plane components and never commits vertical/depth traversal.
- Presentation consumes immutable Ecology and Inventory projections. Animation, tether transforms and creature meshes do not mutate gameplay state.

## Implemented vertical slice

- canonical `creature.hamster` and `creature.grub` unit-item content;
- inventory-first free/stored reconciliation and ordinary pickup/drop integration;
- 96-step ecology day with four deterministic substeps per simulation tick;
- hamster `0.8x` cadence, radius 6, resident steering, search/sleep and one-step post-drop dormancy;
- grub `0.65x` continuous crawl, radius 4 and resident overlap tolerance;
- stable-lowest hamster parent, self-reproducing grub, two successful cycles per individual and atomic cap 10 per species/plane;
- save format v12 with terrain-deposit `v10 -> v11` followed by living-material `v11 -> v12`, preserving both authoritative sections and deterministic continuation fields;
- creature activity projection, scale `0.25/0.20`, ordinary pickup collider proxy and two stable campfire tether slots;
- Domain, Application, save, source-contract and checked-in Unity Play Mode regression fixtures.

## Integration reconciliation

PR #529 is based on current `main` after deterministic terrain deposits and inventory/mushroom regression repairs. The save-version collision discovered during synchronization was resolved without dropping either owner: terrain deposits retain v11 and living materials advance the document to v12.

The final runtime and save source is checked into the feature branch; temporary payload and carrier workflow files were removed before validation.

## Evidence

Final Quality run on commit `c2b879a83de2e8f2d0e02f12f58c300cad4ebc6e` completed successfully:

- architecture, file-size and C# compatibility checks;
- Unity source, creature visual, runtime-evidence and native-field contracts;
- .NET restore/build;
- `1206/1206` tests;
- headless smoke;
- standard deterministic soak;
- large-settlement deterministic soak.

The Unity workflow completed successfully, but licensed EditMode/PlayMode execution was skipped by the activation gate. The checked-in Play Mode fixture is therefore source evidence only until a licensed runner executes it.

## Status boundary

The system is `IMPLEMENTED`: approved gameplay behavior is represented in authoritative owners and covered by automated Quality tests. It is not `VERIFIED` because actual licensed Unity Test Runner evidence is still absent.
