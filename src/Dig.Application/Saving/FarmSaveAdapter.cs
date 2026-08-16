using System;
using System.Linq;
using Dig.Application.Farming;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Saving
{

public static class FarmSaveAdapter
{
    public static FarmSaveData Encode(IFarmRepository farms)
    {
        if (farms == null) throw new ArgumentNullException(nameof(farms));
        FarmSaveData data = new FarmSaveData();
        foreach (EntityId farmId in farms.GetFarmIds()
            .OrderBy(value => value.ToString(), StringComparer.Ordinal))
        {
            FarmSnapshot snapshot = farms.Get(farmId)!.CreateSnapshot();
            data.Farms.Add(new FarmStateSaveData
            {
                BuildingId = farmId.ToString(),
                Mode = (int)snapshot.Mode,
                MushroomSeedEstablished = snapshot.MushroomSeedEstablished,
                MushroomSlotsOccupied = snapshot.MushroomSlotsOccupied,
                ResidualMushrooms = snapshot.ResidualMushrooms,
                HamsterCount = snapshot.HamsterCount,
                GrubCount = snapshot.GrubCount,
                FeedCount = snapshot.FeedCount,
                NextReproductionTick = snapshot.NextReproductionTick,
                NextFeedConsumptionTick = snapshot.NextFeedConsumptionTick,
                EscapingHamsterCount = snapshot.EscapingHamsterCount,
                EscapingGrubCount = snapshot.EscapingGrubCount,
                NextEscapeTick = snapshot.NextEscapeTick,
            });
        }
        return data;
    }

    public static Result<InMemoryFarmRepository> Decode(FarmSaveData? data)
    {
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        if (data?.Farms == null)
        {
            return Result<InMemoryFarmRepository>.Success(farms);
        }

        foreach (FarmStateSaveData saved in data.Farms
            .OrderBy(value => value.BuildingId, StringComparer.Ordinal))
        {
            if (saved == null
                || string.IsNullOrWhiteSpace(saved.BuildingId)
                || !Enum.IsDefined(typeof(FarmMode), saved.Mode))
            {
                return Result<InMemoryFarmRepository>.Failure(SaveErrors.InvalidDocument);
            }

            EntityId farmId = EntityId.Parse(saved.BuildingId);
            if (farms.Get(farmId) != null)
            {
                return Result<InMemoryFarmRepository>.Failure(SaveErrors.InvalidDocument);
            }

            FarmSnapshot snapshot = new FarmSnapshot(
                (FarmMode)saved.Mode,
                saved.MushroomSeedEstablished,
                saved.MushroomSlotsOccupied,
                saved.ResidualMushrooms,
                saved.HamsterCount,
                saved.GrubCount,
                saved.FeedCount,
                saved.NextReproductionTick,
                saved.NextFeedConsumptionTick,
                saved.EscapingHamsterCount,
                saved.EscapingGrubCount,
                saved.NextEscapeTick);
            farms.Save(farmId, FarmState.Restore(snapshot));
        }

        return Result<InMemoryFarmRepository>.Success(farms);
    }
}

}
