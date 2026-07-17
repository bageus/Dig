using System.Collections.Generic;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        internal void RefreshEquipmentPresentation()
        {
            RefreshEquipmentVisuals();
        }

        protected void RefreshEquipmentVisuals()
        {
            if (AgentRenderer == null
                || TerrainSession == null
                || AgentSession == null
                || Hud == null)
            {
                return;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            AgentRenderer.RenderEquipment(TerrainSession.LoadResidentEquipment());
            Hud.SetResidentWorkRates(TerrainSession.LoadResidentWorkRates(agents));
        }
    }
}
