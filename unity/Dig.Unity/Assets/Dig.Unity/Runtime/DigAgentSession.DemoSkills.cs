using System.Collections.Generic;
using Dig.Domain.Agents;

namespace Dig.Unity
{
internal sealed partial class DigAgentSession
{
    private static readonly IReadOnlyCollection<AgentSkillValue> MasterExcavatorSkills =
        new[]
        {
            new AgentSkillValue(
                AgentSkillCatalog.Stonework,
                AgentSkillCatalog.StoneworkThresholdUnits(3)),
        };

    private static IReadOnlyCollection<AgentSkillValue>? ResolveDemoSkills(int index)
    {
        return index == 0 ? MasterExcavatorSkills : null;
    }
}
}
