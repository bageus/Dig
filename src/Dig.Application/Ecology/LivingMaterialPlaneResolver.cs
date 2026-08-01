using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed class LivingMaterialPlane
{
    public LivingMaterialPlane(LivingMaterialPlaneKey key, IReadOnlyCollection<CellId> cells)
    {
        Key = key;
        Cells = cells.OrderBy(value => value).ToArray();
    }

    public LivingMaterialPlaneKey Key { get; }

    public IReadOnlyList<CellId> Cells { get; }
}

public sealed class LivingMaterialPlaneResolver
{
    private readonly NavigationSnapshot _navigation;
    private readonly Dictionary<CellId, LivingMaterialPlane> _planesByCell =
        new Dictionary<CellId, LivingMaterialPlane>();

    public LivingMaterialPlaneResolver(NavigationSnapshot navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public bool TryResolve(CellId cell, out LivingMaterialPlane plane)
    {
        if (_planesByCell.TryGetValue(cell, out plane!))
        {
            return true;
        }

        if (!_navigation.IsWalkable(cell))
        {
            plane = null!;
            return false;
        }

        HashSet<CellId> visited = new HashSet<CellId>();
        Queue<CellId> frontier = new Queue<CellId>();
        visited.Add(cell);
        frontier.Enqueue(cell);
        while (frontier.Count > 0)
        {
            CellId current = frontier.Dequeue();
            foreach (NavigationTransition transition in _navigation.GetTransitions(current))
            {
                if (!IsEcologyCardinalEdge(current, transition))
                {
                    continue;
                }

                if (visited.Add(transition.Target))
                {
                    frontier.Enqueue(transition.Target);
                }
            }
        }

        LivingMaterialPlane created = new LivingMaterialPlane(
            new LivingMaterialPlaneKey(visited.Min()),
            visited);
        foreach (CellId member in visited)
        {
            _planesByCell[member] = created;
        }

        plane = created;
        return true;
    }

    public IReadOnlyList<CellId> GetMovementCandidates(
        LivingMaterialSnapshot creature)
    {
        if (creature == null || !creature.Cell.HasValue)
        {
            return Array.Empty<CellId>();
        }

        CellId from = creature.Cell.Value;
        LivingMaterialSpeciesProfile profile = LivingMaterialEcologyProfiles.Get(creature.Species);
        HashSet<CellId> candidates = new HashSet<CellId>();
        foreach (NavigationTransition transition in _navigation.GetTransitions(from))
        {
            if (IsEcologyCardinalEdge(from, transition))
            {
                candidates.Add(transition.Target);
            }
        }

        foreach (int deltaX in new[] { -1, 1 })
        {
            foreach (int deltaZ in new[] { -1, 1 })
            {
                CellId target = new CellId(
                    from.X + deltaX,
                    from.Y,
                    from.Z + deltaZ);
                if (IsLegalDiagonal(from, target))
                {
                    candidates.Add(target);
                }
            }
        }

        return candidates
            .Where(target => LivingMaterialMovementGeometry.IsWithinWanderRadius(
                creature.AnchorCell,
                target,
                profile.WanderRadius))
            .Where(target => TryResolve(target, out LivingMaterialPlane plane)
                && plane.Key == creature.PlaneKey)
            .OrderBy(target => target)
            .ToArray();
    }

    private bool IsLegalDiagonal(CellId from, CellId target)
    {
        if (!_navigation.IsWalkable(target)
            || target.Y != from.Y
            || Math.Abs(target.X - from.X) != 1
            || Math.Abs(target.Z - from.Z) != 1)
        {
            return false;
        }

        CellId sideX = new CellId(target.X, from.Y, from.Z);
        CellId sideZ = new CellId(from.X, from.Y, target.Z);
        return HasEcologyCardinalEdge(from, sideX)
            && HasEcologyCardinalEdge(from, sideZ)
            && HasEcologyCardinalEdge(sideX, target)
            && HasEcologyCardinalEdge(sideZ, target);
    }

    private bool HasEcologyCardinalEdge(CellId from, CellId target)
    {
        return _navigation.GetTransitions(from)
            .Any(transition => transition.Target == target
                && IsEcologyCardinalEdge(from, transition));
    }

    private static bool IsEcologyCardinalEdge(
        CellId from,
        NavigationTransition transition)
    {
        CellId target = transition.Target;
        int deltaX = Math.Abs(target.X - from.X);
        int deltaZ = Math.Abs(target.Z - from.Z);
        return !transition.LinkKind.HasValue
            && target.Y == from.Y
            && deltaX + deltaZ == 1
            && (transition.TraversalKind == TunnelTraversalKind.SupportedWalk
                || transition.TraversalKind == TunnelTraversalKind.DepthTraverse);
    }
}

}
