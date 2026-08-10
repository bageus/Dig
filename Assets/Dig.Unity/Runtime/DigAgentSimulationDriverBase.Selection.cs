using System.Collections.Generic;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private void RestoreSelection(
            IReadOnlyList<string> selectedAgentIds,
            string? primarySelectedAgentId,
            string? selectedJobId,
            string? selectedBuildingId)
        {
            if (selectedAgentIds.Count > 0)
            {
                AgentRenderer!.RestoreSelection(
                    selectedAgentIds,
                    primarySelectedAgentId);
                Hud!.SetAgentSelection(
                    AgentRenderer.SelectedModel,
                    AgentRenderer.SelectedCount);
                return;
            }

            if (selectedJobId != null)
            {
                DigJobVisual? selectedJob = JobRenderer!.SelectById(selectedJobId);
                Hud!.SetJobSelection(selectedJob?.Model);
                return;
            }

            if (selectedBuildingId != null)
            {
                DigBuildingVisual? selectedBuilding =
                    BuildingRenderer!.SelectById(selectedBuildingId);
                Hud!.SetBuildingSelection(selectedBuilding?.Model);
            }
        }
    }
}
