using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed partial class AdvanceLivingMaterialEcologyCommandHandler
{
    private Result AdvanceMovement(
        LivingMaterialEcologyState ecology,
        InventoryState inventory,
        LivingMaterialPlaneResolver planes,
        IReadOnlyCollection<CellId> residentCells,
        long tick)
    {
        foreach (LivingMaterialSnapshot creature in ecology.GetAll()
            .Where(value => value.IsMovementDue))
        {
            IReadOnlyList<CellId> candidates = planes.GetMovementCandidates(creature);
            LivingMaterialMovementDecision decision = _movement.Plan(
                creature,
                candidates,
                residentCells,
                ecology.WorldSeed);
            if (!decision.CanMove)
            {
                Result blocked = ecology.CommitBlocked(
                    creature.CreatureId,
                    decision.NextDirection,
                    decision.Reason,
                    tick);
                if (blocked.IsFailure)
                {
                    return blocked;
                }

                continue;
            }

            Result movedItem = inventory.MoveAvailable(
                creature.ItemEntityId,
                quantity: 1,
                ItemLocation.InWorld(decision.Target),
                splitStackId: default,
                tick);
            if (movedItem.IsFailure)
            {
                return movedItem;
            }

            Result movedCreature = ecology.CommitMovement(
                creature.CreatureId,
                decision.Target,
                creature.PlaneKey,
                decision.NextDirection,
                tick);
            if (movedCreature.IsFailure)
            {
                throw new InvalidOperationException(
                    "Validated living material movement failed after Inventory commit: "
                    + movedCreature.Error);
            }
        }

        return Result.Success();
    }

    private static Result AdvanceReproduction(
        LivingMaterialEcologyState ecology,
        InventoryState inventory,
        LivingMaterialPlaneResolver planes,
        long tick)
    {
        LivingMaterialSnapshot[] due = ecology.GetReproductionDue().ToArray();
        HashSet<LivingMaterialPlaneKey> processedHamsterPlanes =
            new HashSet<LivingMaterialPlaneKey>();
        foreach (LivingMaterialSnapshot parent in due)
        {
            if (parent.Species == LivingMaterialSpecies.Hamster)
            {
                if (!processedHamsterPlanes.Add(parent.PlaneKey))
                {
                    continue;
                }

                if (ecology.CountFree(parent.Species, parent.PlaneKey) < 2)
                {
                    continue;
                }
            }

            int population = ecology.CountFree(parent.Species, parent.PlaneKey);
            if (population >= LivingMaterialEcologyProfiles.PopulationCapPerPlane)
            {
                continue;
            }

            ItemId offspringItem = LivingMaterialEcologyProfiles.Get(parent.Species).ItemId;
            if (!inventory.Catalog.Contains(offspringItem))
            {
                return Result.Failure(LivingMaterialApplicationErrors.UnknownItem);
            }

            if (!parent.Cell.HasValue
                || !planes.TryResolve(parent.Cell.Value, out LivingMaterialPlane plane)
                || plane.Key != parent.PlaneKey)
            {
                continue;
            }

            CellId offspringCell = SelectOffspringCell(parent, plane);
            Result<LivingMaterialReproductionPlan> planned = ecology.PlanReproduction(
                parent.CreatureId,
                offspringCell);
            if (planned.IsFailure)
            {
                return Result.Failure(planned.Error!);
            }

            Result added = inventory.AddUnit(
                planned.Value.OffspringId,
                offspringItem,
                ItemLocation.InWorld(offspringCell),
                tick);
            if (added.IsFailure)
            {
                return added;
            }

            Result committed = ecology.CommitReproduction(planned.Value, tick);
            if (committed.IsFailure)
            {
                throw new InvalidOperationException(
                    "Validated living material reproduction failed after Inventory commit: "
                    + committed.Error);
            }
        }

        return Result.Success();
    }

    private static CellId SelectOffspringCell(
        LivingMaterialSnapshot parent,
        LivingMaterialPlane plane)
    {
        CellId origin = parent.Cell!.Value;
        LivingMaterialSpeciesProfile profile = LivingMaterialEcologyProfiles.Get(parent.Species);
        CellId[] candidates = plane.Cells
            .Where(value => LivingMaterialMovementGeometry.IsWithinWanderRadius(
                parent.AnchorCell,
                value,
                profile.WanderRadius))
            .OrderBy(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                value,
                origin))
            .ThenBy(value => value)
            .ToArray();
        return candidates.Length == 0 ? origin : candidates[0];
    }
}

}
