using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionAndInventoryPointerInputRegressionTests
{
    [Fact]
    public void Product_icon_owns_both_queue_increment_and_decrement()
    {
        string runtime = RuntimeRoot();
        string production = Normalize(Read(runtime, "DigGameHudCanvas.BuildingProduction.cs"));
        string pointer = Normalize(Read(runtime, "DigProductionIconPointer.cs"));

        Assert.Contains("()=>QueueBuildingProduction(building.Id,product.RecipeId.ToString())", production);
        Assert.Contains("pointer.RightClicked=product.QueuedCount>0", production);
        Assert.Contains("()=>CancelBuildingProduction(building.Id,product.RecipeId.ToString())", production);
        Assert.Contains("if(!hasNonTerminalOrder){return;}", production);
        Assert.DoesNotContain("\"Cancel\"+product.RecipeId", production);
        Assert.DoesNotContain("\"−\"", production);
        Assert.Contains("IPointerClickHandler", pointer);
        Assert.Contains("eventData.button==PointerEventData.InputButton.Right", pointer);
        Assert.Contains("RightClicked?.Invoke()", pointer);
    }

    [Fact]
    public void Generic_inventory_lmb_enters_local_ghost_and_job_pipeline()
    {
        string runtime = RuntimeRoot();
        string canvas = Normalize(Read(runtime, "DigWorldInteraction.CanvasHud.cs"));
        string inventory = Normalize(Read(runtime, "DigWorldInteraction.ResidentInventory.cs"));
        string placement = Normalize(Read(runtime, "DigWorldInteraction.InventoryItemPlacement.cs"));

        Assert.Contains(
            "SelectResidentInventoryLayoutSlot(ResidentInventoryLayoutSlotViewModelslot){ActivateResidentInventoryLayoutSlot(slot);}",
            canvas);
        Assert.DoesNotContain("LMBonopengrounddropsitthere", canvas);
        Assert.Contains("BeginInventoryItemPlacement(slot)", inventory);
        Assert.Contains("ValidateResidentInventoryPlacement", placement);
        Assert.Contains("CreateResidentInventoryPlacement(", placement);
        Assert.Contains("CancelInventoryItemPlacement()", placement);
        Assert.Contains("Inventoryitemplacementordercreated", placement);
    }

    [Fact]
    public void Play_mode_tests_are_friends_of_the_runtime_assembly()
    {
        string runtime = RuntimeRoot();
        string assemblyInfo = Normalize(Read(runtime, "AssemblyInfo.cs"));
        string testAssembly = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "Dig.Unity.PlayModeTests.asmdef")));

        Assert.Contains("InternalsVisibleTo(\"Dig.Unity.PlayModeTests\")", assemblyInfo);
        Assert.Contains("\"name\":\"Dig.Unity.PlayModeTests\"", testAssembly);
    }

    [Fact]
    public void Camera_pan_has_arrow_key_duplicates_for_wasd()
    {
        string camera = Normalize(Read(RuntimeRoot(), "DigCameraController.cs"));

        Assert.Contains("KeyCode.A,KeyCode.LeftArrow,KeyCode.D,KeyCode.RightArrow", camera);
        Assert.Contains("KeyCode.S,KeyCode.DownArrow,KeyCode.W,KeyCode.UpArrow", camera);
    }

    [Fact]
    public void Demo_mushroom_materials_keep_production_stack_capacity_for_supply_deposit()
    {
        string demo = Normalize(Read(
            RuntimeRoot(),
            "DigTerrainWorkSession.ResidentInventoryDemo.cs"));

        Assert.Contains(
            "newItemDefinition(MushroomCapItemId,\"Mushroomcap\",100,false",
            demo);
        Assert.Contains(
            "newItemDefinition(MushroomLegItemId,\"Mushroomleg\",100,false",
            demo);
        Assert.DoesNotContain(
            "newItemDefinition(MushroomCapItemId,\"Mushroomcap\",1,false",
            demo);
        Assert.DoesNotContain(
            "newItemDefinition(MushroomLegItemId,\"Mushroomleg\",1,false",
            demo);
    }

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
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

    private static string Normalize(string value)
    {
        return value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}

}
