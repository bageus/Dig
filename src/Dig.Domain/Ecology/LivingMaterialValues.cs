using System;
using Dig.Domain.Inventory;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public enum LivingMaterialSpecies
{
    Hamster = 0,
    Grub = 1,
}

public enum LivingMaterialContainment
{
    Free = 0,
    Stored = 1,
}

public enum LivingMaterialActivity
{
    Moving = 0,
    ReleaseDormant = 1,
    HamsterSearching = 2,
    HamsterSleeping = 3,
    Blocked = 4,
    Stored = 5,
}

public readonly struct LivingMaterialPlaneKey : IEquatable<LivingMaterialPlaneKey>, IComparable<LivingMaterialPlaneKey>
{
    public LivingMaterialPlaneKey(CellId root)
    {
        Root = root;
    }

    public CellId Root { get; }

    public int CompareTo(LivingMaterialPlaneKey other) => Root.CompareTo(other.Root);

    public bool Equals(LivingMaterialPlaneKey other) => Root == other.Root;

    public override bool Equals(object? obj) => obj is LivingMaterialPlaneKey other && Equals(other);

    public override int GetHashCode() => Root.GetHashCode();

    public override string ToString() => Root.ToString();

    public static bool operator ==(LivingMaterialPlaneKey left, LivingMaterialPlaneKey right) => left.Equals(right);

    public static bool operator !=(LivingMaterialPlaneKey left, LivingMaterialPlaneKey right) => !left.Equals(right);
}

public sealed class LivingMaterialSpeciesProfile
{
    public LivingMaterialSpeciesProfile(
        LivingMaterialSpecies species,
        ItemId itemId,
        int wanderRadius,
        int movementCreditPerEcologyStep,
        int reproductionPeriodSteps)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Living material item id cannot be empty.", nameof(itemId));
        }

        if (wanderRadius <= 0 || movementCreditPerEcologyStep <= 0 || reproductionPeriodSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wanderRadius));
        }

        Species = species;
        ItemId = itemId;
        WanderRadius = wanderRadius;
        MovementCreditPerEcologyStep = movementCreditPerEcologyStep;
        ReproductionPeriodSteps = reproductionPeriodSteps;
    }

    public LivingMaterialSpecies Species { get; }

    public ItemId ItemId { get; }

    public int WanderRadius { get; }

    public int MovementCreditPerEcologyStep { get; }

    public int ReproductionPeriodSteps { get; }
}

public static class LivingMaterialEcologyProfiles
{
    public const int EcologyStepsPerSimulationTick = 4;
    public const int EcologyStepsPerDay =
        GameTimeCadence.TicksPerDay * EcologyStepsPerSimulationTick;
    public const int MovementThreshold = 4000;
    public const int PopulationCapPerPlane = 10;
    public const int MaximumSuccessfulCycles = 2;
    public const int HamsterResidentNoticeRadius = 4;
    public const int HamsterReleaseDormancySteps = 1;

    public static readonly ItemId HamsterItemId = new ItemId("creature.hamster");
    public static readonly ItemId GrubItemId = new ItemId("creature.grub");
    public static readonly ItemId LegacyLarvaItemId = new ItemId("creature.larva");

    public static readonly LivingMaterialSpeciesProfile Hamster = new LivingMaterialSpeciesProfile(
        LivingMaterialSpecies.Hamster,
        HamsterItemId,
        wanderRadius: 6,
        movementCreditPerEcologyStep: 400,
        reproductionPeriodSteps: EcologyStepsPerDay);

    public static readonly LivingMaterialSpeciesProfile Grub = new LivingMaterialSpeciesProfile(
        LivingMaterialSpecies.Grub,
        GrubItemId,
        wanderRadius: 4,
        movementCreditPerEcologyStep: 325,
        reproductionPeriodSteps: EcologyStepsPerDay);

    public static LivingMaterialSpeciesProfile Get(LivingMaterialSpecies species)
    {
        return species == LivingMaterialSpecies.Hamster ? Hamster : Grub;
    }

    public static bool TryResolve(ItemId itemId, out LivingMaterialSpecies species)
    {
        if (itemId == HamsterItemId)
        {
            species = LivingMaterialSpecies.Hamster;
            return true;
        }

        if (itemId == GrubItemId || itemId == LegacyLarvaItemId)
        {
            species = LivingMaterialSpecies.Grub;
            return true;
        }

        species = default;
        return false;
    }
}

}
