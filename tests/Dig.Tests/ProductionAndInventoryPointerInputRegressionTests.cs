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
    public void Generic_inventory_lmb_uses_live_layout_slot_for_ghost_and_job_pipeline()
    {
        string runtime = RuntimeRoot();
        string canvas = Normalize(Read(runtime, "DigWorldInteraction.CanvasHud.cs"));
        string placement = Normalize(Read(runtime, "DigWorldInteraction.InventoryItemPlacement.cs"));
        string application = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Dig.Application",
            "Inventory",
            "ResidentInventoryPlacementHandlers.cs")));

        Assert.Contains(
            "SelectResidentInventoryLayoutSlot(ResidentInventoryLayoutSlotViewModelslot)",
            canvas);
        Assert.Contains("InteractResidentInventoryLayoutSlot(", canvas);
        Assert.Contains("canPlaceSelectedInventoryItem:slot.CanPlace", canvas);
        Assert.Contains("ApplyDecision(decision,inventorySlot:slot)", canvas);
        Assert.DoesNotContain("ActivateResidentInventoryLayoutSlot(slot)", canvas);
        Assert.Contains(
            "BeginInventoryItemPlacement(ResidentInventoryLayoutSlotViewModelslot)",
            placement);
        Assert.Contains("Cursor.visible=false", placement);
        Assert.Contains("Cursor.visible=true", placement);
        Assert.Contains("ValidateResidentInventoryPlacement", placement);
        Assert.Contains("CreateResidentInventoryPlacement(", placement);
        Assert.Contains("CancelInventoryItemPlacement()", placement);
        Assert.Contains("Inventoryitemplacementordercreated", placement);
        Assert.Contains("HasWalkableSupport(world,destination)", application);
    }

    [Fact]
    public void Inventory_quick_drop_uses_c_live_layout_and_shared_authoritative_commit()
    {
        string runtime = RuntimeRoot();
        string hud = Normalize(Read(runtime, "DigGameHudCanvas.Inventory.cs"));
        string canvas = Normalize(Read(runtime, "DigWorldInteraction.CanvasHud.cs"));
        string commands = Normalize(Read(
            runtime,
            "DigWorldInteraction.ResidentInventoryCommands.cs"));
        string cursor = Normalize(Read(runtime, "DigWorldInteraction.ItemInteractionCursor.cs"));

        Assert.Contains("Input.GetKey(KeyCode.C)", hud);
        Assert.Contains(
            "InteractResidentInventoryLayoutSlot(slot,altPressed,dropPressed)",
            hud);
        Assert.Contains(
            "DropResidentInventoryLayoutSlot(ResidentInventoryLayoutSlotViewModelslot)",
            canvas);
        Assert.Contains("dropPressed:dropPressed", canvas);
        Assert.Contains("DropResidentInventoryLayoutSlot(", canvas);
        Assert.Contains("ExecuteResidentInventoryDrop(", commands);
        Assert.Contains("DropResidentInventoryStack(", commands);
        Assert.Contains("SynchronizeLivingMaterials(", commands);
        Assert.Contains("Input.GetKey(KeyCode.C)", cursor);
        Assert.DoesNotContain("Input.GetKey(KeyCode.D)", hud);
        Assert.DoesNotContain("Input.GetKey(KeyCode.D)", cursor);
    }

    [Fact]
    public void Compatibility_slot_conversion_preserves_held_and_interaction_profile()
    {
        string canvas = Normalize(Read(
            RuntimeRoot(),
            "DigWorldInteraction.CanvasHud.cs"));

        Assert.Contains("isEquipped:slot.IsHeld", canvas);
        Assert.Contains("heldQuantity:slot.HeldQuantity", canvas);
        Assert.Contains("interactionProfile:slot.InteractionProfile", canvas);
    }

    [Fact]
    public void Active_placement_reservations_use_blue_inventory_projection()
    {
        string runtime = RuntimeRoot();
        string canvas = Normalize(Read(runtime, "DigGameHudCanvas.Inventory.cs"));
        string query = Normalize(Read(runtime, "DigResidentInventoryPlacementQueries.cs"));

        Assert.Contains("if(IsBlueReservedSlot(slot))", canvas);
        Assert.Contains(
            "_terrainSession?.HasActiveResidentInventoryPlacement(slot.StackId)==true",
            canvas);
        Assert.Contains("newColor(0.10f,0.34f,0.72f,0.96f)", canvas);
        Assert.Contains("newColor(0.72f,0.88f,1f,1f)", canvas);
        Assert.Contains("newColor(0.42f,0.18f,0.18f,0.92f)", canvas);
        Assert.Contains("ResidentInventoryPlacementJobDefinitionplacement", query);
        Assert.Contains("placement.StackId==stack", query);
        Assert.Contains("$\"\\nR:{slot.ReservedQuantity}\"", canvas);
    }

    [Fact]
    public void Play_mode_tests_are_friends_of_the_runtime_assembly()
    {
        string runtime = RuntimeRoot();
        string assemblyInfo = Normalize(Read(runtime, "AssemblyInfo.cs"));
        string testAssembly = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
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