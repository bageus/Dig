using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class BasketSurfaceRuntimeContractTests
{
    [Fact]
    public void Demo_and_unity_projection_keep_baskets_on_surface_and_use_basket_visuals()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Tests", "PlayMode");
        string demo = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");
        string policy = Read(runtime, "DigBasketVisualPolicy.cs");
        string world = Read(runtime, "DigWorldItemVisual.cs");
        string attachments = Read(runtime, "DigResidentInventoryAttachmentVisual.cs");
        string agentCatalog = Read(runtime, "DigAgentRenderer.ItemVisualCatalog.cs");
        string scenario = Read(playMode, "BasketInventoryLifecyclePlayModeTests.cs");

        Assert.Contains("residentStartCell.X + 1", demo);
        Assert.Contains("residentStartCell.X + 2", demo);
        Assert.Contains("DemoBasketItemId", demo);
        Assert.Contains("DemoLargeBasketItemId", demo);
        Assert.Contains("ItemLocation.InWorld(basketCell)", demo);
        Assert.Contains("ItemLocation.InWorld(largeBasketCell)", demo);
        Assert.DoesNotContain("DemoBasketItemId,\n            residentId", demo);
        Assert.DoesNotContain("DemoLargeBasketItemId,\n            residentId", demo);

        Assert.Contains("ResidentInventoryExpansionContent.BasketItemId", policy);
        Assert.Contains("ResidentInventoryExpansionContent.LargeBasketItemId", policy);
        Assert.Contains("CreateBasketParts", policy);
        Assert.Contains("Basket Handle Top", policy);
        Assert.Contains("DigItemCarrySocketPolicy.Cargo", policy);
        Assert.Contains("DigBasketVisualPolicy.CreateInstance", world);
        Assert.Contains("DigBasketVisualPolicy.CreateInstance", attachments);
        Assert.Contains("DigBasketVisualPolicy.Resolve", agentCatalog);
        Assert.Contains(
            "Loaded_cargo_uses_back_basket_and_empty_projection_hides_it",
            scenario);
    }

    private static string Read(string root, string file)
    {
        string path = Path.Combine(root, file);
        Assert.True(File.Exists(path), path);
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
