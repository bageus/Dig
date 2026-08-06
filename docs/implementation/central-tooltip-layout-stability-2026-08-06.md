# Central tooltip layout stability — 2026-08-06

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md`](../design/runtime-building-ui-health-unpack-pickup-correction-2026-08-04.md).  
Tracking issue: [#658](https://github.com/bageus/Dig/issues/658).  
Implementation PR: [#659](https://github.com/bageus/Dig/pull/659).

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

## Executed validation

Code-and-test head `e3b37d1588d3db09ce296b9e6ae5e2862b9a37fa`:

- Quality run `31059058833` — success;
- architecture, file-size, C# compatibility and Unity source-contract gates — success;
- Release build — success;
- .NET tests — `1531/1531` passed;
- headless smoke — success;
- standard deterministic soak — success;
- large-settlement deterministic soak — success;
- Export Stage 2 v2 run `31059058885` — success;
- Export Stage 2 v3 run `31059059136` — success.

Unity workflow `31059058839` completed through the blocked-evidence path. Licensed activation was unavailable, so the actual EditMode/PlayMode Test Runner and executed runtime-evidence validation were skipped. The checked-in layout regression therefore has not executed in licensed Unity.

## Verification boundary

The correction is `IMPLEMENTED IN BRANCH`, not `VERIFIED`. Licensed Unity Play Mode must still execute repeated hover enter/exit and confirm that the central panel, content rectangle and menu icons remain stationary.
