# Simulation runtime implementation

## Status

This document describes the implementation for GitHub Issue #2. It is subordinate to the rules in `docs/development-rules.md` and does not redefine them.

## State ownership

`SimulationState` is the composition root for authoritative runtime foundation state:

- `SimulationClock` owns the simulation tick, fixed tick duration and selected rate;
- `RandomStreamCatalog` owns deterministic named random streams;
- `EntityRegistry` owns the set of live `EntityId` values;
- gameplay systems own their own future domain state and receive runtime services through `SimulationContext`.

The scheduler and runner orchestrate this state but do not duplicate it.

## Tick semantics

The simulation uses a fixed tick duration. `SimulationRunner.Advance` converts elapsed real time into integer simulation ticks.

Rates are integer multipliers:

- paused: 0;
- normal: 1;
- fast: 2;
- very fast: 4.

Paused time is discarded. It is not accumulated for catch-up after resuming.

`SimulationRunner.Step` is intended for tests, headless tools and controlled debugging. It executes an exact number of ticks independently of the selected rate.

## System scheduling

Each `ISimulationSystem` declares:

- a stable name;
- an integer order;
- an interval in ticks.

Before the first execution, systems are sorted by order and then by ordinal name. Registration is sealed after execution begins. This prevents runtime registration order from changing simulation results.

A system executes when the current tick is divisible by its interval.

## Deterministic random streams

Randomness is split into named streams derived from `WorldSeed` and a stable UTF-8 hash of the stream name.

Systems must use dedicated names such as:

```text
runtime.entity_ids
world.terrain
world.resources
agents.decisions
combat.outcomes
```

Adding random calls to one stream must not shift unrelated systems that use another stream.

Stream state is snapshot-friendly. Save/load must restore both the world seed and the current state of every created stream.

## Entity identity

`EntityRegistry` is the single source of truth for live entity identifiers.

It supports:

- deterministic creation of new identifiers;
- registration of restored identifiers;
- duplicate rejection;
- explicit removal;
- sorted snapshots for persistence and diagnostics.

An entity must be registered before other systems publish references to its identifier. Removing an entity must later be coordinated by an Application use case that also cleans indexes, jobs and reservations.

## Command, event and query pipeline

`CommandPipeline` records command name, tick, success and error code. It does not replace domain validation.

Domain events remain facts produced by authoritative state owners and are appended through `IEventSink`.

`QueryPipeline` performs read-only dispatch and does not journal state changes because queries must not have side effects.

## Headless execution

`Dig.Headless` proves that Domain and Application can run without Presentation. The current host executes a small deterministic simulation and prints its final tick and entity count.

Run it with:

```bash
dotnet run --project src/Dig.Headless/Dig.Headless.csproj
```

## Validation

Automated tests cover:

- equivalent results for different frame partitions;
- pause and speed behavior;
- stable system ordering and intervals;
- deterministic and unique entity identifiers;
- registry and random-stream snapshot continuation;
- entity removal;
- command journaling and factual domain events;
- side-effect-free query dispatch.
