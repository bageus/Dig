using System;
using System.Collections.Generic;

namespace Dig.Domain.Farming
{

public sealed partial class FarmState
{
    private void ScheduleReproductionIfReady(long tick)
    {
        int reserve = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterBreederReserve
            : FarmOperationPolicy.GrubBreederReserve;
        int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
        if (_feedCount <= 0 || population < reserve || _nextReproductionTick >= 0)
        {
            return;
        }

        long interval = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterReproductionTicks
            : FarmOperationPolicy.GrubReproductionTicks;
        _nextReproductionTick = checked(tick + interval);
    }

    private void ScheduleFeedingIfReady(long tick)
    {
        int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
        if (population <= 0 || _nextFeedConsumptionTick >= 0)
        {
            return;
        }

        _nextFeedConsumptionTick = checked(tick + FarmOperationPolicy.FeedConsumptionTicks);
    }

    private void ScheduleEscapeIfNeeded(long tick)
    {
        if (_nextEscapeTick >= 0
            || (_escapingHamsterCount <= 0 && _escapingGrubCount <= 0))
        {
            return;
        }

        _nextEscapeTick = checked(tick + 1);
    }

    private void AdvanceEscapingAnimals(
        long tick,
        out int hamstersEscaped,
        out int grubsEscaped)
    {
        hamstersEscaped = 0;
        grubsEscaped = 0;
        if (_nextEscapeTick < 0 || tick < _nextEscapeTick)
        {
            return;
        }

        long elapsedTicks = tick - _nextEscapeTick;
        int escapeSlots = elapsedTicks >= int.MaxValue - 1L
            ? int.MaxValue
            : checked((int)elapsedTicks + 1);

        hamstersEscaped = Math.Min(_escapingHamsterCount, escapeSlots);
        _escapingHamsterCount -= hamstersEscaped;
        escapeSlots -= hamstersEscaped;

        grubsEscaped = Math.Min(_escapingGrubCount, escapeSlots);
        _escapingGrubCount -= grubsEscaped;

        int escaped = checked(hamstersEscaped + grubsEscaped);
        if (_escapingHamsterCount <= 0 && _escapingGrubCount <= 0)
        {
            _nextEscapeTick = -1;
        }
        else
        {
            _nextEscapeTick = checked(_nextEscapeTick + escaped);
        }
    }

    private void RequireMode(FarmMode expected)
    {
        if (Mode != expected)
        {
            throw new InvalidOperationException("The delivery does not match the active farm mode.");
        }
    }

    private static void AddIfPositive(
        ICollection<FarmDeliveryDemand> values,
        FarmDeliveryKind kind,
        int quantity)
    {
        if (quantity > 0) values.Add(new FarmDeliveryDemand(kind, quantity));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
    }

    private static void ValidateSnapshot(FarmSnapshot snapshot)
    {
        bool hasEscapingAnimals = snapshot.EscapingHamsterCount > 0
            || snapshot.EscapingGrubCount > 0;
        if (!Enum.IsDefined(typeof(FarmMode), snapshot.Mode)
            || snapshot.MushroomSlotsOccupied < 0
            || snapshot.MushroomSlotsOccupied > FarmOperationPolicy.MushroomGrowthSlots
            || snapshot.ResidualMushrooms < 0
            || snapshot.ResidualMushrooms > FarmOperationPolicy.MushroomGrowthSlots
            || snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms
                > FarmOperationPolicy.MushroomGrowthSlots
            || (!snapshot.MushroomSeedEstablished
                && snapshot.MushroomSlotsOccupied > 0)
            || (snapshot.Mode != FarmMode.Mushrooms
                && snapshot.MushroomSlotsOccupied > 0)
            || (snapshot.Mode != FarmMode.Mushrooms
                && snapshot.MushroomSeedEstablished)
            || snapshot.HamsterCount < 0
            || snapshot.HamsterCount > FarmOperationPolicy.AnimalCapacity
            || snapshot.GrubCount < 0
            || snapshot.GrubCount > FarmOperationPolicy.AnimalCapacity
            || (snapshot.Mode != FarmMode.Hamsters && snapshot.HamsterCount > 0)
            || (snapshot.Mode != FarmMode.Grubs && snapshot.GrubCount > 0)
            || snapshot.FeedCount < 0
            || snapshot.FeedCount > FarmOperationPolicy.FeedCapacity
            || (snapshot.Mode == FarmMode.Mushrooms && snapshot.FeedCount > 0)
            || snapshot.EscapingHamsterCount < 0
            || snapshot.EscapingGrubCount < 0
            || snapshot.NextReproductionTick < -1
            || snapshot.NextFeedConsumptionTick < -1
            || snapshot.NextEscapeTick < -1
            || (snapshot.Mode == FarmMode.Mushrooms
                && (snapshot.NextReproductionTick != -1
                    || snapshot.NextFeedConsumptionTick != -1))
            || (hasEscapingAnimals != (snapshot.NextEscapeTick >= 0)))
        {
            throw new ArgumentException("Farm snapshot contains invalid values.", nameof(snapshot));
        }
    }
}

}
