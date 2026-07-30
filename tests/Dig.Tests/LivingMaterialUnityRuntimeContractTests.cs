using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialUnityRuntimeContractTests
{
    [Fact]
    public void RuntimeAdvancesAndRendersLivingMaterialsAfterInventoryTransitions()
    {
        string runtime = RuntimeRoot();
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string session = Read(runtime, "DigTerrainWorkSession.LivingMaterials.cs");
        string drop = Read(runtime, "DigWorldInteraction.ResidentInventoryCommands.cs");

        Assert.Contains("InitializeLivingMaterials(agentSession.Tick)", bootstrap);
        Assert.Contains("LoadLivingMaterialCreatures()", bootstrap);
        Assert.Contains("AdvanceLivingMaterials(", loop);
        Assert.Contains("LoadLivingMaterialCampfireTethers()", loop);
        Assert.Contains("AdvanceLivingMaterialEcologyCommandHandler", session);
        Assert.Contains("EcologyStepsPerSimulationTick", ReadSource(
            "Dig.Domain", "Ecology", "LivingMaterialValues.cs"));
        Assert.Contains("SynchronizeLivingMaterials(", drop);
        Assert.Contains("movementDuration: 0.1f", drop);
    }

    [Fact]
    public void OrdinaryPickupProxyIsInvisibleButAuthoritativeItemRemainsInteractive()
    {
        string itemVisual = Read(RuntimeRoot(), "DigWorldItemVisual.cs");

        Assert.Contains("IsLivingMaterial(Model.ItemId)", itemVisual);
        Assert.Contains("livingMaterial", itemVisual);
        Assert.Contains("_interactionCollider!.enabled = interactive", itemVisual);
        Assert.DoesNotContain("RemoveLivingMaterial", itemVisual);
    }

    [Fact]
    public void CampfireUsesIdentityTethersInsteadOfAggregateHamsterPile()
    {
        string renderer = Read(RuntimeRoot(), "DigBuildingInternalStockRenderer.cs");
        string tethers = Read(RuntimeRoot(),
            "DigBuildingInternalStockRenderer.LivingMaterials.cs");
        string visual = Read(RuntimeRoot(), "DigLivingMaterialTetherVisual.cs");

        Assert.Contains("IndexOf(\"hamster\"", renderer);
        Assert.Contains("LivingMaterialCampfireTetherViewModel", tethers);
        Assert.Contains("model.CreatureId", tethers);
        Assert.Contains("Tether post", visual);
        Assert.Contains("Tether rope", visual);
        Assert.Contains("SlotIndex", visual);
    }

    [Fact]
    public void CreatureProjectionUsesApprovedScaleAndActivityPoses()
    {
        string resources = Read(RuntimeRoot(), "DigCreatureRenderer.Resources.cs");
        string rig = Read(RuntimeRoot(), "DigCreatureRig.cs");
        string projector = ReadSource(
            "Dig.Presentation.Abstractions",
            "Creatures",
            "LivingMaterialCreatureVisualProjector.cs");

        Assert.Contains("? 0.25f", resources);
        Assert.Contains("? 0.20f", resources);
        Assert.Contains("hamster.sleeping", rig);
        Assert.Contains("hamster.release_dormant", rig);
        Assert.Contains("hamster.searching", rig);
        Assert.Contains("grub.crawling", rig);
        Assert.Contains("ActivityVariantId", ReadSource(
            "Dig.Presentation.Abstractions", "Creatures", "CreatureVisualSnapshot.cs"));
        Assert.Contains("grub.crawling", projector);
    }

    private static string Read(string root, string file) =>
        File.ReadAllText(Path.Combine(root, file));

    private static string ReadSource(string project, params string[] path)
    {
        string[] parts = new string[path.Length + 3];
        parts[0] = FindRepositoryRoot();
        parts[1] = "src";
        parts[2] = project;
        Array.Copy(path, 0, parts, 3, path.Length);
        return File.ReadAllText(Path.Combine(parts));
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
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
