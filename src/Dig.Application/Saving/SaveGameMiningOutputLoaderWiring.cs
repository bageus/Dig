using System;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class LoadedGameWithMiningOutput
{
    public LoadedGameWithMiningOutput(
        LoadedGameState game,
        RestoredMiningOutputState miningOutput)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        MiningOutput = miningOutput ?? throw new ArgumentNullException(nameof(miningOutput));
    }

    public LoadedGameState Game { get; }
    public RestoredMiningOutputState MiningOutput { get; }
}

public sealed partial class SaveGameLoader
{
    private readonly MiningOutputSaveDocumentSection _miningOutputSection =
        new MiningOutputSaveDocumentSection();

    public Result<LoadedGameWithMiningOutput> LoadWithMiningOutput(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        Dig.Domain.Buildings.BuildingCatalog? buildingCatalog = null,
        TerrainDepositCatalog? terrainDepositCatalog = null)
    {
        Result<LoadedGameState> loaded = Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDepositCatalog);
        if (loaded.IsFailure)
        {
            return Result<LoadedGameWithMiningOutput>.Failure(
                loaded.Error ?? SaveErrors.InvalidDocument);
        }

        return Result<LoadedGameWithMiningOutput>.Success(
            new LoadedGameWithMiningOutput(
                loaded.Value,
                loaded.Value.MiningOutput));
    }
    private Result<RestoredMiningOutputState> RestoreMiningOutput(
        SaveGameDocument document,
        InventoryState inventory,
        WorldSize worldSize)
    {
        return _miningOutputSection.Restore(
            document.MiningOutput ?? new MiningOutputCommitsSaveData(),
            inventory,
            worldSize);
    }

}

}