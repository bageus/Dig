# Unity presentation host

## Purpose

The Unity project is a presentation host for Dig. It owns scenes, rendering, input and editor tooling, while authoritative simulation state remains in the engine-independent core under `src/`.

## Open the project

1. Clone the repository.
2. In Unity Hub, choose **Add project from disk**.
3. Select `<repository>/unity/Dig.Unity`, not the repository root.
4. Open it with Unity `6000.0.71f1` or a compatible Unity 6 LTS patch.
5. After scripts compile, run **Tools > Dig > Create Bootstrap Scene**.

The command creates `Assets/Scenes/Main.unity` with Unity's default camera and light plus a `Dig Runtime` object containing `DigUnityBootstrap`.

## Core integration

`unity/Dig.Unity/Packages/manifest.json` references `src/` as a local UPM package:

```json
"com.bageus.dig.core": "file:../../../src"
```

Assembly definitions mirror the existing `.csproj` dependency direction:

- `Dig.Domain` has no project references;
- `Dig.Application` references `Dig.Domain`;
- `Dig.Infrastructure` references Application and Domain;
- `Dig.Presentation.Abstractions` references Application and Domain;
- `Dig.Headless` is disabled in Unity unless `DIG_HEADLESS` is defined.

This keeps one source tree for both `dotnet` tests and Unity compilation. Engine-specific code belongs under `unity/Dig.Unity/Assets/Dig.Unity` and must not be added to Domain.

## Generated files

Unity-generated folders such as `Library`, `Temp`, `Logs`, `UserSettings`, builds and generated IDE files are ignored. Project assets, scripts, scenes, settings and their `.meta` files should be committed.
