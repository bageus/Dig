using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomUnityRuntimeContractTests
{
    [Fact]
    public void Hover_cursor_and_click_share_mushroom_priority_over_buildings()
    {
        string runtime = RuntimeRoot();
        string priority = Read(runtime, "DigWorldInteraction.ResidentCommandPriority.cs");
        string decisions = Read(runtime, "DigWorldInteraction.Decisions.cs");
        string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");
        string cursorTargets = Read(runtime, "DigWorldInteraction.DirectCommandCursor.Targets.cs");
        string pointerHits = Read(runtime, "DigWorldInteraction.PointerHits.cs");

        Assert.True(
            priority.IndexOf("TryResolveMushroomHit", StringComparison.Ordinal)
            < priority.IndexOf("TryResolveCompletedBuildingHit", StringComparison.Ordinal));
        Assert.True(
            priority.IndexOf("TryResolveMushroomHit", StringComparison.Ordinal)
            < priority.IndexOf("_excavationMode!=DigExcavationDrawingMode.None", StringComparison.Ordinal));
        Assert.Contains("ContextWorldTargetKind.Mushroom", priority);
        Assert.Contains("ApplicationInputCommandKind.ChopMushroom", decisions);
        Assert.Contains("DirectCommandCursorKind.Axe", cursor);
        Assert.Contains("TryResolveReachableMushroomHit(hits,out_)", cursorTargets);
        Assert.Contains("TryResolveReachableMushroomHit(hits,outDigMushroomVisualmushroom)", pointerHits);
        Assert.DoesNotContain("?.SetHovered", pointerHits);
        Assert.Contains("if(_hoveredMushroom!=null)", pointerHits);
        Assert.Contains("_hoveredMushroom.SetHovered(false)", pointerHits);
        Assert.Contains("_hoveredMushroom.SetHovered(true)", pointerHits);
    }

    [Fact]
    public void Mushroom_visual_is_vertical_small_urp_lit_and_highlightable()
    {
        string visual = Read(RuntimeRoot(), "DigMushroomVisual.cs");
        string renderer = Read(RuntimeRoot(), "DigMushroomRenderer.cs");

        Assert.Contains("Shader.Find(\"UniversalRenderPipeline/Lit\")", visual);
        Assert.Contains("MushroomStage.Large=>(0.84f,0.62f)", visual);
        Assert.Contains("_collider!.center=newVector3(0f,height*0.5f,0f)", visual);
        Assert.Contains("_collider.size=newVector3", visual);
        Assert.Contains("transform.localRotation=Quaternion.identity", visual);
        Assert.Contains("internalvoidSetHovered(boolhovered)", visual);
        Assert.Contains("Color.Lerp(_baseColors[index],Color.white,HoverBlend)", visual);
        Assert.Equal(2, Count(visual, "if(renderer==null)"));
        Assert.DoesNotContain("Shader.Find(\"Standard\")", visual);
        Assert.DoesNotContain("MushroomStage.Large=>(1.34f", visual);
        Assert.Contains("DigTunnelProjection.ResidentFootSink", renderer);
        Assert.Contains("site.Cell.Z)+newVector3(0f,DigTunnelProjection.ResidentFootSink,0f)", renderer);
        Assert.DoesNotContain("FrontOffset", renderer);
        Assert.Contains("SetParent(transform,worldPositionStays:true)", renderer);
        Assert.DoesNotContain("SetParent(transform,worldPositionStays:false)", renderer);
    }


    [Fact]
    public void Foreground_mushroom_material_blocks_axes_and_uses_pickup_targeting()
    {
        string runtime = RuntimeRoot();
        string priority = Read(runtime, "DigWorldInteraction.ResidentCommandPriority.cs");
        string pointerHits = Read(runtime, "DigWorldInteraction.PointerHits.cs");

        int itemBlock = priority.IndexOf(
            "_itemRenderer.TryGetItem(hits[index],out_)",
            StringComparison.Ordinal);
        int mushroomResolution = priority.IndexOf(
            "_mushroomRenderer.TryGetMushroom(hits[index],outmushroom)",
            StringComparison.Ordinal);
        Assert.True(itemBlock >= 0 && itemBlock < mushroomResolution);
        Assert.Contains("Aphysicaldropinfrontofaregrownsite", priority);
        Assert.Contains("TryResolveWorldItemHit(hits,outDigWorldItemVisualcandidate)", pointerHits);
        Assert.Contains("candidate.Model.CanPickup", pointerHits);
        Assert.Contains("_itemRenderer.TryGetItem(hits[index],out_)", priority);
        Assert.Contains("building=null!;returnfalse", priority);
    }

    [Fact]
    public void Runtime_uses_authoritative_mushroom_jobs_drops_skills_and_growth()
    {
        string runtime = RuntimeRoot();
        string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
        string navigation = Read(runtime, "DigTerrainWorkSession.MushroomNavigation.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");
        string inventory = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");

        Assert.Contains("StartDirectMushroomChopCommandHandler", mushrooms);
        Assert.Contains("CompleteMushroomChopCommandHandler", mushrooms);
        Assert.Contains("AdvanceMushroomGrowthCommand", mushrooms);
        Assert.Contains("MushroomChopJobDefinition", navigation);
        Assert.Contains("AdvanceMushrooms(AgentSession.Tick,agents)", loop);
        Assert.Contains("MushroomRenderer!.Render(TerrainSession!.LoadMushrooms())", loop);
        Assert.Contains("InitializeMushroomDemo(agentSession.Tick)", bootstrap);
        Assert.Contains("mushroomRenderer.Render(terrainSession.LoadMushrooms())", bootstrap);
        Assert.Contains("MushroomCapItemId", inventory);
        Assert.Contains("MushroomLegItemId", inventory);
    }

    [Fact]
    public void Mushroom_work_projects_status_target_facing_and_repeating_chop_pose()
    {
        string workFacing = Read(RuntimeRoot(), "DigAgentRenderer.WorkFacing.cs");
        string visualFacing = Read(RuntimeRoot(), "DigAgentVisual.WorkFacing.cs");
        string movement = Read(RuntimeRoot(), "DigAgentVisual.Movement.cs");
        string rig = Read(RuntimeRoot(), "DigResidentRig.cs");
        string presenter = Read(PresentationAgentsRoot(), "ResidentActivityPresenter.cs");

        Assert.Contains("job.IsMushroomChop", workFacing);
        Assert.Contains("animateToolWork:hasToolWork", workFacing);
        Assert.Contains("ApplyToolWorkAnimation()", movement);
        Assert.Contains("ToolWorkAnimationPeriodSeconds", visualFacing);
        Assert.Contains("ResidentActionVisualState.Dig", visualFacing);
        Assert.Contains("-58f+swing*0.72f", rig);
        Assert.Contains("Добываетгриб", presenter);
        Assert.Contains("definitionisMushroomChopJobDefinition", presenter);
        Assert.Contains("ResidentActivityKind.GatherMushroom", presenter);
    }

    [Fact]
    public void Mushroom_site_blocks_buildings_but_not_inventory_items()
    {
        string runtime = RuntimeRoot();
        string placement = Read(runtime, "DigBuildingBoxPlacement.cs");
        string barrels = Read(runtime, "DigTerrainWorkSession.Barrels.cs");
        string items = Read(runtime, "DigTerrainWorkSession.ResidentInventoryDemo.cs");

        Assert.Equal(2, Count(placement, "BuildingPlacementBlockedCells"));
        Assert.Contains("MushroomBuildingBlockedCells", barrels);
        Assert.Contains(".Concat(BarrelBuildingBlockedCells)", barrels);
        Assert.DoesNotContain("BuildingPlacementBlockedCells", items);
        Assert.DoesNotContain("MushroomBuildingBlockedCells", items);
    }

    [Fact]
    public void PlayMode_tests_respect_boundaries_and_cover_visual_regressions()
    {
        string playMode = PlayModeRoot();
        string topology = Read(playMode, "PostExcavationTopologyPlayModeTests.cs");
        string mushrooms = Read(playMode, "MushroomChoppingPlayModeTests.cs");
        string materialTargeting = Read(
            playMode,
            "MushroomMaterialTargetingPlayModeTests.cs");
        string depthProjection = Read(
            playMode,
            "MushroomDepthProjectionPlayModeTests.cs");
        string hoverRemoval = Read(
            playMode,
            "MushroomHoverRemovalPlayModeTests.cs");

        Assert.DoesNotContain(".Offset(", topology);
        Assert.Contains("newCellId(cell.X-1,cell.Y,cell.Z)", topology);
        Assert.Contains("newCellId(cell.X,cell.Y+1,cell.Z)", topology);
        Assert.DoesNotContain("renderer.Render(", mushrooms);
        Assert.DoesNotContain("renderer.ActiveCount", mushrooms);
        Assert.Contains("Invoke(renderer,\"Render\",(object)new[]{large})", mushrooms);
        Assert.Contains("GetProperty(renderer,\"ActiveCount\")", mushrooms);
        Assert.Contains("UniversalRenderPipeline/Lit", mushrooms);
        Assert.Contains("Invoke(visual,\"SetHovered\",true)", mushrooms);
        Assert.Contains("collider.center.y-(collider.size.y*0.5f)", mushrooms);
        Assert.Contains("Quaternion.Euler(90f,0f,0f)", mushrooms);
        Assert.Contains("drops.All(value=>value.CanPickup)", mushrooms);
        Assert.Contains("GetComponentInParent<DigMushroomVisual>()", mushrooms);
        Assert.Contains("TryResolveMushroomHit", materialTargeting);
        Assert.Contains("TryResolveWorldItemHit", materialTargeting);
        Assert.Contains("GetComponentInParent<DigMushroomVisual>()", materialTargeting);
        Assert.Contains(
            "Renderer_keeps_mushrooms_inside_authoritative_z0_to_z3_depth_slabs",
            depthProjection);
        Assert.Contains("depthOrigin+(z*depthSpacing)", depthProjection);
        Assert.Contains("collider.bounds.center.z", depthProjection);
        Assert.Contains("depthSlabHalfExtent", depthProjection);
        Assert.Contains(
            "Destroyed_hovered_mushroom_is_cleared_without_mesh_reference_access",
            hoverRemoval);
        Assert.Contains("DestroyImmediate(mushroomRoot)", hoverRemoval);
        Assert.Contains("Invoke(interaction,\"ClearPointerHover\")", hoverRemoval);
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Read(string root, string file) => Normalize(
        File.ReadAllText(Path.Combine(root, file)));

    private static string RuntimeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "unity",
        "Dig.Unity",
        "Assets",
        "Dig.Unity",
        "Runtime");

    private static string PresentationAgentsRoot() => Path.Combine(
        FindRepositoryRoot(),
        "src",
        "Dig.Presentation.Abstractions",
        "Agents");

    private static string PlayModeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "unity",
        "Dig.Unity",
        "Assets",
        "Dig.Unity",
        "Tests",
        "PlayMode");

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

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
}
}
