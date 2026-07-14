# Quality soak, performance budgets and invariants

## Purpose

The quality soak is a deterministic headless scenario for detecting cross-system regressions before UI or content scale hides them. It is not a benchmark of final release hardware. It establishes reproducible CI baselines, identifies expensive systems and fails on structural corruption.

The implementation completes issue #15 and extends the normal quality workflow rather than creating a separate test pipeline.

## Profiles and commands

Run the standard profile locally:

```bash
dotnet run --project src/Dig.Headless/Dig.Headless.csproj \
  --configuration Release -- \
  --soak --profile standard \
  --report soak-report-standard.json
```

Run the large settlement profile:

```bash
dotnet run --project src/Dig.Headless/Dig.Headless.csproj \
  --configuration Release -- \
  --soak --profile large \
  --report soak-report-large.json
```

Named defaults:

| Profile | Main ticks | Residents | Food | Hauling workers | One-run time budget |
|---|---:|---:|---:|---:|---:|
| `standard` | 2000 | 8 | 5000 | 4 | 30 seconds |
| `large` | 1000 | 64 | 64000 | 16 | 10 seconds |

Supported arguments:

- `--profile`: `standard` or `large`, default `standard`;
- `--seed`: deterministic 32-bit input seed, default `4242`;
- `--ticks`: overrides the profile main duration, minimum `100`;
- `--residents`: overrides the profile population from `2` to `64`;
- `--max-seconds`: overrides the one-run wall-clock budget;
- `--report`: JSON report path, default `soak-report-<profile>.json`.

The command runs the selected scenario twice with identical parameters and compares a SHA-256 hash of authoritative state. Timing data and the profile name are excluded from the hash.

## Scenario

One run contains:

- a fixed-tick `SimulationRunner`;
- multiple residents using real food, bed and leisure reservations;
- a bounded execution journal;
- recurring world item creation;
- deterministic automatic hauling;
- profile-scaled independent hauling workers;
- per-tick cross-system invariant validation;
- a final twenty-tick drain after resource spawning stops.

The drain requires all hauling work to finish. At the end there must be no active hauling jobs, Jobs reservations or Storage incoming reservations.

## State hash

The deterministic hash includes, in stable order:

- final tick and entity count;
- resident logical cell position, needs, schedule phase, active action and target;
- item stack identity, item type, quantity, location and reservations;
- job definition, status, stage, worker and retry count;
- Storage incoming reservations;
- building facility reservations.

Performance samples, wall-clock time and retained event ordering outside authoritative state are not hashed.

Adding logical resident positions in issue #52 intentionally changed both profile hashes. The current position-aware hashes are:

```text
standard: 8DF64EE713D040AFF7EB8330B5557B8C672D2C103FAD8E3F527544284F514659
large:    2AFB7E6757414C9A2DA34D8005C3478C74028DA7D38B7A35CD9BF95F1D45669A
```

Both position-aware baseline runs matched deterministic replay and contained zero invariant and performance-budget violations.

## Scheduler profiling

`SimulationScheduler` records one `SystemPerformanceSample` for every executed system, including executions that throw. A sample contains:

- simulation tick;
- stable system name;
- elapsed `Stopwatch` timestamp ticks;
- bytes allocated on the executing thread.

`InMemorySimulationPerformance` aggregates samples online. It does not retain every tick, so profiling memory remains bounded as simulation duration grows.

The report orders systems by total elapsed time and contains execution count, total and average time, maximum execution time and total and average allocated bytes.

## Profile budgets

Global budgets and stable-name overrides belong to the selected profile. A system override replaces all three global execution limits only for that system.

Standard dedicated budgets:

| System | Average execution | Average allocation | Maximum execution |
|---|---:|---:|---:|
| `agents.settlement` | 500 microseconds | 50000 bytes | 100 milliseconds |
| `soak.hauling` | 100 microseconds | 25000 bytes | 100 milliseconds |
| `soak.invariants` | 150 microseconds | 25000 bytes | 50 milliseconds |

Large dedicated budgets:

| System | Average execution | Average allocation | Maximum execution |
|---|---:|---:|---:|
| `agents.settlement` | 1800 microseconds | 325000 bytes | 100 milliseconds |
| `soak.hauling` | 150 microseconds | 20000 bytes | 100 milliseconds |
| `soak.invariants` | 500 microseconds | 175000 bytes | 50 milliseconds |

Budgets must be tightened from retained reports rather than guessed. A budget increase requires a documented reason and should not be used to hide a regression.

## First Linux CI baseline

The first successful standard GitHub Actions run on July 14, 2026 produced:

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

