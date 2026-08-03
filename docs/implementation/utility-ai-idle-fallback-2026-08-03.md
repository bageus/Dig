# Utility AI Idle Fallback During Cooldown — 2026-08-03

Status: `APPROVED`

Authoritative behavior remains defined by:

- [`../design/enemy-vision-and-resident-task-transition-pause-2026-08-03.md`](../design/enemy-vision-and-resident-task-transition-pause-2026-08-03.md);
- [`agents-utility-ai.md`](agents-utility-ai.md).

Tracking: #577. Implementation PR: #583.

## Runtime symptom

Unity stopped the simulation loop with:

`InvalidOperationException: Utility AI has no eligible candidate.`

The exception originated in `AgentDecisionSystem.SelectCandidate` after continuity rules left all seven utility candidates ineligible.

## Root cause

An active intent can become unavailable before `DecisionCooldownTicks` expires. The observed path is an active `Flee` action after the threat or escape route disappears:

1. `Flee` is unavailable because there is no valid escape route/current threat;
2. non-critical alternatives are blocked by the action-switch cooldown;
3. `Idle` was also blocked by the same cooldown;
4. `SelectCandidate` received no eligible candidate and threw instead of returning a deterministic fallback.

The cooldown is an anti-oscillation rule. It must not remove the always-available safe state when the current action can no longer continue.

## Fix

- `Idle` bypasses action-switch cooldown as well as command/job transition pause;
- current-intent hysteresis remains unchanged and still keeps an available current action selected;
- `PlayerOrder`, critical survival and emergency candidates retain their existing bypass rules;
- no second fallback path or exception swallowing was added.

## Regression coverage

`AgentDecisionIdleFallbackTests.Unavailable_current_intent_during_cooldown_falls_back_to_idle` creates an active `Flee`, removes every world capability including the escape route during the next cooldown tick, and verifies:

- no exception is thrown;
- unavailable `Flee` is diagnosed as `rejected.unavailable`;
- `Idle` is selected with `selected.utility`.

## Automated evidence

Code-and-test head `4ae491df1be0f0282e998c27894c5742179af389`:

- Quality `30773599097` / run 8480: success;
- architecture, file-size and C# compatibility checks: success;
- Unity source contracts: success;
- Release restore/build and full .NET suite: success;
- headless smoke: success;
- standard deterministic soak: success;
- large-settlement deterministic soak: success;
- Export Stage 2 v2 `30773599070`: success;
- Export Stage 2 v3 `30773599066`: success;
- Unity workflow `30773599064` / run 695: workflow success, but actual EditMode/PlayMode execution and runtime-evidence validation were skipped because licensed activation was unavailable.

## Verification boundary

The code and regression are present in draft PR #583 and automated non-Unity validation is green. The bug fix must not be marked merged before PR #583 merges, and must not be marked runtime `VERIFIED` without an actual licensed Unity Test Runner execution.