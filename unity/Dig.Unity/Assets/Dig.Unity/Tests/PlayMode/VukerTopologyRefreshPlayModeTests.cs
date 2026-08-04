using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class VukerTopologyRefreshPlayModeTests
{
    [Test]
    public void TopologyRefreshAllowsVukerInNewlyExcavatedSupportedCell()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        VukerIndividualSnapshot vuker = agents.LoadVukerEcology()
            .Individuals
            .OrderBy(value => value.EntityId.ToString())
            .First();
        TunnelNavigationVolume before = agents.TunnelVolume;
        HashSet<CellId> initiallySupported = before.SupportedCells.ToHashSet();
        WorldCellViewModel[] cells = world.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToArray();
        Dictionary<CellId, WorldCellViewModel> byCell = cells.ToDictionary(
            value => new CellId(value.X, value.Y, value.Z));

        CellId? target = FindNewSupportedCell(
            world,
            vuker.Position,
            cells,
            byCell,
            initiallySupported);

        Assert.That(target.HasValue, Is.True,
            "The demo world must expose an excavatable supported frontier cell.");
        agents.SynchronizeNavigation(
            world.LoadSnapshot(),
            world.PlannedTunnelCells,
            world.PlannedVerticalTunnelCells);
        Assert.That(agents.TunnelVolume.SupportedCells, Does.Contain(target!.Value));
        Assert.That(agents.MoveResident(
            vuker.EntityId.ToString(),
            target.Value).IsSuccess, Is.True);

        Result advanced = Result.Success();
        Assert.DoesNotThrow(() => advanced = agents.Advance());
        Assert.That(advanced.IsSuccess, Is.True, advanced.Error?.ToString());
        Assert.That(agents.LoadVukerEcology().Individuals
            .Single(value => value.EntityId == vuker.EntityId)
            .Position, Is.EqualTo(target.Value));
    }

    private static CellId? FindNewSupportedCell(
        DigWorldSession world,
        CellId origin,
        IReadOnlyList<WorldCellViewModel> cells,
        IReadOnlyDictionary<CellId, WorldCellViewModel> byCell,
        ISet<CellId> initiallySupported)
    {
        foreach (WorldCellViewModel candidateView in cells
            .Where(value => value.IsSolid && value.IsExplored)
            .OrderBy(value => System.Math.Abs(value.X - origin.X)
                + System.Math.Abs(value.Y - origin.Y)
                + System.Math.Abs(value.Z - origin.Z))
            .ThenBy(value => value.X)
            .ThenBy(value => value.Y)
            .ThenBy(value => value.Z))
        {
            CellId candidate = new CellId(
                candidateView.X,
                candidateView.Y,
                candidateView.Z);
            CellId support = new CellId(
                candidate.X,
                candidate.Y + 1,
                candidate.Z);
            if (!byCell.TryGetValue(support, out WorldCellViewModel supportView)
                || !supportView.IsSolid
                || !HorizontalNeighbours(candidate).Any(initiallySupported.Contains))
            {
                continue;
            }

            Result excavated = world.ExcavateSpatialCell(candidate);
            if (excavated.IsFailure)
            {
                continue;
            }

            TunnelNavigationVolume refreshed = world.CreateTunnelNavigationVolume();
            if (refreshed.SupportedCells.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<CellId> HorizontalNeighbours(CellId cell)
    {
        yield return new CellId(cell.X - 1, cell.Y, cell.Z);
        yield return new CellId(cell.X + 1, cell.Y, cell.Z);
        yield return new CellId(cell.X, cell.Y - 1, cell.Z);
        yield return new CellId(cell.X, cell.Y + 1, cell.Z);
    }
}

}
