# Play Mode runtime visibility regression — 2026-07-28

Статус: `IMPLEMENTED` compile correction; фактический Unity Test Runner остаётся обязательным для `VERIFIED`.

Authoritative systems:

- [`../design/destructible-barrels.md`](../design/destructible-barrels.md), tracking [#443](https://github.com/bageus/Dig/issues/443);
- [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md), tracking [#423](https://github.com/bageus/Dig/issues/423).

## Runtime symptom

Unity Safe Mode reported six Play Mode assembly errors:

- direct access to internal `DigTunnelProjection.DepthOrigin` and `DepthSpacing` from `BarrelDestructionPlayModeTests`;
- unresolved `MissingMethodException` and `MissingMemberException` because the barrel fixture did not import `System`;
- direct access to internal `DigMushroomVisual.Model` from `MushroomDepthProjectionPlayModeTests`.

The fixtures compile in `Dig.Unity.PlayModeTests`, which is a separate assembly from `Dig.Unity`; sharing a namespace does not grant access to internal runtime members.

## Correction

- barrel depth expectations resolve the authoritative projection constants through reflection on the runtime assembly instead of duplicating them or exposing production internals;
- the barrel fixture imports `System`, restoring the standard reflection exception types;
- mushroom depth ordering and assertions resolve the internal visual model through the existing reflection boundary;
- no public/runtime API or gameplay behavior was widened or changed.

## Regression coverage

`PlayModeRuntimeVisibilityContractTests` requires:

- no direct `DigTunnelProjection.*` access in the barrel fixture;
- reflection lookup of both depth constants;
- no direct `value.Model` or `visual.Model` access in the mushroom depth fixture;
- reflection lookup of the mushroom model.

The existing Play Mode scenarios continue to verify barrel depth/height/destruction/landing and mushroom Z0–Z3 projection. Their runtime execution still must be performed by Unity Test Runner before either system is promoted to `VERIFIED`.

## Unity `Object` ambiguity follow-up

Adding `using System;` restored the reflection exception types but also made unqualified `Object.Destroy(...)` ambiguous between `System.Object` and `UnityEngine.Object` in the Play Mode assembly. All three fixture cleanup calls now use the explicit `UnityEngine.Object.Destroy(...)` owner.

`PlayModeRuntimeVisibilityContractTests` rejects unqualified `Object.Destroy(...)` calls and requires the three explicit Unity cleanup calls, preventing this Safe Mode compiler regression without changing runtime or barrel behavior.
