# Presentation issue #14 closure assessment — 2026-07-29

Status: repository implementation is `IMPLEMENTED`; issue closure is blocked by missing executed Unity Play Mode evidence.

Tracking: [#14](https://github.com/bageus/Dig/issues/14). Quality dependency: [#15](https://github.com/bageus/Dig/issues/15).

Authoritative specification: [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).

## Scope reviewed

The assessment compared:

- `docs/systems/README.md`;
- issue #14 acceptance and comments;
- `docs/development-rules.md` runtime Definition of Done;
- Presentation, input, HUD, notification, camera and visual implementation notes;
- current issue/PR evidence through merged PR #498;
- the latest Unity Play Mode workflow run on the PR #498 head.

## Implemented repository evidence

The current repository contains:

- Unity bootstrap and engine-independent package wiring;
- immutable world, resident, building, item, job and diagnostic projections;
- side-view camera and depth projection;
- logical-position/visual-interpolation separation;
- deterministic context input decisions with UI shielding and one-command maximum;
- resident roster/read models, typed activity descriptors and bounded row pooling;
- event-driven notification ticker with stable source keys;
- debug job/reservation/route/chunk overlays;
- renderer rebuild paths that re-read snapshots instead of owning gameplay state;
- unavailable-session HUD guards added by PR #498;
- checked-in Play Mode regressions for representative Presentation workflows.

The following implementation maps provide the detailed code/test references:

- [`unity-presentation-host.md`](unity-presentation-host.md);
- [`unity-world-vertical-slice.md`](unity-world-vertical-slice.md);
- [`unity-resident-presentation.md`](unity-resident-presentation.md);
- [`unity-job-overlay.md`](unity-job-overlay.md);
- [`unity-terrain-work-vertical-slice.md`](unity-terrain-work-vertical-slice.md);
- [`context-input-router.md`](context-input-router.md);
- [`resident-roster-read-models.md`](resident-roster-read-models.md);
- [`unity-notification-ticker.md`](unity-notification-ticker.md);
- [`unity-side-view-camera.md`](unity-side-view-camera.md);
- [`unity-visual-asset-pipeline.md`](unity-visual-asset-pipeline.md);
- [`settlement-management-menu.md`](settlement-management-menu.md).

## Latest automated checks inspected

PR #498 head `69b8217c083462d86c04d35725f3547591576259` recorded:

- Quality run `30403981314`: success;
- Release build: success;
- .NET tests: 1096 passed, 0 failed;
- headless smoke: success;
- standard deterministic soak: success;
- large-settlement deterministic soak: success;
- Stage 2 v2 `30403981312`: success;
- Stage 2 v3 `30403981262`: success.

Unity Play Mode run `30403981270` completed at workflow level, but the actual `Run Play Mode tests` step was `skipped`. No result XML or Unity runtime log artifact was produced.

## Remaining blocker

`docs/development-rules.md` requires a factually passed end-to-end or Play Mode scenario for runtime/Unity interaction and forbids task completion based only on source contracts, compilation or checked-in test source.

The repository workflow already exposes the activation prerequisite. The missing external configuration is one of:

- Personal Unity activation: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`;
- Pro activation: `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.

Without those repository secrets or an equivalent local licensed Unity run, issue #14 must remain open and the system must remain below `VERIFIED`.

## Closure procedure after activation

1. Run `.github/workflows/unity-playmode.yml` against current `main` with Unity `6000.0.71f1`.
2. Confirm `Run Play Mode tests` executed and passed rather than being skipped.
3. Retain result XML and logs as workflow artifacts.
4. Confirm no Console errors in the representative scene acceptance.
5. Add the run/artifact links to issue #14.
6. Change the authoritative system status from `IMPLEMENTED` to `VERIFIED` and close #14 as `completed`.

## Residual child work

Closing #14 after runtime evidence does not close broader gameplay/content issues. Context-specific rules and additional content remain tracked by #113, #115–#118, #387, #390 and #398 according to their own acceptance.
