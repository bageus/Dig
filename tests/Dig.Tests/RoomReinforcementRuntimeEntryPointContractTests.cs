using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
    public sealed class RoomReinforcementRuntimeEntryPointContractTests
    {
        [Fact]
        public void Approved_correction_preserves_ordinary_placement_and_requires_B_chord_for_reinforcement()
        {
            string specification = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "docs",
                "design",
                "room-purpose-and-manual-reinforcement-runtime-entrypoints-2026-08-06.md"));

            Assert.Contains("ordinary item-placement command", specification);
            Assert.Contains("explicit `B + LMB` chord", specification);
            Assert.Contains("`material.mushroom_leg`", specification);
            Assert.Contains("`material.stone`", specification);
            Assert.Contains("wooden-support placement", specification);
            Assert.Contains("stone floor reinforcement ghost", specification);
            Assert.Contains("junction support/trim ghost", specification);
            Assert.Contains("invalid target, unreachable target or failed preflight preserves", specification);
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
