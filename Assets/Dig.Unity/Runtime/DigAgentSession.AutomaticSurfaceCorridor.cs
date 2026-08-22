using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private readonly Dictionary<EntityId, SurfacePose> _automaticBoundaryApproaches =
        new Dictionary<EntityId, SurfacePose>();

    private Result MoveThroughAutomaticSurfaceCorridor(
        AgentState agent,
        CellId destination)
    {
        CellId current = agent.Position;
        SurfaceMoverKind mover = ResolveSurfaceMoverKind(agent.Id);
        if (VerticalSurfaceSteering.TryBuildNextPose(
            agent.SurfacePose,
            destination,
            face => IsExposedClimbFace(agent.Position, face),
            out SurfacePose verticalPose,
            out bool crossesVerticalBoundary))
        {
            if (!SurfaceTraversalPolicy.CanUse(mover, verticalPose))
            {
                return Result.Failure(AgentErrors.InvalidPosition);
            }

            if (crossesVerticalBoundary
                && !_tunnelTraffic.CanMove(agent.Id, current, destination, Tick))
            {
                return Result.Success();
            }

            if (!_surfaceTraffic.CanOccupy(agent.Id, verticalPose, Tick))
            {
                return Result.Success();
            }
            Result climbed = MoveOnReservedSurface(agent, verticalPose, mover);
            if (climbed.IsSuccess)
            {
                if (crossesVerticalBoundary)
                {
                    _tunnelTraffic.RecordMove(agent.Id, current, destination, Tick);
                }
                SaveAutomaticSurfaceProgress(agent);
            }
            return climbed;
        }

        if (agent.SurfacePose.IsVertical
            && VerticalSurfaceSteering.TryDetachToFloor(
                agent.SurfacePose,
                out SurfacePose floorPose))
        {
            if (!_surfaceTraffic.CanOccupy(agent.Id, floorPose, Tick))
            {
                return Result.Success();
            }
            Result detached = MoveOnReservedSurface(agent, floorPose, mover);
            if (detached.IsSuccess)
            {
                SaveAutomaticSurfaceProgress(agent);
            }
            return detached;
        }

        if (!SurfaceCorridorSteering.TryBuildBoundaryPoses(
            current,
            destination,
            out SurfacePose exitPose,
            out SurfacePose entryPose))
        {
            _automaticBoundaryApproaches.Remove(agent.Id);
            return MoveThroughCellTraffic(agent, current, destination);
        }

        if (!_automaticBoundaryApproaches.TryGetValue(
            agent.Id,
            out SurfacePose approachedExit)
            || approachedExit != exitPose)
        {
            if (!_surfaceTraffic.CanOccupy(agent.Id, exitPose, Tick))
            {
                return Result.Success();
            }
            Result approached = MoveOnReservedSurface(agent, exitPose);
            if (approached.IsFailure)
            {
                return approached;
            }

            _automaticBoundaryApproaches[agent.Id] = exitPose;
            SaveAutomaticSurfaceProgress(agent);
            return Result.Success();
        }

        if (!_tunnelTraffic.CanMove(agent.Id, current, destination, Tick))
        {
            return Result.Success();
        }

        if (!_surfaceTraffic.CanOccupy(agent.Id, entryPose, Tick))
        {
            return Result.Success();
        }
        Result crossed = MoveOnReservedSurface(agent, entryPose);
        if (crossed.IsSuccess)
        {
            _tunnelTraffic.RecordMove(agent.Id, current, destination, Tick);
            _automaticBoundaryApproaches.Remove(agent.Id);
            SaveAutomaticSurfaceProgress(agent);
        }

        return crossed;
    }

    private SurfaceMoverKind ResolveSurfaceMoverKind(EntityId agentId)
    {
        if (!_enemyDefinitions.TryGetValue(
            agentId,
            out Dig.Domain.Content.EnemyCombatDefinition? definition))
        {
            return SurfaceMoverKind.Resident;
        }

        return definition.Traversal.HasFlag(
            Dig.Domain.Content.EnemyTraversalCapability.VerticalClimb)
                ? SurfaceMoverKind.CaveMonster
                : SurfaceMoverKind.GroundEnemy;
    }

    private bool IsExposedClimbFace(CellId cell, SurfaceFace face)
    {
        if (face == SurfaceFace.Floor
            || face == SurfaceFace.NegativeZ && cell.Z == 0)
        {
            return false;
        }

        CellId neighbour = face switch
        {
            SurfaceFace.NegativeX => new CellId(cell.X - 1, cell.Y, cell.Z),
            SurfaceFace.PositiveX => new CellId(cell.X + 1, cell.Y, cell.Z),
            SurfaceFace.NegativeZ => new CellId(cell.X, cell.Y, cell.Z - 1),
            SurfaceFace.PositiveZ => new CellId(cell.X, cell.Y, cell.Z + 1),
            _ => cell,
        };
        return !TunnelVolume.IsOpen(neighbour);
    }

    private Result MoveThroughCellTraffic(
        AgentState agent,
        CellId current,
        CellId destination)
    {
        if (!_tunnelTraffic.CanMove(agent.Id, current, destination, Tick))
        {
            return Result.Success();
        }
        if (!_surfaceTraffic.CanOccupy(
            agent.Id,
            SurfacePose.FloorCentre(destination),
            Tick))
        {
            return Result.Success();
        }

        Result moved = _movementHandler.Handle(new MoveAgentCommand(
            agent.Id,
            destination,
            Tick));
        if (moved.IsSuccess)
        {
            _tunnelTraffic.RecordMove(agent.Id, current, destination, Tick);
            RecordCellTrafficPose(agent);
        }

        return moved;
    }

    private void SaveAutomaticSurfaceProgress(AgentState agent)
    {
        _repository.Save(agent);
        _tunnelJournal!.Append(agent.DequeueUncommittedEvents());
    }
}

}
