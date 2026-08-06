# Room-purpose and manual reinforcement runtime entry points — 2026-08-06

Status: `IMPLEMENTED IN BRANCH`; runtime verification pending.

Authoritative design: [`../design/room-purpose-and-manual-reinforcement-runtime-entrypoints-2026-08-06.md`](../design/room-purpose-and-manual-reinforcement-runtime-entrypoints-2026-08-06.md).

Tracking issue: [#660](https://github.com/bageus/Dig/issues/660).

Pull request: [#661](https://github.com/bageus/Dig/pull/661).

## Implemented scope

- authoritative stable `RoomId` registration and room-purpose state;
- one room-purpose marker above each eligible completed room;
- room-purpose HUD for `None`, `Bedroom`, `Kitchen/Dining`, `Workshop` and `Farm`;
- explicit `B + LMB` routing before ordinary item interaction;
- exact resident-inventory mushroom-leg/stone source validation and reservation;
- wooden support, stone floor trim and junction stone trim ghosts;
- owner-locked manual reinforcement Job creation, route, work, completion and cancellation;
- exactly-once material consumption and `+0.7` Woodworking/Stonework grant;
- completed stone floor trim projection and topology-overwrite cleanup;
- Domain/Application/Presentation source-contract regressions.

Ordinary item placement without `B` remains routed through the existing item-placement path. Automatic wooden supports and junction trims remain enabled and markerless.

## Validation boundary

Local repository validation before publication:

- `python tools/quality/check_quality.py` — passed;
- `python tools/quality/check_unity_source_contracts.py` — passed.

The source payload applied to branch head `908129cb77968b93e5c7e735b9d6d22131ebef39` only after an exact SHA-256 verification. Release build, .NET tests, smoke, soaks and Unity execution must be recorded on a later exact head before the implementation can leave draft or be called runtime-verified.

## Remaining scope

- integrate room-purpose state and pending manual reinforcement into the top-level save document/migration path;
- implement the separate room improvement order, delivery costs and bonuses owned by the broader room-upgrade specification;
- execute Unity Play Mode for room marker selection, ordinary placement, both reinforcement materials, invalid target, cancel/retry and repeated placement.
