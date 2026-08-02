# Cave monster combat vertical slice — 2026-08-02

Status: `IMPLEMENTED` in merged [PR #562](https://github.com/bageus/Dig/pull/562); full enemy hierarchy remains `QUESTIONNAIRE` in [#559](https://github.com/bageus/Dig/issues/559). The Unity Play Mode named-argument compile regression is corrected in follow-up branch `fix/cave-combat-playmode-slot-argument`; licensed runtime evidence remains required. Patrol/vision/persistent-aggro/hover behavior and the Health-bar shader correction are implemented in follow-up PR from `agent/cave-monster-patrol-aggro-hover`.

Authoritative design: [`../design/enemy-combat-and-cave-encounters.md`](../design/enemy-combat-and-cave-encounters.md).

## Implemented workflow

- fresh demo composes two stable `enemy.vuker` actors on supported lower-cave cells before input;
- click targets the existing creature identity and no longer creates a hidden placeholder combatant;
- outside combat each cave monster performs a deterministic same-Y X/Z patrol within radius 6, one step every 4 simulation ticks, using common movement and tunnel traffic; this is slower than hamster/grub ecology wandering;
- sight range 6 plus tunnel LoS creates autonomous aggro;
- existing Combat intent/execution/resolution owners drive approach, adjacent-edge melee, retaliation, cooldown, target loss and death cleanup;
- cave-monster aggro is persistent: temporary sight loss, normal expiry, tactical retreat and retry exhaustion cannot make it voluntarily leave combat while the target remains available;
- a direct resident command cancels only the resident attack intent; the monster keeps its target and pursuit;
- enemy actors use common tunnel Navigation, including vertical and depth traversal in combat;
- Inventory equipment selection is data-driven through `ItemId -> WeaponProfileId`; current content maps `weapon.club` to one-handed combat and falls back to fists;
- the same held item reference uses `HeldItemPurpose.WeaponUse`, so the rendered weapon is not a duplicate;
- offensive skill scales hit chance and damage; Defense reduces incoming damage; confirmed non-miss actions grant offensive/Defense skill exactly once;
- resident and hostile creature projections expose compact combat-only Health bars from authoritative Health;
- hostile hover uses the creature highlight even without selected resident; sword cursor still requires a valid selected attacker;
- Health-bar materials prefer the authored lightweight `Dig/Stylized Unlit` shader, avoiding runtime compilation of the full package URP Unlit shader that produced `out of memory during compilation` in the reported Unity console.

## Starting balance

- resident Health 100; cave monster Health 70;
- fists: 60% accuracy, 5 damage, 2-tick cooldown;
- club: 65% accuracy, 8.5 damage, 2-tick cooldown;
- cave bite: 70% accuracy, 6.5 damage, 3-tick cooldown;
- maximum offensive bonus: +25 percentage points accuracy and +40% damage;
- maximum Defense reduction: 30%;
- confirmed hit: +0.25 offensive point; received hit: +0.10 Defense point.

All values use deterministic fixed-point definitions.

## Unity Play Mode compile correction

The local Unity compiler reported `CS1739` in `CaveMonsterCombatPlayModeTests.cs`. The fixture called `ItemLocation.InResidentSlot` with named argument `index`, while the authoritative Domain signature is `slotIndex`.

Correction:

- the Play Mode fixture uses `slotIndex: 2`;
- production and Inventory behavior are unchanged;
- `CombatSpatialUnityRuntimeContractTests` requires the fixture and `ItemLocation.InResidentSlot` signature to stay aligned, preventing the same Unity-only compile drift from returning.

The regular .NET solution build does not compile the Unity Play Mode assembly, so actual Unity compilation or the source-contract guard is required for this boundary.

## Regression coverage

- Domain tests cover enemy traversal/group/patrol/sight definitions, deterministic flat patrol, stationary no-patrol constraints, data-driven resident weapon mapping, exact balance values, skill caps, deterministic damage composition and 95% hit cap;
- Application tests cover offensive plus received-hit Defense grants and replay idempotency;
- source contracts reject click-created combatants and require pre-seeded actors, Inventory weapon selection, patrol, persistent aggro, direct disengage binding, hover highlight and both Health-bar projections;
- source contracts also bind the cave-combat Play Mode slot argument to the authoritative `ItemLocation.InResidentSlot(..., int slotIndex)` signature;
- checked-in Play Mode scenarios cover fresh pair, slow patrol, hover highlight, sight aggro, asymmetric disengage, club draw, approach/combat, both bars, damage and both skill grants.

## Verification boundary

Repository Quality, Release build/tests, smoke and deterministic soaks must pass for the final follow-up head. `VERIFIED` additionally requires the licensed Unity runner to compile and execute the complete Play Mode scenario; a skipped Unity job is not runtime evidence.

## Remaining hierarchy work

Predatory vine ambush, swallower item ingestion/drop and spider wall/ceiling ambush remain unimplemented.

Q-ENEMY-001 is answered:

- the predatory vine is fully stationary after spawn;
- it has no horizontal movement, vertical climbing or depth/Z traversal;
- its legal initial attachment surfaces are a horizontal tunnel interior, a cave floor or a cave wall;
- cave-ceiling attachment is not allowed;
- it attacks from its current anchor without approach movement.
