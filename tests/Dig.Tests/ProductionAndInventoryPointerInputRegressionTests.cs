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