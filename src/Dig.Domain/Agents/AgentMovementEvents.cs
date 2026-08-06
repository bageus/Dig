using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed class AgentMoved : IDomainEvent
{
    public AgentMoved(
        long tick,
        EntityId agentId,
        CellId previousPosition,
        CellId currentPosition)
    {
        Tick = tick;
        AgentId = agentId;
        PreviousPosition = previousPosition;
        CurrentPosition = currentPosition;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public CellId PreviousPosition { get; }

    public CellId CurrentPosition { get; }

}

public sealed class AgentSurfaceMoved : IDomainEvent
{
    public AgentSurfaceMoved(
        long tick,
        EntityId agentId,
        SurfacePose previousPose,
        SurfacePose currentPose)
    {
        Tick = tick;
        AgentId = agentId;
        PreviousPose = previousPose;
        CurrentPose = currentPose;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public SurfacePose PreviousPose { get; }
    public SurfacePose CurrentPose { get; }
}

}
