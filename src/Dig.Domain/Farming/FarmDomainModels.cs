using Dig.Domain.Core;

namespace Dig.Domain.Farming
{
    public readonly struct FarmModeTransition
    {
        public FarmModeTransition(
            FarmMode previousMode,
            FarmMode newMode,
            int releasedFeedQuantity,
            int escapingAnimalsCount,
            FarmMode escapingKind)
        {
            PreviousMode = previousMode;
            NewMode = newMode;
            ReleasedFeedQuantity = releasedFeedQuantity;
            EscapingAnimalsCount = escapingAnimalsCount;
            EscapingKind = escapingKind;
        }

        public FarmMode PreviousMode { get; }
        public FarmMode NewMode { get; }
        public int ReleasedFeedQuantity { get; }
        public int EscapingAnimalsCount { get; }
        public FarmMode EscapingKind { get; }
    }

    public readonly struct FarmAdvanceResult
    {
        public FarmAdvanceResult(
            int producedUnits,
            bool consumedFeed,
            int escapedAnimalsCount,
            FarmMode escapedKind)
        {
            ProducedUnits = producedUnits;
            ConsumedFeed = consumedFeed;
            EscapedAnimalsCount = escapedAnimalsCount;
            EscapedKind = escapedKind;
        }

        public int ProducedUnits { get; }
        public bool ConsumedFeed { get; }
        public int EscapedAnimalsCount { get; }
        public FarmMode EscapedKind { get; }
    }

    public sealed class FarmSnapshot
    {
        public FarmSnapshot(
            EntityId buildingId,
            FarmMode mode,
            bool starterDelivered,
            int feedQuantity,
            int mushroomSlots,
            int animalPopulation,
            int collectableAnimals,
            int escapingAnimals,
            FarmMode escapingKind)
        {
            BuildingId = buildingId;
            Mode = mode;
            StarterDelivered = starterDelivered;
            FeedQuantity = feedQuantity;
            MushroomSlots = mushroomSlots;
            AnimalPopulation = animalPopulation;
            CollectableAnimals = collectableAnimals;
            EscapingAnimals = escapingAnimals;
            EscapingKind = escapingKind;
        }

        public EntityId BuildingId { get; }
        public FarmMode Mode { get; }
        public bool StarterDelivered { get; }
        public int FeedQuantity { get; }
        public int MushroomSlots { get; }
        public int AnimalPopulation { get; }
        public int CollectableAnimals { get; }
        public int EscapingAnimals { get; }
        public FarmMode EscapingKind { get; }
    }
}
