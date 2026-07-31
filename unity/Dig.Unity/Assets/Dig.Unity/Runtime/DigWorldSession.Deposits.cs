using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private const int DemoDepositAlgorithmVersion = 1;
    private const int DemoDepositDensityPermille = 160;
    private const int BalanceTbdDepositWorkEffortPermille = 1_000;
    private static readonly TerrainDepositCatalog DemoDepositCatalog =
        new TerrainDepositCatalog(new[]
        {
            Definition(
                "deposit.iron_ore",
                "Iron ore",
                "ore.iron",
                maximumYield: 8,
                generationWeight: 28,
                DefaultSkillGrantProfileIds.Metallurgy),
            Definition(
                "deposit.gold_ore",
                "Gold ore",
                "ore.gold",
                maximumYield: 5,
                generationWeight: 8,
                DefaultSkillGrantProfileIds.Metallurgy),
            Definition(
                "deposit.crystal_ore",
                "Crystal ore",
                "ore.crystal",
                maximumYield: 6,
                generationWeight: 12,
                DefaultSkillGrantProfileIds.Alchemy),
            Definition(
                "deposit.coal",
                "Coal",
                "material.coal",
                maximumYield: 10,
                generationWeight: 24,
                DefaultSkillGrantProfileIds.Alchemy),
            Definition(
                "deposit.stone",
                "Stone",
                "material.stone",
                maximumYield: 12,
                generationWeight: 28,
                DefaultSkillGrantProfileIds.StoneExtraction),
        });

    private readonly TerrainDepositPresenter _terrainDepositPresenter =
        new TerrainDepositPresenter();

    internal IReadOnlyList<TerrainDepositDefinition> TerrainDepositDefinitions =>
        DemoDepositCatalog.Definitions;

    internal TerrainDepositVolumeViewModel LoadTerrainDeposits()
    {
        WorldState worldState = _repository.Get();
        WorldViewModel world = LoadView();
        TerrainDepositPresentationInput[] inputs = worldState.TerrainDeposits.Snapshot()
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

    internal bool RevealTerrainDeposit(CellId cell, long tick)
    {
        Result<bool> revealed = _repository.Get().RevealTerrainDeposit(cell, tick);
        if (revealed.IsFailure)
        {
            throw new InvalidOperationException(revealed.Error!.ToString());
        }

        return revealed.Value;
    }

    internal int ResolveDepositWorkEffortPermille(CellId cell)
    {
        return _repository.Get().TerrainDeposits.TryGet(
            cell,
            out TerrainDepositInstance deposit)
            && !deposit.IsDepleted
                ? deposit.Definition.WorkEffortPermille
                : 1_000;
    }

    internal SkillGrantProfile ResolveExcavationSkillGrantProfile(CellId cell)
    {
        return _repository.Get().TerrainDeposits.TryGet(
            cell,
            out TerrainDepositInstance deposit)
            && !deposit.IsDepleted
                ? deposit.Definition.SkillGrantProfile
                : DefaultSkillProgressionContent.Catalog.GetProfile(
                    DefaultSkillGrantProfileIds.StoneExtraction);
    }

    private void InitializeDemoDeposits(int seed)
    {
        _miningOutputWorldSeed = seed;
        WorldState world = _repository.Get();
        WorldSnapshot snapshot = world.CreateSnapshot();
        TerrainDepositHostCell[] candidates = snapshot.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.IsSolid)
            .Where(cell => !IsProtected(cell.Id))
            .Select(cell => new TerrainDepositHostCell(
                cell.Id,
                world.Materials.Get(cell.State.MaterialId)
                    ?? throw new InvalidOperationException(
                        $"Missing host material '{cell.State.MaterialId}'.")))
            .OrderBy(cell => cell.Cell)
            .ToArray();
        TerrainDepositGenerationResult generated =
            new TerrainDepositGenerator().Generate(
                world.Size,
                candidates,
                DemoDepositCatalog,
                new TerrainDepositGenerationSettings(
                    seed,
                    DemoDepositAlgorithmVersion,
                    DemoDepositDensityPermille,
                    maximumClusterSize: 4));
        world.ReplaceTerrainDeposits(
            generated.Deposits,
            generated.AlgorithmVersion);
    }

    private static TerrainDepositDefinition Definition(
        string id,
        string displayName,
        string outputItemId,
        int maximumYield,
        int generationWeight,
        SkillGrantProfileId skillGrantProfileId)
    {
        return new TerrainDepositDefinition(
            id,
            displayName,
            new ItemId(outputItemId),
            maximumYield,
            generationWeight,
            DefaultSkillProgressionContent.Catalog.GetProfile(skillGrantProfileId),
            version: 1,
            workEffortPermille: BalanceTbdDepositWorkEffortPermille,
            allowedHostMaterialIds: new[] { new MaterialId("demo.rock") });
    }
}

}
