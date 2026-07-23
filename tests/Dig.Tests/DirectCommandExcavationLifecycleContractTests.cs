using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class DirectCommandExcavationLifecycleContractTests
    {
        [Fact]
        public void Direct_commands_release_pickups_and_dig_work_and_depth_can_be_erased()
        {
            string root = FindRepositoryRoot();
            string runtime = Path.Combine(
                root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
            string direct = File.ReadAllText(Path.Combine(
                runtime, "DigTerrainWorkSession.DirectCommands.cs"));
            string movement = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.TunnelMovement.cs"));
            string cursor = File.ReadAllText(Path.Combine(
                runtime, "DigExcavationCursorRenderer.cs"));
            string cursorDriver = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.ExcavationCursor.cs"));
            string depth = File.ReadAllText(Path.Combine(
                runtime, "DigWorldInteraction.TunnelDepthExcavation.cs"));

            Assert.Contains("WorldItemPickupJobDefinition", direct);
            Assert.Contains("inventory.ReleaseReservations(job.Id, tick)", direct);
            Assert.Contains("ReleaseJobAssignmentCommand", direct);
            Assert.Contains("ClearManualGroupForAgent(residentId)", direct);
            Assert.Contains("PrepareResidentsForDirectCommand", movement);
            Assert.Contains("RefreshDirectCommandPresentation", movement);

            Assert.Contains("_synchronizedWorldVersion", cursor);
            Assert.Contains("!cell.IsSolid", cursor);
            Assert.Contains("SynchronizeExcavationDesignations", cursorDriver);
            Assert.Contains("_session!.LoadView()", cursorDriver);

            Assert.Contains("TryHandleDepthExcavationErase", depth);
            Assert.Contains("job.Model.TargetZ.Value <= 0", depth);
            Assert.Contains("ApplyExcavationEraseBatch(new[] { target })", depth);
            Assert.Contains("RemoveDepthDesignationTint(target)", depth);
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
