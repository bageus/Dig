using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private static readonly IReadOnlyDictionary<string, CellId> NoSpatialWorkTargets =
        new Dictionary<string, CellId>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, CellId> _spatialWorkTargets =
        NoSpatialWorkTargets;

    internal void SetSpatialWorkMovementTargets(
        IReadOnlyDictionary<string, CellId> targets)
    {
        _spatialWorkTargets = targets
            ?? throw new ArgumentNullException(nameof(targets));
    }

    private bool TryAdvanceSpatialWorkMovement(
        AgentState agent,
        out Result result)
    {
        if (!_spatialWorkTargets.TryGetValue(
            agent.Id.ToString(),
            out CellId destination))
        {
            result = Result.Success();
            return false;
        }

        if (_tunnelVolume == null || _tunnelJournal == null)
        {
            result = Result.Failure(new DomainError(
                "agents.spatial_work.navigation_missing",
                "Spatial work movement requires initialized tunnel navigation."));
            return true;
        }

        TunnelPathResult path = _tunnelVolume.FindPath(
            agent.Position,
            destination);
        if (!path.Succeeded || path.Path == null)
        {
            result = Result.Failure(new DomainError(
                $"agents.spatial_work.{path.FailureReason.ToString().ToLowerInvariant()}",
                path.Detail));
            return true;
        }

        CellId current = agent.Position;
        CellId next = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : destination;
        if (!_tunnelTraffic.CanMove(agent.Id, current, next, _tick))
        {
            result = Result.Success();
            return true;
        }

        result = agent.MoveTo(next, _tick);
        if (result.IsFailure)
        {
            return true;
        }

        _tunnelTraffic.RecordMove(agent.Id, current, next, _tick);
        _repository.Save(agent);
        _tunnelJournal.Append(agent.DequeueUncommittedEvents());
        return true;
    }
}

}
