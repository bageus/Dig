using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed class AgentSkillGrantApplied : IDomainEvent
{
    public AgentSkillGrantApplied(
        long tick,
        EntityId agentId,
        SkillRedistributionReport report)
    {
        Tick = tick;
        AgentId = agentId;
        Report = report;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public SkillRedistributionReport Report { get; }
}

public sealed class SkillProgressionResultConfirmed : IDomainEvent
{
    public SkillProgressionResultConfirmed(SkillGrantBundle bundle)
    {
        Bundle = bundle ?? throw new System.ArgumentNullException(nameof(bundle));
    }

    public long Tick => Bundle.Tick;
    public EntityId AgentId => Bundle.AgentId;
    public SkillGrantBundle Bundle { get; }
}

public sealed class AgentSkillCapacityChanged : IDomainEvent
{
    public AgentSkillCapacityChanged(
        long tick,
        EntityId agentId,
        int previousCapacityUnits,
        int capacityUnits)
    {
        Tick = tick;
        AgentId = agentId;
        PreviousCapacityUnits = previousCapacityUnits;
        CapacityUnits = capacityUnits;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public int PreviousCapacityUnits { get; }
    public int CapacityUnits { get; }
}

}
