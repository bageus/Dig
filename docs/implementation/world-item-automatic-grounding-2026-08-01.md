# Automatic grounding for every world item — 2026-08-01

Status: `IMPLEMENTED` in draft PR #556. Runtime verification is pending.

Tracking: #387, #396.

Authoritative design: `docs/design/world-item-gravity-selection-and-pickup.md` and `docs/design/entity-fall-knockback-and-vertical-shafts.md`.

## Problem

Application gravity already reconciled every free `ItemLocation.World` stack, but Unity floor placement used `ItemVisualProfile.WorldScale.y / 2`. This assumed a centered prefab pivot and a mesh matching the declared scale. New content with another pivot or hierarchy could hover or intersect the floor until an author added item-specific metadata or an offset.

## Correction

- every new item or material is automatically covered by `WorldItemGravitySettlement`; there is no item allowlist;
- `DigWorldItemGrounding` aligns the actual lower bound of active renderers with the projected floor;
- grounding runs after item scale, stack layout and reservation rotation;
- world stacks, building internal stock, inventory placement ghosts and BuildingBox relocation previews and plans use the same owner;
- interaction colliders are derived from the same visible geometry;
- carry attachments, living-material tethers and completed buildings retain their dedicated projection owners.

## Regression evidence

- a Domain/Application regression creates previously unknown material and tool definitions and confirms both settle without registration;
- source contracts reject the former `WorldScale.y * 0.5f` assumption and require the shared grounding owner;
- a checked-in Play Mode scenario compares centered-pivot and bottom-pivot hierarchies on the same floor plane.

## Final-head checks

PR #556 head `8aa4f3adc46458899524adb37199e1d6c3abb219` passed architecture and compatibility gates, Unity source and item-visual contracts, Release build, the complete .NET test suite, headless smoke, both deterministic soak profiles with replay equality, and Stage 2 v2/v3 exports.

The Unity workflow could not execute EditMode or PlayMode tests because the runner had no usable Unity activation. The runtime scenario is present but has not executed, so the correction remains `IMPLEMENTED`, not `VERIFIED`.
