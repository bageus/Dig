# Unity tool preparation policy controls

## Purpose

The Unity HUD exposes the existing `JobToolPreparationMode` choice without moving simulation ownership into the presentation layer.

The selected policy applies only to future Job assignment attempts. Existing claimed or in-progress Jobs keep their resident, tool reservation and Inventory state.

## Modes

- `Automatic` allows `InventoryJobToolPreparationService` to perform a safe carried-tool switch before the Job claim.
- `Suggest only` selects and reserves the matching tool but leaves Inventory locations unchanged. The Job overlay records `Suggested` instead of `Switched`.

The runtime starts in `Automatic` mode.

## Composition

`JobToolPreparationModeControl` is a small Presentation-owned selection state implementing `IJobToolPreparationModeSource`.

Unity passes the same control to the tool-aware assignment handlers for dynamic digging, BuildingBox packing and BuildingBox assembly. `AssignAvailableJobsHandler` resolves the live source when present; command-only callers without a source continue to use `AssignAvailableJobsCommand.ToolPreparationMode` exactly as before.

HUD policy buttons update only the selection state. They do not execute a Job command, mutate Inventory or rewrite retained assignment diagnostics.

## Typed Job actions

`JobOverlayViewModel.Actions` contains immutable `JobActionViewModel` values keyed by `JobActionKind`. Unity renders the supplied label, enabled state and disabled reason; it does not infer action availability from assignment diagnostics, resident text or reservation rows.

A retained `Suggested` assignment produces `PrepareSuggestedTool`. Presentation evaluates disabled reasons in the same order as the Application use case:

1. the Job must be `Claimed` or `InProgress` with an assigned resident;
2. the retained assignment resident must match the current Job resident;
3. the suggested stack must still have the matching Tool reservation.

The action therefore remains visible when a suggestion becomes stale, but is disabled with a stable code and message. Assignments already marked `Switched` do not expose the manual action.

## Generic rendering and action dispatch

`DigHudOverlay.DrawJobAction` is the single renderer for every typed Job action. It renders the supplied label, combines `action.IsEnabled` with the surrounding `GUI.enabled` state, restores the previous GUI state, and shows the supplied disabled reason. The common rendering path has no action-specific drawing method or `switch`.

Only a successful button click calls `JobActionDispatcher`. The dispatcher is an immutable execution routing table from `JobActionKind` to a typed handler. It rejects duplicate registrations, validates the Job id, rejects disabled actions before invoking a handler, and fails explicitly when no route is registered.

Unity registers `PrepareSuggestedTool` with its execution handler in one lazy composition point. Adding another action requires only a new typed model from Presentation and a registered execution route; common button rendering remains unchanged.

## Equip suggested tool action

When `PrepareSuggestedTool` is enabled and clicked, the HUD sends only the Job id and current simulation tick to `PrepareSuggestedJobToolHandler`; it does not trust or mutate rendered Inventory state.

Before switching, the Application use case verifies all authoritative conditions again:

1. the Job still exists and is `Claimed` or `InProgress`;
2. the retained report still contains a `Suggested` assignment;
3. the report resident still matches the Job resident;
4. the suggested stack is still reserved as the Job's Tool reservation.

Only then does `InventoryJobToolPreparationService` execute the safe switch. The Job and all reservations remain unchanged. The retained report preserves the assignment score, uses the current tick and changes the preparation outcome to `Switched`.

After success Unity reloads the indexed Job overlay and refreshes equipment visuals and resident work-rate presentation from authoritative Inventory snapshots.

## Validation

Engine-independent tests verify:

- the control defaults to `Automatic` and changes only on an explicit selection;
- a live `Suggest` source overrides an automatic command without invoking tool preparation;
- a live `Automatic` source overrides a suggest command and invokes tool preparation;
- the reported preparation outcome matches the effective mode;
- a valid suggested action switches Inventory and updates the retained diagnostic;
- missing Tool reservation and stale resident data reject the command before Inventory mutation;
- Presentation enables a valid typed action and exposes stable disabled reasons for invalid status, stale resident and missing Tool reservation;
- non-suggested assignments expose no manual preparation action;
- the dispatcher routes the exact enabled immutable action, rejects disabled actions, rejects duplicate and missing routes, and validates the Job id before handler invocation.

The normal Quality workflow validates architecture boundaries, file sizes, C# 9 compatibility, Release build, all tests, headless smoke and deterministic soak profiles. Unity Editor button interaction remains a local Play Mode check.
