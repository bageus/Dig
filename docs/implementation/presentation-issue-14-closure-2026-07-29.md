# Presentation issue #14 closure assessment — 2026-07-29

Status: repository implementation is `IMPLEMENTED`; issue #14 may close as completed because all remaining licensed Unity verification acceptance moved intact to [#511](https://github.com/bageus/Dig/issues/511).

Tracking: [#14](https://github.com/bageus/Dig/issues/14). Runtime verification owner: [#511](https://github.com/bageus/Dig/issues/511).

Authoritative specification: [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).

## Scope reviewed

The assessment compared:

- `docs/systems/README.md`;
- issue #14 acceptance and comments;
- `docs/development-rules.md` runtime Definition of Done;
- Presentation, input, HUD, notification, camera and visual implementation notes;
- current merged repository implementation and checked-in Unity tests;
- the Unity workflow activation behavior;
- issue #16's approved closure path that permits explicit transfer of remaining runtime gates without losing acceptance.

## Implemented repository evidence

The repository contains:

- Unity bootstrap and engine-independent package wiring;
- immutable world, resident, building, item, job and diagnostic projections;
- side-view camera and depth projection;
- logical-position/visual-interpolation separation;
- deterministic context input decisions with UI shielding and one-command maximum;
- resident roster/read models, typed activity descriptors and bounded row pooling;
- event-driven notification ticker with stable source keys;
- debug job/reservation/route/chunk overlays;
- renderer rebuild paths that re-read snapshots instead of owning gameplay state;
- guarded unavailable-session projections;
- checked-in EditMode/PlayMode regressions for representative Presentation workflows;
- representative `Main.unity` bootstrap/Console test;
- machine-readable Unity evidence validation.

Detailed implementation maps:

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
- [`settlement-management-menu.md`](settlement-management-menu.md);
- [`unity-runtime-verification-gate.md`](unity-runtime-verification-gate.md).

## Verification boundary

The repository workflow previously completed green while the actual Unity runner step was skipped because activation was unavailable. That run remains `blocked`, not verified.

The updated workflow:

- selects EditMode and PlayMode together;
- requires an EditMode project/scene contract;
- requires a PlayMode representative Main-scene/Console contract;
- retains raw XML/runtime artifacts;
- publishes `unity-runtime-evidence.json` with `verified`, `failed` or `blocked` status.

No issue or document may treat workflow conclusion alone as evidence.

## Closure decision

Issue #14 owns Presentation implementation. Its `IMPLEMENTED` acceptance is complete and remains covered by repository tests.

The following are not marked complete and are not discarded:

- licensed Unity Test Runner execution;
- result XML and runtime logs;
- representative-scene Console evidence;
- Unity runtime performance baseline/budgets;
- child workflow-specific end-to-end verification.

They are now owned by #511. Therefore closing #14 does not change the system status to `VERIFIED` and does not claim a Unity run that did not occur.

## Procedure for future `VERIFIED` status

1. Configure an approved licensed execution path.
2. Run `.github/workflows/unity-playmode.yml` against current `main` with Unity `6000.0.71f1`.
3. Confirm `unity-runtime-evidence.json` has `status: verified` and the current commit SHA.
4. Retain raw EditMode/PlayMode XML and representative runtime logs.
5. Record measured Unity runtime baseline and approved budgets in #511.
6. Rerun and pass those budgets.
7. Update this Presentation specification to `VERIFIED` only after its relevant runtime acceptance passes.

## Residual child work

Closing #14 does not close broader gameplay/content issues. Context-specific rules and runtime verification remain tracked by #113, #115–#118, #387, #390, #398 and #511 according to their own acceptance.
