using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class LivingMaterialEcologyState
{
    public IReadOnlyList<LivingMaterialSnapshot> GetReproductionDue()
    {
        return _creatures.Values
            .Select(value => value.ToSnapshot())
            .Where(value => value.IsReproductionDue(EcologyStep))
            .OrderBy(value => value.PlaneKey)
            .ThenBy(value => value.Species)
            .ThenBy(value => value.CreatureId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public int CountFree(
        LivingMaterialSpecies species,
        LivingMaterialPlaneKey planeKey)
    {
        return _creatures.Values.Count(value =>
            value.Species == species
            && value.PlaneKey == planeKey
            && value.Containment == LivingMaterialContainment.Free);
    }

    public Result<LivingMaterialReproductionPlan> PlanReproduction(
        EntityId parentId,
        CellId offspringCell)
    {
        if (!_creatures.TryGetValue(parentId, out LivingMaterialIndividual? parent))
        {
            return Result<LivingMaterialReproductionPlan>.Failure(
                LivingMaterialErrors.NotFound);
        }

        LivingMaterialSnapshot snapshot = parent.ToSnapshot();
        if (!snapshot.IsReproductionDue(EcologyStep))
        {
            return Result<LivingMaterialReproductionPlan>.Failure(
                LivingMaterialErrors.InvalidState);
        }

        LivingMaterialSpeciesProfile profile =
            LivingMaterialEcologyProfiles.Get(parent.Species);
        if (!LivingMaterialMovementGeometry.IsWithinWanderRadius(
            parent.AnchorCell,
            offspringCell,
            profile.WanderRadius))
        {
            return Result<LivingMaterialReproductionPlan>.Failure(
                LivingMaterialErrors.InvalidMovement);
        }

        EntityId offspringId = LivingMaterialDeterminism.CreateOffspringId(
            parentId,
            parent.Species,
            parent.ReproductionCyclesCompleted + 1);
        if (_creatures.ContainsKey(offspringId) || _creatureByItem.ContainsKey(offspringId))
        {
            return Result<LivingMaterialReproductionPlan>.Failure(
                LivingMaterialErrors.AlreadyExists);
        }

        return Result<LivingMaterialReproductionPlan>.Success(
            new LivingMaterialReproductionPlan(
                parentId,
                offspringId,
                parent.Species,
                offspringCell,
                parent.PlaneKey,
                parent.ReproductionCyclesCompleted,
                EcologyStep));
    }

    public Result CommitReproduction(
        LivingMaterialReproductionPlan plan,
        long tick)
    {
        ValidateTick(tick);
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!_creatures.TryGetValue(plan.ParentId, out LivingMaterialIndividual? parent)
            || parent.Species != plan.Species
            || parent.PlaneKey != plan.PlaneKey
            || parent.ReproductionCyclesCompleted != plan.ExpectedParentCycles
            || plan.EcologyStep != EcologyStep
            || !parent.ToSnapshot().IsReproductionDue(EcologyStep)
            || _creatures.ContainsKey(plan.OffspringId)
            || _creatureByItem.ContainsKey(plan.OffspringId))
        {
            return Result.Failure(LivingMaterialErrors.ReproductionConflict);
        }

        parent.ReproductionCyclesCompleted++;
        parent.NextReproductionStep = checked(
            EcologyStep + LivingMaterialEcologyProfiles.Get(parent.Species).ReproductionPeriodSteps);
        parent.DeterministicSequence = checked(parent.DeterministicSequence + 1);
        LivingMaterialIndividual offspring = LivingMaterialIndividual.Create(
            WorldSeed,
            plan.OffspringId,
            plan.OffspringId,
            plan.Species,
            plan.OffspringCell,
            plan.PlaneKey,
            EcologyStep);
        _creatures.Add(plan.OffspringId, offspring);
        _creatureByItem.Add(plan.OffspringId, plan.OffspringId);
        IncrementVersion(parent);
        IncrementVersion(offspring);
        Raise(new LivingMaterialReproduced(
            tick,
            plan.ParentId,
            plan.OffspringId,
            plan.Species,
            plan.OffspringCell));
        return Result.Success();
    }
}

}
