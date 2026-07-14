using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Agents;

public interface IBuildingFacilitiesRepository
{
    BuildingFacilitiesState Get();

    void Save(BuildingFacilitiesState facilities);
}

public sealed class SettlementAgentDiagnostic
{
    public SettlementAgentDiagnostic(
        EntityId agentId,
        AgentDecision decision,
        AgentActivityTarget? target,
        bool actionCompleted,
        string? blockedReason)
    {
        AgentId = agentId;
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
        Target = target;
        ActionCompleted = actionCompleted;
        BlockedReason = blockedReason;
    }

    public EntityId AgentId { get; }

    public AgentDecision Decision { get; }

    public AgentActivityTarget? Target { get; }

    public bool ActionCompleted { get; }

    public string? BlockedReason { get; }
}

public sealed class SettlementTickReport
{
    public SettlementTickReport(
        long tick,
        IReadOnlyCollection<SettlementAgentDiagnostic> agents)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Agents = new ReadOnlyCollection<SettlementAgentDiagnostic>(agents
            .OrderBy(value => value.AgentId.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<SettlementAgentDiagnostic> Agents { get; }
}
