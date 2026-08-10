using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class BuildingBoxUnpackHudContractTests
    {
        [Fact]
        public void World_box_selection_uses_lower_context_unpack_action()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "Assets", "Dig.Unity", "Runtime");
            string placement = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.BuildingBoxes.cs"));
            string selection = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.BuildingBoxSelection.cs"));
            string hud = File.ReadAllText(Path.Combine(
                runtime, "DigGameHudCanvas.BuildingBoxes.cs"));
            string context = File.ReadAllText(Path.Combine(
                runtime, "DigGameHudCanvas.Context.cs"));

            Assert.Contains("_selectedBuildingBox = item", selection);
            Assert.Contains("SetBuildingBoxVisualSelection(visual)", selection);
            Assert.Contains("ActivateBuildingRosterForSelection", selection);
            Assert.Contains("ActivateSelectedBuildingBoxFromHud", selection);
            Assert.Contains("BeginBuildingPlacement", placement);
            Assert.Contains("CancelBuildingPlacement", placement);
            Assert.Contains("BuildingBoxFunctionsPresenter", hud);
            Assert.Contains("SetButtonActive(unpack, action.IsActive)", hud);
            Assert.Contains("action.IsActive ? \"Cancel unpacking\" : \"Unpack\"", hud);
            Assert.Contains("ShowBuildingBoxFunctions(buildingBox)", context);
            Assert.DoesNotContain("private void OnGUI()", hud);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Repository root was not found.");
        }
    }
}
