namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase
    {
        private void RestoreSelection(
            string? selectedAgentId,
            string? selectedJobId,
            string? selectedBuildingId)
        {
            if (selectedAgentId != null)
            {
                DigAgentVisual? selectedAgent = AgentRenderer!.SelectById(selectedAgentId);
                Hud!.SetAgentSelection(selectedAgent?.Model);
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
