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

        Result<RestoredMiningOutputState> miningOutput =
            _miningOutputSection.Restore(
                document.MiningOutput ?? new MiningOutputCommitsSaveData(),
                loaded.Value.Inventory,
                loaded.Value.World.Size);
        if (miningOutput.IsFailure)
        {
            return Result<LoadedGameWithMiningOutput>.Failure(
                miningOutput.Error ?? MiningOutputSaveErrors.InvalidSnapshot);
        }

        return Result<LoadedGameWithMiningOutput>.Success(
            new LoadedGameWithMiningOutput(loaded.Value, miningOutput.Value));
    }
}

}
