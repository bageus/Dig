# Cave monster combat vertical slice — 2026-08-02

Status: `IMPLEMENTED` in draft [PR #562](https://github.com/bageus/Dig/pull/562); full enemy hierarchy remains `QUESTIONNAIRE` in [#559](https://github.com/bageus/Dig/issues/559).

Authoritative design: [`../design/enemy-combat-and-cave-encounters.md`](../design/enemy-combat-and-cave-encounters.md).

## Implemented workflow

- fresh demo composes two stable `enemy.vuker` actors on supported lower-cave cells before input;
- click targets the existing creature identity and no longer creates a hidden placeholder combatant;
- existing Combat intent/execution/resolution owners drive approach, adjacent-edge melee, retaliation, cooldown, target loss and death cleanup;
- enemy actors use common tunnel Navigation, including vertical and depth traversal;
- Inventory equipment selection is data-driven through `ItemId -> WeaponProfileId`; current content maps `weapon.club` to one-handed combat and falls back to fists;
- the same held item reference uses `HeldItemPurpose.WeaponUse`, so the rendered weapon is not a duplicate;
- offensive skill scales hit chance and damage; Defense reduces incoming damage; confirmed non-miss actions grant offensive/Defense skill exactly once;
- resident and hostile creature projections expose compact combat-only Health bars from authoritative Health.

## Starting balance

- resident Health 100; cave monster Health 70;
- fists: 60% accuracy, 5 damage, 2-tick cooldown;
- club: 65% accuracy, 8.5 damage, 2-tick cooldown;
- cave bite: 70% accuracy, 6.5 damage, 3-tick cooldown;
- maximum offensive bonus: +25 percentage points accuracy and +40% damage;
- maximum Defense reduction: 30%;
- confirmed hit: +0.25 offensive point; received hit: +0.10 Defense point.

All values use deterministic fixed-point definitions.

## Regression coverage

- Domain tests cover enemy traversal/group definitions, data-driven resident weapon mapping, exact balance values, skill caps, deterministic damage composition and 95% hit cap;
- Application tests cover offensive plus received-hit Defense grants and replay idempotency;
- source contracts reject click-created combatants and require pre-seeded actors, Inventory weapon selection, autonomous intent and both Health-bar projections;
- checked-in Play Mode scenario covers fresh pair, club draw, approach/combat, both bars, damage and both skill grants.

## Verification boundary

Repository Quality, Release build/tests, smoke and deterministic soaks must pass for the final PR head. `VERIFIED` additionally requires the licensed Unity runner to execute the Play Mode scenario; a skipped Unity job is not runtime evidence.

## Remaining hierarchy work

Predatory vine ambush, swallower item ingestion/drop and spider wall/ceiling ambush remain unimplemented.

Q-ENEMY-001 is answered:

- the predatory vine is fully stationary after spawn;
- it has no horizontal movement, vertical climbing or depth/Z traversal;
- its legal initial attachment surfaces are a horizontal tunnel interior, a cave floor or a cave wall;
- cave-ceiling attachment is not allowed;
- it attacks from its current anchor without approach movement.
