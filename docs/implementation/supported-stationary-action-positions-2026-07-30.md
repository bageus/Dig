# Supported stationary action positions — 2026-07-30

Status: `IMPLEMENTED` pending licensed Unity Play Mode verification.

Tracking: #423, #459, PR #521.

## Root cause

Mushroom work-position resolution treated vertical `Y±1` cells as neighbours and did not require full actor support. In Dig coordinates `Y` is vertical and `Z` is depth, so side voids could make a resident select an airborne or vertically displaced work cell while a valid supported cell existed behind the mushroom.

Food meal start consumed the carried portion before any world-support policy was consulted. Active meals were advanced by Agent autonomy even when the resident cell no longer had full support.

## Correction

The shared Unity stationary-action policy now:
- generates same-height neighbours on `X±1` and bounded depth `Z±1`;
- requires `HasFullActorSupport` below every stationary action cell;
- permits only supported walk/depth transitions for mushroom approach;
- revalidates support before mushroom swings;
- guards meal start before reservation/consume and interrupts an active meal before another bite when support is lost.

The mushroom resolver therefore selects a supported depth cell when left/right cells are void instead of allowing airborne work.

## Evidence

Fast .NET regressions cover the selector source contract and quantity-safe meal rejection. A checked-in Unity Play Mode scenario boots the real demo world and calls the actual mushroom resolver for a side-void/depth-supported case. Hosted Unity execution may remain blocked when activation is unavailable; do not promote the systems to `VERIFIED` without licensed runtime results.
