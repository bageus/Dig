# Unity unified overlay framework

## Ownership boundary

Overlay state is presentation state. Simulation, jobs, reservations, navigation, excavation, building placement and selection commands remain owned by Domain/Application and their existing input adapters.

Disabling an overlay layer changes only visibility. It does not cancel work, release reservations, remove designations, change a route or commit a preview.

## Typed layers and priority

`OverlayLayerKind` separates semantic groups from concrete renderers:

| Layer | Priority | Default profile | Toggle |
|---|---:|---|---|
| Hover | 950 | Release/Debug | — |
| Selection | 900 | Release/Debug | — |
| Preview | 800 | Release/Debug | — |
| Reservations | 700 | Release/Debug | — |
| Designation | 600 | Release/Debug | — |
| Jobs | 500 | Release/Debug | F3 / 3 |
| Routes | 300 | Debug only | F4 / 4 |
| Diagnostics | 100 | hidden by default | — |

Higher-priority overlays receive a higher renderer sorting order. Terrain depth remains in world-space; overlay priority does not change the authoritative `Z=0..3` target.

A layer may register several roots. This allows cave previews, building previews and later footprint previews to coexist in the Preview layer without replacing each other.

## Visibility profiles

`DigOverlayManager` owns one immutable `OverlayVisibilitySnapshot`.

- `Release` hides every debug-only layer even if a stale user override requests it;
- `Debug` uses default visibility, including routes;
- `All` enables every layer unless the user explicitly toggles one off;
- F2 cycles Release, Debug and All;
- F3 or 3 toggles Jobs;
- F4 or 4 toggles Routes.

The manager toggles registered roots only. It does not recreate their markers or call simulation commands.

## Accessibility metadata

Every migrated renderer resolves `OverlaySemanticKind` through shape and pattern metadata as well as colour.

Examples:

- selected job: Diamond + Double pattern;
- invalid cave preview: Cross + CrossHatch plus two diagonal front-face lines;
- blocked job: Cross + CrossHatch, reduced scale and tilt;
- attention job: Diamond + Dashed pattern and tilt;
- route: Line + Dashed pattern.

`DigOverlayMetadata` records layer, semantic, shape and pattern on renderer objects for diagnostics and future UI accessibility descriptions.

## Shared materials and allocation policy

`DigOverlayManager` owns one cached material per semantic kind. Materials use the shared unlit shader and GPU instancing. Migrated renderers do not allocate their own status or valid/invalid materials.

Current migrated systems:

- job markers and worker links;
- navigation route lines;
- cave-room valid/invalid previews.

The cave preview retains a fixed pool of fourteen line renderers: twelve box edges and two invalid-cross edges. Route and job roots retain their existing keyed reuse behavior. Visibility toggles never allocate markers.

## Pointer boundary

The framework does not participate in hit resolution.

- job marker colliders retain existing typed job picking;
- route and cave-preview line renderers have no interaction collider;
- UI click shielding remains in `DigWorldInteraction` and `DigGameHudCanvas`;
- pointer priority and command dispatch are unchanged by layer sorting or visibility.

One pointer event therefore continues through the existing input path exactly once.

## Runtime composition

Renderers may receive a manager explicitly or resolve the one attached to the shared bootstrap object. Registration is idempotent. Unregistering one root does not hide or remove other roots in the same layer.

The legacy `DigOverlayHotkeys` component remains harmless compatibility scaffolding while `DigOverlayManager` owns all active F2/F3/F4 behavior. Migrated roots no longer use its historical name-based lookup strings.

## Remaining #211 work

This foundation establishes typed layers, priority, visibility profiles, shared style/material ownership and three representative renderer integrations. Follow-up slices still need:

- selection, hover and marquee integration;
- excavation/designation markers;
- building footprint and placement ghost integration;
- storage demand and reservation overlays;
- deposits, fog/vision, dirty-chunk and navigation diagnostics;
- pooled generic marker/line/decal factories;
- full depth-offset policy for every `Z=0..3` overlay target;
- Play Mode validation of click shielding, rebuild persistence and allocation budgets.
