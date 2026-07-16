namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        protected void RefreshEquipmentVisuals()
        {
            if (AgentRenderer == null || TerrainSession == null)
            {
                return;
            }

            AgentRenderer.RenderEquipment(TerrainSession.LoadResidentEquipment());
        }
    }
}