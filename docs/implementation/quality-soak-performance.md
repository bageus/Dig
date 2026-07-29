> **Status (2026-07-29): IMPLEMENTED repository quality system.** Blocking architecture/source contracts, Release build, full .NET tests, headless smoke, `standard`/`large` deterministic soak profiles, retained reports and evidence-tool self-tests are active. Licensed Unity execution is intentionally owned by [#511](https://github.com/bageus/Dig/issues/511); a blocked activation manifest does not promote any system to `VERIFIED`.

# Quality soak, performance budgets and invariants

## Purpose

The quality soak is a deterministic headless scenario for detecting cross-system regressions before UI or content scale hides them. It is not a benchmark of final release hardware. It establishes reproducible CI baselines, identifies expensive systems and fails on structural corruption.

The headless implementation completes the deterministic part of issue #15. PR #412 restored the blocking smoke and soak gates and added a source contract that prevents them from being silently removed. The repository quality/CI acceptance is complete; licensed Unity EditMode/PlayMode execution is tracked independently by #511.

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
- profile-scaled hauling workers selected from the authoritative resident repository;
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
standard: 6C7E4D06A3C97F1F05F64E208E393C97C7316891E148E726ECEC53A481E58AD7
large:    8CDFAFF348C42E690D665441B66364CBEC53D490233B7AE2A6AC02CFFE885DC2
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
| `agents.settlement` | 1800 microseconds | 400000 bytes | 75 milliseconds |
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

Issue #39 added a second profile rather than replacing the fast standard regression. The first 64-resident Linux run produced:

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

## Restored CI incident and remediation

The first restored `standard` soak exposed a real integration defect: the scenario created hauling worker entity IDs in `SimulationState.Entities` without adding matching residents to `IAgentRepository`. Hauling completion correctly attempted a skill grant and failed with `agents.repository.not_found`. The scenario now selects its hauling workers from registered residents, and `HeadlessSoakScenarioTests` requires at least one completed hauling job so this path cannot regress behind source-only checks.

The restored gate also exposed stale profiling costs. `SettlementInvariantChecker` was materializing full resident snapshots every tick only to inspect active targets. It now uses the owner-provided allocation-free `AgentState.ActiveActionTarget` query. On the restored standard CI profile this changed invariant checks from approximately `60,550` allocated bytes and `266.39 us` per execution to `64` bytes and `21.69 us`.

The large settlement budget was calibrated from retained current Linux CI evidence rather than guessed: `agents.settlement` currently averages `375,810` allocated bytes and `1,255.41 us`, so the blocking limits are `400,000` bytes and `1,800 us`. The `large` step uses `if: always()` only so its diagnostics still run when `standard` fails; it has no `continue-on-error` and remains a blocking gate.

Final restored evidence is Quality run `30221694341`:

| Profile | Result | Elapsed | Completed hauling | Active jobs/reservations | Deterministic replay |
|---|---|---:|---:|---:|---|
| `standard` | passed | 578.91 ms | 100 | 0 | matched |
| `large` | passed | 1386.50 ms | 50 | 0 | matched |

Both reports contain zero budget and invariant violations. Stage 2 v2 run `30221694265` and v3 run `30221694264` also passed for the same branch head.

## Bounded diagnostics

`InMemoryExecutionJournal` retains up to `1000` commands and `5000` events. When a capacity is reached, it removes the oldest entries and increments dropped-entry counters. Both JSON reports expose retained and dropped totals.

## Invariants

`SettlementInvariantChecker` checks positive quantities and reservations, reservation capacity, Jobs worker ownership, hauling external links, terminal cleanup, Storage incoming links and resident food/facility targets. Failed checks retain deterministic sorting by code, entity and detail.

## CI evidence

The current `.github/workflows/quality.yml` runs, in blocking order:

1. architecture and Unity source contracts;
2. Release restore/build and the complete .NET test suite;
3. the normal headless smoke scenario;
4. the `standard` deterministic soak profile;
5. the `large` deterministic soak profile.

The workflow uploads `headless-smoke-log`, `soak-report-standard` and `soak-report-large` artifacts. Each soak artifact retains both the JSON report and console log. `tools/quality/check_quality_workflow_contracts.py` fails when these commands or artifacts are removed or changed to non-blocking execution.

`.github/workflows/unity-playmode.yml` is a separate licensed gate. It selects EditMode and PlayMode together, retains raw results and always publishes `unity-runtime-evidence.json`. Without activation the manifest is `blocked`; with activation the validator requires passing XML, required mode tests and a clean representative-scene runtime log. Actual licensed execution and Unity runtime budget calibration remain tracked by #511, not by closed repository-quality issue #15.

## Repository implementation closure

Issue #15 can close as `IMPLEMENTED` because its repository-owned quality infrastructure is present and blocking:

- architecture, dependency, file-size and Unity source contracts;
- Release restore/build and the complete .NET suite;
- headless smoke;
- deterministic standard/large soak with retained JSON/log artifacts;
- stable performance budgets and invariant diagnostics;
- Unity workflow configuration for EditMode + PlayMode;
- machine-readable evidence validator with self-test.

The external licensed run is not discarded or reclassified as passed. It moves intact to #511, whose `verified` manifest is required before any Unity system becomes `VERIFIED`.
