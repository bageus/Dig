# Unity tool preparation policy controls

## Purpose

The Unity HUD exposes the existing `JobToolPreparationMode` choice without moving simulation ownership into the presentation layer.

The selected policy applies only to future Job assignment attempts. Existing claimed or in-progress Jobs keep their resident and target reservations.

## Modes

- `Automatic` allows `InventoryJobToolPreparationService` to perform a safe carried-tool switch before the Job claim.
- `Suggest only` selects and reserves the matching tool but leaves Inventory locations unchanged. The Job overlay records `Suggested` instead of `Switched`.

The runtime starts in `Automatic` mode.

## Composition

`JobToolPreparationModeControl` is a small Presentation-owned selection state implementing `IJobToolPreparationModeSource`.

Unity passes the same control to the tool-aware assignment handlers for dynamic digging, BuildingBox packing and BuildingBox assembly. `AssignAvailableJobsHandler` resolves the live source when present; command-only callers without a source continue to use `AssignAvailableJobsCommand.ToolPreparationMode` exactly as before.

HUD policy buttons update only the selection state. They do not execute a Job command, mutate Inventory or rewrite retained assignment diagnostics.

## Execution readiness

`SuggestedToolJobExecutionReadinessPolicy` is an Application-owned gate used by `AdvanceJobHandler`. While the latest retained assignment outcome is `Suggested`, an advance command succeeds as a no-op:

- a `Claimed` Job does not transition to `InProgress`;
- an already in-progress Job does not advance another stage;
- the resident and all reservations remain intact;
- the no-op does not interrupt the simulation tick or prevent other Jobs from advancing.

The gate is resolved when the retained outcome changes to `Switched` or `Bypassed`. Unity composes the policy with the same indexed assignment report source used by the HUD.

Presentation maps the resolved Job and retained report into an always-present immutable `JobExecutionReadinessViewModel`:

- normal, resolved and terminal Jobs are `Ready` and carry no reason;
- an active Job with a retained `Suggested` outcome is `WaitingForToolDecision`;
- the waiting state exposes the stable code `jobs.waiting_for_tool_decision` and an explanatory message.

Unity renders only non-ready readiness states. It does not derive waiting from action labels, disabled reasons or diagnostic text. The existing projection refresh after prepare or bypass removes the waiting notice immediately.

## Discoverability

Waiting Jobs are visible before selection in two ways:

1. `JobAttentionProjection` creates a bounded priority-ordered HUD summary with at most three rows and a hidden-row count.
2. `DigJobRenderer` uses a dedicated waiting material whenever the immutable readiness model is non-ready.

The attention summary supplies description, readiness label, resident and stable reason code. Selecting a row selects the existing marker and opens the normal Job panel; it does not resolve or mutate the Job.

## Typed Job actions

`JobOverlayViewModel.Actions` contains immutable `JobActionViewModel` values keyed by `JobActionKind`. Unity renders the supplied label, enabled state and disabled reason; it does not infer action availability from assignment diagnostics, resident text or reservation rows.

A retained `Suggested` assignment produces two actions:

1. `PrepareSuggestedTool` equips the reserved stack. It is enabled only when the active resident and exact Tool reservation still match the suggestion.
2. `BypassSuggestedTool` explicitly proceeds without switching. It remains enabled for any active Job, including stale-resident or missing-reservation recovery cases.

Both actions are disabled when the Job is no longer `Claimed` or `InProgress`. Assignments marked `Switched`, `Bypassed` or `AlreadyEquipped` expose no manual tool actions.

## Generic rendering and action dispatch

`DigHudOverlay.DrawJobAction` is the single renderer for every typed Job action. It renders the supplied label, combines `action.IsEnabled` with the surrounding `GUI.enabled` state, restores the previous GUI state, and shows the supplied disabled reason. The common rendering path has no action-specific drawing method or `switch`.

Only a successful button click calls `JobActionDispatcher`. The dispatcher is an immutable execution routing table from `JobActionKind` to a typed handler. It rejects duplicate registrations, validates the Job id, rejects disabled actions before invoking a handler, and fails explicitly when no route is registered.

Unity registers prepare and bypass execution handlers in one lazy composition point. Adding another action requires only a new typed model from Presentation and a registered execution route; common button rendering remains unchanged.

## Equip suggested tool

When `PrepareSuggestedTool` is clicked, the HUD sends only the Job id and current simulation tick to `PrepareSuggestedJobToolHandler`.

Before switching, the Application use case verifies all authoritative conditions again:

1. the Job still exists and is `Claimed` or `InProgress`;
2. the retained report still contains a `Suggested` assignment;
3. the report resident still matches the Job resident;
4. the suggested stack is still reserved as the Job's Tool reservation.

Only then does `InventoryJobToolPreparationService` execute the safe switch. The Job and all reservations remain unchanged. The retained report preserves the assignment score, uses the current tick and changes the preparation outcome to `Switched`.

After success Unity reloads the indexed Job overlay and refreshes equipment visuals and resident work-rate presentation from authoritative Inventory snapshots.

## Proceed without suggested tool

`BypassSuggestedJobToolHandler` is the explicit recovery path. It verifies that the Job is active and the retained assignment is still `Suggested`, then:

- releases only the Job-owned Tool reservation when it still exists;
- preserves the Job, resident, Job target and designation reservations;
- records `Bypassed` with the current assigned resident and the original score/tool stack;
- allows the next `AdvanceJobCommand` to progress normally.

A missing Tool reservation or stale report resident does not prevent bypass. This avoids a deadlock when the suggestion can no longer be prepared safely.

## Validation

Engine-independent tests verify:

- the control defaults to `Automatic` and changes only on an explicit selection;
- a live `Suggest` source overrides an automatic command without invoking tool preparation;
- a live `Automatic` source overrides a suggest command and invokes tool preparation;
- a pending suggestion keeps the Job claimed and preserves reservations without returning a failure;
- `Switched` and `Bypassed` outcomes unblock advancement;
- bypass releases only the Tool reservation and emits the bounded reservation-release event;
- bypass recovers stale-resident and missing-reservation suggestions;
- Presentation exposes prepare and bypass with distinct enabled-state rules;
- Presentation exposes waiting readiness only for active retained suggestions and returns to `Ready` after resolution;
- attention projection filters, orders and bounds waiting rows while preserving compact details;
- resolved assignments expose no manual tool actions;
- the dispatcher routes exact enabled actions and rejects disabled, duplicate or missing routes.

The normal Quality workflow validates architecture boundaries, file sizes, C# 9 compatibility, Release build, all tests, headless smoke and deterministic soak profiles. Unity Editor button interaction remains a local Play Mode check.
