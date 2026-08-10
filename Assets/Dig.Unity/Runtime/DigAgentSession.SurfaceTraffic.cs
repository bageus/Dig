using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private readonly SurfaceTrafficCoordinator _surfaceTraffic =
        new SurfaceTrafficCoordinator();

    private void BeginTunnelTrafficTick(long tick)
    {
        _tunnelTraffic.BeginTick(tick);
        _surfaceTraffic.BeginTick(
            tick,
            _repository.GetAll()
                .Where(agent => agent.IsAlive)
                .Select(agent => new KeyValuePair<EntityId, SurfacePose>(
                    agent.Id,
                    agent.SurfacePose)));
    }

    private Result MoveOnReservedSurface(AgentState agent, SurfacePose target)
    {
        Result moved = agent.MoveOnSurface(target, _tick);
        if (moved.IsSuccess)
        {
            _surfaceTraffic.RecordPose(agent.Id, target, _tick);
        }
        return moved;
    }

    private Result MoveOnReservedSurface(
        AgentState agent,
        SurfacePose target,
        SurfaceMoverKind mover)
    {
        Result moved = agent.MoveOnSurface(target, mover, _tick);
        if (moved.IsSuccess)
        {
            _surfaceTraffic.RecordPose(agent.Id, target, _tick);
        }
        return moved;
    }

    private void RecordCellTrafficPose(AgentState agent)
    {
        _surfaceTraffic.RecordPose(agent.Id, agent.SurfacePose, _tick);
    }
}

}
