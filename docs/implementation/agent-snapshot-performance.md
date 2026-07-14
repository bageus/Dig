# Agent snapshot performance

## Ownership

`AgentState` remains the owner of resident runtime state. `AgentSkillSet` owns skill levels and `AgentTraitSet` owns resident traits. Issue #43 caches only sorted read-only capability lists inside those owners.

Needs, health, schedule phase, active action, player order and last decision are still captured freshly for every snapshot. The skill list is refreshed when a skill value changes. Older snapshots retain their previous list and remain unchanged. The trait list is created once because traits have no mutation operation.

## Snapshot construction

The public `AgentSnapshot` constructor still sorts and copies supplied skill and trait collections. Later changes to those input collections cannot change the snapshot.

`AgentState.CreateSnapshot` uses an internal path for capability lists already normalized by their owners. This avoids repeated sorting, array copies and read-only wrappers on every tick. Snapshot-local skill dictionaries and trait sets were removed. These resident collections are small, so point lookups scan the immutable lists directly.

Tests cover stable ordering, list reuse, skill refresh, old-snapshot immutability and public defensive copying.

## Measured result

The first 64-resident large-profile run produced:

| Metric | Before #43 | After #43 | Change |
|---|---:|---:|---:|
| Settlement average time | 842.997 us | 816.582 us | -3.1% |
| Settlement average allocations | 172511 bytes | 90613 bytes | -47.5% |
| Settlement total allocations | 175961832 bytes | 92425648 bytes | -47.5% |
| Invariant average allocations | 134439 bytes | 18850 bytes | -86.0% |
| Whole large run | 1166.24 ms | 975.85 ms | -16.3% |

The standard profile measured 105.06 us and 11755 bytes for `agents.settlement`, and 39.33 us and 2414 bytes for `soak.invariants`.

Both hashes remained unchanged:

```text
standard: B315282B332B67B4EEE68D3B3C59D997013C014A947B534106DA7FD75EC04480
large:    42B798277A05A1099E5C8DF3EC59F0B8931AE8C49BB6C10AD11238F2B8D0CC99
```

The runs retained quantity conservation, completed all hauling work and ended with zero active hauling jobs, reservations, invariant violations or budget violations.

## Budgets

| Profile and system | Average time | Average allocations | Maximum execution |
|---|---:|---:|---:|
| standard `agents.settlement` | 500 us | 20000 bytes | 100 ms |
| standard `soak.invariants` | 150 us | 10000 bytes | 50 ms |
| large `agents.settlement` | 1500 us | 125000 bytes | 75 ms |
| large `soak.invariants` | 500 us | 40000 bytes | 50 ms |

Further changes must preserve old-snapshot immutability, public input isolation, stable capability ordering and both deterministic hashes.
