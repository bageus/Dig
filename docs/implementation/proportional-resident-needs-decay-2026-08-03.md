# Proportional resident needs decay — 2026-08-03

**Status:** IMPLEMENTED IN BRANCH  
**Authoritative design:** `docs/design/runtime-needs-supply-sleep-food-recovery.md`  
**Tracking:** #159  
**Pull request:** #598

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

## Runtime and fixture correction

The production runtime now resolves passive decay from the authoritative schedule day length. The standard and large deterministic soak fixtures and the resident headless smoke use the same 24-tick day instead of compressing a full day into six or eight ticks while leaving Eat/Sleep action durations unchanged. This preserves the gameplay contract and keeps the test workloads representative rather than weakening survival checks.

## Regression scope

Domain tests cover exact two-/three-day totals, proportional per-tick bounds, the unchanged 100-tick Mood total, and parity between passive-only and ordinary Work execution. A checked-in Unity Play Mode scenario covers the full demo periods and verifies that active ordinary work does not accelerate passive need loss.

## Validation — code/test head `3878d207450fdeb1a1a743c73248938910708ac7`

Quality run `30829493729` passed:

- architecture, file-size and C# 9 compatibility checks;
- Unity source contracts;
- Release build with `0` warnings and `0` errors;
- all `1438/1438` .NET tests;
- headless smoke at tick `20`;
- standard deterministic soak: 8 residents, 2000 ticks plus drain, replay verified;
- large deterministic soak: 64 residents, 1000 ticks plus drain, replay verified.

Stage 2 source exports passed:

- v2 `30829493860`;
- v3 `30829493734`.

Unity workflow `30829494983` completed through the blocked-evidence path. Actual EditMode/PlayMode execution and runtime-evidence validation were skipped because licensed activation was unavailable.

## Verification boundary

The implementation is covered by checked-in Play Mode source but remains `IMPLEMENTED IN BRANCH`, not `VERIFIED`, until licensed Unity EditMode/PlayMode executes the full 48-/72-tick scenario in the real composition.
