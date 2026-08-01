# Campfire production runtime cadence correction

Status: `APPROVED`; implementation and licensed Unity runtime evidence are tracked separately.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Authoritative parent specification: [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md).

## Confirmed correction — 2026-08-01

The production material duration stored by `ProductionState` and resolved by `ProductionStepTiming` is already expressed in authoritative simulation ticks.

Consequently:

- while the assigned worker is stationary at `ProductionWorkJob.WorkPosition` and carries the exact reserved material, every simulation tick advances one material-work tick;
- Unity runtime must not add an odd/even or other modulo cadence gate on top of the resolved duration;
- movement remains authoritative while the worker travels `internal stock -> workstation -> output package`;
- `ProductionWorkJob` in `PerformWork` projects the workstation target and a visible looping craft/build pose;
- carrying the reserved input at the workstation must not look like idle;
- after the material reaches its resolved duration, the existing exactly-once input consumption, package close and terminal order/job workflow remains unchanged.

## Failure, retry and concurrency

- A missing carried reservation still rejects work without consuming input or advancing progress.
- Route failure, forced movement, cancel, output-space blocking and save/load keep the parent specification behavior.
- Presentation animation is derived feedback and never owns production progress.
- Repeated rendering or pooled resident visuals must clear the production pose when movement or another action wins.

## Acceptance

- [ ] Unity production runtime has no additional `tick % 2` material-work gate;
- [ ] production work target and explicit production-work identity reach resident presentation;
- [ ] stationary production uses the looping build/craft pose while travel keeps movement/carry feedback;
- [ ] a checked-in Play Mode scenario executes package creation, internal-stock acquisition, progress, input consumption, package close and terminal order/job;
- [ ] the closed package contains the expected manifest and no reserved production material remains in resident inventory;
- [ ] Quality/build/.NET/smoke/soaks pass;
- [ ] licensed Unity EditMode/PlayMode execution is required before status may become `VERIFIED`.

## Verification boundary

Source contracts and checked-in Play Mode code establish `IMPLEMENTED`. Only retained evidence from a licensed Unity Test Runner establishes `VERIFIED`.
