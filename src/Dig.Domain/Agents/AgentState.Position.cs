using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public const int MaximumDepthIndex = 3;

    private SpatialCellId _spatialPosition = new SpatialCellId(0, 0, 0);

    public AgentState(
        EntityId id,
        string name,
        AgentNeedsSnapshot initialNeeds,
        DailySchedule schedule,
        IEnumerable<AgentSkillValue>? skills,
        IEnumerable<AgentTraitId>? traits,
        CellId initialPosition)
        : this(
            id,
            name,
            initialNeeds,
            schedule,
            skills,
            traits,
            new SpatialCellId(initialPosition.X, initialPosition.Y, 0))
    {
    }

    public AgentState(
        EntityId id,
        string name,
        AgentNeedsSnapshot initialNeeds,
        DailySchedule schedule,
        IEnumerable<AgentSkillValue>? skills,
        IEnumerable<AgentTraitId>? traits,
        SpatialCellId initialPosition)
        : this(id, name, initialNeeds, schedule, skills, traits)
    {
        RequireValidPosition(initialPosition);
        _spatialPosition = initialPosition;
    }

    public CellId Position => _spatialPosition.Projection;

    public SpatialCellId SpatialPosition => _spatialPosition;

    public int Depth => _spatialPosition.Z;

    public Result MoveTo(CellId targetPosition, long tick)
    {
        return MoveTo(
            new SpatialCellId(targetPosition.X, targetPosition.Y, _spatialPosition.Z),
            tick);
    }

    public Result MoveTo(SpatialCellId targetPosition, long tick)
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

        if (_spatialPosition == targetPosition)
        {
            return Result.Success();
        }

        SpatialCellId previousPosition = _spatialPosition;
        _spatialPosition = targetPosition;
        Version = checked(Version + 1);
        Raise(new AgentMoved(tick, Id, previousPosition, targetPosition));
        return Result.Success();
    }

    private static bool IsValidPosition(SpatialCellId position)
    {
        return position.X >= 0
            && position.Y >= 0
            && position.Z >= 0
            && position.Z <= MaximumDepthIndex;
    }

    private static void RequireValidPosition(SpatialCellId position)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }
    }
}

}
