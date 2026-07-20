#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "World"
RUNTIME = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)


def read(path: Path) -> str:
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8-sig")


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing deposit contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden deposit contract {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    input_path = PRESENTATION / "TerrainDepositPresentationInput.cs"
    input_text = read(input_path)
    errors.extend(require(
        input_path,
        input_text,
        (
            "SpatialCellId cell",
            "string depositId",
            "bool isRevealed",
            "int remainingYield",
            "int maximumYield",
            "long version",
        ),
    ))

    view_path = PRESENTATION / "TerrainDepositViewModels.cs"
    view = read(view_path)
    errors.extend(require(
        view_path,
        view,
        (
            "TerrainDepositVisualState",
            "Hidden",
            "Revealed",
            "Damaged",
            "Depleted",
            "TerrainDepositConnection",
            "NegativeX",
            "PositiveX",
            "NegativeY",
            "PositiveY",
            "NegativeZ",
            "PositiveZ",
            "Only revealed or damaged deposits may expose a visual id.",
            "Hidden or depleted deposits cannot expose visual connections.",
        ),
    ))

    presenter_path = PRESENTATION / "TerrainDepositPresenter.cs"
    presenter = read(presenter_path)
    errors.extend(require(
        presenter_path,
        presenter,
        (
            "source.RemainingYield == 0",
            "!source.IsRevealed",
            "TerrainDepositVisualState.Hidden",
            "TerrainDepositVisualState.Revealed",
            "TerrainDepositVisualState.Damaged",
            "TerrainDepositVisualState.Depleted",
            "visibleDepositId = string.Empty",
            "ResolveConnections",
            "CalculateVersion",
        ),
    ))
    errors.extend(reject(
        presenter_path,
        presenter,
        ("UnityEngine", "UnityEngine.Random", "displayName"),
    ))

    decoration_view_path = (
        PRESENTATION / "TerrainDepositDecorationViewModels.cs"
    )
    decoration_view = read(decoration_view_path)
    errors.extend(require(
        decoration_view_path,
        decoration_view,
        (
            "TerrainDepositDecorationCellViewModel",
            "MaximumVariant = 3",
            "MaximumRotationQuarterTurns = 3",
            "MaximumScaleBand = 3",
            "MaximumConnectorsPerFace = 2",
            "OffsetBandX",
            "OffsetBandY",
            "TerrainDepositDecorationVolumeViewModel",
        ),
    ))

    decoration_presenter_path = (
        PRESENTATION / "TerrainDepositDecorationPresenter.cs"
    )
    decoration_presenter = read(decoration_presenter_path)
    errors.extend(require(
        decoration_presenter_path,
        decoration_presenter,
        (
            "if (!source.IsVisible)",
            "CalculateSeed(source.Cell, source.VisibleDepositId)",
            "ResolveScaleBand",
            "RotationQuarterTurns",
            "cells.Sort(CompareCells)",
            "CalculateVersion(cells)",
        ),
    ))
    errors.extend(reject(
        decoration_presenter_path,
        decoration_presenter,
        ("UnityEngine", "UnityEngine.Random", "SourceVersion", "displayName"),
    ))

    profile_path = RUNTIME / "DigTerrainDepositVisualProfile.cs"
    profile = read(profile_path)
    errors.extend(require(
        profile_path,
        profile,
        (
            "DigTerrainDepositProfileKind",
            "Iron",
            "Gold",
            "Crystal",
            "Coal",
            "Stone",
            "Material? revealedMaterial",
            "Material? damagedMaterial",
        ),
    ))
    errors.extend(reject(
        profile_path,
        profile,
        ("hiddenMaterial", "depletedMaterial", "Collider", "GameObject"),
    ))

    catalog_path = RUNTIME / "DigTerrainVisualCatalog.Deposits.cs"
    catalog = read(catalog_path)
    errors.extend(require(
        catalog_path,
        catalog,
        (
            "DigTerrainDepositVisualProfile[] depositProfiles",
            "ResolveDeposit(",
            "state != TerrainDepositVisualState.Revealed",
            "state != TerrainDepositVisualState.Damaged",
            "RequireDepositKind(",
        ),
    ))

    deposits_path = RUNTIME / "DigTerrainRenderSnapshotBuilder.Deposits.cs"
    deposits = read(deposits_path)
    errors.extend(require(
        deposits_path,
        deposits,
        (
            "_previousDepositDecorations",
            "TerrainDepositDecorationVolumeViewModel? decorationVolume",
            "solidCells.Contains(key)",
            "MarkChangedDepositDecorations",
            "CalculateDepositDecorationSignature",
            "decoration.Variant",
            "decoration.RotationQuarterTurns",
            "decoration.ScaleBand",
            "Deposit decoration dimensions must match terrain.",
        ),
    ))
    errors.extend(reject(
        deposits_path,
        deposits,
        ("UnityEngine.Random", "TerrainDepositVisualState.Hidden"),
    ))

    snapshot_path = RUNTIME / "DigTerrainRenderSnapshot.cs"
    snapshot = read(snapshot_path)
    errors.extend(require(
        snapshot_path,
        snapshot,
        (
            "_depositDecorations",
            "TryGetDepositDecoration",
            "TerrainDepositDecorationCellViewModel",
        ),
    ))

    key_path = RUNTIME / "DigTerrainMaterialKey.cs"
    key = read(key_path)
    errors.extend(require(
        key_path,
        key,
        (
            "DepositId",
            "DepositState",
            "DepositDamageBand",
            "DepositConnections",
            "HasVisibleDeposit",
        ),
    ))

    mesh_path = RUNTIME / "DigTerrainChunkMeshBuilder.cs"
    mesh = read(mesh_path)
    errors.extend(require(
        mesh_path,
        mesh,
        (
            "ResolveTerrainKey",
            "string.Empty",
            "TerrainDepositVisualState.Hidden",
            "AddDepositDecorations(",
        ),
    ))
    errors.extend(reject(
        mesh_path,
        mesh,
        ("TryGetDepositDecoration", "deposit.VisibleDepositId"),
    ))

    topology_path = RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorations.cs"
    topology = read(topology_path)
    errors.extend(require(
        topology_path,
        topology,
        (
            "snapshot.TryGetDepositDecoration(",
            "ResolveDepositKey",
            "AddDepositFace(",
            "position.Offset(0, 0, 1)",
            "position.Offset(0, 0, -1)",
            "position.Offset(-1, 0, 0)",
            "position.Offset(1, 0, 0)",
            "position.Offset(0, -1, 0)",
            "position.Offset(0, 1, 0)",
        ),
    ))

    geometry_path = (
        RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorationGeometry.cs"
    )
    geometry = read(geometry_path)
    errors.extend(require(
        geometry_path,
        geometry,
        (
            "AddDepositCluster",
            "AddDepositConnectors",
            "MaximumConnectorsPerFace",
            "AddDepositConnector",
            "AddDecorationTriangle",
        ),
    ))
    for path, text in ((topology_path, topology), (geometry_path, geometry)):
        errors.extend(reject(
            path,
            text,
            ("GameObject", "Collider", "UnityEngine.Random", "Instantiate("),
        ))

    renderer_path = RUNTIME / "DigTerrainChunkRenderer.cs"
    renderer = read(renderer_path)
    errors.extend(require(
        renderer_path,
        renderer,
        (
            "MixDeposit(",
            "key.HasVisibleDeposit",
            "catalog.ResolveDeposit(key.DepositId, key.DepositState)",
        ),
    ))

    renderer_deposits_path = RUNTIME / "DigTerrainChunkRenderer.Deposits.cs"
    renderer_deposits = read(renderer_deposits_path)
    errors.extend(require(
        renderer_deposits_path,
        renderer_deposits,
        (
            "snapshot.TryGetDepositDecoration(",
            "decoration.Variant",
            "decoration.RotationQuarterTurns",
            "decoration.ScaleBand",
            "decoration.OffsetBandX",
            "decoration.OffsetBandY",
        ),
    ))

    adapter_path = RUNTIME / "DigWorldRenderer.VisualCatalog.cs"
    adapter = read(adapter_path)
    errors.extend(require(
        adapter_path,
        adapter,
        (
            "TerrainDepositDecorationPresenter",
            "TerrainDepositDecorationVolumeViewModel? _terrainDepositDecorations",
            "SetTerrainDeposits",
            "_depositDecorationPresenter.Present(deposits)",
            "_terrainDepositDecorations,",
        ),
    ))
    errors.extend(reject(
        adapter_path,
        adapter,
        ("terrain.metal_bearing_rock", "GameObject.CreatePrimitive"),
    ))

    if errors:
        print("Unity terrain deposit contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity terrain deposit contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
