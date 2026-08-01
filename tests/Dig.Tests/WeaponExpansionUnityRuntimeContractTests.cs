using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class WeaponExpansionUnityRuntimeContractTests
{
    [Fact]
    public void Demo_visuals_and_play_mode_cover_weapon_expansions_and_club_slot()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets",
            "Dig.Unity", "Tests", "PlayMode");
        string demo = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");
        string policy = Read(runtime, "DigBasketVisualPolicy.Equipment.cs");
        string world = Read(runtime, "DigWorldItemVisual.cs");
        string attachments = Read(runtime, "DigResidentInventoryAttachmentVisual.cs");
        string agentCatalog = Read(runtime, "DigAgentRenderer.ItemVisualCatalog.cs");
        string scenario = Read(playMode, "WeaponExpansionLifecyclePlayModeTests.cs");

        Assert.Contains("CombatEquipmentContent.CreateItems()", demo);
        Assert.Contains("DemoScabbardItemId", demo);
        Assert.Contains("DemoHarnessItemId", demo);
        Assert.Contains("DemoClubItemId", demo);
        Assert.Contains("ItemLocation.InWorld(sheathCell)", demo);
        Assert.Contains("ItemLocation.InWorld(harnessCell)", demo);
        Assert.Contains("ItemLocation.InWorld(clubCell)", demo);
        Assert.DoesNotContain("ResidentInventoryCompartment.Weapon", demo);
        Assert.Contains("CombatEquipmentContent.ClubItemId", demo);

        Assert.Contains("ResidentInventoryExpansionContent.SheathItemId", policy);
        Assert.Contains("ResidentInventoryExpansionContent.WeaponHarnessItemId", policy);
        Assert.Contains("CombatEquipmentContent.ClubItemId", policy);
        Assert.Contains("Weapon Harness Left Strap", policy);
        Assert.Contains("Sheath Body", policy);
        Assert.Contains("Club Head", policy);
        Assert.Contains("DigItemCarrySocketPolicy.Weapon", policy);
        Assert.Contains("DigBasketVisualPolicy.CreateInstance", world);
        Assert.Contains("DigBasketVisualPolicy.CreateInstance", attachments);
        Assert.Contains("DigBasketVisualPolicy.Resolve", agentCatalog);

        Assert.Contains(
            "Club_pickup_uses_active_weapon_compartment_and_harness_tier",
            scenario);
        Assert.Contains("ResidentInventoryCompartment.Weapon", scenario);
        Assert.Contains("WeaponCapacity", scenario);
        Assert.Contains(
            "Sparse_main_and_loaded_cargo_compact_to_weapon_then_main_low_indices",
            scenario);
        Assert.Contains("NormalizeResidentInventory(ResidentId, tick: 1)", scenario);
        Assert.Contains("ResidentInventoryCompartment.Cargo", scenario);
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
