# Living-material diagonal and cross-depth movement — 2026-08-01

Status: `IMPLEMENTED` in feature branch; licensed Unity EditMode/PlayMode evidence remains required.

Authoritative specification: [`../design/hamsters-and-grubs-ecology.md`](../design/hamsters-and-grubs-ecology.md).
Tracking issue: [#524](https://github.com/bageus/Dig/issues/524).

## Reported regression

Hamster and grub movement was restricted to a straight `X` line on one fixed `Z` layer. Adjacent navigation cells at another depth and diagonal cells were ignored.

## Root cause

- the ecology component resolver accepted only same-`Y/Z` `SupportedWalk` transitions;
- the movement planner treated the saved `-1/+1` direction as an exclusive `X` axis;
- Domain movement validation rejected every `Z` change;
- legacy `PlaneKey` values represented the former per-depth component and ordinary reconciliation would have treated a component-key change as a new drop/release.

## Implemented correction

- connected movement regions use cardinal `SupportedWalk` and `DepthTraverse` transitions at constant `Y`;
- `VerticalClimb`, `ShaftGapTraverse` and explicit traversal links are excluded;
- candidates include orthogonal `X/Z` and diagonal `X±1/Z±1` steps;
- diagonal candidates require both orthogonal side cells and both legal two-edge routes;
- radius is Chebyshev distance in `X/Z`;
- deterministic planner selection includes depth-only and diagonal candidates while preserving horizontal facing;
- boundary recovery reverses horizontal direction instead of remaining trapped in depth-only oscillation;
- hamster resident steering uses same-height `X/Z` distance;
- a stale save/component key is rebound without release dormancy, movement-credit reset or identity replacement;
- reproduction and population cap use the merged movement region across depth.

## Regression coverage

- region resolution joins legal depth cells and excludes vertical cells;
- open corners expose diagonal candidates;
- blocked orthogonal side/path removes the diagonal candidate;
- Domain accepts diagonal and depth steps and rejects `Y` changes and long jumps;
- planner accepts diagonal-only and depth-only candidate sets deterministically;
- hamster separation can choose a farther `Z` candidate;
- application movement visits another `Z` and produces diagonal steps while preserving Inventory identity/location;
- legacy plane-root reconciliation preserves activity, credit, direction and deterministic sequence;
- checked-in Unity Play Mode scenarios cover diagonal/depth acceptance, height rejection and corner shielding.

Actual Unity runner execution is still required before changing the system to `VERIFIED`.
