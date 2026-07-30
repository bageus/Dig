using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private static CellId[] GetSameHeightActionCandidates(CellId target)
    {
        List<CellId> candidates = new List<CellId>
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
        };
        if (target.Z > CellId.MinimumDepth)
        {
            candidates.Add(new CellId(target.X, target.Y, target.Z - 1));
        }

        if (target.Z < CellId.MaximumDepth)
        {
            candidates.Add(new CellId(target.X, target.Y, target.Z + 1));
        }

        return candidates.Distinct().ToArray();
    }

    internal bool HasFullStandingSupport(CellId cell)
    {
        CellId support = new CellId(cell.X, cell.Y + 1, cell.Z);
        return _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Any(value => value.X == support.X
                && value.Y == support.Y
                && value.Z == support.Z
                && value.HasFullActorSupport);
    }

    internal Result InterruptUnsupportedStationaryActions(long tick)
    {
        if (_productionAgents == null)
        {
            return Result.Success();
        }

        foreach (AgentState resident in _productionAgents.GetAll()
            .Where(value => value.HasActiveFoodMeal)
            .OrderBy(value => value.Id))
        {
            if (HasFullStandingSupport(resident.Position))
            {
                continue;
            }

            Result interrupted = resident.InterruptFoodMeal(
                "unsupported_standing_position",
                tick);
            if (interrupted.IsFailure)
            {
                return interrupted;
            }

            _productionAgents.Save(resident);
            _journal.Append(resident.DequeueUncommittedEvents());
        }

        return Result.Success();
    }
}

internal sealed class DigTerrainResidentStandingSupportQuery
    : IResidentStandingSupportQuery
{
    private readonly DigTerrainWorkSession _session;

    internal DigTerrainResidentStandingSupportQuery(DigTerrainWorkSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public bool HasFullStandingSupport(CellId cell) =>
        _session.HasFullStandingSupport(cell);
}
}
