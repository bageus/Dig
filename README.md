# Dig

Dig is an independent 2.5D colony simulation inspired by the systemic ideas of classic underground settlement games. All runtime code, architecture, content, balance and assets are being created from scratch.

## Current status

The repository contains an engine-independent C# core with explicit Domain, Application, Infrastructure and Presentation boundaries. The deterministic simulation foundation includes fixed ticks, speed control, named random streams, entity identity, system scheduling and a headless host.

The logical world foundation provides authoritative cells, immutable material definitions, chunk versioning, local invalidation, atomic terrain changes and immutable snapshots.

Navigation derives versioned walkability caches and regions from world snapshots, refreshes changed chunks locally, supports multiple traversal profiles plus ladder and elevator links, and returns diagnosable paths with stale-result validation.

Residents own needs, schedules, skills, traits, player orders and one active action. Deterministic Utility AI explains every selected and rejected intent, uses hysteresis and cooldown against oscillation, and keeps critical survival above ordinary work or direct orders.

Jobs own their complete lifecycle from creation through completion, cancellation or failure. Typed digging, hauling and building-work jobs use deterministic worker scoring, dependency checks, bounded retries and a single worker and position reservation ledger.

Inventory owns immutable item definitions plus authoritative stack quantities, partial reservations and typed locations. Storage owns filters, priorities and incoming capacity. Transactional hauling conserves item quantity from a world cell to a stockpile or construction site and releases all reservations on completion or cancellation.

Buildings own immutable definitions, validated footprints, projects, construction progress and durability. Placement reads World and Navigation-derived reachability, materials remain in Inventory, and final construction atomically consumes delivered resources before emitting one completed building.

Unity is the selected presentation host. Engine-specific scenes, rendering, input and editor tooling live under `unity/Dig.Unity`; authoritative simulation rules remain usable without Unity.

## Repository structure

```text
src/
  Dig.Domain/                    Authoritative game rules and derived runtime state
  Dig.Application/               Commands, queries and simulation orchestration
  Dig.Infrastructure/            Technical adapter implementations
  Dig.Presentation.Abstractions/ Read models and presentation contracts
  Dig.Headless/                  Engine-free simulation bootstrap
  package.json                   Local Unity Package Manager entry point

tests/
  Dig.Tests/                     Domain and cross-layer tests

unity/
  Dig.Unity/                     Unity 6 presentation host and editor tooling

tools/quality/                   Architecture and file-size checks

docs/
  architecture/                  System and module design
  implementation/                Notes for implemented systems
  adr/                           Architecture decision records
```

## Requirements

- .NET 8 SDK for build and tests;
- Python 3.12+ for local quality checks;
- Unity `6000.0.71f1` or a compatible Unity 6 LTS patch for the presentation host.

The core libraries target `netstandard2.1` and are shared with Unity through a local UPM package.

## Build and test

```bash
python tools/quality/check_quality.py
dotnet restore Dig.sln
dotnet build Dig.sln --configuration Release --no-restore
dotnet test Dig.sln --configuration Release --no-build
dotnet run --project src/Dig.Headless/Dig.Headless.csproj
```

GitHub Actions runs the quality, build, test and headless smoke checks for pushes and pull requests.

## Open in Unity

1. Clone the repository.
2. In Unity Hub, choose **Add project from disk**.
3. Select `unity/Dig.Unity`, not the repository root.
4. Open the project and wait for the local core package to compile.
5. Run **Tools > Dig > Create Bootstrap Scene**.

See [`docs/implementation/unity-presentation-host.md`](docs/implementation/unity-presentation-host.md) for the integration details.

## Architectural rules

The authoritative development rules are in [`docs/development-rules.md`](docs/development-rules.md). In particular:

- every mutable state has one authoritative owner;
- Domain does not depend on the game engine, UI or file system;
- commands, events and queries are different contracts;
- handwritten source files cannot exceed 350 lines;
- system behavior must be testable and diagnosable;
- original game code and data are references only and are not part of the new implementation.

## Roadmap

See [`docs/ROADMAP.md`](docs/ROADMAP.md) and the [roadmap issue](https://github.com/bageus/Dig/issues/16).

Architecture foundation is tracked by issue [#1](https://github.com/bageus/Dig/issues/1). The deterministic simulation runtime is tracked by issue [#2](https://github.com/bageus/Dig/issues/2). The logical world and chunk model is tracked by issue [#3](https://github.com/bageus/Dig/issues/3). Navigation in the changing world is tracked by issue [#4](https://github.com/bageus/Dig/issues/4). Residents, needs and Utility AI are tracked by issue [#5](https://github.com/bageus/Dig/issues/5). Jobs and reservations are tracked by issue [#6](https://github.com/bageus/Dig/issues/6). Inventory, storage and hauling are tracked by issue [#7](https://github.com/bageus/Dig/issues/7). Buildings and construction are tracked by issue [#8](https://github.com/bageus/Dig/issues/8). The next progression stage is production and technology in issue [#9](https://github.com/bageus/Dig/issues/9). Unity presentation work is tracked by issue [#14](https://github.com/bageus/Dig/issues/14).

## License

A project license has not been selected yet. Until one is added, no permission to reuse the source code is granted by default.
