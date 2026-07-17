# Unity jobs and reservations overlay

## Purpose

The job overlay makes work lifecycle, execution readiness and reservation ownership visible in the Unity settlement slice. It is a debug presentation of `JobSystem` and `ReservationLedger`, not a second job controller.

## State ownership

`JobSystem` remains the only owner of job status, assigned worker, current stage, retries and terminal state. Its `ReservationLedger` remains the only owner of active Job, Agent, Tool, Position and Designation claims.

Unity receives immutable `JobOverlayViewModel` values. A marker, line renderer, attention row or HUD panel can be deleted and rebuilt without changing jobs, inventory or reservations.

## Read path

`GetJobsQuery` supplies stable job snapshots. `GetJobReservationsQuery` supplies the ledger snapshot. `JobOverlayPresenter` combines them into one model per job containing:

- stable id and description;
- status, stage and priority;
- assigned resident id;
- digging target cell when the definition is a `DigJobDefinition`;
- preferred typed tool kind declared by the job;
- optional immutable assignment diagnostics from `JobAssignmentReport`;
- always-present immutable execution readiness;
- typed actions supplied by Presentation;
- retry state and diagnostic reason;
- every active reservation kind, value, owner and acquired tick.

The assignment diagnostic preserves typed `AlreadyEquipped`, `Suggested`, `Switched`, `Bypassed` or `None` preparation outcomes. A failed assignment exposes the stable domain error code and message instead of inventing a UI-only reason. The presentation model copies its reservation list and does not expose repositories or mutable domain objects.

## Unity representation

`DigJobRenderer` creates one small target marker for each job with a known cell target. Colors describe current status:

- amber: available;
- blue: claimed;
- green: in progress;
- red: blocked or failed;
- grey: completed or cancelled;
- magenta: execution is waiting for a player decision;
- white: selected.

The waiting material is chosen from `JobExecutionReadinessViewModel`, not reconstructed from status, actions or diagnostic text. After prepare or bypass refreshes the immutable model, the marker automatically returns to its normal status material.

An active cyan line connects the target marker to the assigned resident while the job still owns reservations. The line endpoint reads the interpolated resident visual position, but it never changes the assigned worker or logical job target.

Press `F3` to disable or restore the complete job debug root. Toggling the overlay changes only active Unity GameObjects; it does not execute a command and cannot affect simulation behavior.

## Attention summary

`JobAttentionProjection` filters non-ready Jobs, orders them by descending priority and stable Job id, and returns a bounded immutable summary. The default HUD limit is three rows; `HiddenCount` reports any additional Jobs without growing the overlay indefinitely.

Each row shows the Job description, readiness label, assigned resident and stable reason code. `Select` resolves the corresponding existing marker and opens the normal selected Job panel. The summary does not execute a tool command or change readiness.

## Demo lifecycle

The bootstrap creates real digging jobs for the initially designated cavern cells. It uses the normal Application handlers:

1. `CreateDigJobHandler` adds and makes each job available.
2. `AssignAvailableJobsHandler` scores deterministic resident candidates and claims jobs.
3. The immutable assignment report is retained by the indexed runtime journal.
4. `AdvanceJobHandler` starts and advances work stages when execution readiness permits.
5. The presenter reloads immutable jobs, reservations, assignment diagnostics and readiness after each change.

When a job reaches `Completed`, `JobSystem` releases all ledger entries. The marker remains as a terminal diagnostic, while its worker link disappears because no active reservations remain.

A future reassignment pass must replace the retained report with its new command result. Presentation never reconstructs preparation history from current equipment or UI state.

## Selection and HUD

Left-clicking a marker or choosing an attention row selects the job and shows:

- description and priority;
- status and stage;
- worker and target cell;
- preferred tool kind;
- non-ready execution state and stable reason;
- assignment score and preparation outcome when supplied;
- stable assignment failure code and message when assignment failed;
- typed resolution actions;
- retry state and failure reason;
- all active reservation keys and acquisition ticks.

`JobSelectionProjection` rebinds the selected stable Job id to the latest immutable model whenever the jobs collection is replaced. The HUD therefore follows status, stage, readiness and reservation refreshes instead of retaining an older snapshot; selection is cleared when the Job is no longer projected. The Unity adapter performs this scan only when the collection reference changes and does not allocate per frame.

The diagnostic is textual and does not rely on color alone. Selecting a cell or resident clears job selection. Selecting a job clears cell and resident selection.

## Validation

Engine-independent tests verify:

- digging target coordinates, status, stage, priority and assigned resident are preserved;
- preferred mining tool kind is projected from a digging job;
- assignment diagnostics preserve score, tick, outcome and tool stack;
- waiting readiness is projected only for active retained suggestions;
- attention rows filter ready Jobs and use stable priority/id ordering;
- the attention summary is bounded and reports hidden rows;
- compact readiness, worker and reason details are preserved;
- stable Job selection rebinds to the latest immutable model and clears when removed;
- claimed jobs expose Job, Agent, Tool, Position and Designation reservations;
- completed jobs expose no active ledger entries.

Normal CI still validates architecture, the 350-line limit, C# 9 compatibility, Release compilation, all tests, headless smoke and both deterministic soak profiles. Unity Editor execution remains a local Play Mode check.
