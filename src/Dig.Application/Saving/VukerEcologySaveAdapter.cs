using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class VukerEcologySaveAdapter
{
    private const int CurrentTimingCadenceVersion = 1;
    private const int LegacySimulationTicksPerDay = 24;

    public static VukerEcologySaveData Encode(VukerEcologyState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        VukerEcologySnapshot snapshot = state.CaptureSnapshot();
        VukerEcologySaveData data = new VukerEcologySaveData
        {
            WorldSeed = snapshot.WorldSeed,
            CurrentTick = snapshot.CurrentTick,
            NextPairSequence = snapshot.NextPairSequence,
            Version = snapshot.Version,
            TimingCadenceVersion = CurrentTimingCadenceVersion,
        };
        foreach (VukerIndividualSnapshot individual in snapshot.Individuals)
        {
            data.Individuals.Add(new VukerIndividualSaveData
            {
                EntityId = individual.EntityId.ToString(),
                Lifecycle = (int)individual.Lifecycle,
                Disposition = (int)individual.Disposition,
                RegionX = individual.Region.Root.X,
                RegionY = individual.Region.Root.Y,
                RegionZ = individual.Region.Root.Z,
                PositionX = individual.Position.X,
                PositionY = individual.Position.Y,
                PositionZ = individual.Position.Z,
                IsAlive = individual.IsAlive,
                BirthTick = individual.BirthTick,
                MaturityTick = individual.MaturityTick,
                KidnapReservedBy = Id(individual.KidnapReservedBy),
                TamedByResidentId = Id(individual.TamedByResidentId),
                ActivePairId = individual.ActivePairId?.ToString(),
                Version = individual.Version,
            });
        }

        foreach (VukerPairSnapshot pair in snapshot.Pairs)
        {
            data.Pairs.Add(new VukerPairSaveData
            {
                PairId = pair.PairId.ToString(),
                FirstParentId = pair.FirstParentId.ToString(),
                SecondParentId = pair.SecondParentId.ToString(),
                RegionX = pair.Region.Root.X,
                RegionY = pair.Region.Root.Y,
                RegionZ = pair.Region.Root.Z,
                SuccessfulCycles = pair.SuccessfulCycles,
                NextBirthTick = pair.NextBirthTick,
                IsActive = pair.IsActive,
                TerminalReason = pair.TerminalReason,
                BlockedReason = pair.BlockedReason,
                Version = pair.Version,
            });
        }

        return data;
    }

    public static Result<VukerEcologyState> Decode(
        VukerEcologySaveData? data,
        ulong fallbackWorldSeed)
    {
        data ??= new VukerEcologySaveData { WorldSeed = fallbackWorldSeed };
        try
        {
            List<VukerIndividualSnapshot> individuals = (data.Individuals
                ?? new List<VukerIndividualSaveData>())
                .OrderBy(value => value.EntityId, StringComparer.Ordinal)
                .Select(value => DecodeIndividual(value, data))
                .ToList();
            List<VukerPairSnapshot> pairs = (data.Pairs
                ?? new List<VukerPairSaveData>())
                .OrderBy(value => value.PairId, StringComparer.Ordinal)
                .Select(value => DecodePair(value, data))
                .ToList();
            return VukerEcologyState.Restore(new VukerEcologySnapshot(
                data.WorldSeed == 0 ? fallbackWorldSeed : data.WorldSeed,
                data.CurrentTick,
                data.NextPairSequence,
                data.Version,
                individuals,
                pairs));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is FormatException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is NullReferenceException)
        {
            return Result<VukerEcologyState>.Failure(
                VukerEcologyErrors.InvalidSnapshot);
        }
    }

    private static VukerIndividualSnapshot DecodeIndividual(
        VukerIndividualSaveData saved,
        VukerEcologySaveData data)
    {
        if (!Enum.IsDefined(typeof(VukerLifecycleStage), saved.Lifecycle)
            || !Enum.IsDefined(typeof(VukerDisposition), saved.Disposition)
            || saved.BirthTick < 0
            || saved.MaturityTick < saved.BirthTick
            || saved.Version < 0)
        {
            throw new InvalidOperationException("Invalid Vuker individual save data.");
        }

        return new VukerIndividualSnapshot(
            EntityId.Parse(saved.EntityId),
            (VukerLifecycleStage)saved.Lifecycle,
            (VukerDisposition)saved.Disposition,
            new VukerRegionKey(new CellId(
                saved.RegionX, saved.RegionY, saved.RegionZ)),
            new CellId(saved.PositionX, saved.PositionY, saved.PositionZ),
            saved.IsAlive,
            saved.BirthTick,
            MigrateDueTick(data, saved.MaturityTick),
            OptionalId(saved.KidnapReservedBy),
            OptionalId(saved.TamedByResidentId),
            string.IsNullOrWhiteSpace(saved.ActivePairId)
                ? (VukerPairId?)null
                : new VukerPairId(saved.ActivePairId),
            saved.Version);
    }

    private static VukerPairSnapshot DecodePair(
        VukerPairSaveData saved,
        VukerEcologySaveData data)
    {
        if (saved.SuccessfulCycles < 0
            || saved.SuccessfulCycles
                > VukerEcologyProfile.MaximumSuccessfulCyclesPerPair
            || saved.NextBirthTick < 0
            || saved.Version < 0)
        {
            throw new InvalidOperationException("Invalid Vuker pair save data.");
        }

        return new VukerPairSnapshot(
            new VukerPairId(saved.PairId),
            EntityId.Parse(saved.FirstParentId),
            EntityId.Parse(saved.SecondParentId),
            new VukerRegionKey(new CellId(
                saved.RegionX, saved.RegionY, saved.RegionZ)),
            saved.SuccessfulCycles,
            MigrateDueTick(data, saved.NextBirthTick),
            saved.IsActive,
            saved.TerminalReason,
            saved.BlockedReason,
            saved.Version);
    }

    private static string? Id(EntityId? id) => id?.ToString();

    private static long MigrateDueTick(VukerEcologySaveData data, long dueTick)
    {
        if (data.TimingCadenceVersion >= CurrentTimingCadenceVersion
            || dueTick <= data.CurrentTick)
        {
            return dueTick;
        }

        long remaining = dueTick - data.CurrentTick;
        return checked(data.CurrentTick + (remaining
            * GameTimeCadence.TicksPerDay
            / LegacySimulationTicksPerDay));
    }

    private static EntityId? OptionalId(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? (EntityId?)null
            : EntityId.Parse(value);
    }
}

}
