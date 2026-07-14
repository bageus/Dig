# Unity jobs and reservations overlay

## Purpose

The job overlay makes work lifecycle and reservation ownership visible in the Unity settlement slice. It is a debug presentation of `JobSystem` and `ReservationLedger`, not a second job controller.

## State ownership

`JobSystem` remains the only owner of job status, assigned worker, current stage, retries and terminal state. Its `ReservationLedger` remains the only owner of active Job, Agent, Position and Designation claims.

Unity receives immutable `JobOverlayViewModel` values. A marker, line renderer or HUD panel can be deleted and rebuilt without changing jobs or reservations.

## Read path

`GetJobsQuery` supplies stable job snapshots. `GetJobReservationsQuery` supplies the ledger snapshot. `JobOverlayPresenter` combines them into one model per job containing:

- stable id and description;
- status, stage and priority;
- assigned resident id;
- digging target cell when the definition is a `DigJobDefinition`;
- retry state and diagnostic reason;
- every active reservation kind, value, owner and acquired tick.

The presentation model copies its reservation list and does not expose the repository or mutable domain objects.

## Unity representation

`DigJobRenderer` creates one small target marker for each job with a known cell target. Colors describe current status:

- amber: available;
- blue: claimed;
- green: in progress;
- red: blocked or failed;
- grey: completed or cancelled;
- white: selected.

An active cyan line connects the target marker to the assigned resident while the job still owns reservations. The line endpoint reads the interpolated resident visual position, but it never changes the assigned worker or logical job target.

Press `F3` to disable or restore the complete job debug root. Toggling the overlay changes only active Unity GameObjects; it does not execute a command and cannot affect simulation behavior.

## Demo lifecycle

The bootstrap creates real digging jobs for the initially designated cavern cells. It uses the normal Application handlers:

1. `CreateDigJobHandler` adds and makes each job available.
2. `AssignAvailableJobsHandler` scores deterministic resident candidates and claims jobs.
3. `AdvanceJobHandler` starts and advances work stages every third resident demo tick.
4. The presenter reloads immutable jobs and reservations after each advance.

When a job reaches `Completed`, `JobSystem` releases all ledger entries. The marker remains as a terminal diagnostic, while its worker link disappears because no active reservations remain.

The demonstration does not yet excavate the target cell on job completion. Connecting job finalization to World excavation and produced Inventory resources belongs to the full playable digging orchestration slice.

## Selection and HUD

Left-clicking a marker selects the job and shows:

- description and priority;
- status and stage;
- worker and target cell;
- retry state and failure reason;
- all active reservation keys and acquisition ticks.

Selecting a cell or resident clears job selection. Selecting a job clears cell and resident selection.

## Validation

Engine-independent tests verify:

- digging target coordinates are mapped correctly;
- status, stage, priority and assigned resident are preserved;
- claimed jobs expose Job, Agent, Position and Designation reservations;
- reservation order is stable;
- completed jobs expose no active ledger entries.

Normal CI still validates architecture, the 350-line limit, C# 9 compatibility, Release compilation, all tests, headless smoke and both deterministic soak profiles. Unity Editor execution remains a local Play Mode check.