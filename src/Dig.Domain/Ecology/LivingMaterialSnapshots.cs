using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed class LivingMaterialSnapshot
{
    public LivingMaterialSnapshot(
        EntityId creatureId,
        EntityId itemEntityId,
        LivingMaterialSpecies species,
        LivingMaterialContainment containment,
        CellId? cell,
        CellId anchorCell,
        LivingMaterialPlaneKey planeKey,
        int direction,
        LivingMaterialActivity activity,
        int activityStepsRemaining,
        int movementCredit,
        int successfulMovementSteps,
        int nextSearchAtStep,
        int nextSleepAtStep,
        int reproductionCyclesCompleted,
        long nextReproductionStep,
        long deterministicSequence,
        string? blockedReason,
        long version,
        SurfacePose? surfacePose = null)
    {
        if (creatureId.IsEmpty || itemEntityId.IsEmpty)
        {
            throw new ArgumentException("Creature and item ids are required.");
        }

        if (direction < -1 || direction > 1
            || activityStepsRemaining < 0
            || movementCredit < 0
            || successfulMovementSteps < 0
            || nextSearchAtStep < 0
            || nextSleepAtStep < 0
            || reproductionCyclesCompleted < 0
            || reproductionCyclesCompleted > LivingMaterialEcologyProfiles.MaximumSuccessfulCycles
            || nextReproductionStep < 0
            || deterministicSequence < 0
            || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        CreatureId = creatureId;
        ItemEntityId = itemEntityId;
        Species = species;
        Containment = containment;
        Cell = cell;
        AnchorCell = anchorCell;
        PlaneKey = planeKey;
        Direction = direction;
        Activity = activity;
        ActivityStepsRemaining = activityStepsRemaining;
        MovementCredit = movementCredit;
        SuccessfulMovementSteps = successfulMovementSteps;
        NextSearchAtStep = nextSearchAtStep;
        NextSleepAtStep = nextSleepAtStep;
        ReproductionCyclesCompleted = reproductionCyclesCompleted;
        NextReproductionStep = nextReproductionStep;
        DeterministicSequence = deterministicSequence;
        BlockedReason = string.IsNullOrWhiteSpace(blockedReason) ? null : blockedReason.Trim();
        Version = version;
        SurfacePose = surfacePose
            ?? Dig.Domain.Navigation.SurfacePose.FloorCentre(cell ?? anchorCell);
        if (cell.HasValue && SurfacePose.Cell != cell.Value)
        {
            throw new ArgumentException("Creature surface pose must belong to its cell.");
        }
    }

    public EntityId CreatureId { get; }
    public EntityId ItemEntityId { get; }
    public LivingMaterialSpecies Species { get; }
    public LivingMaterialContainment Containment { get; }
    public CellId? Cell { get; }
    public CellId AnchorCell { get; }
    public LivingMaterialPlaneKey PlaneKey { get; }
    public int Direction { get; }
    public LivingMaterialActivity Activity { get; }
    public int ActivityStepsRemaining { get; }
    public int MovementCredit { get; }
    public int SuccessfulMovementSteps { get; }
    public int NextSearchAtStep { get; }
    public int NextSleepAtStep { get; }
    public int ReproductionCyclesCompleted { get; }
    public long NextReproductionStep { get; }
    public long DeterministicSequence { get; }
    public string? BlockedReason { get; }
    public long Version { get; }
    public SurfacePose SurfacePose { get; }

    public bool IsFree => Containment == LivingMaterialContainment.Free && Cell.HasValue;

    public bool IsMovementDue => IsFree
        && (Activity == LivingMaterialActivity.Moving || Activity == LivingMaterialActivity.Blocked)
        && MovementCredit >= LivingMaterialEcologyProfiles.MovementThreshold;

    public bool IsReproductionDue(long ecologyStep) => IsFree
        && Activity != LivingMaterialActivity.ReleaseDormant
        && ReproductionCyclesCompleted < LivingMaterialEcologyProfiles.MaximumSuccessfulCycles
        && ecologyStep >= NextReproductionStep;
}

public sealed class LivingMaterialEcologySnapshot
{
    public LivingMaterialEcologySnapshot(
        ulong worldSeed,
        long ecologyStep,
        long version,
        IReadOnlyCollection<LivingMaterialSnapshot> creatures)
    {
        if (ecologyStep < 0 || version < 0 || creatures == null)
        {
            throw new ArgumentOutOfRangeException(nameof(ecologyStep));
        }

        LivingMaterialSnapshot[] ordered = creatures
            .OrderBy(value => value.PlaneKey)
            .ThenBy(value => value.Species)
            .ThenBy(value => value.CreatureId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (ordered.Select(value => value.CreatureId).Distinct().Count() != ordered.Length
            || ordered.Select(value => value.ItemEntityId).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException("Creature and linked item ids must be unique.", nameof(creatures));
        }

        WorldSeed = worldSeed;
        EcologyStep = ecologyStep;
        Version = version;
        Creatures = new ReadOnlyCollection<LivingMaterialSnapshot>(ordered);
    }

    public ulong WorldSeed { get; }
    public long EcologyStep { get; }
    public long Version { get; }
    public IReadOnlyList<LivingMaterialSnapshot> Creatures { get; }
}

public sealed class LivingMaterialReproductionPlan
{
    public LivingMaterialReproductionPlan(
        EntityId parentId,
        EntityId offspringId,
        LivingMaterialSpecies species,
        CellId offspringCell,
        LivingMaterialPlaneKey planeKey,
        int expectedParentCycles,
        long ecologyStep)
    {
        ParentId = parentId;
        OffspringId = offspringId;
        Species = species;
        OffspringCell = offspringCell;
        PlaneKey = planeKey;
        ExpectedParentCycles = expectedParentCycles;
        EcologyStep = ecologyStep;
    }

    public EntityId ParentId { get; }
    public EntityId OffspringId { get; }
    public LivingMaterialSpecies Species { get; }
    public CellId OffspringCell { get; }
    public LivingMaterialPlaneKey PlaneKey { get; }
    public int ExpectedParentCycles { get; }
    public long EcologyStep { get; }
}

}
