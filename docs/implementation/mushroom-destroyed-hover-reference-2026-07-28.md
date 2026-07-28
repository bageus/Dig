# Mushroom destroyed-hover reference correction — 2026-07-28

Status: `IMPLEMENTED` pending licensed Unity Play Mode execution.

Authoritative design: [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md).
Tracking: [#423](https://github.com/bageus/Dig/issues/423), input lifecycle [#390](https://github.com/bageus/Dig/issues/390).

## Runtime symptom

After a visible mushroom was removed, Unity reported `MissingReferenceException` for a destroyed `UnityEngine.MeshRenderer`.

## Root cause

`DigWorldInteraction` retained the previously hovered `DigMushroomVisual` across the renderer-removal tick. Hover cleanup used C# null-conditional calls such as `_hoveredMushroom?.SetHovered(false)`. The null-conditional operator checks the managed reference and bypasses Unity's overloaded destroyed-object equality, so `SetHovered` ran on a destroyed visual. `RestoreColors` then accessed cached stem/cap renderers which Unity had already destroyed.

## Correction

- cached resident, world-item and mushroom hover targets are checked through explicit Unity-object liveness guards before presentation methods are called;
- destroyed targets are cleared without invoking `SetHovered`;
- mushroom property-block loops skip renderers Unity has already destroyed;
- input priority, authoritative mushroom state, jobs, drops, save data and growth behavior are unchanged.

## Regression evidence

- `MushroomUnityRuntimeContractTests` rejects null-conditional dispatch on cached Unity hover targets and requires the renderer liveness guards;
- `MushroomHoverRemovalPlayModeTests` highlights a mushroom, destroys it, executes the real `ClearPointerHover` path and requires no exception plus a cleared cached target.

The system remains `IMPLEMENTED` until the Play Mode fixture is actually executed by a licensed Unity Test Runner.
