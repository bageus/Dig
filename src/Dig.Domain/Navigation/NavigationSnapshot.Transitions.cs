using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public sealed partial class NavigationSnapshot
{
    private void AddNormalTransitions(
        CellId from,
        Dictionary<CellId, NavigationTransition> transitions)
    {
        AddIfWalkable(
            transitions,
            new CellId(from.X - 1, from.Y, from.Z),
            Profile.OrthogonalCost);
        AddIfWalkable(
            transitions,
            new CellId(from.X + 1, from.Y, from.Z),
            Profile.OrthogonalCost);
        AddIfWalkable(
            transitions,
            new CellId(from.X, from.Y, from.Z - 1),
            Profile.OrthogonalCost);
        AddIfWalkable(
            transitions,
            new CellId(from.X, from.Y, from.Z + 1),
            Profile.OrthogonalCost);

        if (Profile.Mode == TraversalMode.Free)
        {
            AddIfWalkable(
                transitions,
                new CellId(from.X, from.Y - 1, from.Z),
                Profile.OrthogonalCost);
            AddIfWalkable(
                transitions,
                new CellId(from.X, from.Y + 1, from.Z),
                Profile.OrthogonalCost);
            return;
        }

        foreach (int direction in new[] { -1, 1 })
        {
            for (int step = 1; step <= Profile.MaxStepUp; step++)
            {
                AddIfWalkable(
                    transitions,
                    new CellId(from.X + direction, from.Y + step, from.Z),
                    checked(Profile.StepCost * step));
            }

            for (int step = 1; step <= Profile.MaxStepDown; step++)
            {
                AddIfWalkable(
                    transitions,
                    new CellId(from.X + direction, from.Y - step, from.Z),
                    checked(Profile.StepCost * step));
            }
        }
    }

    private void AddIfWalkable(
        Dictionary<CellId, NavigationTransition> transitions,
        CellId target,
        int cost)
    {
        if (IsWalkable(target))
        {
            AddLowestCost(
                transitions,
                new NavigationTransition(target, cost));
        }
    }

    private static void AddLowestCost(
        Dictionary<CellId, NavigationTransition> transitions,
        NavigationTransition candidate)
    {
        if (!transitions.TryGetValue(
            candidate.Target,
            out NavigationTransition existing)
            || candidate.Cost < existing.Cost)
        {
            transitions[candidate.Target] = candidate;
        }
    }

    private static Dictionary<CellId, IReadOnlyList<NavigationTransition>>
        BuildLinkTransitions(
            IReadOnlyList<TraversalLink> links,
            TraversalProfile profile)
    {
        Dictionary<CellId, List<NavigationTransition>> mutable =
            new Dictionary<CellId, List<NavigationTransition>>();
        foreach (TraversalLink link in links)
        {
            if (!profile.Allows(link.Kind))
            {
                continue;
            }

            AddLink(mutable, link.From, link.To, link);
            if (link.Bidirectional)
            {
                AddLink(mutable, link.To, link.From, link);
            }
        }

        return mutable.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<NavigationTransition>)new ReadOnlyCollection<NavigationTransition>(
                pair.Value
                    .OrderBy(transition => transition.Target)
                    .ThenBy(transition => transition.Cost)
                    .ToArray()));
    }

    private static void AddLink(
        Dictionary<CellId, List<NavigationTransition>> transitions,
        CellId from,
        CellId to,
        TraversalLink link)
    {
        if (!transitions.TryGetValue(from, out List<NavigationTransition>? list))
        {
            list = new List<NavigationTransition>();
            transitions.Add(from, list);
        }

        list.Add(new NavigationTransition(to, link.Cost, link.Kind));
    }

}

}
