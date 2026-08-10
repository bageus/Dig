namespace Dig.Unity
{
    public sealed partial class DigAgentRenderer
    {
        internal void PlayInventoryFullReaction(string agentId)
        {
            if (!string.IsNullOrWhiteSpace(agentId)
                && _agents.TryGetValue(agentId, out DigAgentVisual? agent))
            {
                agent.PlayInventoryFullReaction();
            }
        }
    }
}
