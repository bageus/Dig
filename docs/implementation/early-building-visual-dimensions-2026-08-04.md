# Early building visual dimensions — 2026-08-04

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/representative-building-visual-dimensions.md`](../design/representative-building-visual-dimensions.md).  
Tracking issue: [#620](https://github.com/bageus/Dig/issues/620).

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

## Validation

`check_unity_building_representative_contracts.py` now validates:

- all three stable ids and profile kinds;
- exact declared dimensions;
- grounded visual bounds;
- unchanged `1×1` logical footprint;
- required silhouette part names;
- anchors and renderer/triangle budgets;
- runtime use of visual bounds for selection colliders.

`EarlyBuildingVisualDimensionsContractTests` validates JSON dimensions, silhouettes, Domain footprints and source contracts.

`EarlyBuildingVisualDimensionsPlayModeTests` is checked in for actual renderer bounds, floor grounding, selection collider dimensions, silhouette parts, logical-footprint separation and compact BuildingBox geometry.

## Verification boundary

Repository Quality, Release build, full .NET suite, source contracts, headless smoke and deterministic soaks must pass on the PR head. A successful Unity workflow through blocked-evidence fallback does not count as executed Play Mode evidence; licensed Unity Test Runner execution is required before `VERIFIED`.
