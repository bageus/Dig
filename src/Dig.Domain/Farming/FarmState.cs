using System;
using System.Collections.Generic;

namespace Dig.Domain.Farming
{

public sealed class FarmState
{
    private bool _mushroomSeedEstablished;
    private int _mushroomSlotsOccupied;
    private int _residualMushrooms;
    private int _hamsterCount;
    private int _grubCount;
    private int _feedCount;
    private int _escapingHamsterCount;
    private int _escapingGrubCount;
    private long _nextReproductionTick = -1;
    private long _nextFeedConsumptionTick = -1;
    private long _nextEscapeTick = -1;

    public FarmState(FarmMode initialMode = FarmMode.Mushrooms)
    {
        Mode = initialMode;
        if (initialMode == FarmMode.Mushrooms)
        {
            EnableMushroomGrowth(fillSlotsImmediately: true);
        }
    }

    public FarmMode Mode { get; private set; }

    // Kept for snapshot/API compatibility. This now means that mushroom growth is enabled,
    // not that a physical seed item was delivered.
    public bool MushroomSeedEstablished => _mushroomSeedEstablished;
    public int MushroomSlotsOccupied => _mushroomSlotsOccupied;
    public int ResidualMushrooms => _residualMushrooms;
    public int HamsterCount => _hamsterCount;
    public int GrubCount => _grubCount;
    public int FeedCount => _feedCount;
    public int EscapingHamsterCount => _escapingHamsterCount;
    public int EscapingGrubCount => _escapingGrubCount;

    public int AvailableHamsters => Math.Max(
        0,
        _hamsterCount - FarmOperationPolicy.HamsterBreederReserve);

    public int AvailableGrubs => Math.Max(
        0,
        _grubCount - FarmOperationPolicy.GrubBreederReserve);

