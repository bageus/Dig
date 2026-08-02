using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed class VukerCaveRegion
{
    public VukerCaveRegion(VukerRegionKey key, IReadOnlyCollection<CellId> cells)
    {
        Key = key;
        Cells = new ReadOnlyCollection<CellId>(
            (cells ?? throw new ArgumentNullException(nameof(cells)))
                .OrderBy(value => value)
                .ToArray());
    }

    public VukerRegionKey Key { get; }
    public IReadOnlyList<CellId> Cells { get; }
}

public sealed class VukerCaveRegionResolver
{
    private readonly Dictionary<CellId, VukerCaveRegion> _byCell =
        new Dictionary<CellId, VukerCaveRegion>();
    private readonly IReadOnlyList<VukerCaveRegion> _regions;

    public VukerCaveRegionResolver(TunnelNavigationVolume volume)
    {
        if (volume == null)
        {
            throw new ArgumentNullException(nameof(volume));
        }

        HashSet<CellId> remaining = new HashSet<CellId>(volume.SupportedCells);
        List<VukerCaveRegion> regions = new List<VukerCaveRegion>();
        while (remaining.Count > 0)
        {
            CellId root = remaining.OrderBy(value => value).First();
            Queue<CellId> queue = new Queue<CellId>();
            List<CellId> cells = new List<CellId>();
            queue.Enqueue(root);
            remaining.Remove(root);
            while (queue.Count > 0)
            {
                CellId current = queue.Dequeue();
                cells.Add(current);
                foreach (CellId candidate in Neighbours(current))
                {
                    if (!remaining.Contains(candidate))
                    {
                        continue;
                    }

                    TunnelTraversalKind kind = volume.ClassifyTraversal(current, candidate);
                    if (!IsVukerRegionEdge(kind))
                    {
                        continue;
                    }

                    remaining.Remove(candidate);
                    queue.Enqueue(candidate);
                }
            }

            VukerCaveRegion region = new VukerCaveRegion(
                new VukerRegionKey(cells.Min()),
                cells);
            regions.Add(region);
            foreach (CellId cell in cells)
            {
                _byCell.Add(cell, region);
            }
        }

        _regions = new ReadOnlyCollection<VukerCaveRegion>(
            regions.OrderBy(value => value.Key).ToArray());
    }

    public IReadOnlyList<VukerCaveRegion> Regions => _regions;

    public bool TryResolve(CellId cell, out VukerCaveRegion region)
    {
        return _byCell.TryGetValue(cell, out region!);
    }

    public bool TryResolveKey(CellId cell, out VukerRegionKey key)
    {
        if (_byCell.TryGetValue(cell, out VukerCaveRegion? region))
        {
            key = region.Key;
            return true;
        }

        key = default;
        return false;
    }

    public CellId? FindNearestFreeCell(
        VukerRegionKey regionKey,
        CellId origin,
        IReadOnlyCollection<CellId> occupied)
    {
        if (occupied == null)
        {
            throw new ArgumentNullException(nameof(occupied));
        }

        VukerCaveRegion? region = _regions.FirstOrDefault(value => value.Key == regionKey);
        if (region == null)
        {
            return null;
        }

        HashSet<CellId> blocked = new HashSet<CellId>(occupied);
        return region.Cells
            .Where(cell => !blocked.Contains(cell))
            .OrderBy(cell => Distance(origin, cell))
            .ThenBy(cell => cell)
            .Cast<CellId?>()
            .FirstOrDefault();
    }

    private static int Distance(CellId first, CellId second)
    {
        return Math.Abs(first.X - second.X)
            + Math.Abs(first.Y - second.Y)
            + Math.Abs(first.Z - second.Z);
    }

    private static bool IsVukerRegionEdge(TunnelTraversalKind kind)
    {
        return kind == TunnelTraversalKind.SupportedWalk
            || kind == TunnelTraversalKind.VerticalClimb
            || kind == TunnelTraversalKind.DepthTraverse;
    }

    private static IEnumerable<CellId> Neighbours(CellId cell)
    {
        yield return new CellId(cell.X - 1, cell.Y, cell.Z);
        yield return new CellId(cell.X + 1, cell.Y, cell.Z);
        yield return new CellId(cell.X, cell.Y - 1, cell.Z);
        yield return new CellId(cell.X, cell.Y + 1, cell.Z);
        yield return new CellId(cell.X, cell.Y, cell.Z - 1);
        yield return new CellId(cell.X, cell.Y, cell.Z + 1);
    }
}

public sealed class VukerBirthPlan
{
    public VukerBirthPlan(
        VukerPairId pairId,
        EntityId childId,
        VukerRegionKey region,
        CellId position)
    {
        PairId = pairId;
        ChildId = childId;
        Region = region;
        Position = position;
    }

    public VukerPairId PairId { get; }
    public EntityId ChildId { get; }
    public VukerRegionKey Region { get; }
    public CellId Position { get; }
}

public sealed class VukerBirthPlanner
{
    private readonly VukerCaveRegionResolver _regions;

    public VukerBirthPlanner(VukerCaveRegionResolver regions)
    {
        _regions = regions ?? throw new ArgumentNullException(nameof(regions));
    }

    public Result<VukerBirthPlan> Plan(
        VukerEcologyState state,
        VukerPairSnapshot pair,
        IReadOnlyCollection<CellId> occupied,
        long tick)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (pair == null)
        {
            throw new ArgumentNullException(nameof(pair));
        }

        if (!pair.IsDue(tick))
        {
            return Result<VukerBirthPlan>.Failure(VukerEcologyErrors.BirthNotDue);
        }

        if (state.CountLiving(pair.Region) >= VukerEcologyProfile.PopulationCapPerRegion)
        {
            return Result<VukerBirthPlan>.Failure(VukerEcologyErrors.PopulationCapReached);
        }

        VukerIndividualSnapshot? first = state.GetIndividual(pair.FirstParentId);
        VukerIndividualSnapshot? second = state.GetIndividual(pair.SecondParentId);
        if (first == null || second == null || !first.IsAlive || !second.IsAlive)
        {
            return Result<VukerBirthPlan>.Failure(VukerEcologyErrors.InvalidLifecycle);
        }

        VukerIndividualSnapshot anchor = string.Compare(
            first.EntityId.ToString(),
            second.EntityId.ToString(),
            StringComparison.Ordinal) <= 0 ? first : second;
        CellId? cell = _regions.FindNearestFreeCell(pair.Region, anchor.Position, occupied);
        if (!cell.HasValue)
        {
            return Result<VukerBirthPlan>.Failure(new DomainError(
                "ecology.vuker.birth_cell_blocked",
                "No legal free birth cell exists in the connected cave region."));
        }

        EntityId childId = state.CreateDeterministicChildId(
            pair.PairId,
            pair.SuccessfulCycles);
        return Result<VukerBirthPlan>.Success(new VukerBirthPlan(
            pair.PairId,
            childId,
            pair.Region,
            cell.Value));
    }
}

}
