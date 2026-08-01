# Automatic grounding for every world item — 2026-08-01

Status: `IMPLEMENTED` in the correction branch; licensed Unity runtime evidence remains pending.

Tracking: [#387](https://github.com/bageus/Dig/issues/387), [#396](https://github.com/bageus/Dig/issues/396).

Authoritative design: [`../design/world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md), [`../design/entity-fall-knockback-and-vertical-shafts.md`](../design/entity-fall-knockback-and-vertical-shafts.md).

## Problem

Authoritative gravity already reconciled every free `ItemLocation.World` stack, but Unity floor placement derived vertical position from `ItemVisualProfile.WorldScale.y / 2`. That silently assumed a centered prefab pivot and a mesh matching the declared scale. New content with another pivot or hierarchy could hover or intersect the floor until an author added item-specific metadata or an offset.

## Correction

- every new item/material remains automatically covered by `WorldItemGravitySettlement`; there is no item allowlist;
- one `DigWorldItemGrounding` presentation owner places the visual at the projected floor and then aligns the actual lower bound of active renderers to that plane;
- grounding is applied after item scale, stack layout and reservation rotation;
- ordinary world stacks, building internal-stock units, inventory placement ghosts and BuildingBox relocation previews/plans use the same helper;
- interaction colliders are rebuilt from the same visible geometry when available, preserving visual/raycast consistency;
- carry attachments, living-material tether projections and completed buildings keep their dedicated owners.

## Regression evidence

- Domain/Application regression creates previously unknown material and tool definitions and confirms both settle through the common gravity pass without registration;
- source contract rejects the previous `WorldScale.y * 0.5f` floor assumption and requires the shared grounding owner across every ordinary item projection;
- checked-in Play Mode scenario compares centered-pivot and bottom-pivot hierarchies and requires both lower bounds to touch the same projected floor without per-item metadata.

## Verification boundary

Repository Quality, Release build/tests, smoke and deterministic soak evidence must be recorded for the final PR head. The checked-in Play Mode scenario does not promote the system to `VERIFIED` until a licensed Unity runner actually executes it.
