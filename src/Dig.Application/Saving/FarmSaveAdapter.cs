using System;
using System.Linq;
using Dig.Application.Farming;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public static class FarmSaveAdapter
{
    public static FarmSaveData Encode(
        IFarmRepository farms,
        FarmLogisticsReservations? reservations = null)
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
        if (reservations != null)
        {
            foreach (FarmLogisticsReservation reservation in reservations.GetAll())
            {
                data.Reservations.Add(new FarmLogisticsReservationSaveData
                {
                    JobId = reservation.JobId.ToString(),
                    BuildingId = reservation.BuildingId.ToString(),
                    Kind = (int)reservation.Kind,
                    Quantity = reservation.Quantity,
                    Direction = (int)reservation.Direction,
                });
            }
        }
        return data;
    }

    public static Result<FarmLogisticsReservations> DecodeReservations(FarmSaveData? data)
    {
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        if (data?.Reservations == null)
        {
            return Result<FarmLogisticsReservations>.Success(reservations);
        }

        foreach (FarmLogisticsReservationSaveData saved in data.Reservations
            .OrderBy(value => value.JobId, StringComparer.Ordinal))
        {
            if (saved == null
                || string.IsNullOrWhiteSpace(saved.JobId)
                || string.IsNullOrWhiteSpace(saved.BuildingId)
                || saved.Quantity <= 0
                || !Enum.IsDefined(typeof(FarmDeliveryKind), saved.Kind)
                || !Enum.IsDefined(typeof(FarmLogisticsDirection), saved.Direction))
            {
                return Result<FarmLogisticsReservations>.Failure(SaveErrors.InvalidDocument);
            }

            FarmLogisticsReservation reservation = new FarmLogisticsReservation(
                EntityId.Parse(saved.JobId),
                EntityId.Parse(saved.BuildingId),
                (FarmDeliveryKind)saved.Kind,
                saved.Quantity,
                (FarmLogisticsDirection)saved.Direction);
            if (!reservations.TryRestore(reservation))
            {
                return Result<FarmLogisticsReservations>.Failure(SaveErrors.InvalidDocument);
            }
        }
        return Result<FarmLogisticsReservations>.Success(reservations);
    }

    public static Result ValidateReservations(
        FarmLogisticsReservations reservations,
        IFarmRepository farms,
        JobSystem jobs)
    {
        if (reservations == null || farms == null || jobs == null)
            throw new ArgumentNullException(
                reservations == null ? nameof(reservations)
                : farms == null ? nameof(farms) : nameof(jobs));
        FarmItemCatalog items = FarmItemCatalog.Default;
        foreach (FarmLogisticsReservation reservation in reservations.GetAll())
        {
            JobSnapshot? job = jobs.Get(reservation.JobId);
            if (farms.Get(reservation.BuildingId) == null
                || job?.Definition is not HaulJobDefinition haul
                || job.IsTerminal
                || haul.ItemId != items.Resolve(reservation.Kind)
                || haul.Quantity != reservation.Quantity)
            {
                return Result.Failure(SaveErrors.InvalidDocument);
            }

            bool validDirection = reservation.Direction == FarmLogisticsDirection.Incoming
                ? haul.Destination == ItemLocation.InBuilding(reservation.BuildingId)
                : haul.Destination.Kind == ItemLocationKind.World;
            if (!validDirection) return Result.Failure(SaveErrors.InvalidDocument);
        }
        return Result.Success();
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
