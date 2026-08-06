# Central tooltip layout stability — 2026-08-06

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md`](../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md).  
Tracking issue: [#658](https://github.com/bageus/Dig/issues/658).

## Root cause

`DigGameHudCanvas.ContextHover.RefreshContextHoverInfo` activated the hover panel only while text was present and changed `_bottomContent.offsetMax.y` between `-8f` and `-52f`. The central layout therefore gained and lost 44 pixels on pointer enter/exit, moving its icons and controls.

## Correction

- every active central context layout reserves the hover region before any hover starts;
- the hover panel stays layout-present while the context panel is open;
- empty hover state clears text but does not collapse the region;
- production hover keeps priority over world-target hover;
- centered text and pointer-transparent background/text behavior remain unchanged;
- the content inset is one fixed constant and no longer depends on hover visibility.

## Regression coverage

- `ContextHoverLayoutContractTests` rejects conditional content resizing and requires the shared reservation path;
- `ContextHoverLayoutPlayModeTests` records context-panel, content and representative-icon geometry before hover, during production hover and after hover exit and requires exact stability.

## Verification boundary

Quality, deterministic and Unity workflow evidence must be recorded after execution. Actual licensed Unity Play Mode remains required for `VERIFIED` status.
