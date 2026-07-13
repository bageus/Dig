# Dig

Dig is an independent 2.5D colony simulation inspired by the systemic ideas of classic underground settlement games. All runtime code, architecture, content, balance and assets are being created from scratch.

## Current status

The repository contains an engine-independent C# core with explicit Domain, Application, Infrastructure and Presentation boundaries. The deterministic simulation foundation includes fixed ticks, speed control, named random streams, entity identity, system scheduling and a headless host.

The logical world foundation now provides authoritative cells, immutable material definitions, chunk versioning, local invalidation, atomic terrain changes and immutable snapshots for future navigation and rendering systems.

The concrete Unity or Godot adapter has not been selected yet. Domain rules must remain usable without either engine.

## Repository structure

```text
src/
  Dig.Domain/                    Authoritative game rules, runtime and world state
  Dig.Application/               Commands, queries and simulation orchestration
  Dig.Infrastructure/            Technical adapter implementations
  Dig.Presentation.Abstractions/ Read models and presentation contracts
  Dig.Headless/                  Engine-free simulation bootstrap

tests/
  Dig.Tests/                     Domain and cross-layer tests

tools/quality/                   Architecture and file-size checks

docs/
  architecture/                  System and module design
  implementation/                Notes for implemented systems
  adr/                           Architecture decision records
```

## Requirements

- .NET 8 SDK for build and tests;
- Python 3.12+ for local quality checks.

The core libraries target `netstandard2.1` so they can be integrated into a future engine adapter.

## Build and test

```bash
python tools/quality/check_quality.py
dotnet restore Dig.sln
dotnet build Dig.sln --configuration Release --no-restore
dotnet test Dig.sln --configuration Release --no-build
dotnet run --project src/Dig.Headless/Dig.Headless.csproj
```

GitHub Actions runs the quality, build, test and headless smoke checks for pushes and pull requests.

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

Architecture foundation is tracked by issue [#1](https://github.com/bageus/Dig/issues/1). The deterministic simulation runtime is tracked by issue [#2](https://github.com/bageus/Dig/issues/2). The logical world and chunk model is tracked by issue [#3](https://github.com/bageus/Dig/issues/3). The next technical stage is navigation in the changing world, tracked by issue [#4](https://github.com/bageus/Dig/issues/4).

## License

A project license has not been selected yet. Until one is added, no permission to reuse the source code is granted by default.
