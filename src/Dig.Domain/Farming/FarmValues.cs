using System;
using Dig.Domain.Runtime;

namespace Dig.Domain.Farming
{

public enum FarmMode
{
    Mushrooms = 0,
    Hamsters = 1,
    Grubs = 2,
}

public enum FarmDeliveryKind
{
    MushroomSeed = 0,
    Hamster = 1,
    Grub = 2,
    MushroomFeed = 3,
}

public readonly struct FarmDeliveryDemand
{
    public FarmDeliveryDemand(FarmDeliveryKind kind, int quantity)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Kind = kind;
        Quantity = quantity;
    }

    public FarmDeliveryKind Kind { get; }

    public int Quantity { get; }
}

public readonly struct FarmModeTransition
{
    public FarmModeTransition(
        FarmMode previousMode,
        FarmMode currentMode,
        int detachedMushrooms,
        int releasedHamsters,
        int releasedGrubs,
        int releasedFeed)
    {
        PreviousMode = previousMode;
        CurrentMode = currentMode;
        DetachedMushrooms = detachedMushrooms;
        ReleasedHamsters = releasedHamsters;
        ReleasedGrubs = releasedGrubs;
        ReleasedFeed = releasedFeed;
    }

    public FarmMode PreviousMode { get; }

    public FarmMode CurrentMode { get; }

    public int DetachedMushrooms { get; }

    public int ReleasedHamsters { get; }

    public int ReleasedGrubs { get; }

    public int ReleasedFeed { get; }
}

public readonly struct FarmAdvanceResult
{
    public FarmAdvanceResult(
        int mushroomsRegrown,
        int hamstersBorn,
        int grubsBorn,
        int feedConsumed)
    {
        MushroomsRegrown = mushroomsRegrown;
        HamstersBorn = hamstersBorn;
        GrubsBorn = grubsBorn;
        FeedConsumed = feedConsumed;
    }

    public int MushroomsRegrown { get; }

    public int HamstersBorn { get; }

    public int GrubsBorn { get; }

    public int FeedConsumed { get; }
}

public static class FarmOperationPolicy
{
    public const int MushroomGrowthSlots = 3;
    public const int HamsterBreederReserve = 2;
    public const int GrubBreederReserve = 1;
    public const int AnimalCapacity = 8;
    public const int FeedCapacity = 2;
    public const int HamsterReproductionHours = 2;
    public const int GrubReproductionHours = 1;

    public static long HamsterReproductionTicks =>
        GameTimeCadence.TicksFromHours(HamsterReproductionHours);

    public static long GrubReproductionTicks =>
        GameTimeCadence.TicksFromHours(GrubReproductionHours);

    public static long FeedConsumptionTicks => GameTimeCadence.TicksPerDay / 2L;
}

public sealed class FarmSnapshot
{
    public FarmSnapshot(
        FarmMode mode,
        bool mushroomSeedEstablished,
        int mushroomSlotsOccupied,
        int residualMushrooms,
        int hamsterCount,
        int grubCount,
        int feedCount,
        long nextReproductionTick,
        long nextFeedConsumptionTick)
    {
        Mode = mode;
        MushroomSeedEstablished = mushroomSeedEstablished;
        MushroomSlotsOccupied = mushroomSlotsOccupied;
        ResidualMushrooms = residualMushrooms;
        HamsterCount = hamsterCount;
        GrubCount = grubCount;
        FeedCount = feedCount;
        NextReproductionTick = nextReproductionTick;
        NextFeedConsumptionTick = nextFeedConsumptionTick;
    }

    public FarmMode Mode { get; }
    public bool MushroomSeedEstablished { get; }
    public int MushroomSlotsOccupied { get; }
    public int ResidualMushrooms { get; }
    public int HamsterCount { get; }
    public int GrubCount { get; }
    public int FeedCount { get; }
    public long NextReproductionTick { get; }
    public long NextFeedConsumptionTick { get; }
}

}
