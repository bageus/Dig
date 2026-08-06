using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public const int MaximumDepthIndex = CellId.MaximumDepth;

    private CellId _position = new CellId(0, 0, 0);
    private SurfacePose _surfacePose = SurfacePose.FloorCentre(new CellId(0, 0, 0));

    public AgentState(
        EntityId id,
        string name,
        AgentNeedsSnapshot initialNeeds,
        DailySchedule schedule,
        IEnumerable<AgentSkillValue>? skills,
        IEnumerable<AgentTraitId>? traits,
        CellId initialPosition)
        : this(id, name, initialNeeds, schedule, skills, traits)
    {
        RequireValidPosition(initialPosition);
        _position = initialPosition;
        _surfacePose = SurfacePose.FloorCentre(initialPosition);
    }

    public CellId Position => _position;

    public SurfacePose SurfacePose => _surfacePose;


    public int Depth => _position.Z;

    public Result MoveTo(CellId targetPosition, long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (!IsValidPosition(targetPosition))
        {
            return Result.Failure(AgentErrors.InvalidPosition);
        }

        if (_position == targetPosition)
        {
            return Result.Success();
        }

        CellId previousPosition = _position;
        _position = targetPosition;
        _surfacePose = SurfacePose.FloorCentre(targetPosition);
        Version = checked(Version + 1);
        Raise(new AgentMoved(tick, Id, previousPosition, targetPosition));
        return Result.Success();
    }

    public Result MoveOnSurface(SurfacePose targetPose, long tick)
    {
        return MoveOnSurface(targetPose, SurfaceMoverKind.Resident, tick);
    }

    public Result MoveOnSurface(
        SurfacePose targetPose,
        SurfaceMoverKind moverKind,
        long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (!IsValidPosition(targetPose.Cell)
            || !SurfaceTraversalPolicy.CanUse(moverKind, targetPose))
        {
            return Result.Failure(AgentErrors.InvalidPosition);
        }

        if (_surfacePose == targetPose)
        {
            return Result.Success();
        }

        SurfacePose previousPose = _surfacePose;
        CellId previousPosition = _position;
        _surfacePose = targetPose;
        _position = targetPose.Cell;
        Version = checked(Version + 1);
        Raise(new AgentSurfaceMoved(tick, Id, previousPose, targetPose));
        if (previousPosition != _position)
        {
            Raise(new AgentMoved(tick, Id, previousPosition, _position));
        }

        return Result.Success();
    }

    public Result RestorePosition(CellId position)
    {
        if (!IsValidPosition(position))
        {
            return Result.Failure(AgentErrors.InvalidPosition);
        }

        _position = position;
        _surfacePose = SurfacePose.FloorCentre(position);
        return Result.Success();
    }

    public Result RestoreSurfacePose(SurfacePose pose)
    {
        return RestoreSurfacePose(pose, SurfaceMoverKind.Resident);
    }

    public Result RestoreSurfacePose(SurfacePose pose, SurfaceMoverKind moverKind)
    {
        if (!IsValidPosition(pose.Cell)
            || !SurfaceTraversalPolicy.CanUse(moverKind, pose))
        {
            return Result.Failure(AgentErrors.InvalidPosition);
        }

        _position = pose.Cell;
        _surfacePose = pose;
        return Result.Success();
    }

    private static bool IsValidPosition(CellId position)
    {
        return position.X >= 0
            && position.Y >= 0
            && position.Z >= CellId.MinimumDepth
            && position.Z <= CellId.MaximumDepth;
    }

    private static void RequireValidPosition(CellId position)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }
    }
}

}
