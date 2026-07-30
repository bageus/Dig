using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomMovementPlannerSourceContractTests
{
    [Fact]
    public void Partial_class_has_one_mushroom_movement_planner_owner()
    {
        string runtime = RuntimeRoot();
        string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
        string navigation = Read(runtime, "DigTerrainWorkSession.MushroomNavigation.cs");
        int declarations = 0;

        foreach (string path in Directory.GetFiles(runtime, "DigTerrainWorkSession*.cs"))
        {
            declarations += Count(
                Normalize(File.ReadAllText(path)),
                "boolTryPlanMushroomMovement(");
        }

        Assert.Equal(1, declarations);
        Assert.DoesNotContain("TryPlanMushroomMovement(", mushrooms);
        Assert.Contains("privateboolTryPlanMushroomMovement(", navigation);
        Assert.Contains("_routePlans[job.Id]=newTerrainWorkRoutePlan", navigation);
        Assert.Contains("GetSameHeightActionCandidates(target)", navigation);
        Assert.Contains(".Where(HasFullStandingSupport)", navigation);
        Assert.Contains("IsSupportedStationaryActionPath(navigation,path.Path)", navigation);
        Assert.DoesNotContain("target.Y-1", navigation);
        Assert.DoesNotContain("target.Y+1", navigation);
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

    private static string RuntimeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "unity",
        "Dig.Unity",
        "Assets",
        "Dig.Unity",
        "Runtime");

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
