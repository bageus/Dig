#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRODUCTION_ROOTS = (
    ROOT / "src",
    ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime",
)

FORBIDDEN_FRAGMENTS = (
    "SpatialCellId",
    ".SpatialPosition",
    "WithAdditionalOpenCells",
    "WithSynchronizedFrontLayer",
    "ExpandTunnelVolume",
    "SynchronizeFrontNavigation",
    "TerrainDepthVolumePresenter",
    "TerrainDepthVolumeViewModel",
    "SetTerrainDepthVolume",
)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, fragments: tuple[str, ...]) -> list[str]:
    text = read(path)
    relative = path.relative_to(ROOT)
    return [f"{relative}: missing authoritative XYZ contract {fragment!r}"
            for fragment in fragments if fragment not in text]


def scan_production() -> list[tuple[Path, str]]:
    files: list[tuple[Path, str]] = []
    for root in PRODUCTION_ROOTS:
        for path in sorted(root.rglob("*.cs")):
            files.append((path, read(path)))
    return files


def cell_id_argument_count(text: str, start: int) -> int | None:
    marker = "new CellId("
    index = start + len(marker)
    depth = 1
    commas = 0
    in_string = False
    escaped = False
    while index < len(text):
        char = text[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
        elif char == '"':
            in_string = True
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
            if depth == 0:
                return commas + 1
        elif char == "," and depth == 1:
            commas += 1
        index += 1
    return None


def reject_two_dimensional_construction(path: Path, text: str) -> list[str]:
    errors: list[str] = []
    marker = "new CellId("
    offset = 0
    while True:
        start = text.find(marker, offset)
        if start < 0:
            break
        count = cell_id_argument_count(text, start)
        if count == 2:
            line = text.count("\n", 0, start) + 1
            errors.append(
                f"{path.relative_to(ROOT)}:{line}: production CellId construction must provide X, Y and Z")
        offset = start + len(marker)
    return errors


def main() -> int:
    errors: list[str] = []
    production = scan_production()
    for path, text in production:
        for fragment in FORBIDDEN_FRAGMENTS:
            if fragment in text:
                errors.append(
                    f"{path.relative_to(ROOT)}: forbidden parallel-coordinate fragment {fragment!r}")
        errors.extend(reject_two_dimensional_construction(path, text))

    coordinates = ROOT / "src" / "Dig.Domain" / "World" / "Coordinates.cs"
    errors.extend(require(coordinates, (
        "public const int MinimumDepth = 0;",
        "public const int MaximumDepth = 3;",
        "public CellId(int x, int y, int z)",
        "return X == other.X && Y == other.Y && Z == other.Z;",
        "return HashCode.Combine(X, Y, Z);",
        'return $"{X},{Y},{Z}";',
        "public const int RequiredDepth = CellId.MaximumDepth + 1;",
        "if (depth != RequiredDepth)",
    )))

    save_contracts = ROOT / "src" / "Dig.Application" / "Saving" / "SaveContracts.cs"
    errors.extend(require(save_contracts, (
        "public const int CurrentVersion = 5;",
        "public AgentPositionsSaveData AgentPositions",
        "public TerrainDepositsSaveData TerrainDeposits",
        "IReadOnlyDictionary<EntityId, CellId>? agentPositions",
        "IReadOnlyCollection<TerrainDepositInstance>? terrainDeposits",
    )))
    migrations = ROOT / "src" / "Dig.Application" / "Saving" / "SaveMigrations.cs"
    errors.extend(require(migrations, (
        "SaveFormat.CurrentVersion",
        "AgentPositions",
        "TerrainDeposits",
        "CellZ",
        "OriginZ",
        "WorkPositionZ",
    )))

    tunnel = ROOT / "src" / "Dig.Domain" / "Navigation" / "TunnelNavigationVolume.FrontSync.cs"
    errors.extend(require(tunnel, (
        "FromWorldSnapshot(",
        "WorldSnapshot world",
    )))

    world_renderer = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime" / "DigWorldRenderer.cs"
    errors.extend(require(world_renderer, (
        "Dictionary<CellId, DigCellVisual>",
        "Dictionary<ChunkId, Transform>",
        "SelectAt(int x, int y, int z)",
    )))
    terrain_builder = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime" / "DigTerrainRenderSnapshotBuilder.cs"
    errors.extend(require(terrain_builder, (
        "int depth = world.Depth;",
        "AddWorldChunks(",
        "sourceCell.Z",
    )))

    world_tests = ROOT / "tests" / "Dig.Tests" / "AuthoritativeXyzWorldTests.cs"
    errors.extend(require(world_tests, (
        "WorldSize.RequiredDepth",
        "ReservationKey.ForPosition",
        "Same_xy_on_four_depth_layers_are_distinct_stable_cells_and_reservations",
        "World_mutation_changes_only_the_requested_depth_layer",
        "Authoritative_world_rejects_depth_outside_zero_to_three",
    )))
    round_trip = ROOT / "tests" / "Dig.Tests" / "SaveGameRoundTripTests.cs"
    errors.extend(require(round_trip, (
        "new CellId(1, 1, 2)",
        "new CellId(5, 4, 3)",
        "loaded.TerrainDeposits",
    )))

    hasher = ROOT / "src" / "Dig.Headless" / "Soak" / "HeadlessSoakStateHasher.cs"
    errors.extend(require(hasher, (
        ".Append(agent.Position)",
        ".Append(stack.Location)",
    )))

    deleted_paths = (
        ROOT / "src" / "Dig.Domain" / "World" / "SpatialCellId.cs",
        ROOT / "src" / "Dig.Domain" / "Navigation" / "TunnelNavigationVolume.Dynamic.cs",
        ROOT / "src" / "Dig.Presentation.Abstractions" / "World" / "TerrainDepthVolumePresenter.cs",
        ROOT / "src" / "Dig.Presentation.Abstractions" / "World" / "TerrainDepthVolumeViewModel.cs",
        ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime" / "DigWorldRenderer.DepthTerrain.cs",
        ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime" / "DigTerrainRenderSnapshotBuilder.Depth.cs",
    )
    for path in deleted_paths:
        if path.exists():
            errors.append(f"{path.relative_to(ROOT)}: obsolete parallel XYZ owner must be deleted")

    if errors:
        print("Authoritative XYZ contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: one bounded authoritative XYZ model across gameplay owners")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
