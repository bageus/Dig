using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private bool TryAdvanceManualSurfaceStep(
        AgentState agent,
        ManualTunnelMovementOrder order,
        CellId next,
        out Result result)
    {
        CellId current = agent.Position;
        if (SurfaceCorridorSteering.TryBuildBoundaryPoses(
            current,
            next,
            out SurfacePose exitPose,
            out SurfacePose entryPose))
        {
            if (agent.SurfacePose.IsVertical
                && VerticalSurfaceSteering.TryDetachToFloor(
                    agent.SurfacePose,
                    out SurfacePose floorPose))
            {
                if (!_surfaceTraffic.CanOccupy(agent.Id, floorPose, _tick))
                {
                    result = Result.Success();
                    return true;
                }
                result = MoveOnReservedSurface(agent, floorPose);
                if (result.IsSuccess)
                {
                    SaveManualMovementProgress(agent);
                }
                return true;
            }

            result = AdvanceHorizontalSurfaceCorridor(
                agent,
                order,
                exitPose,
                entryPose);
            return true;
        }

        if (!VerticalSurfaceSteering.TryBuildNextPose(
            agent.SurfacePose,
            next,
            face => IsExposedClimbFace(agent.Position, face),
            out SurfacePose verticalPose,
            out bool crossesBoundary))
        {
            result = Result.Success();
            return false;
        }

        if (crossesBoundary
            && !_tunnelTraffic.CanMove(agent.Id, current, next, _tick))
        {
            result = Result.Success();
            return true;
        }

        if (!_surfaceTraffic.CanOccupy(agent.Id, verticalPose, _tick))
        {
            result = Result.Success();
            return true;
        }
        result = MoveOnReservedSurface(agent, verticalPose);
        if (result.IsFailure)
        {
            CancelManualMovementWithWarning(
                agent.Id,
                result.Error!,
                ResidentMovementInterruptionReason.MovementRejected);
            result = Result.Success();
            return true;
        }

        if (crossesBoundary)
        {
            _tunnelTraffic.RecordMove(agent.Id, current, next, _tick);
            order.ConfirmStep(next);
        }

        SaveManualMovementProgress(agent);
        result = Result.Success();
        return true;
    }
}

}
