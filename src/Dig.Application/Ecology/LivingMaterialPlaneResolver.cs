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
                if (!IsFlatSupportedEdge(current, transition))
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
        return _navigation.GetTransitions(from)
            .Where(transition => IsFlatSupportedEdge(from, transition))
            .Select(transition => transition.Target)
            .Where(target => Math.Abs(target.X - creature.AnchorCell.X) <= profile.WanderRadius)
            .Where(target => TryResolve(target, out LivingMaterialPlane plane)
                && plane.Key == creature.PlaneKey)
            .OrderBy(target => target)
            .ToArray();
    }

    private static bool IsFlatSupportedEdge(
        CellId from,
        NavigationTransition transition)
    {
        CellId target = transition.Target;
        return transition.TraversalKind == TunnelTraversalKind.SupportedWalk
            && !transition.LinkKind.HasValue
            && target.Y == from.Y
            && target.Z == from.Z
            && Math.Abs(target.X - from.X) == 1;
    }
}

}
