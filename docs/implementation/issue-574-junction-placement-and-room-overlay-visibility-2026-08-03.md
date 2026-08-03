# Issue 574 — junction placement-only flow and room planning overlays

Status: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

## Confirmed correction

- vertical/horizontal tunnel junction has no reinforcement point, room-style marker or automatic stone-trim job;
- stone trim at a junction or ordinary horizontal floor starts only through the existing resident-owned placement mode and exact selected stone stack;
- automatic tunnel work is limited to wooden supports;
- room markers and room-purpose overlays are planning affordances, not persistent world geometry;
- resident or building selection hides those planning overlays;
- completed/partial physical room-improvement geometry stays visible.

## Root cause

The previous junction slice treated the decorative junction target like automatic tunnel work. Runtime synchronization could create a low-priority `JunctionStoneTrim` job, reserve stone and expose the job through ordinary overlays. That contradicted the placement-mode ownership of tunnel reinforcement/decoration.

Room markers were rendered unconditionally by the presentation driver. Their active state did not follow the current HUD/input context, so resident/building selection could leave room planning affordances visible.

## Implementation

### Junction trim

- `SynchronizeTunnelJunctionTrimPlacementHandler` creates no jobs and reserves no source;
- it deterministically cancels legacy non-terminal automatic junction-trim jobs and releases their Inventory reservations;
- automatic candidate publication filters to `WoodenSupport`;
- automatic finalization rejects legacy junction-trim definitions with `manual_placement_required` before material, infrastructure or skill mutation;
- legacy save codec support remains so old documents can load and be cleaned up safely;
- manual `TunnelManualWorkKind.JunctionStoneTrim` validation/completion remains authoritative.

### Room planning visibility

- `DigRoomInfrastructureRenderer.SetPlanningOverlayVisibility` activates/deactivates marker objects only;
- physical progress objects are owned separately and remain active;
- `DigWorldInteraction.IsRoomPlanningOverlayVisible` rejects resident, building, job, building-placement and building-box contexts;
- room selection remains part of the planning workflow;
- `DigRoomInfrastructurePresentationDriver` evaluates visibility in `LateUpdate`, after selection input for the frame;
- hidden markers cannot intercept raycasts.

## Regression coverage

Domain/Application:

- a pending junction target with available stone creates no automatic job or reservation;
- legacy automatic junction work is cancelled and releases the exact source reservation;
- manual junction completion remains valid after placement-only synchronization;
- automatic junction completion is rejected before quantity, provenance or Stonework changes.

Unity/source contracts:

- runtime composes placement-only synchronization before ordinary assignment;
- automatic candidates include only wooden supports;
- marker visibility derives from current resident/building/job/placement context;
- Play Mode fixture hides the marker while preserving physical room progress and restores it when planning visibility returns.

## Verification boundary

Repository Quality, Release build, .NET tests, headless smoke and deterministic soaks are required before merge. Actual licensed Unity EditMode/PlayMode execution is required before promoting this behavior to `VERIFIED`.
