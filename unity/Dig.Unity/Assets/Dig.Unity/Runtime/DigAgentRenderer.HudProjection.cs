using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    internal IReadOnlyList<AgentViewModel> GetHudModels()
    {
        AgentViewModel[] models = _agents.Values
            .Select(agent => agent.Model)
            .Where(model => model.IsAlive)
            .OrderBy(model => model.Name, StringComparer.Ordinal)
            .ThenBy(model => model.Id, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<AgentViewModel>(models);
    }
}

}
