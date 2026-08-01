using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialPhysicsUnityContractTests
{
    [Fact]
    public void LivingMaterialInteractionProxiesAreTriggersAndPooledSpeciesResetMode()
    {
        string resources = ReadRuntime("DigCreatureRenderer.Resources.cs");

        Assert.Contains("collider.enabled = true", resources);
        Assert.Contains(
            "collider.isTrigger = IsLivingMaterialPhysicsProxy(speciesId)",
            resources);
        Assert.Contains("creature.hamster", resources);
        Assert.Contains("creature.grub", resources);
        Assert.Contains("creature.larva", resources);
    }

    [Fact]
    public void PlayModeCoversRaycastAndMovableItemImmobility()
    {
        string playMode = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "LivingMaterialPhysicsPassThroughPlayModeTests.cs"));

        Assert.Contains("interaction.isTrigger", playMode);
        Assert.Contains("interaction.Raycast", playMode);
        Assert.Contains("AddComponent<Rigidbody>()", playMode);
        Assert.Contains("Physics.SyncTransforms()", playMode);
        Assert.Contains("new WaitForFixedUpdate()", playMode);
        Assert.Contains("Vector3.Distance(body.position", playMode);
        Assert.Contains("body.linearVelocity.sqrMagnitude", playMode);
    }

    [Fact]
    public void WorldItemsKeepTriggerTargetingAndUnfinishedPackagesStayNonInteractive()
    {
        string itemVisual = ReadRuntime("DigWorldItemVisual.cs");
        string presenter = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Dig.Presentation.Abstractions",
            "Inventory",
            "InventoryWorldPresenter.cs"));

        Assert.Contains("_interactionCollider.isTrigger = true", itemVisual);
        Assert.Contains("_interactionCollider!.enabled = interactive", itemVisual);
        Assert.Contains("UnfinishedPackageItemId", presenter);
        Assert.Contains("return WorldItemInteractionKind.None", presenter);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            file));
    }

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
