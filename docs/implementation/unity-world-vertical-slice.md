# Unity world vertical slice

## Purpose

The first interactive Unity slice renders the authoritative logical world without moving game state into scene objects. It is intentionally asset-light: the current cells use generated primitives so camera, input, read models and command flow can be validated before final art is introduced.

## Ownership and flow

`WorldState` remains the only owner of terrain, exploration and cell designation. The Unity scene owns only rebuildable GameObjects, camera state, selection and HUD text.

The data flow is:

1. `GetWorldSnapshotQueryHandler` reads one immutable `WorldSnapshot` from `IWorldRepository`.
2. `WorldPresenter` converts it into stable ordered world, chunk and cell view models.
3. `DigWorldRenderer` rebuilds chunk roots and cell visuals from that view model.
4. Pointer input identifies a `DigCellVisual` through its collider.
5. The interaction component sends `DesignateDiggingCommand` through its Application handler.
6. Unity reads a fresh view model and rebuilds the visual state.

Deleting `World Visuals` or rebuilding the renderer does not mutate the world.

## Start the slice

Open `unity/Dig.Unity`, then run **Tools > Dig > Create Bootstrap Scene** if `Assets/Scenes/Main.unity` does not exist. Enter Play mode.

`DigUnityBootstrap` creates a deterministic 20 by 14 demonstration cavern, configures the camera, renders every cell and starts the HUD. The generated world exists only as the current composition-root scenario; it will be replaced by generation and save loading without changing the renderer contracts.

## Controls

- `W`, `A`, `S`, `D`: pan the camera;
- mouse wheel: zoom;
- `Q` and `E`: rotate the 2.5D view by 90 degrees;
- left mouse button: select a cell;
- right mouse button: toggle the selected cell designation through Application.

The HUD displays the authoritative world version and selected cell material, solidity, designation, hardness and temperature. Invalid commands show the Domain error rather than mutating presentation state.

## Visual conventions

- low floor cubes are non-solid explored cells;
- tall cubes are solid cells;
- orange cells are designated;
- selected cells are brightened locally;
- chunk GameObject names include their authoritative chunk version.

Colors and primitive meshes are presentation-only and can be replaced without changing Domain or Application.

## Validation

The .NET quality workflow verifies C# 9 compatibility, the full world query, immutable presentation ordering, side-effect-free repeated reads and the command-to-next-view round trip. It also runs all existing tests plus standard and large deterministic soaks.

Unity Editor compilation and Play mode remain a host-specific validation step. After pulling this change, reopen the project or reimport the local `com.bageus.dig.core` package before entering Play mode.
