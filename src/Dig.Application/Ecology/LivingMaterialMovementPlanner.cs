using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Ecology;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed class LivingMaterialMovementPlanner
{
    public LivingMaterialMovementDecision Plan(
        LivingMaterialSnapshot creature,
        IReadOnlyCollection<CellId> candidates,
        IReadOnlyCollection<CellId> residentCells,
        ulong worldSeed)
    {
        if (creature == null || candidates == null || residentCells == null)
        {
            throw new ArgumentNullException(nameof(creature));
        }

        if (!creature.Cell.HasValue)
        {
            return new LivingMaterialMovementDecision(false, default, 1, "stored");
        }

        CellId current = creature.Cell.Value;
        int desired = ResolveDesiredDirection(creature, residentCells, current, worldSeed);
        CellId? preferred = candidates
            .Where(value => Math.Sign(value.X - current.X) == desired)
            .OrderBy(value => value)
            .Select(value => (CellId?)value)
            .FirstOrDefault();
        if (preferred.HasValue)
        {
            return new LivingMaterialMovementDecision(
                true,
                preferred.Value,
                desired,
                string.Empty);
        }

        CellId[] ordered = candidates.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            return new LivingMaterialMovementDecision(
                false,
                current,
                desired == 0 ? 1 : -desired,
                "no-flat-candidate");
        }

        int index = LivingMaterialDeterminism.SelectInclusive(
            worldSeed,
            creature.CreatureId,
            creature.DeterministicSequence,
            "obstacle-direction",
            0,
            ordered.Length - 1);
        CellId target = ordered[index];
        return new LivingMaterialMovementDecision(
            true,
            target,
            Math.Sign(target.X - current.X),
            "obstacle-reselected");
    }

    private static int ResolveDesiredDirection(
        LivingMaterialSnapshot creature,
        IReadOnlyCollection<CellId> residentCells,
        CellId current,
        ulong worldSeed)
    {
        int desired = creature.Direction;
        if (desired == 0)
        {
            desired = LivingMaterialDeterminism.SelectInclusive(
                worldSeed,
                creature.CreatureId,
                creature.DeterministicSequence,
                "missing-direction",
                0,
                1) == 0 ? -1 : 1;
        }

        if (creature.Species != LivingMaterialSpecies.Hamster)
        {
            return desired;
        }

        CellId? nearest = residentCells
            .Where(value => value.Y == current.Y && value.Z == current.Z)
            .Where(value => Math.Abs(value.X - current.X)
                <= LivingMaterialEcologyProfiles.HamsterResidentNoticeRadius)
            .OrderBy(value => Math.Abs(value.X - current.X))
            .ThenBy(value => value)
            .Select(value => (CellId?)value)
            .FirstOrDefault();
        if (!nearest.HasValue || nearest.Value.X == current.X)
        {
            return desired;
        }

        return nearest.Value.X < current.X ? 1 : -1;
    }
}

}
