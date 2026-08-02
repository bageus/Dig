# Resident free-time needs actions — implementation note

**Status:** IN PROGRESS  
**Date:** 2026-08-02  
**Authoritative design:** `docs/design/resident-schedule-needs-actions.md`  
**Tracking:** #159, #142, #113

## Intended implementation

- Gate automatic `Eat` in `AgentDecisionCandidates` while the schedule is Work, without preventing an already active/manual Eat.
- Keep the existing threshold event pipeline as the sole hunger-notification source.
- Add `FloorSleep` as a typed activity target.
- Resolve Sleep as Bed first and Floor fallback, with no phantom facility reservation.
- Apply targeted Eat/Sleep/Leisure effects proportionally on each real action interval.
- Remove the complete-only duplicate targeted effect.
- Apply Floor tier limits during each Sleep interval.
- Remove positive need recovery from Idle.

## Regression plan

- Domain: Work hunger versus direct order, free-time hunger, deterministic interval remainder.
- Application: food reservation, work-time non-consumption plus hunger event, Bed/Floor concurrency, progressive Leisure/Sleep/Eat, interruption.
- Unity Play Mode: authoritative settlement composition executes work-hunger notification and Floor Sleep; direct feed/Bed preference remain required for final runtime verification.

## Verification boundary

Repository source/unit checks cannot prove Unity runtime behavior. The note remains `IN PROGRESS` until final-head CI runs. It becomes `IMPLEMENTED` after Quality passes and remains not `VERIFIED` when the licensed Unity test step is skipped.