using Dig.Domain.Core;
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

}
