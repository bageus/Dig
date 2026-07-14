# Quality soak, performance budgets and invariants

## Purpose

The quality soak is a deterministic headless scenario for detecting cross-system regressions before UI or content scale hides them. It is not a benchmark of final release hardware. It establishes a reproducible CI baseline, identifies expensive systems and fails on structural corruption.

The implementation completes issue #15 and extends the normal quality workflow rather than creating a separate test pipeline.

## Command

Run the default scenario locally:

```bash
dotnet run --project src/Dig.Headless/Dig.Headless.csproj \
  --configuration Release -- \
  --soak --ticks 2000 --residents 8 \
  --max-seconds 30 --report soak-report.json
```

Supported arguments:

- `--seed`: deterministic 32-bit input seed, default `4242`;
- `--ticks`: main simulation duration, minimum `100`, default `2000`;
- `--residents`: resident count from `2` to `64`, default `8`;
- `--max-seconds`: wall-clock budget for one run, default `30`;
- `--report`: JSON report path, default `soak-report.json`.

The command runs the scenario twice with identical parameters and compares a SHA-256 hash of authoritative state. Timing data is excluded from the hash.

## Scenario

One run contains:

- a fixed-tick `SimulationRunner`;
- multiple residents using real food, bed and leisure reservations;
- a bounded execution journal;
- recurring world item creation;
- deterministic automatic hauling;
- several independent hauling workers;
- per-tick cross-system invariant validation;
- a final twenty-tick drain after resource spawning stops.

The drain requires all hauling work to finish. At the end there must be no active hauling jobs, Jobs reservations or Storage incoming reservations.

## State hash

The deterministic hash includes, in stable order:

- final tick and entity count;
- resident needs, schedule phase, active action and target;
- item stack identity, item type, quantity, location and reservations;
- job definition, status, stage, worker and retry count;
- Storage incoming reservations;
- building facility reservations.

Performance samples, wall-clock time and retained event ordering outside authoritative state are not hashed.

## Scheduler profiling

`SimulationScheduler` records one `SystemPerformanceSample` for every executed system, including executions that throw. A sample contains:

- simulation tick;
- stable system name;
- elapsed `Stopwatch` timestamp ticks;
- bytes allocated on the executing thread.

`InMemorySimulationPerformance` aggregates samples online. It does not retain every tick, so profiling memory remains bounded as simulation duration grows.

The report orders systems by total elapsed time and contains:

- execution count;
- total milliseconds;
- average microseconds;
- maximum execution milliseconds;
- total allocated bytes;
- average allocated bytes.

## Budgets

Global CI budgets are deliberately conservative:

| Metric | Budget |
|---|---:|
| Main simulation duration | 2000 ticks |
| Drain duration | 20 ticks |
| Residents | 8 |
| Wall-clock time for one run | 30 seconds |
| Average system execution | 10000 microseconds |
| Average allocation per execution | 2000000 bytes |
| Maximum single execution | 500 milliseconds |

`SimulationPerformanceBudget` also supports stable-name overrides. A system override replaces all three global execution limits only for that system.

Dedicated regression budgets:

| System | Average execution | Average allocation | Maximum execution |
|---|---:|---:|---:|
| `agents.settlement` | 500 microseconds | 50000 bytes | 100 milliseconds |
| `soak.hauling` | 100 microseconds | 25000 bytes | 100 milliseconds |

Budgets must be tightened from retained reports rather than guessed. A budget increase requires a documented reason and should not be used to hide a regression.

## First Linux CI baseline

The first successful GitHub Actions run on July 14, 2026 produced:

| Result | Value |
|---|---:|
| Final tick | 2020 |
| Wall-clock time | 1643.38 ms |
| Residents | 8 |
| Spawned / total / stored ore | 500 / 500 / 500 |
| Completed hauling jobs | 100 |
| Active hauling jobs | 0 |
| Jobs reservations | 0 |
| Storage reservations | 0 |
| Retained / dropped events | 5000 / 23350 |
| Deterministic replay | matched |

System baseline:

