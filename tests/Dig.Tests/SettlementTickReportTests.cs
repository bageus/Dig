using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests;

public sealed class SettlementTickReportTests
{
    [Fact]
    public void Public_constructor_copies_and_sorts_input()
    {
        AgentDecision decision = AgentTestFactory.CreateForcedDecision(
            AgentIntentKind.Work,
            1);
        EntityId first = AgentTestFactory.DefaultAgentId;
        EntityId second = EntityId.Parse("22222222222222222222222222222222");
        List<SettlementAgentDiagnostic> values = new List<SettlementAgentDiagnostic>
        {
            new SettlementAgentDiagnostic(second, decision, null, false, null),
            new SettlementAgentDiagnostic(first, decision, null, false, null),
        };

        SettlementTickReport report = new SettlementTickReport(1, values);
        values.Clear();

        Assert.Equal(2, report.Agents.Count);
        Assert.Equal(first, report.Agents[0].AgentId);
        Assert.Equal(second, report.Agents[1].AgentId);
    }
}
