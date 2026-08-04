# Dwarf AnimationModule compile correction

Дата: 2026-08-04.  
Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).  
Tracking issue: [#641](https://github.com/bageus/Dig/issues/641).

## Runtime report

Root Unity project entered Safe Mode with `CS1069` because `DwarfEtalonAnimatorBridge` referenced `UnityEngine.Animator`, while the host did not directly declare `UnityEngine.AnimationModule`.

## Root cause

The checked-in dwarf bridge requires the built-in animation module. The root Unity manifest omitted its package declaration. The canonical `unity/Dig.Unity` manifest and runtime asmdef omitted the same dependency, and the quality gate did not validate it.

## Correction

- root Unity host directly declares `com.unity.modules.animation`;
- canonical Dig Unity host declares and pins the module;
- `Dig.Unity.asmdef` references `UnityEngine.AnimationModule`;
- dwarf integration documentation records the host and custom-asmdef requirements;
- the Unity module quality gate validates the root host, canonical host and runtime assembly;
- a .NET source contract couples the Animator bridge to those declarations.

No gameplay, animation state-machine or simulation rule changed.

## Verification boundary

Passing source contracts and .NET CI proves the dependency contract is checked in. Actual exit from Unity Safe Mode requires a licensed Unity package resolve/compile or Test Runner execution; until that evidence exists, status remains `IMPLEMENTED`, not `VERIFIED`.
