namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private void SynchronizeExcavatedTunnelNavigation()
        {
            if (AgentSession == null || WorldSession == null)
            {
                return;
            }

            AgentSession.SynchronizeFrontNavigation(
                WorldSession.LoadSnapshot(),
                WorldSession.PlannedVerticalTunnelCells);
        }
    }
}
