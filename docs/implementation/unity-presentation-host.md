# Unity presentation host

## Purpose

The Unity project is a presentation host for Dig. It owns scenes, rendering, input and editor tooling, while authoritative simulation state remains in the engine-independent core under `src/`.

## Open the project

1. Clone the repository.
2. In Unity Hub, choose **Add project from disk**.
3. Select `<repository>/unity/Dig.Unity`, not the repository root.
4. Open it with Unity `6000.0.71f1` or a compatible Unity 6 LTS patch.
5. After scripts compile, run **Tools > Dig > Create Bootstrap Scene**.
6. Open `Assets/Scenes/Main.unity` and enter Play mode.

The command creates the scene with Unity's default camera and light plus a `Dig Runtime` object containing `DigUnityBootstrap`.

## Interactive world slice

At runtime the bootstrap creates a deterministic demonstration cavern and renders it from immutable Presentation view models. Camera movement, cell selection, HUD diagnostics and designation input are available without any external art assets.

See [`unity-world-vertical-slice.md`](unity-world-vertical-slice.md) for controls, state ownership, visual conventions and the query-command-refresh flow.

## Interactive resident slice

The same scene creates four authoritative resident aggregates with logical cell positions. Their needs and Utility AI decisions advance on fixed demo ticks, movement is committed through Application commands, and rebuildable Unity visuals interpolate between confirmed cells.

Left-click a resident to inspect its needs, active action, decision reason and all Utility AI alternatives. See [`unity-resident-presentation.md`](unity-resident-presentation.md) for ownership, interpolation and current demo limitations.

## Jobs and reservations overlay

Initially designated cavern cells receive real digging jobs. The scene assigns residents through the common candidate scorer, advances job stages through Application handlers and renders target markers plus active worker links from immutable snapshots.

Left-click a marker to inspect lifecycle and reservation ownership. Press `F3` to hide or restore the complete diagnostic layer without changing simulation state. See [`unity-job-overlay.md`](unity-job-overlay.md) for colors, ownership and current orchestration limitations.

## Terrain work completion

Assigned residents now follow real Navigation paths to adjacent work cells. A final Application coordinator excavates the authoritative World cell, creates an Inventory resource stack and completes the Job only after all inputs pass preflight validation. Dirty world chunks refresh Navigation before later routes are calculated.

Cyan route lines can be toggled with `F4`. Excavated output appears as a rebuildable world-item visual. See [`unity-terrain-work-vertical-slice.md`](unity-terrain-work-vertical-slice.md) for the commit order, ownership rules, route selection and current demo limitations.

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
