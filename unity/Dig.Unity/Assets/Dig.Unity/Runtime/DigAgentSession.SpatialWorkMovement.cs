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
    private static readonly IReadOnlyDictionary<string, SurfacePose> NoSpatialWorkTargets =
        new Dictionary<string, SurfacePose>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, SurfacePose> _spatialWorkTargets =
        NoSpatialWorkTargets;

    internal void SetSpatialWorkMovementTargets(
        IReadOnlyDictionary<string, SurfacePose> targets)
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
            out SurfacePose destination))
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
            destination.Cell);
        if (!path.Succeeded || path.Path == null)
        {
            result = Result.Failure(new DomainError(
                $"agents.spatial_work.{path.FailureReason.ToString().ToLowerInvariant()}",
                path.Detail));
            return true;
        }

        CellId next = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : destination.Cell;
        if (!IsMovementStepDue(
            agent,
            next,
            ResidentMovementCommandSource.SpatialWork,
            repeatedManualCommand: false,
            remainingPathSteps: Math.Max(0, path.Path.Cells.Count - 1)))
        {
            result = Result.Success();
            return true;
        }

        if (agent.Position == destination.Cell)
        {
            if (destination.Face == SurfaceFace.Floor
                && !_tunnelVolume.HasFullActorSupport(destination.Cell))
            {
                if (agent.SurfacePose.IsVertical)
                {
                    result = Result.Success();
                    return true;
                }

                if (!VerticalSurfaceSteering.TryAttachToWall(
                    agent.SurfacePose,
                    face => IsExposedClimbFace(agent.Position, face),
                    out SurfacePose climbingPose))
                {
                    result = Result.Failure(new DomainError(
                        "agents.spatial_work.support_unavailable",
                        "Unsupported spatial work requires an exposed climbing face."));
                    return true;
                }

                result = MoveOnReservedSurface(agent, climbingPose);
                if (result.IsSuccess)
                {
                    SaveAutomaticSurfaceProgress(agent);
                }
                return true;
            }

            if (!_surfaceTraffic.CanOccupy(agent.Id, destination, _tick))
            {
                result = Result.Success();
                return true;
            }
            result = MoveOnReservedSurface(agent, destination);
            if (result.IsSuccess)
            {
                SaveAutomaticSurfaceProgress(agent);
            }
            return true;
        }

        result = MoveThroughTunnelTraffic(agent, next);
        return true;
    }
}

}
