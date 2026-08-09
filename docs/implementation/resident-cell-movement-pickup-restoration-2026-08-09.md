# Resident cell movement and pickup restoration — 2026-08-09

## Status

`IMPLEMENTED IN BRANCH`, pending executed Unity Play Mode verification.

Authoritative movement design: [`../design/continuous-surface-movement.md`](../design/continuous-surface-movement.md).
Related pickup correction: PR #682. Regression source: PR #683.

## Reported regression

Playable Unity showed residents spending several simulation updates changing pose inside a
cell while appearing to tread in place, followed by a fast visual transition into the next
cell. Direct world-item pickup could also reach the source area without transferring the
stack into resident inventory.

## Root cause

PR #683 inserted `SurfacePoseSteering.MoveTowards` microsteps into the runtime execution of
manual horizontal corridors, automatic horizontal corridors, final manual positioning and
same-cell spatial-work positioning. The coarse cell route therefore gained extra in-cell
simulation steps that were not part of the previously accepted runtime behaviour.

The atomic world-item transfer from PR #682 is still present. Pickup execution accepts a
resident in the source cell on a fully supported floor without requiring exact local U/V.
The regression is therefore corrected at the movement-to-source boundary rather than by
weakening inventory ownership or pickup completion rules.

## Correction

- manual horizontal corridor approach again commits the exit boundary pose directly;
- automatic resident/enemy corridor approach again commits the exit boundary pose directly;
- adjacent-cell crossing still commits the matching entry pose and records tunnel traffic;
- same-cell spatial work commits its required destination pose directly;
- final manual positioning commits the selected target pose directly and completes the
  movement order without additional 200-unit steering ticks;
- vertical traversal, surface traffic reservations and authoritative `SurfacePose` storage
  remain unchanged;
- hamster/worm free-surface movement and persistence from PR #683 remain unchanged;
- PR #682 atomic `World -> AgentInventory` pickup completion remains unchanged.

## Regression coverage

The source contracts are restored to require direct `exitPose`, `entryPose` and
`destination` commits instead of runtime `SurfacePoseSteering.MoveTowards` calls.

`ForcedPickupReplacementPlayModeTests` now additionally requires that on the first completed
simulation tick where a resident's authoritative cell equals a remote pickup source:

- the exact stack is already `AgentInventory`;
- the stack no longer appears in the world-item read model;
- the resident inventory layout contains that stack.

This protects the full route-arrival-pickup boundary instead of only testing pickup when the
item starts in the resident's current cell.

## Verification boundary

Repository quality/source tests can verify contracts and deterministic non-Unity behaviour.
The visible movement cadence and the remote pickup path still require an actually executed
licensed Unity Play Mode/runtime scenario before this correction may be called `VERIFIED`.