    public IReadOnlyList<FarmDeliveryDemand> GetDeliveryDemands()
    {
        List<FarmDeliveryDemand> demands = new List<FarmDeliveryDemand>(2);
        switch (Mode)
        {
            case FarmMode.Mushrooms:
                // Mushroom cultivation is selected by farm mode; #731 does not require
                // a separate seed delivery before growth can start.
                break;
            case FarmMode.Hamsters:
                AddIfPositive(
                    demands,
                    FarmDeliveryKind.Hamster,
                    FarmOperationPolicy.HamsterBreederReserve - _hamsterCount);
                AddIfPositive(
                    demands,
                    FarmDeliveryKind.MushroomFeed,
                    FarmOperationPolicy.FeedCapacity - _feedCount);
                break;
            case FarmMode.Grubs:
                AddIfPositive(
                    demands,
                    FarmDeliveryKind.Grub,
                    FarmOperationPolicy.GrubBreederReserve - _grubCount);
                AddIfPositive(
                    demands,
                    FarmDeliveryKind.MushroomFeed,
                    FarmOperationPolicy.FeedCapacity - _feedCount);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return demands;
    }

    public FarmModeTransition SwitchMode(FarmMode mode, long tick)
    {
        ValidateTick(tick);
        if (mode == Mode)
        {
            return new FarmModeTransition(Mode, Mode, 0, 0, 0, 0);
        }

        FarmMode previous = Mode;
        int detachedMushrooms = 0;
        int releasedHamsters = 0;
        int releasedGrubs = 0;
        int releasedFeed = _feedCount;
        if (previous == FarmMode.Mushrooms)
        {
            detachedMushrooms = _mushroomSlotsOccupied;
            _residualMushrooms += detachedMushrooms;
            _mushroomSlotsOccupied = 0;
            _mushroomSeedEstablished = false;
        }
        else if (previous == FarmMode.Hamsters)
        {
            releasedHamsters = _hamsterCount;
            _escapingHamsterCount = checked(_escapingHamsterCount + releasedHamsters);
            _hamsterCount = 0;
        }
        else
        {
            releasedGrubs = _grubCount;
            _escapingGrubCount = checked(_escapingGrubCount + releasedGrubs);
            _grubCount = 0;
        }

        _feedCount = 0;
        _nextFeedConsumptionTick = -1;
        _nextReproductionTick = -1;
        Mode = mode;
        if (mode == FarmMode.Mushrooms)
        {
            EnableMushroomGrowth(fillSlotsImmediately: false);
        }
        ScheduleEscapeIfNeeded(tick);
        return new FarmModeTransition(
            previous,
            mode,
            detachedMushrooms,
            releasedHamsters,
            releasedGrubs,
            releasedFeed);
    }

    public void Deliver(FarmDeliveryKind kind, int quantity, long tick)
    {
        ValidateTick(tick);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        switch (kind)
        {
            case FarmDeliveryKind.MushroomSeed:
                // Legacy compatibility for callers/saves produced by the first #731 implementation.
                // Growth no longer depends on this delivery kind.
                RequireMode(FarmMode.Mushrooms);
                if (quantity != 1)
                    throw new InvalidOperationException("Mushroom seed compatibility delivery accepts one item.");
                EnableMushroomGrowth(fillSlotsImmediately: true);
                break;
            case FarmDeliveryKind.Hamster:
                RequireMode(FarmMode.Hamsters);
                _hamsterCount = Math.Min(
                    FarmOperationPolicy.AnimalCapacity,
                    checked(_hamsterCount + quantity));
                ScheduleReproductionIfReady(tick);
                ScheduleFeedingIfReady(tick);
                break;
            case FarmDeliveryKind.Grub:
                RequireMode(FarmMode.Grubs);
                _grubCount = Math.Min(
                    FarmOperationPolicy.AnimalCapacity,
                    checked(_grubCount + quantity));
                ScheduleReproductionIfReady(tick);
                ScheduleFeedingIfReady(tick);
                break;
            case FarmDeliveryKind.MushroomFeed:
                if (Mode == FarmMode.Mushrooms)
                    throw new InvalidOperationException("Mushroom mode does not use feed stock.");
                _feedCount = Math.Min(
                    FarmOperationPolicy.FeedCapacity,
                    checked(_feedCount + quantity));
                ScheduleFeedingIfReady(tick);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public bool HarvestMushroom()
    {
        if (Mode == FarmMode.Mushrooms && _mushroomSlotsOccupied > 0)
        {
            _mushroomSlotsOccupied--;
            return true;
        }

        if (_residualMushrooms > 0)
        {
            _residualMushrooms--;
            return true;
        }

        return false;
    }

    public bool CollectHamster()
    {
        if (Mode != FarmMode.Hamsters || AvailableHamsters <= 0) return false;
        _hamsterCount--;
        return true;
    }

    public bool CollectGrub()
    {
        if (Mode != FarmMode.Grubs || AvailableGrubs <= 0) return false;
        _grubCount--;
        return true;
    }

    public FarmAdvanceResult Advance(long tick)
    {
        ValidateTick(tick);
        AdvanceEscapingAnimals(tick, out int hamstersEscaped, out int grubsEscaped);

        int regrown = 0;
        if (Mode == FarmMode.Mushrooms)
        {
            if (_mushroomSeedEstablished
                && _mushroomSlotsOccupied < FarmOperationPolicy.MushroomGrowthSlots)
            {
                regrown = FarmOperationPolicy.MushroomGrowthSlots - _mushroomSlotsOccupied;
                _mushroomSlotsOccupied = FarmOperationPolicy.MushroomGrowthSlots;
            }
            return new FarmAdvanceResult(
                regrown,
                0,
                0,
                0,
                hamstersEscaped,
                grubsEscaped);
        }

        int hamstersBorn = 0;
        int grubsBorn = 0;
        int feedConsumed = 0;
        long reproductionInterval = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterReproductionTicks
            : FarmOperationPolicy.GrubReproductionTicks;

        // Process ecology events chronologically so a large simulation jump produces the
        // same result as advancing one game tick at a time. Feed wins ties: animals born
        // at the same instant have not lived through the preceding half-day feeding cycle.
        while (true)
        {
            bool feedDue = _nextFeedConsumptionTick >= 0 && tick >= _nextFeedConsumptionTick;
            bool reproductionDue = _nextReproductionTick >= 0 && tick >= _nextReproductionTick;
            if (!feedDue && !reproductionDue) break;

            if (feedDue && (!reproductionDue || _nextFeedConsumptionTick <= _nextReproductionTick))
            {
                // Feeding is a farm-wide upkeep event, not a per-animal cost: one cap is
                // consumed at the start of each half-day while an animal mode is active.
                int consumed = _feedCount > 0 ? 1 : 0;
                _feedCount -= consumed;
                feedConsumed = checked(feedConsumed + consumed);
                _nextFeedConsumptionTick = checked(
                    _nextFeedConsumptionTick + FarmOperationPolicy.FeedConsumptionTicks);
                continue;
            }

            if (Mode == FarmMode.Hamsters)
            {
                int born = Math.Min(
                    _hamsterCount,
                    FarmOperationPolicy.AnimalCapacity - _hamsterCount);
                _hamsterCount += born;
                hamstersBorn = checked(hamstersBorn + born);
            }
            else
            {
                int born = Math.Min(
                    _grubCount,
                    FarmOperationPolicy.AnimalCapacity - _grubCount);
                _grubCount += born;
                grubsBorn = checked(grubsBorn + born);
            }

            _nextReproductionTick = checked(_nextReproductionTick + reproductionInterval);
        }

        return new FarmAdvanceResult(
            0,
            hamstersBorn,
            grubsBorn,
            feedConsumed,
            hamstersEscaped,
            grubsEscaped);
    }

    public FarmSnapshot CreateSnapshot()
    {
        return new FarmSnapshot(
            Mode,
            _mushroomSeedEstablished,
            _mushroomSlotsOccupied,
            _residualMushrooms,
            _hamsterCount,
            _grubCount,
            _feedCount,
            _nextReproductionTick,
            _nextFeedConsumptionTick,
            _escapingHamsterCount,
            _escapingGrubCount,
            _nextEscapeTick);
    }

    public static FarmState Restore(FarmSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        ValidateSnapshot(snapshot);
        FarmState state = new FarmState(snapshot.Mode)
        {
            _mushroomSeedEstablished = snapshot.MushroomSeedEstablished,
            _mushroomSlotsOccupied = snapshot.MushroomSlotsOccupied,
            _residualMushrooms = snapshot.ResidualMushrooms,
            _hamsterCount = snapshot.HamsterCount,
            _grubCount = snapshot.GrubCount,
            _feedCount = snapshot.FeedCount,
            _nextReproductionTick = snapshot.NextReproductionTick,
            _nextFeedConsumptionTick = snapshot.NextFeedConsumptionTick,
            _escapingHamsterCount = snapshot.EscapingHamsterCount,
            _escapingGrubCount = snapshot.EscapingGrubCount,
            _nextEscapeTick = snapshot.NextEscapeTick,
        };

        // Migrate snapshots produced while mushroom mode incorrectly required an item seed.
        if (state.Mode == FarmMode.Mushrooms)
        {
            state._mushroomSeedEstablished = true;
        }
        return state;
    }

    private void EnableMushroomGrowth(bool fillSlotsImmediately)
    {
        _mushroomSeedEstablished = true;
        if (fillSlotsImmediately)
        {
            _mushroomSlotsOccupied = FarmOperationPolicy.MushroomGrowthSlots;
        }
    }

    private void ScheduleReproductionIfReady(long tick)
    {
        int reserve = Mode == FarmMode.Hamsters
            ? FarmOperationPolicy.HamsterBreederReserve
            : FarmOperationPolicy.GrubBreederReserve;
        int population = Mode == FarmMode.Hamsters ? _hamsterCount : _grubCount;
        if (population < reserve || _nextReproductionTick >= 0) return;
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

        long interval = FarmOperationPolicy.FeedConsumptionTicks;
        long remainder = tick % interval;
        _nextFeedConsumptionTick = remainder == 0
            ? tick
            : checked(tick + (interval - remainder));
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
            throw new InvalidOperationException("The delivery does not match the active farm mode.");
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
        if (snapshot.MushroomSlotsOccupied < 0
            || snapshot.MushroomSlotsOccupied > FarmOperationPolicy.MushroomGrowthSlots
            || snapshot.ResidualMushrooms < 0
            || snapshot.HamsterCount < 0
            || snapshot.HamsterCount > FarmOperationPolicy.AnimalCapacity
            || snapshot.GrubCount < 0
            || snapshot.GrubCount > FarmOperationPolicy.AnimalCapacity
            || snapshot.FeedCount < 0
            || snapshot.FeedCount > FarmOperationPolicy.FeedCapacity
            || snapshot.EscapingHamsterCount < 0
            || snapshot.EscapingGrubCount < 0
            || snapshot.NextReproductionTick < -1
            || snapshot.NextFeedConsumptionTick < -1
            || snapshot.NextEscapeTick < -1)
        {
            throw new ArgumentException("Farm snapshot contains invalid values.", nameof(snapshot));
        }
    }
}

}
