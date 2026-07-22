using System;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        internal void SynchronizeTunnelInteractionTargets(
            DigTunnelDemoRenderer tunnelRenderer)
        {
            if (tunnelRenderer == null)
            {
                throw new ArgumentNullException(nameof(tunnelRenderer));
            }

            SynchronizeExcavatedTunnelNavigation();
            if (AgentSession != null)
            {
                tunnelRenderer.Initialize(AgentSession.TunnelVolume);
            }
        }

        private void SynchronizeExcavatedTunnelNavigation()
        {
            if (AgentSession == null || WorldSession == null)
            {
                return;
            }

            AgentSession.SynchronizeNavigation(
                WorldSession.LoadSnapshot(),
                WorldSession.PlannedVerticalTunnelCells);
        }
    }
}
