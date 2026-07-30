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
- uses ordinary resident Navigation for mushroom travel, including approved climb/shaft/depth transitions, while requiring full support only at the final stationary action cell;
- revalidates support before mushroom swings;
- guards meal start before reservation/consume and interrupts an active meal before another bite when support is lost.

The mushroom resolver therefore selects a supported depth cell when left/right cells are void instead of allowing airborne work.

## Route regression correction — 2026-07-30

The initial correction accidentally applied the stationary support invariant to every cell and transition in the travel path. That rejected otherwise valid direct and automatic mushroom jobs whenever the resident had to climb or cross an approved shaft/depth route before reaching a fully supported work cell. The resolver and replanner now validate ordinary Navigation reachability plus full support at the final work position only; support is still revalidated before every swing.

## Evidence

Fast regressions cover the selector source contract and quantity-safe meal rejection. The checked-in Unity Play Mode scenario boots the real demo world and calls the actual mushroom resolver for a side-void/depth-supported case.

PR #521 initially failed the repository file-size gate because `MushroomChoppingPlayModeTests.cs` grew to 445 lines. The supported-depth scenario was moved into the focused `MushroomWorkPositionPlayModeTests.cs` fixture, keeping both Play Mode files below the 350-line limit without reducing coverage. Final Quality run `30564804822` passed architecture/file-size checks, Release build, all 1172 .NET tests, headless smoke and both deterministic soaks. Stage 2 v2/v3 exports `30564804684` and `30564804712` also passed. Hosted Unity workflow `30564804945` completed the evidence path; do not promote the systems to `VERIFIED` without licensed executed runtime results.

## Unity compile regression — 2026-07-30

The Unity partial type briefly contained two identical `HasFullStandingSupport(CellId)` declarations: the authoritative shared implementation in `DigTerrainWorkSession.SupportedActionPositions.cs` and a legacy private copy in `DigTerrainWorkSession.BarrelNavigation.cs`. Unity therefore failed with `CS0111` before Play Mode could start even though the repository .NET solution and source-string gates were green.

The legacy barrel copy was removed. Barrel, mushroom, BuildingBox and meal workflows now call the single shared implementation. `UnityStandingSupportMemberContractTests` scans every `DigTerrainWorkSession*.cs` runtime partial and fails unless exactly one non-public declaration exists in the authoritative supported-action file.
