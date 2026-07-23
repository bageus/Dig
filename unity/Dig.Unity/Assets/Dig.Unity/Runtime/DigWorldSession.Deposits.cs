using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
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
            generationWeight: 28,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Metallurgy)),
        new TerrainDepositDefinition(
            "deposit.gold_ore",
            "Gold ore",
            new ItemId("ore.gold"),
            maximumYield: 5,
            generationWeight: 8,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Metallurgy)),
        new TerrainDepositDefinition(
            "deposit.crystal_ore",
            "Crystal ore",
            new ItemId("ore.crystal"),
            maximumYield: 6,
            generationWeight: 12,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Alchemy)),
        new TerrainDepositDefinition(
            "deposit.coal",
            "Coal",
            new ItemId("material.coal"),
            maximumYield: 10,
            generationWeight: 24,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Alchemy)),
        new TerrainDepositDefinition(
            "deposit.stone",
            "Stone",
            new ItemId("material.stone"),
            maximumYield: 12,
            generationWeight: 28,
            skillGrantProfile: DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.StoneExtraction)),
    };

    private readonly TerrainDepositPresenter _terrainDepositPresenter =
        new TerrainDepositPresenter();
    private readonly TerrainDepositState _terrainDeposits = new TerrainDepositState();

    internal IReadOnlyList<TerrainDepositDefinition> TerrainDepositDefinitions =>
        DemoDepositDefinitions;

    internal TerrainDepositVolumeViewModel LoadTerrainDeposits()
    {
        WorldViewModel world = LoadView();
        TerrainDepositPresentationInput[] inputs = _terrainDeposits.Snapshot()
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
            depth: world.Depth,
            inputs);
    }

    internal bool TryGetTerrainDepositOutput(
        CellId cell,
        out ItemId outputItemId,
        out int outputQuantity)
    {
        if (_terrainDeposits.TryGet(cell, out TerrainDepositInstance deposit)
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

    internal bool RevealTerrainDeposit(CellId cell, long tick)
    {
        return _terrainDeposits.Reveal(cell, tick);
    }

    internal int RevealTerrainDepositsAdjacentTo(CellId excavatedCell, long tick)
    {
        return _terrainDeposits.RevealAdjacentTo(excavatedCell, tick);
    }

    internal void DepleteTerrainDeposit(CellId cell, long tick)
    {
        _terrainDeposits.Deplete(cell, tick);
    }

    internal SkillGrantProfile ResolveExcavationSkillGrantProfile(CellId cell)
    {
        return _terrainDeposits.TryGet(cell, out TerrainDepositInstance deposit)
            && !deposit.IsDepleted
                ? deposit.Definition.SkillGrantProfile
                : DefaultSkillProgressionContent.Catalog.GetProfile(
                    DefaultSkillGrantProfileIds.StoneExtraction);
    }

    private void InitializeDemoDeposits(int seed)
    {
        WorldViewModel world = LoadView();
        CellId[] candidates = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Select(cell => new CellId(cell.X, cell.Y, cell.Z))
            .Where(cell => !IsProtected(cell))
            .OrderBy(cell => cell)
            .ToArray();
        TerrainDepositGenerator generator = new TerrainDepositGenerator();
        IReadOnlyList<TerrainDepositInstance> generated = generator.Generate(
            world.Width,
            world.Height,
            depth: world.Depth,
            candidates,
            DemoDepositDefinitions,
            new TerrainDepositGenerationSettings(
                seed,
                DemoDepositAlgorithmVersion,
                DemoDepositDensityPermille,
                maximumClusterSize: 4));
        _terrainDeposits.ReplaceAll(generated);
    }
}

}
