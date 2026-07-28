# Barrel direct-command agent reservation correction — 2026-07-28

Status: `IMPLEMENTED` pending licensed Unity Play Mode execution.

Authoritative design: [`../design/destructible-barrels.md`](../design/destructible-barrels.md).
Tracking: [#443](https://github.com/bageus/Dig/issues/443), direct-command lifecycle [#390](https://github.com/bageus/Dig/issues/390).

## Runtime symptom

A selected resident received a direct barrel attack order while another active job still owned the resident reservation. Unity reported:

`InvalidOperationException: Validated barrel attack start failed: jobs.agent_unavailable`

from `StartDirectBarrelAttackCommandHandler`.

## Root cause

The runtime direct-command adapter now releases every nonterminal assignment before starting a replacement command, but the Application barrel-start boundary still assumed `MakeAvailable -> Claim -> Start` could not fail and converted every rejection into an invariant exception. A stale or same-tick reservation conflict therefore stopped the runtime instead of returning the existing typed `jobs.agent_unavailable` reason.

## Correction

- barrel start preflights `ReservationKey.ForAgent(workerId)` before creating a job;
- expected availability/claim/start failures return typed `Result` failures;
- if a rejection occurs after the new job is added, the new attempt is cancelled and its reservations are released;
- the barrel lifecycle, contents and materialization state remain unchanged on rejection;
- the Unity direct-command path continues to replace every existing nonterminal assignment through `PrepareResidentsForDirectCommand` before claiming the barrel job.

The start handler was moved to `BarrelAttackStartUseCase.cs` to keep the application file-size boundary and separate start transaction ownership from arrival/hit/destruction handlers.

## Regression evidence

- `BarrelAttackApplicationTests` verifies an already-reserved worker returns `JobErrors.AgentUnavailable` without an exception, zombie job or barrel mutation;
- `BarrelDirectCommandReservationContractTests` locks the typed failure, rollback and generic direct-assignment replacement contracts;
- `BarrelAttackSurfacePlayModeTests` creates an active ordinary job, issues the real direct barrel command, verifies the old job becomes available and verifies the barrel attack owns the selected resident reservation.

The barrel system remains `IMPLEMENTED` until the executable Play Mode scenario runs in a licensed Unity Test Runner.
