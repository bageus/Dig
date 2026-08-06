using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Application.Agents
{

public sealed class SurfaceTrafficCoordinator
{
    private long _tick = -1;

    public void BeginTick(
        long tick,
        IEnumerable<KeyValuePair<EntityId, SurfacePose>> occupants)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
        if (occupants == null)
        {
            throw new ArgumentNullException(nameof(occupants));
        }
        if (_tick == tick)
        {
            return;
        }

        _tick = tick;
        // Occupants are intentionally not retained as an authoritative barrier.
        // Shared poses and visual overlap are allowed by the movement specification.
    }

    public bool CanOccupy(EntityId agentId, SurfacePose target, long tick)
    {
        ValidateAgent(agentId);
        ValidateTick(tick);
        _ = target;
        return true;
    }

    public void RecordPose(EntityId agentId, SurfacePose pose, long tick)
    {
        ValidateAgent(agentId);
        ValidateTick(tick);
        _ = pose;
    }

    private void ValidateTick(long tick)
    {
        if (_tick != tick)
        {
            throw new InvalidOperationException(
                "Surface traffic must be initialized for the current tick.");
        }
    }

    private static void ValidateAgent(EntityId agentId)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id is required.", nameof(agentId));
        }
    }
}

}
