using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private const int DemoDepositAlgorithmVersion = 1;
    private const int DemoDepositDensityPermille = 160;
    private static readonly TerrainDepositDefinition[] DemoDepositDefinitions =
    {
        new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            new ItemId("ore.iron"),
            maximumYield: 8,
            generationWeight: 28),
        new TerrainDepositDefinition(
            "deposit.gold_ore",
            "Gold ore",
            new ItemId("ore.gold"),
            maximumYield: 5,
            generationWeight: 8),
        new TerrainDepositDefinition(
            "deposit.crystal_ore",
            "Crystal ore",
            new ItemId("ore.crystal"),
            maximumYield: 6,
            generationWeight: 12),
        new TerrainDepositDefinition(
            "deposit.coal",
            "Coal",
            new ItemId("material.coal"),
            maximumYield: 10,
            generationWeight: 24),
        new TerrainDepositDefinition(
            "deposit.stone",
            "Stone",
            new ItemId("material.stone"),
            maximumYield: 12,
            generationWeight: 28),
    };

    private readonly TerrainDepositPresenter _terrainDepositPresenter =
        new TerrainDepositPresenter();
    private readonly Dictionary<SpatialCellId, TerrainDepositInstance> _terrainDeposits =
        new Dictionary<SpatialCellId, TerrainDepositInstance>();

    internal IReadOnlyList<TerrainDepositDefinition> TerrainDepositDefinitions =>
        DemoDepositDefinitions;

    internal TerrainDepositVolumeViewModel LoadTerrainDeposits()
    {
        WorldViewModel world = LoadView();
        TerrainDepositPresentationInput[] inputs = _terrainDeposits.Values
            .OrderBy(value => value.Cell)
            .Select(value => new TerrainDepositPresentationInput(
                value.Cell,
                value.Definition.Id,
                value.IsRevealed,
                value.RemainingYield,
                value.Definition.MaximumYield,
                value.Version))
            .ToArray();
        return _terrainDepositPresenter.Present(
            world.Width,
            world.Height,
            depth: 4,
            inputs);
    }

    internal bool TryGetTerrainDepositOutput(
        CellId cell,
        out ItemId outputItemId,
        out int outputQuantity)
    {
        SpatialCellId spatial = new SpatialCellId(cell.X, cell.Y, 0);
        if (_terrainDeposits.TryGetValue(spatial, out TerrainDepositInstance? deposit)
            && !deposit.IsDepleted)
        {
            outputItemId = deposit.Definition.OutputItemId;
            outputQuantity = deposit.RemainingYield;
            return true;
        }

        outputItemId = default;
        outputQuantity = 0;
        return false;
    }

    internal void DepleteTerrainDeposit(CellId cell, long tick)
    {
        SpatialCellId spatial = new SpatialCellId(cell.X, cell.Y, 0);
        if (!_terrainDeposits.TryGetValue(spatial, out TerrainDepositInstance? deposit)
            || deposit.IsDepleted)
        {
            return;
        }

        long nextVersion = Math.Max(deposit.Version + 1, tick);
        _terrainDeposits[spatial] = deposit.Deplete(nextVersion);
    }

    private void InitializeDemoDeposits(int seed)
    {
        WorldViewModel world = LoadView();
        SpatialCellId[] candidates = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Select(cell => new CellId(cell.X, cell.Y))
            .Where(cell => !IsProtected(cell))
            .Select(cell => new SpatialCellId(cell.X, cell.Y, 0))
            .OrderBy(cell => cell)
            .ToArray();
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        IReadOnlyList<TerrainDepositInstance> generated = generator.Generate(
            world.Width,
            world.Height,
            depth: 4,
            candidates,
            DemoDepositDefinitions,
            new TerrainDepositGenerationSettings(
                seed,
                DemoDepositAlgorithmVersion,
                DemoDepositDensityPermille,
                maximumClusterSize: 4));
        _terrainDeposits.Clear();
        for (int index = 0; index < generated.Count; index++)
        {
            TerrainDepositInstance deposit = generated[index];
            _terrainDeposits.Add(deposit.Cell, deposit);
        }
    }
}

}