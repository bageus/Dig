# Living-material layout and campfire VFX correction — 2026-08-04

Status: `IMPLEMENTATION IN PROGRESS`.

Authoritative specification: [`../design/living-material-layout-and-campfire-vfx-correction-2026-08-04.md`](../design/living-material-layout-and-campfire-vfx-correction-2026-08-04.md).  
Tracking issue: [#619](https://github.com/bageus/Dig/issues/619).

## Root cause

`DigWorldItemRenderer` incremented one visual slot for every world Inventory stack before checking whether the stack had visible geometry. Living-material linked stacks deliberately render zero item instances because `DigCreatureRenderer` owns their visible creature projection. When hamster/grub entered a cell first in read-model order, an unrelated package was assigned the next offset and appeared to be pushed despite trigger-only physics.

`DigPresentationEffectRuntime` also created deterministic ambient dust at the world centre and a persistent `CampfireGlow` particle emitter. Active campfire production additionally projected `ProductionWorkApplied` pulses.

## Correction

- `DigWorldItemVisualPolicy` owns the shared living-material ID classifier;
- hidden living-material proxies receive no co-cell layout slot;
- package/item offsets are unchanged when living material enters or leaves the cell;
- campfire glow keeps only the realtime light;
- campfire production pulse events are filtered at the Unity runtime boundary;
- periodic ambient-dust facts are no longer created;
- non-campfire production and other approved VFX remain unchanged.

## Regression map

- Play Mode: hamster/grub/legacy larva enter and leave the unfinished-package cell without changing package position or rotation;
- existing Play Mode trigger/raycast Rigidbody pass-through remains;
- .NET presenter test requires light-only campfire glow;
- source contract requires shared classifier, slot exclusion and no runtime ambient emission.

## Verification boundary

Release build, full tests, source contracts, headless smoke and deterministic soaks are required before merge. Actual particle absence and package stability in the representative Unity scene remain below `VERIFIED` until licensed Play Mode or local user confirmation.
