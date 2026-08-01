# Demo completed campfire Z1 depth correction — 2026-08-01

Status: `IMPLEMENTED` in draft PR [#550](https://github.com/bageus/Dig/pull/550); licensed Unity runtime evidence remains required.

Authoritative specifications:

- [`../design/demo-starting-scenario.md`](../design/demo-starting-scenario.md);
- [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md);
- [`../design/world-3d-depth.md`](../design/world-3d-depth.md).

Tracking issue: [#389](https://github.com/bageus/Dig/issues/389).

## Reported symptom

The fresh demo completed campfire was created on surface `Z0`. That contradicted the layer-derived BuildingBox contract: `Z0` is relocation space for the physical box, while unpacked/active/completed buildings use `Z1–Z3`.

## Root cause

`DigTerrainWorkSession.FindSurfaceCampfirePlacement()` reused `TunnelDemoLayout.ShaftZ`. The demo shaft is intentionally on the nearest depth `Z0`, so the fixture bypassed the ordinary building-placement intent and committed a completed building to the box-only layer.

## Correction

The deterministic surface anchor keeps the same X/Y location but uses exact building depth `Z1`:

```text
X = ShaftX - 2
Y = SurfaceY
Z = 1
```

Footprint support and work-position validation use the same exact `Z1`. No fallback to `Z0`, another surface or the lower cave is allowed.

## Regression coverage

- `DemoCampfirePlacementTests` verifies the domain demo platform exposes a supported `Z1` anchor and source rejects `layout.ShaftZ` for completed-campfire placement.
- `DemoCampfireDepthPlayModeTests` initializes the real demo sessions and requires the completed campfire origin, work position and every footprint cell to remain on `Z1`.
- The existing packed campfire box lower-cave workflow remains unchanged.

## Verification boundary

Repository Quality validates compilation, source contracts and the checked-in Play Mode fixture. Runtime remains `IMPLEMENTED`, not `VERIFIED`, until a licensed Unity runner actually executes the Play Mode scenario and records runtime evidence.
