# Utility AI performance

## State ownership

`AgentState` remains the authoritative owner of resident needs, schedule, current action, player order and last decision. `AgentDecisionSystem` is a deterministic evaluator only: it reads an immutable `AgentSnapshot` and `AgentDecisionContext`, calculates utility scores and returns an `AgentDecision` without mutating another aggregate.

Candidate values exist only for one decision call. Option diagnostics remain immutable values stored by the resulting decision, and settlement diagnostics remain part of the per-tick report.

## Stable decision contract

Every live decision exposes seven `UtilityOptionDiagnostic` entries ordered by `AgentIntentKind`. The same order is the deterministic tie-break: unavailable or cooldown-blocked candidates are excluded, the highest final score wins, and equal scores select the lowest stable intent rank.

Critical survival, player orders, hysteresis, cooldown, reason codes and rejected-option details are unchanged. `AgentDecision.Explanation` remains lazy and cached.

## Allocation stages

Issue #41 replaced heap candidate objects and LINQ pipelines with value candidates and bounded loops, made option diagnostics values, removed one option-array copy and deferred explanation formatting.

Issue #45 removes the remaining temporary candidate array:

- seven unmanaged candidates are evaluated in a stack-local span;
- player-order identity is passed separately from scoring values;
- candidate indices are fixed to `AgentIntentKind`, so no runtime sort is needed;
- the equal-score tie-break is covered by a dedicated regression;
- `SettlementAgentDiagnostic` is a readonly value;
- `ResidentSettlementSystem` writes one pre-sized diagnostic array in stable repository order;
- `SettlementTickReport` accepts that stable array without sorting or copying it again;
- the public report constructor still sorts and copies caller-provided collections.

Full decision and settlement diagnostics remain available on every tick; no sampling or reduced-detail mode is used.

## Measured Linux CI result

The 64-resident large profile after #43 was the baseline for #45:

| `agents.settlement` metric | Before #45 | After #45 | Change |
|---|---:|---:|---:|
| Average execution | 765.282 us | 744.534 us | -2.7% |
| Average allocations | 90611 bytes | 64369 bytes | -29.0% |
| Total allocations | 92423680 bytes | 65657320 bytes | -29.0% |
| Whole large run | 926.08 ms | 912.05 ms | -1.5% |

The standard profile measured 110.41 us and 8203 allocated bytes per settlement execution.

Both deterministic state hashes remained unchanged:

```text
standard: B315282B332B67B4EEE68D3B3C59D997013C014A947B534106DA7FD75EC04480
large:    42B798277A05A1099E5C8DF3EC59F0B8931AE8C49BB6C10AD11238F2B8D0CC99
```

Both profiles retained quantity conservation, completed all hauling work and ended with zero active hauling jobs, zero Jobs or Storage reservations, zero invariant violations and zero budget violations.

## Regression budgets

| Profile | Average time | Average allocations | Maximum execution |
|---|---:|---:|---:|
| `standard` | 500 us | 12000 bytes | 100 ms |
| `large` | 1500 us | 80000 bytes | 75 ms |

Budget increases require a retained CI report and a documented reason. Further optimizations must preserve option order, equal-score tie-breaking, report input isolation, reason codes and both state hashes.