Original system baseline:

| System | Average time | Maximum time | Average allocations |
|---|---:|---:|---:|
| `agents.settlement` | 570.62 us | 39.88 ms | 295927 bytes |
| `soak.invariants` | 141.49 us | 19.02 ms | 85733 bytes |
| `soak.hauling` | 96.77 us | 23.01 ms | 60767 bytes |
| `soak.resource_spawn` | 8.39 us | 0.35 ms | 580 bytes |

## Settlement allocation optimization

Issue #32 replaced repeated full-state reads with owner-owned point queries. Inventory and Facilities no longer create full result arrays for each resident, the agent repository caches stable iteration order and settlement reuses one decision snapshot on the normal path.

Measured result:

| Result | Before | After | Change |
|---|---:|---:|---:|
| Settlement average time | 570.62 us | 213.73 us | -62.5% |
| Settlement maximum time | 39.88 ms | 30.16 ms | -24.4% |
| Settlement average allocations | 295927 bytes | 34517 bytes | -88.3% |
| Settlement total allocations | 597773120 bytes | 69724984 bytes | -88.3% |

## Hauling allocation optimization

Issue #34 completed the planner optimization that PR #35 had only started. It snapshots only available world stacks, computes destination occupancy directly, returns only the winning Storage zone, avoids empty planning collections and tracks soak-created jobs by id.

Measured result:

| Result | Before | After | Change |
|---|---:|---:|---:|
| Hauling average time | 162.24 us | 35.30 us | -78.2% |
| Hauling maximum time | 23.11 ms | 15.97 ms | -30.9% |
| Hauling average allocations | 60767 bytes | 13114 bytes | -78.4% |
| Hauling total allocations | 122749848 bytes | 26490424 bytes | -78.4% |

## Invariant checker allocation optimization

Issue #37 replaced full diagnostic snapshots with owner-owned inspection visitors. Inventory, Jobs, Storage and Facilities traverse their authoritative collections directly; lookup buffers are reused and valid reports share an empty result.

Measured result:

| Result | Before | After | Change |
|---|---:|---:|---:|
| Invariant average time | 165.69 us | 68.31 us | -58.8% |
| Invariant maximum time | 19.02 ms | 7.67 ms | -59.7% |
| Invariant average allocations | 84509 bytes | 16875 bytes | -80.0% |
| Invariant total allocations | 170710016 bytes | 34088096 bytes | -80.0% |

## Large settlement baseline

Issue #39 adds a second CI profile rather than replacing the fast standard regression. The first 64-resident Linux run produced:

| Result | Value |
|---|---:|
| Final tick | 1020 |
| Wall-clock time | 1398.87 ms |
| Residents / hauling workers | 64 / 16 |
| Initial / remaining food | 64000 / 55487 |
| Spawned / total / stored ore | 250 / 250 / 250 |
| Completed hauling jobs | 50 |
| Active hauling jobs | 0 |
| Jobs / Storage reservations | 0 / 0 |
| Retained / dropped events | 5000 / 105803 |
| Deterministic replay | matched |

Large system baseline:

| System | Average time | Maximum time | Average allocations |
|---|---:|---:|---:|
| `agents.settlement` | 1064.00 us | 29.47 ms | 270800 bytes |
| `soak.invariants` | 239.79 us | 5.07 ms | 134433 bytes |
| `soak.hauling` | 62.79 us | 26.56 ms | 7769 bytes |
| `soak.resource_spawn` | 19.28 us | 0.62 ms | 360 bytes |

The large profile exposes population-scale costs while retaining the same authoritative mechanics. Its state hash differs from standard because load parameters and initial state differ, but repeated large runs must match each other.

## Bounded diagnostics

`InMemoryExecutionJournal` retains up to `1000` commands and `5000` events. When a capacity is reached, it removes the oldest entries and increments dropped-entry counters. Both JSON reports expose retained and dropped totals.

## Invariants

`SettlementInvariantChecker` checks positive quantities and reservations, reservation capacity, Jobs worker ownership, hauling external links, terminal cleanup, Storage incoming links and resident food/facility targets. Failed checks retain deterministic sorting by code, entity and detail.

## CI artifacts

GitHub Actions runs the normal headless smoke, then both deterministic profiles. `soak-report-standard.json` and `soak-report-large.json` are uploaded even when a profile fails and are retained for fourteen days.

Each report contains profile identity, load parameters, state hash and replay result, quantity conservation, active/completed hauling counts, reservation counts, journal pressure, per-system performance summaries, budget violations, invariant violations and overall success.