using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Agents
{

public interface IBuildingFacilitiesRepository
{
    BuildingFacilitiesState Get();

    void Save(BuildingFacilitiesState facilities);
}

public readonly struct SettlementAgentDiagnostic
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
        : this(tick, SortCopy(agents))
    {
    }

    private SettlementTickReport(long tick, SettlementAgentDiagnostic[] agents)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Agents = agents ?? throw new ArgumentNullException(nameof(agents));
    }

    public long Tick { get; }
    public IReadOnlyList<SettlementAgentDiagnostic> Agents { get; }

    internal static SettlementTickReport CreateFromStableOrder(
        long tick,
        SettlementAgentDiagnostic[] agents,
        int count)
    {
        if (agents is null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        if (count < 0 || count > agents.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == agents.Length)
        {
            return new SettlementTickReport(tick, agents);
        }

        SettlementAgentDiagnostic[] result = new SettlementAgentDiagnostic[count];
        Array.Copy(agents, result, count);
        return new SettlementTickReport(tick, result);
    }

    private static SettlementAgentDiagnostic[] SortCopy(
        IReadOnlyCollection<SettlementAgentDiagnostic> agents)
    {
        if (agents is null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        return agents
            .OrderBy(value => value.AgentId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }
}
}
