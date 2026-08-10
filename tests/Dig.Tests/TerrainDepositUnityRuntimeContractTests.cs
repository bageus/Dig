using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepositUnityRuntimeContractTests
{
    [Fact]
    public void Unity_uses_world_owned_deposits_and_checks_in_lifecycle_playmode()
    {
        string root = FindRepositoryRoot();
        string session = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigWorldSession.Deposits.cs"));
        string completion = File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainWorkSession.cs"));
        string playModePath = Path.Combine(
            root,
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "TerrainDepositLifecyclePlayModeTests.cs");
        string startupRegressionPath = Path.Combine(
            root,
            "Assets/Dig.Unity/Tests/PlayMode/"
                + "DemoStartupRegressionPlayModeTests.cs");
        string terrainDemoPath = Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/"
                + "DigWorldSession.TerrainDemo.cs");

        Assert.DoesNotContain("_terrainDeposits", session);
        Assert.Contains("worldState.TerrainDeposits.Snapshot()", session);
        Assert.Contains("using Dig.Domain.Core;", session);
        Assert.Contains("SkillGrantProfileId skillGrantProfileId", session);
        Assert.Contains("GetProfile(skillGrantProfileId)", session);
        Assert.DoesNotContain("string skillGrantProfileId", session);
        Assert.DoesNotContain("demo.rock", session);
        Assert.Contains("DefaultTerrainMaterials.StoneRock", session);
        Assert.Contains("DefaultTerrainMaterials.MetalBearingRock", session);
        Assert.Contains("DefaultTerrainMaterials.CrystallineRock", session);
        Assert.Contains("DefaultTerrainMaterials.LavaRock", session);
        Assert.Contains(
            "candidate.Material.IsSolid && candidate.Material.IsMineable",
            session);
        Assert.DoesNotContain("DepleteTerrainDeposit", completion);
        Assert.DoesNotContain("RevealTerrainDepositsAdjacentTo", completion);
        Assert.True(File.Exists(playModePath));
        Assert.True(File.Exists(startupRegressionPath));
        Assert.True(File.Exists(terrainDemoPath));
        string terrainDemo = File.ReadAllText(terrainDemoPath);
        Assert.Contains("DemoTerrainBandMaterials", terrainDemo);
        Assert.Contains("DefaultTerrainMaterials.Unmineable", terrainDemo);
        Assert.Contains(
            "Hidden_xyz_deposit_reveals_then_depletes_through_world_owner",
            File.ReadAllText(playModePath));
        Assert.Contains(
            "Demo_creation_excludes_unmineable_patch_from_deposit_hosts",
            File.ReadAllText(startupRegressionPath));
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
