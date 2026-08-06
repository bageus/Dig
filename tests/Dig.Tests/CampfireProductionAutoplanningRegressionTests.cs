using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class CampfireProductionAutoplanningRegressionTests
    {
        [Fact]
        public void Claimed_or_in_progress_work_keeps_resident_work_intent_available()
        {
            string source = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "unity",
                "Dig.Unity",
                "Assets",
                "Dig.Unity",
                "Runtime",
                "DigTerrainWorkSession.ResidentNeeds.cs"));

            int ownershipCheck = source.IndexOf(
                "job.AssignedAgentId == agent.Id",
                StringComparison.Ordinal);
            int scheduleGate = source.IndexOf(
                "agent.ScheduledActivity == ScheduleActivity.Work",
                StringComparison.Ordinal);

            Assert.True(ownershipCheck >= 0);
            Assert.True(scheduleGate > ownershipCheck);
            Assert.Contains("job.Status == JobStatus.Claimed", source);
            Assert.Contains("job.Status == JobStatus.InProgress", source);
            Assert.Contains("if (ownsCurrentWork)", source);
            Assert.Contains("return true;", source);
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
