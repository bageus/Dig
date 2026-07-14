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

## Initial budgets

The first CI budgets are deliberately conservative:

| Metric | Budget |
|---|---:|
| Main simulation duration | 2000 ticks |
| Drain duration | 20 ticks |
| Residents | 8 |
| Wall-clock time for one run | 30 seconds |
| Average system execution | 10000 microseconds |
| Average allocation per execution | 2000000 bytes |
| Maximum single execution | 500 milliseconds |

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

The settlement system is the current primary optimization candidate. This is an observed baseline, not evidence that its allocations are acceptable for release scale.

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
