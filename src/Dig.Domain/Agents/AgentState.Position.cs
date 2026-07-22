using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public const int MaximumDepthIndex = CellId.MaximumDepth;

    private CellId _position = new CellId(0, 0, 0);

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
    }

    public CellId Position => _position;


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
        Version = checked(Version + 1);
        Raise(new AgentMoved(tick, Id, previousPosition, targetPosition));
        return Result.Success();
    }

    public Result RestorePosition(CellId position)
    {
        if (!IsValidPosition(position))
        {
            return Result.Failure(AgentErrors.InvalidPosition);
        }

        _position = position;
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
