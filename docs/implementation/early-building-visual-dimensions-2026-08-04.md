# Early building visual dimensions — 2026-08-04

Status: `IMPLEMENTED`.

Authoritative specification: [`../design/representative-building-visual-dimensions.md`](../design/representative-building-visual-dimensions.md).  
Tracking issue: [#620](https://github.com/bageus/Dig/issues/620).  
Implementation PR: [#623](https://github.com/bageus/Dig/pull/623).  
Merge commit: `3c5ca3b54a88c340f5efc01997355336dbf1756a`.

## Change

Separate Presentation-owned visual bounds were added to the representative building profile:

```text
VisualBoundsCenter
VisualBoundsSize
```

They no longer reuse `footprintSize`. `BuildingDefinition.Footprint` remains the only owner of placement, occupancy, support and Navigation blocking.

## Profiles

- `building.tent`: `3.0 × 2.0 × 2.0`; two mirrored wedge roof halves, groundsheet, entrance flap and ridge pole.
- `building.stone_mason`: `3.5 × 2.5 × 2.5`; stone foundation, masonry hall, roof, workbench and cut-stone props.
- `building.wood_workshop`: `2.5 × 2.0 × 2.0`; wooden foundation/frame, two roof halves, saw bench and timber log.

All three current content definitions keep their authoritative `1×1` logical footprint.

## Runtime

- representative selection collider is generated from declared visual bounds;
- completed and Z1–Z3 final-building ghost resolve the same completed profile;
- BuildingBox and Packing continue to use compact crate geometry based on logical footprint;
- Assembly scaffold scales from visual bounds while preserving the existing assembly progress contract;
- built-in fallback pack contains the same three profiles as the JSON resource pack;
- profile kinds are explicit: `Tent`, `StoneMason`, `WoodWorkshop`.

## Regression coverage

`check_unity_building_representative_contracts.py` validates:

- all three stable ids and profile kinds;
- exact declared dimensions;
- grounded visual bounds;
- unchanged `1×1` logical footprint;
- required silhouette part names;
- anchors and renderer/triangle budgets;
- runtime use of visual bounds for selection colliders.

`EarlyBuildingVisualDimensionsContractTests` validates JSON dimensions, silhouettes, Domain footprints and source contracts.

`EarlyBuildingVisualDimensionsPlayModeTests` is checked in for actual renderer bounds, floor grounding, selection collider dimensions, silhouette parts, logical-footprint separation and compact BuildingBox geometry.

## Executed validation

PR head `349a8cd6410c8c3a752ead603091fe94316ca700`:

- Quality run `30896466884`: architecture/file-size/C# compatibility checks, Unity source contracts and presentation gates passed;
- Release build passed;
- `.NET`: `1485/1485` tests passed;
- headless smoke passed;
- standard deterministic soak passed;
- large-settlement deterministic soak passed;
- Export Stage 2 v2 run `30896466868` passed;
- Export Stage 2 v3 run `30896466840` passed.

Unity workflow `30896466474` completed through the blocked-evidence path. Licensed Unity activation was unavailable, therefore actual EditMode/PlayMode execution and runtime-evidence validation were skipped. The checked-in Play Mode scenario has not been executed in Unity, so the system remains `IMPLEMENTED`, not `VERIFIED`.

The unrelated system-index corrections were applied on `main` in commit `c765c6e5ba26b2bdb41c96608068bebbce012f5d` without changing feature code.
