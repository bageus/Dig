# Proportional resident needs decay — 2026-08-03

**Status:** IMPLEMENTED IN BRANCH  
**Authoritative design:** `docs/design/runtime-needs-supply-sleep-food-recovery.md`  
**Tracking:** #159

## Root cause

The default policy applied very large passive deltas (`-400` Nutrition, `-350` Alertness, `-100` Mood) every simulation tick. Generic actions also applied separate negative need deltas on every action advance, so Work, PlayerOrder, Flee and Idle could drain the same need a second time. The resulting runtime duration was measured in seconds rather than complete in-game days.

## Corrected contract

- UI `100` / Domain `10_000` Nutrition spans exactly two schedule days.
- UI `100` / Domain `10_000` Alertness spans exactly three schedule days.
- `DailySchedule.TicksPerDay` owns day length; the demo uses 24 ticks.
- Mood keeps the existing 100-tick passive span.
- Fixed-point remainders are distributed through a periodic cumulative-fraction resolver; adjacent deltas differ by at most one Domain unit.
- Passive decay is the single ordinary negative need owner. Generic Work, PlayerOrder, Flee and Idle actions have zero need delta. Eat, Sleep and Rest retain only positive recovery components.
- Critical survival penalties remain explicit and are applied after passive decay as before.
- Tick phase is authoritative, so save/load needs no new accumulator or migration.

## Regression scope

Domain tests cover exact two-/three-day totals, proportional per-tick bounds, the unchanged 100-tick Mood total, and parity between passive-only and ordinary Work execution. A checked-in Unity Play Mode scenario covers the full demo periods and verifies that active ordinary work does not accelerate passive need loss.

## Verification boundary

Repository Quality, build, deterministic and source-contract checks are required before merge. Actual licensed Unity EditMode/PlayMode execution remains required before promoting the runtime behavior to `VERIFIED`.
