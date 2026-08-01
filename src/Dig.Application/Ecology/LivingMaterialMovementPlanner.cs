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
        CellId[] ordered = candidates.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            return new LivingMaterialMovementDecision(
                false,
                current,
                -desired,
                "no-navigation-candidate");
        }

        CellId[] forward = ordered
            .Where(value => Math.Sign(value.X - current.X) == desired)
            .ToArray();
        bool obstacle = forward.Length == 0;
        CellId[] pool;
        if (!obstacle)
        {
            pool = ordered
                .Where(value => Math.Sign(value.X - current.X) == desired
                    || value.X == current.X)
                .ToArray();
        }
        else
        {
            CellId[] reverse = ordered
                .Where(value => Math.Sign(value.X - current.X) == -desired)
                .ToArray();
            pool = reverse.Length == 0 ? ordered : reverse;
        }

        pool = PreferHamsterResidentSeparation(creature, pool, residentCells);
        int index = LivingMaterialDeterminism.SelectInclusive(
            worldSeed,
            creature.CreatureId,
            creature.DeterministicSequence,
            obstacle ? "obstacle-direction" : "movement-candidate",
            0,
            pool.Length - 1);
        CellId target = pool[index];
        int nextDirection = Math.Sign(target.X - current.X);
        if (nextDirection == 0)
        {
            nextDirection = desired;
        }

        return new LivingMaterialMovementDecision(
            true,
            target,
            nextDirection,
            obstacle ? "obstacle-reselected" : string.Empty);
    }

    private static CellId[] PreferHamsterResidentSeparation(
        LivingMaterialSnapshot creature,
        CellId[] candidates,
        IReadOnlyCollection<CellId> residentCells)
    {
        if (creature.Species != LivingMaterialSpecies.Hamster
            || !creature.Cell.HasValue)
        {
            return candidates;
        }

        CellId current = creature.Cell.Value;
        CellId? nearest = residentCells
            .Where(value => value.Y == current.Y)
            .Where(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                    value,
                    current)
                <= LivingMaterialEcologyProfiles.HamsterResidentNoticeRadius)
            .OrderBy(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                value,
                current))
            .ThenBy(value => value)
            .Select(value => (CellId?)value)
            .FirstOrDefault();
        if (!nearest.HasValue)
        {
            return candidates;
        }

        int maximumDistance = candidates.Max(value =>
            LivingMaterialMovementGeometry.ChebyshevDistanceXZ(value, nearest.Value));
        return candidates
            .Where(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                value,
                nearest.Value) == maximumDistance)
            .OrderBy(value => value)
            .ToArray();
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
            .Where(value => value.Y == current.Y)
            .Where(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                    value,
                    current)
                <= LivingMaterialEcologyProfiles.HamsterResidentNoticeRadius)
            .OrderBy(value => LivingMaterialMovementGeometry.ChebyshevDistanceXZ(
                value,
                current))
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
