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

            Result navigation = TerrainSession.RefreshCommittedTerrainNavigation();
            SynchronizeExcavatedTunnelNavigation();

            Result tunnelInfrastructure = AgentSession == null
                ? Result.Success()
                : TerrainSession.SynchronizeTunnelInfrastructureRuntime(
                    tick,
                    AgentSession.LoadView(),
                    AgentSession.TunnelVolume.Cells);
            Result roomInfrastructure = AgentSession == null
                ? Result.Success()
                : TerrainSession.SynchronizeRoomInfrastructureRuntime(
                    tick,
                    AgentSession.LoadView(),
                    AgentSession.TunnelVolume.Cells);

            DigTunnelDemoRenderer? renderer = GetComponent<DigTunnelDemoRenderer>();
            if (renderer != null && AgentSession != null)
            {
                renderer.Initialize(AgentSession.TunnelVolume);
                renderer.SetDepthExcavationSources(AgentSession.TunnelDepthExcavations);
            }

            Result settlement = TerrainSession.SettleWorldItems(tick);
            if (current.IsFailure)
            {
                return current;
            }

            if (navigation.IsFailure)
            {
                return navigation;
            }

            if (tunnelInfrastructure.IsFailure)
            {
                return tunnelInfrastructure;
            }

            if (roomInfrastructure.IsFailure)
            {
                return roomInfrastructure;
            }

            return settlement;
        }
    }
}
