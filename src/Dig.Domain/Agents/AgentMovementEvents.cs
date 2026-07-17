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
        : this(
            tick,
            agentId,
            new SpatialCellId(previousPosition.X, previousPosition.Y, 0),
            new SpatialCellId(currentPosition.X, currentPosition.Y, 0))
    {
    }

    public AgentMoved(
        long tick,
        EntityId agentId,
        SpatialCellId previousPosition,
        SpatialCellId currentPosition)
    {
        Tick = tick;
        AgentId = agentId;
        PreviousSpatialPosition = previousPosition;
        CurrentSpatialPosition = currentPosition;
    }

    public long Tick { get; }

    public EntityId AgentId { get; }

    public CellId PreviousPosition => PreviousSpatialPosition.Projection;

    public CellId CurrentPosition => CurrentSpatialPosition.Projection;

    public SpatialCellId PreviousSpatialPosition { get; }

    public SpatialCellId CurrentSpatialPosition { get; }
}

}
