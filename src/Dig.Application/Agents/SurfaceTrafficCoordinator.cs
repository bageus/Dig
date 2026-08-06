using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Application.Agents
{

public sealed class SurfaceTrafficCoordinator
{
    private readonly Dictionary<EntityId, SurfacePose> _occupied =
        new Dictionary<EntityId, SurfacePose>();
    private readonly Dictionary<EntityId, SurfacePose> _deferredTargets =
        new Dictionary<EntityId, SurfacePose>();
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
        _occupied.Clear();
        foreach (KeyValuePair<EntityId, SurfacePose> occupant in occupants)
        {
            if (!occupant.Key.IsEmpty)
            {
                _occupied[occupant.Key] = occupant.Value;
            }
        }
    }

    public bool CanOccupy(EntityId agentId, SurfacePose target, long tick)
    {
        ValidateAgent(agentId);
        ValidateTick(tick);
        if (target.IsVertical)
        {
            _deferredTargets.Remove(agentId);
            return true;
        }

        foreach (KeyValuePair<EntityId, SurfacePose> occupant in _occupied)
        {
            if (occupant.Key != agentId
                && !occupant.Value.IsVertical
                && !SurfaceSpatialMath.HasClearance(
                    target,
                    occupant.Value,
                    SurfaceSpatialMath.DefaultClearanceUnits))
            {
                if (_deferredTargets.TryGetValue(agentId, out SurfacePose deferred)
                    && deferred == target)
                {
                    _deferredTargets.Remove(agentId);
                    return true;
                }

                _deferredTargets[agentId] = target;
                return false;
            }
        }
        _deferredTargets.Remove(agentId);
        return true;
    }

    public void RecordPose(EntityId agentId, SurfacePose pose, long tick)
    {
        ValidateAgent(agentId);
        ValidateTick(tick);
        _occupied[agentId] = pose;
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
