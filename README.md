# Dig

Dig is an independent 2.5D colony simulation inspired by the systemic ideas of classic underground settlement games. All runtime code, architecture, content, balance and assets are being created from scratch.

## Current status

The repository is in the architecture-foundation stage. The first implementation provides an engine-independent C# core with explicit Domain, Application, Infrastructure and Presentation boundaries.

The concrete Unity or Godot adapter has not been selected yet. Domain rules must remain usable without either engine.

## Repository structure

```text
src/
  Dig.Domain/                    Authoritative game rules and state
  Dig.Application/               Commands, queries and orchestration
  Dig.Infrastructure/            Technical adapter implementations
  Dig.Presentation.Abstractions/ Read models and presentation contracts

tests/
  Dig.Tests/                     Domain and cross-layer tests

tools/quality/                   Architecture and file-size checks

docs/
  architecture/                  System and module design
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
```

GitHub Actions runs the same checks for pushes and pull requests.

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

The current implementation corresponds to issue [#1](https://github.com/bageus/Dig/issues/1). The next technical stage is the deterministic simulation loop and entity identity described in issue [#2](https://github.com/bageus/Dig/issues/2).

## License

A project license has not been selected yet. Until one is added, no permission to reuse the source code is granted by default.
