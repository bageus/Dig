using System;
using System.IO;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class DeepGameplayInteractionRegressionTests
{
    private static readonly EntityId Resident = EntityId.Parse(
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private static readonly EntityId Target = EntityId.Parse(
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
    private static readonly CellId Cell = new CellId(3, 3);

    [Fact]
    public void Loose_items_use_plain_left_click_while_building_boxes_keep_alt_pickup()
    {
        ContextInputRouter router = new ContextInputRouter();
        ContextInputState state = new ContextInputState(selectedResidentId: Resident);
        ContextInputDecision loose = router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left),
            state,
            new ContextPointerTarget(
                ContextWorldTargetKind.GenericItem,
                Target,
                Cell,
                reachable: true,
                itemActionAvailable: true,
                itemInteractionAction: ItemWorldInteractionAction.Pickup));
        ContextInputDecision boxSelection = router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left),
            state,
            new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                Target,
                Cell,
                reachable: true,
                itemActionAvailable: true,
                itemInteractionAction: ItemWorldInteractionAction.SelectBuildingBox));
        ContextInputDecision boxPickup = router.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left,
                altPressed: true),
            state,
            new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                Target,
                Cell,
                reachable: true,
                itemActionAvailable: true,
                itemInteractionAction: ItemWorldInteractionAction.Pickup));

        Assert.Equal(ApplicationInputCommandKind.PickupWorldItem, loose.CommandKind);
        Assert.True(boxSelection.Effects.HasFlag(
            PresentationInputEffect.SelectBuildingBox));
        Assert.Equal(ApplicationInputCommandKind.None, boxSelection.CommandKind);
        Assert.Equal(ApplicationInputCommandKind.PickupBuildingBox, boxPickup.CommandKind);
    }

    [Fact]
    public void Side_view_placement_prefers_a_horizontal_work_position()
    {
        WorldState world = CreateEmptyWorld();
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("campfire.side_view"),
            "Campfire",
            new[] { new CellOffset(0, 0) },
            new[]
            {
                new CellOffset(0, -1),
                new CellOffset(-1, 0),
                new CellOffset(1, 0),
                new CellOffset(0, 1),
            },
            new[]
            {
                new BuildingMaterialRequirement(new ItemId("stone"), 1),
            },
            requiredWork: 3,
            maximumDurability: 100);
        CellId origin = new CellId(3, 3);

        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            definition,
            origin,
            BuildingOrientation.North,
            world.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[]
            {
                new CellId(3, 2),
                new CellId(2, 3),
                new CellId(4, 3),
                new CellId(3, 4),
            });

        Assert.True(placement.Succeeded);
        Assert.Equal(origin.Y, placement.WorkPosition.Y);
        Assert.Equal(new CellId(2, 3), placement.WorkPosition);
    }

    [Fact]
    public void Carried_source_and_loose_item_on_target_do_not_block_unpacking()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness(carriedByResident: true);
        CellId origin = new CellId(3, 3);
        EntityId looseStack = BuildingBoxHarness.Id(99);
        Result added = harness.Inventory.AddStack(
            looseStack,
            harness.BoxItemId,
            quantity: 1,
            ItemLocation.InWorld(origin),
            tick: 0);

        Result confirmed = harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            origin);

        Assert.True(added.IsSuccess, added.Error?.ToString());
        Assert.True(confirmed.IsSuccess, confirmed.Error?.ToString());
        Assert.Equal(ItemLocation.InResidentSlot(
                harness.WorkerId,
                ResidentInventoryCompartment.Main,
                0),
            harness.Inventory.GetStack(harness.SourceStackId)!.Location);
        Assert.Equal(ItemLocation.InWorld(origin),
            harness.Inventory.GetStack(looseStack)!.Location);
    }

    [Fact]
    public void Runtime_keeps_placement_hover_excavation_and_manual_movement_responsive()
    {
        string runtime = RuntimeRoot();
        string ghost = Read(runtime, "DigBuildingBoxGhostRenderer.WorldSpace.cs");
        string hover = Read(runtime, "DigWorldInteraction.WorldObjectHover.cs");
        string tint = Read(runtime, "DigVisualTintTarget.cs");
        string cadence = Read(runtime, "DigAgentSession.MovementModes.cs");
        string execution = Read(runtime, "DigPackableBuildingExecution.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string agentAdvance = Read(runtime, "DigAgentSimulationDriverBase.AgentAdvance.cs");
        string directMovement = Read(runtime, "DigTerrainWorkDirectMovement.cs");
        string manual = Read(runtime, "DigTerrainWorkManualExcavation.cs");
        string multiWorker = Read(runtime, "DigTerrainWorkManualExcavation.MultiWorker.cs");

        Assert.Contains("_root.rotation=Quaternion.identity", ghost);
        Assert.Contains("return item;", hover);
        Assert.Contains("TryGetMushroom", hover);
        Assert.Contains("TryGetBarrel", hover);
        Assert.Contains("TryGetBuilding", hover);
        Assert.Contains("RefreshHoverTintsIfStale(next)", hover);
        Assert.Contains("HasStaleHoverTints()", hover);
        Assert.Contains("if(tint==null)", hover);
        Assert.Contains("HasLiveRendererCache()", tint);
        Assert.Contains("if(renderer==null)", tint);
        Assert.Contains("ResidentInventoryMovementCadence.IsDue", cadence);
        Assert.Contains("resolution.AuthoritativeCadenceMultiplier", cadence);
        Assert.Contains("durationSeconds:1", execution);
        Assert.DoesNotContain("Hud!.SetCommandResult(result);return;", loop);
        Assert.Contains("ReconcileChangedTerrain(tick,agents)", agentAdvance);
        Assert.Contains("firstError==null", agentAdvance);
        Assert.Contains("_releaseAssignment!.Handle", directMovement);
        Assert.Contains("RemoveAllRoutePlans(assignments[index].Id)", directMovement);
        Assert.DoesNotContain("Resultreleased=_releaseAssignment", directMovement);
        Assert.Contains("DirectJobAssignmentPlanner", manual);
        Assert.DoesNotContain("ManualExcavationGroup", manual);
        Assert.Contains("ResolveDirectResidentCell(agentId,seed,index)", multiWorker);
        Assert.Contains("_specificAssignment!.Handle", multiWorker);
    }

    [Fact]
    public void Carried_boxes_and_integer_values_are_projected_in_management_ui()
    {
        string runtime = RuntimeRoot();
        string held = Read(runtime, "DigGameHudCanvas.HeldBuildingBoxes.cs");
        string roster = Read(runtime, "DigGameHudCanvas.Roster.cs");
        string management = Read(runtime, "DigGameHudCanvas.ManagementBuildings.cs");
        string skills = Read(runtime, "DigGameHudCanvas.SkillInspector.cs");
        string packing = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Dig.Application",
            "Buildings",
            "BuildingBoxPackingLifecycleHandlers.cs")));

        Assert.Contains("ResidentInventorySlotVisualKind.BuildingBox", held);
        Assert.Contains("Heldby", roster);
        Assert.Contains("Heldby", management);
        Assert.Contains("units/AgentSkillCatalog.UnitsPerPoint", skills);
        Assert.DoesNotContain("ToString(\"0.##\")", skills);
        Assert.Contains("ItemLocation.InWorld(building.Origin)", packing);
    }

    private static WorldState CreateEmptyWorld()
    {
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            air,
            explored: true).Value;
    }

    private static string Read(string runtime, string file)
    {
        return Normalize(File.ReadAllText(Path.Combine(runtime, file)));
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

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("
", string.Empty, StringComparison.Ordinal);
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
