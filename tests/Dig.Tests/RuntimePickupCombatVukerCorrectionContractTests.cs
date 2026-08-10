using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimePickupCombatVukerCorrectionContractTests
{
    [Fact]
    public void Direct_command_preparation_cancels_manual_movement()
    {
        string direct = ReadRuntime("DigTerrainWorkSession.DirectCommands.cs");
        string composition = ReadRuntime("DigAgentSimulationDriverBase.cs");

        Assert.Contains(
            "BindDirectCommandManualMovementCancellation",
            direct,
            StringComparison.Ordinal);
        Assert.Contains(
            "_cancelResidentManualMovement?.Invoke(residentId)",
            direct,
            StringComparison.Ordinal);
        Assert.Contains(
            "AgentSession.CancelManualTunnelMovement",
            composition,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Combat_effects_use_enemy_depth_and_world_coordinates()
    {
        string runtime = ReadRuntime("DigPresentationEffectRuntime.cs");
        string instance = ReadRuntime("DigPooledVfxInstance.cs");

        Assert.Contains("LoadEnemyCreatures", runtime, StringComparison.Ordinal);
        Assert.Contains("enemy.CellZ", runtime, StringComparison.Ordinal);
        Assert.Contains("transform.position = new Vector3", instance,
            StringComparison.Ordinal);
        Assert.DoesNotContain("transform.localPosition = new Vector3", instance,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Vuker_visual_and_ecology_fit_tunnel_traversal()
    {
        string renderer = ReadRuntime("DigCreatureRenderer.Resources.cs");
        string resolver = Read(
            "src", "Dig.Application", "Ecology", "VukerEcologyPlanning.cs");

        Assert.Contains("VukerTunnelFitScale = 0.68f", renderer,
            StringComparison.Ordinal);
        Assert.Contains("appearance.Family == CreatureVisualFamily.Vuker", renderer,
            StringComparison.Ordinal);
        Assert.Contains("new HashSet<CellId>(volume.Cells)", resolver,
            StringComparison.Ordinal);
        Assert.Contains("Where(supported.Contains)", resolver,
            StringComparison.Ordinal);
    }

    private static string ReadRuntime(string fileName)
    {
        return Read(
            "Assets", "Dig.Unity", "Runtime", fileName);
    }

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }

        return File.ReadAllText(path);
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