| System | Average time | Maximum time | Average allocations |
|---|---:|---:|---:|
| `agents.settlement` | 570.62 us | 39.88 ms | 295927 bytes |
| `soak.invariants` | 141.49 us | 19.02 ms | 85733 bytes |
| `soak.hauling` | 96.77 us | 23.01 ms | 60767 bytes |
| `soak.resource_spawn` | 8.39 us | 0.35 ms | 580 bytes |

The baseline identified settlement snapshot and collection creation as the first Alpha optimization target.

## Settlement allocation optimization

Issue #32 replaced repeated full-state reads with owner-owned point queries:

- Inventory category availability scans authoritative stacks without creating an `InventorySnapshot`;
- food target selection returns only the stable winning stack id;
- reservation validation reads the requested stack directly;
- Facilities availability and target selection scan authoritative definitions without result arrays;
- release and resident-reservation checks avoid temporary LINQ collections;
- the in-memory agent repository caches stable resident iteration order;
- settlement reuses one decision snapshot on the normal path.

The first optimized Linux CI soak produced:

| Result | Before | After | Change |
|---|---:|---:|---:|
| Overall wall-clock | 1643.38 ms | 1286.58 ms | -21.7% |
| Settlement average time | 570.62 us | 213.73 us | -62.5% |
| Settlement maximum time | 39.88 ms | 30.16 ms | -24.4% |
| Settlement average allocations | 295927 bytes | 34517 bytes | -88.3% |
| Settlement total allocations | 597773120 bytes | 69724984 bytes | -88.3% |

## Hauling allocation optimization

Issue #34 completed the planner optimization that PR #35 had only started. The final implementation:

- snapshots only world stacks with available quantity;
- computes one storage occupancy directly from Inventory;
- returns only the winning Storage destination;
- avoids empty planning lists and sorted wrappers;
- uses the same point occupancy query during hauling job creation;
- tracks soak-created jobs by id instead of materializing complete terminal job history multiple times per tick.

The budget-enforced Linux CI soak produced:

| Result | Before | After | Change |
|---|---:|---:|---:|
| Overall wall-clock | 1286.58 ms | 620.77 ms | -51.7% |
| Hauling average time | 162.24 us | 35.95 us | -77.8% |
| Hauling maximum time | 23.11 ms | 13.23 ms | -42.8% |
| Hauling average allocations | 60767 bytes | 13117 bytes | -78.4% |
| Hauling total allocations | 122749848 bytes | 26496616 bytes | -78.4% |

Authoritative behavior did not change across the settlement and hauling optimizations. The retained state hash is:

```text
B315282B332B67B4EEE68D3B3C59D997013C014A947B534106DA7FD75EC04480
```

The final run also retained 500 / 500 / 500 spawned, total and stored ore, completed 100 hauling jobs and ended with zero active hauling jobs, Jobs reservations, Storage reservations, invariant violations or budget violations.

## Bounded diagnostics

`InMemoryExecutionJournal` accepts optional command and event capacities. When a capacity is reached, it removes the oldest entries and increments dropped-entry counters.

The soak uses:

- up to `1000` retained commands;
- up to `5000` retained events.

The JSON report exposes retained and dropped totals so diagnostic pressure remains visible without unbounded memory growth.

## Invariants

`SettlementInvariantChecker` is reusable outside the soak host. It checks:

- positive item quantities and reservations;
- reserved quantity not exceeding stack quantity;
- one reservation record per owner and stack;
- Jobs reservations referencing an active job and its assigned worker;
- one active reserved job per worker;
- active hauling jobs owning exact Inventory and Storage reservations;
- terminal hauling jobs owning no external reservations;
- Storage incoming reservations referencing active hauling jobs;
- food actions owning an item reservation;
- sleep and leisure actions owning the correct facility reservation;
- facility reservations referencing the matching active resident target.

The checker returns an immutable report and can throw with all sorted violations in debug, test or soak modes.

## CI artifacts

GitHub Actions runs the standard headless smoke first, then the deterministic soak. `soak-report.json` is uploaded even when the soak step fails and is retained for fourteen days.

The report contains:

- state hash and replay result;
- quantity conservation totals;
- active and completed hauling counts;
- reservation counts;
- journal retention pressure;
- per-system performance summaries;
- budget violations;
- invariant violations;
- overall success.
