# Utility AI performance

## State ownership

`AgentState` remains the authoritative owner of resident needs, schedule, current action, player order and last decision. `AgentDecisionSystem` is a deterministic evaluator only: it reads an immutable `AgentSnapshot` and `AgentDecisionContext`, calculates utility scores and returns an `AgentDecision` without mutating another aggregate.

The performance representation does not introduce a second copy of mutable resident state. Candidate values exist only for one decision call, and option diagnostics are immutable values owned by the resulting decision.

## Stable decision contract

Every live decision still exposes seven `UtilityOptionDiagnostic` entries ordered by `AgentIntentKind`, the same order used as the deterministic tie-break. Each entry retains:

- base and final score;
- availability and critical flags;
- selected flag;
- selected or rejected reason code;
- human-readable diagnostic detail.

Selection semantics are unchanged:

1. unavailable and cooldown-blocked candidates are excluded;
2. the highest final utility score wins;
3. equal scores use the lowest stable intent rank;
4. critical survival and player orders retain their existing precedence rules;
5. hysteresis and cooldown use the same policy values.

`AgentDecision.Explanation` remains the same public string. The internal decision path formats and caches it only when a caller reads the property. The public constructor still validates and defensively copies external option collections.

## Allocation changes

Issue #41 removes allocation work that scaled with every resident and every simulation tick:

- seven heap candidate objects are replaced by one compact value array;
- LINQ filtering and two ordering pipelines are replaced by bounded loops and one seven-value sort;
- `UtilityOptionDiagnostic` is a readonly value type;
- the internal decision path transfers its completed option array without copying it a second time;
- explanation formatting is deferred until requested.

No decision-detail mode or sampling shortcut is used. Full diagnostic content remains available for every decision.

## Measured Linux CI result

The large profile uses 64 residents, 16 hauling workers, 1000 main ticks and a 20-tick drain.

| `agents.settlement` metric | Before #41 | After #41 | Change |
|---|---:|---:|---:|
| Average execution | 911.76 us | 874.06 us | -4.1% |
| Average allocations | 270794 bytes | 172517 bytes | -36.3% |
| Total allocations | approximately 276 MB | 175968024 bytes | -36.3% |
| Maximum execution | 22.82 ms | 28.45 ms | CI variance, within budget |

The standard profile measured 154.78 us and 21995 allocated bytes per settlement execution.

Both deterministic state hashes remained unchanged:

```text
standard: B315282B332B67B4EEE68D3B3C59D997013C014A947B534106DA7FD75EC04480
large:    42B798277A05A1099E5C8DF3EC59F0B8931AE8C49BB6C10AD11238F2B8D0CC99
```

Both profiles retained quantity conservation, completed all hauling work and ended with zero active hauling jobs, zero Jobs or Storage reservations, zero invariant violations and zero budget violations.

## Regression budgets

The measured result tightens settlement allocation budgets:

| Profile | Average time | Average allocations | Maximum execution |
|---|---:|---:|---:|
| `standard` | 500 us | 35000 bytes | 100 ms |
| `large` | 1500 us | 220000 bytes | 75 ms |

Budget increases require a retained CI report and a documented reason. A new optimization must preserve option order, reason codes, state hashes and resident-cycle tests.
