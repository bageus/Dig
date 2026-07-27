using Dig.Domain.Core;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private Result ReconcileCommittedTerrainRuntime(Result current, long tick)
        {
            if (TerrainSession == null || !TerrainSession.HasWorldChanged)
            {
                return current;
            }

            // World is already authoritative. Rebuild every derived topology owner in
            // the same tick so a visually open cell is also traversable and selectable.
            Result navigation = TerrainSession.RefreshCommittedTerrainNavigation();
            SynchronizeExcavatedTunnelNavigation();

            DigTunnelDemoRenderer? renderer = GetComponent<DigTunnelDemoRenderer>();
            if (renderer != null && AgentSession != null)
            {
                renderer.Initialize(AgentSession.TunnelVolume);
                renderer.SetDepthExcavationSources(AgentSession.TunnelDepthExcavations);
            }

            // Support loss must be resolved before pickup/hauling can reserve the item.
            // Do this even when a derived navigation operation produced a recoverable
            // warning; that warning cannot restore terrain already removed from World.
            Result settlement = TerrainSession.SettleWorldItems(tick);
            if (current.IsFailure)
            {
                return current;
            }

            if (navigation.IsFailure)
            {
                return navigation;
            }

            return settlement;
        }
    }
}
