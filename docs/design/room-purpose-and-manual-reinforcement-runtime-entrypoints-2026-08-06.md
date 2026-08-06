# Room-purpose and manual tunnel-reinforcement runtime entry points

Status: `APPROVED`.

Date: 2026-08-06.

Tracking issue: [#660](https://github.com/bageus/Dig/issues/660).

System index: [`../systems/README.md`](../systems/README.md).

Related authoritative specifications:

- [`room-purposes-upgrades-and-tunnel-reinforcement.md`](room-purposes-upgrades-and-tunnel-reinforcement.md);
- [`service-markers-and-tunnel-overwrite-correction.md`](service-markers-and-tunnel-overwrite-correction.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md);
- [`universal-forced-resident-order-replacement.md`](universal-forced-resident-order-replacement.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md).

This correction records the latest user-confirmed observable behavior. It overrides the older exclusion of a manual exact-inventory reinforcement mode, but does not remove ordinary item placement or the existing low-priority automatic tunnel reinforcement jobs.

## 1. Room-purpose entry point

- Every eligible completed room projects one small interaction point centered above the room bounds.
- The point exists only as the room-purpose entry point; it is not a Job marker and does not create a second room identity.
- LMB on the point opens the room-purpose mode for that authoritative `RoomId`.
- The mode offers the approved room types from the room-purpose specification and projects the selected type, requirements, blocked reason and current upgrade state.
- Closing/cancelling the mode changes no room state.
- Reopening the point reads the current authoritative room state rather than preserving Presentation-only selection.

## 2. Ordinary placement remains available

- Selecting a mushroom leg or stone and using the ordinary item-placement command continues to place that exact item as an ordinary world stack in a valid cell.
- Entering reinforcement placement requires the explicit `B + LMB` chord and must never replace ordinary placement routing without `B`.
- Hover and click must resolve the same exact selected stack identity/version.

## 3. Manual reinforcement chord

When at least one living resident is selected and an eligible exact world/inventory item is targeted:

- hold `B` and press LMB on `material.mushroom_leg` to enter wooden-support placement;
- hold `B` and press LMB on `material.stone` to enter stone-reinforcement placement;
- the command is a forced resident order and therefore uses the common direct-command replacement boundary only after target and route validation succeeds;
- the exact selected material stack is reserved for the command; material is consumed only when the resident completes the reinforcement;
- cancellation, invalid target, unreachable target or failed preflight preserves the item and the resident's current valid command.

## 4. Ghost and target rules

### Mushroom leg

- The placement ghost is a wooden tunnel support.
- It is valid only in a supported horizontal tunnel cell accepted by the authoritative tunnel-infrastructure rules.
- It must not create an ordinary loose leg at completion; ordinary leg placement remains the non-`B` path.

### Stone

- The placement mode derives the ghost from the hovered valid tunnel topology:
  - horizontal tunnel floor: stone floor reinforcement ghost;
  - horizontal/vertical junction: junction support/trim ghost.
- Invalid cave, room, unsupported, occupied or duplicate reinforcement targets project a blocked ghost/reason and cannot commit.
- The exact completed reinforcement becomes tunnel geometry and follows the existing destructive topology-overwrite policy.

## 5. Input priority and repeat behavior

- `B + LMB` reinforcement routing has priority over ordinary LMB item pickup/use/placement only while `B` is held and the selected exact item supports reinforcement.
- Without `B`, existing pickup, use and ordinary placement capabilities remain authoritative.
- After one successful placement, the mode remains active only while the same selected stack still has available quantity; otherwise it exits safely.
- Repeated clicks cannot create duplicate jobs, duplicate reservations or two reinforcements for one target.
- Multiple residents are resolved through normal forced-order and exclusive target/material reservation rules.

## 6. State ownership, save/load and diagnostics

- Room state remains owned by the room aggregate and its existing codecs.
- Item identity, quantity and reservation remain owned by Inventory.
- Tunnel target, pending/completed reinforcement and provenance remain owned by tunnel infrastructure state.
- JobSystem owns worker assignment and lifecycle; Presentation owns only point/ghost/cursor projection.
- Save/load restores pending manual reinforcement, exact material reservation, worker ownership and room-purpose state without duplicate consumption or completion.
- Diagnostics expose resident, material stack, target cell, reinforcement kind, blocked reason and cancellation reason.

## 7. Acceptance

- campfire production regression is independently covered and is not coupled to this UI implementation;
- room point appears above every eligible room and opens the correct room-purpose mode;
- ordinary mushroom-leg/stone placement still works without `B`;
- `B + LMB` on a mushroom leg creates a wooden-support ghost and one validated forced order;
- `B + LMB` on stone projects floor or junction reinforcement from topology;
- invalid/rejected placement preserves current command and material;
- successful work consumes exactly one selected material and commits exactly one reinforcement;
- cancel/retry, duplicate target, multi-resident conflict and save/load are covered;
- licensed Unity Play Mode must execute room point -> purpose mode and both ordinary-placement/reinforcement workflows before status becomes `VERIFIED`.
