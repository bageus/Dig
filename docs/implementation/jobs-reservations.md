# Jobs and reservations

## Scope

The jobs module owns the lifecycle of executable settlement work. The first concrete work type is a typed digging job targeting a `CellId`; it is not represented by a string script.

`JobSystem` is the only object allowed to change job status. `ReservationLedger` is the only owner of active reservations. Callers receive immutable `JobSnapshot` and `ReservationSnapshot` values.

## Lifecycle

A job follows explicit transitions:

```text
Created -> Available -> Claimed -> InProgress -> Completed
                         |             |
                         +-----------> Blocked -> Available
                         |             |
                         +-----------> Cancelled
                         +-----------> Failed
```

The implemented statuses are:

- `Created` — definition registered but not offered;
- `Available` — dependencies are complete and candidates may claim it;
- `Claimed` — one worker and all required targets are reserved;
- `InProgress` — typed stages are advancing;
- `Blocked` — execution stopped with a diagnostic reason and retry tick;
- `Completed`, `Cancelled`, `Failed` — terminal states.

Digging uses the typed stages `TravelToTarget`, `PerformWork` and `Finalize`. Completing the final stage completes the job.

## Reservation ownership

Claiming a digging job atomically reserves:

- the job itself;
- the assigned `AgentId`;
- the target work position;
- the target digging designation.

Reserving the worker enforces at most one active job per resident. Reserving the target prevents two workers from claiming the same cell. If any key conflicts, no partial reservation is retained.

Completion, blocking, cancellation and failure release every reservation owned by the job. A blocked job must be claimed again after retry.

## Assignment

`AssignAvailableJobsHandler` processes jobs in deterministic order by priority, creation tick and job id. Candidate scores combine:

- job priority;
- worker skill;
- path or distance cost supplied by the candidate provider.

Ties are resolved by stable `AgentId` ordering. If the best worker is already reserved, assignment tries the next eligible candidate. The resulting report records both successful assignments and stable failure reasons.

## Dependencies and retries

A job cannot become available until every dependency has completed. Missing, active, cancelled or failed dependencies keep it unavailable.

Blocked jobs use `JobRetryPolicy` with a maximum retry count and delay in simulation ticks. Retrying before `NextRetryTick` fails with a stable error. Exceeding the retry limit moves the job to `Failed` with the `retry_exhausted` diagnostic code; no unbounded retry loop is possible.

## Diagnostics

`JobSnapshot` exposes status, typed stage, assigned worker, retry count, next retry tick and reason. `JobPresenter` converts this snapshot into a read-only diagnostic view suitable for Unity inspectors and debug overlays.

Domain events record status changes and reservation releases. Application handlers publish them through the shared `IEventSink`.

## Current integration boundary

The jobs module does not duplicate navigation, world or resident state. Candidate distance and availability are supplied through `IJobCandidateProvider`. Future simulation integration will use Navigation to reach the work position and World commands to excavate the target during `PerformWork`.

Inventory, produced resources, tools and hauling remain part of issue #7. Their identifiers already have dedicated reservation kinds so they can join the same atomic claim model without creating a second reservation owner.
