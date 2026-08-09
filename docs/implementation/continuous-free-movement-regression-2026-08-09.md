# Continuous free movement regression — 2026-08-09

Authoritative design: [`../design/continuous-surface-movement.md`](../design/continuous-surface-movement.md).

## Regression

The earlier integration persisted precise resident/enemy destinations, but horizontal
route execution still committed whole approach and boundary poses. Living-material
ecology stored only `CellId`, and its renderer always projected the cell centre. The
playable result therefore still read as grid movement, especially for autonomous actors.

## Correction

- Navigation cells remain the coarse topology and invalidation boundary.
- `SurfacePoseSteering` advances an authoritative pose by at most 200 fixed-point units.
- Manual resident routes, automatic resident/enemy routes and final manual positioning
  use the same bounded steering rule.
- Hamsters and worms persist deterministic `SurfacePose` coordinates; old saves fall back
  to the floor centre.
- Creature presentation consumes authoritative surface coordinates and only interpolates
  between confirmed positions.

## Evidence

- domain regression coverage for bounded, arbitrary two-axis steering;
- presentation coverage for non-centre hamster/worm projection and legacy fallback;
- `python tools/quality/check_quality.py`;
- licensed Unity Play Mode verification remains required before `VERIFIED` status.
