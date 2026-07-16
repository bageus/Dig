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
- the selected tool stack when tool-aware assignment chose one;
- the target work position;
- the target digging designation.

Reserving the worker enforces at most one active job per resident. Reserving the target prevents two workers from claiming the same cell. Reserving the selected tool prevents another assignment from planning the same carried or equipped item. If any key conflicts, no partial reservation is retained.

Completion, blocking, cancellation and failure release every reservation owned by the job. A blocked job must be claimed again after retry.

## Assignment

`AssignAvailableJobsHandler` processes jobs in deterministic order by priority, creation tick and job id. Candidate scores combine:

- job priority;
- matching-tool readiness;
- worker skill;
- path or distance cost supplied by the candidate provider.

A resident already holding the preferred tool ranks above one who can safely switch to it; both rank above candidates without a matching usable tool. Digging prefers mining equipment and building work prefers construction equipment. Ties are resolved by stable `AgentId` ordering.

`InventoryAwareJobCandidateProvider` derives tool readiness from authoritative inventory snapshots and immutable equipment profiles. It does not copy inventory state. A switch is offered only when the matching tool is a single unreserved carried item and the current equipped item can be safely returned to the resident inventory.

Assignment supports two preparation modes:

- `Suggest` claims the job, reserves the selected tool and reports that a switch is recommended without mutating inventory;
- `Automatic` performs the validated inventory switch before the atomic job claim and reports the completed switch.

If the best worker or selected tool is already reserved, assignment tries the next eligible candidate. The resulting report records the selected tool, preparation outcome, successful assignments and stable failure reasons.

## Dependencies and retries

A job cannot become available until every dependency has completed. Missing, active, cancelled or failed dependencies keep it unavailable.

Blocked jobs use `JobRetryPolicy` with a maximum retry count and delay in simulation ticks. Retrying before `NextRetryTick` fails with a stable error. Exceeding the retry limit moves the job to `Failed` with the `retry_exhausted` diagnostic code; no unbounded retry loop is possible.

## Diagnostics

`JobSnapshot` exposes status, typed stage, assigned worker, retry count, next retry tick and reason. `JobPresenter` converts this snapshot into a read-only diagnostic view suitable for Unity inspectors and debug overlays.

Domain events record status changes and reservation releases. Application handlers publish them through the shared `IEventSink`. Tool-aware assignment reports whether the preferred tool was already equipped, suggested for switching or switched automatically.

## Current integration boundary

The jobs module does not duplicate navigation, world, inventory or resident state. Candidate distance and availability are supplied through `IJobCandidateProvider`. Tool suitability is declared by a small Jobs-owned `JobToolKind` contract and resolved by an Application adapter against authoritative Inventory snapshots and `EquipmentRates`.

Future simulation integration will use Navigation to reach the work position and World commands to excavate the target during `PerformWork`. Inventory remains the only owner of item location and performs safe tool switching transactionally; Jobs owns only the selected tool reservation for the active assignment.
