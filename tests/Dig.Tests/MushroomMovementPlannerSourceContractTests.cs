using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomMovementPlannerSourceContractTests
{
    [Fact]
    public void Mushroom_travel_uses_normal_navigation_and_supports_only_final_work_cell()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Tests", "PlayMode");
        string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
        string navigation = Read(runtime, "DigTerrainWorkSession.MushroomNavigation.cs");
        string supported = Read(runtime, "DigTerrainWorkSession.SupportedActionPositions.cs");
        int declarations = 0;

        foreach (string path in Directory.GetFiles(runtime, "DigTerrainWorkSession*.cs"))
        {
            declarations += Count(Normalize(File.ReadAllText(path)), "boolTryPlanMushroomMovement(");
        }

        Assert.Equal(1, declarations);
        Assert.DoesNotContain("TryPlanMushroomMovement(", mushrooms);
        Assert.Contains("privateboolTryPlanMushroomMovement(", navigation);
        Assert.Contains("_routePlans[job.Id]=newTerrainWorkRoutePlan", navigation);
        Assert.Contains("GetSameHeightActionCandidates(target)", navigation);
        Assert.Contains(".Where(HasFullStandingSupport)", navigation);
        Assert.Contains("HasFullStandingSupport(definition.WorkPosition)", navigation);
        Assert.DoesNotContain("IsSupportedStationaryActionPath", navigation);
        Assert.DoesNotContain("IsSupportedStationaryActionPath", supported);
        Assert.DoesNotContain("path.Cells.Any", supported);
        Assert.Contains("returncandidates.Distinct().ToArray();", supported);
        Assert.DoesNotContain("target.Y-1", navigation);
        Assert.DoesNotContain("target.Y+1", navigation);

        string direct = File.ReadAllText(Path.Combine(playMode, "MushroomChoppingPlayModeTests.cs"));
        string automatic = File.ReadAllText(Path.Combine(playMode, "CampfireFoodWorkflowPlayModeTests.cs"));
        Assert.Contains("Direct_command_completes_large_mushroom_drops_and_same_cell_regrowth", direct);
        Assert.Contains("Missing_cap_creates_large_mushroom_dependency_then_world_supply", automatic);
    }

    private static string Read(string root, string file) => Normalize(
        File.ReadAllText(Path.Combine(root, file)));

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
}
}
