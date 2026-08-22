using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private Result AdvanceHorizontalSurfaceCorridor(
        AgentState agent,
        ManualTunnelMovementOrder order,
        SurfacePose exitPose,
        SurfacePose entryPose)
    {
        if (order.CorridorPhase == SurfaceCorridorPhase.ApproachBoundary)
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

            order.ConfirmBoundaryApproach();
            SaveManualMovementProgress(agent);
            return Result.Success();
        }

        CellId current = agent.Position;
        CellId next = order.NextCell;
        if (!_tunnelTraffic.CanMove(agent.Id, current, next, Tick))
        {
            return Result.Success();
        }

        if (!_surfaceTraffic.CanOccupy(agent.Id, entryPose, Tick))
        {
            return Result.Success();
        }
        Result crossed = MoveOnReservedSurface(agent, entryPose);
        if (crossed.IsFailure)
        {
            return crossed;
        }

        _tunnelTraffic.RecordMove(agent.Id, current, next, Tick);
        order.ConfirmBoundaryCrossing(next);
        if (order.IsComplete)
        {
            return CompleteManualMovement(agent, order);
        }

        SaveManualMovementProgress(agent);
        return Result.Success();
    }

    private void SaveManualMovementProgress(AgentState agent)
    {
        _repository.Save(agent);
        _tunnelJournal!.Append(agent.DequeueUncommittedEvents());
    }
}

}
