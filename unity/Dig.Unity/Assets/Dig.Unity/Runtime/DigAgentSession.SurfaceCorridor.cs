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
            if (!_surfaceTraffic.CanOccupy(agent.Id, exitPose, _tick))
            {
                return Result.Success();
            }
            SurfacePose nextPose = SurfacePoseSteering.MoveTowards(
                agent.SurfacePose,
                exitPose);
            Result approached = MoveOnReservedSurface(agent, nextPose);
            if (approached.IsFailure)
            {
                return approached;
            }

            if (nextPose == exitPose)
            {
                order.ConfirmBoundaryApproach();
            }
            SaveManualMovementProgress(agent);
            return Result.Success();
        }

        CellId current = agent.Position;
        CellId next = order.NextCell;
        if (!_tunnelTraffic.CanMove(agent.Id, current, next, _tick))
        {
            return Result.Success();
        }

        if (!_surfaceTraffic.CanOccupy(agent.Id, entryPose, _tick))
        {
            return Result.Success();
        }
        Result crossed = MoveOnReservedSurface(agent, entryPose);
        if (crossed.IsFailure)
        {
            return crossed;
        }

        _tunnelTraffic.RecordMove(agent.Id, current, next, _tick);
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
