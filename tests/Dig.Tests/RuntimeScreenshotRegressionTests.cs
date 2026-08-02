using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class RuntimeScreenshotRegressionTests
{
    [Fact]
    public void Spatial_excavation_owns_designation_and_stale_jobs_are_cancelled()
    {
        string runtime = RuntimeRoot();
        string driver = Read(runtime, "DigAgentSimulationDriverBase.CaveRooms.cs");
        string spatial = Read(runtime, "DigTerrainSpatialExcavation.cs");
        string designations = Read(runtime, "DigTerrainWorkDesignations.cs");
        string erase = Read(SourceRoot(), "Dig.Application/World/EraseExcavationBatch.cs");

        Assert.Contains(
            "Result worldDesignation = WorldSession.SetDesignation",
            driver);
        Assert.Contains(
            "Result rollback = WorldSession.SetDesignation",
            driver);
        Assert.Contains("plan.Target", driver);
        Assert.Contains("active: true", driver);
        Assert.Contains("active: false", driver);
        Assert.Contains("target.State.Designation != CellDesignation.Dig", spatial);
        Assert.Contains("jobs.Cancel", spatial);
        Assert.Contains("CompleteExcavationQuarterTarget", spatial);
        Assert.Contains("SpatialDigJobDefinition spatial", erase);
        Assert.Contains("_spatialDigJobs.Remove(cells[index])", designations);
    }

    [Fact]
    public void Barrel_and_building_zones_use_visible_world_space_presentation()
    {
        string runtime = RuntimeRoot();
        string barrel = Read(runtime, "DigBarrelVisual.cs");
        string barrelRenderer = Read(runtime, "DigBarrelRenderer.cs");
        string stock = Read(runtime, "DigBuildingInternalStockRenderer.cs");
        string zones = Read(runtime, "DigBuildingInternalStockRenderer.Zones.cs");
        string bay = Read(runtime, "DigBuildingInternalStockBayVisual.cs");

        Assert.Contains("PresentationScale = 0.70f", barrel);
        Assert.Contains("VisualHeight => 0.49f", barrel);
        Assert.Contains("worldPositionStays: true", barrelRenderer);
        Assert.Contains("visual.transform.rotation = Quaternion.identity", barrelRenderer);
        Assert.Contains("ResolveInternalZoneCell", stock + zones);
        Assert.Contains("ResolveOutputZoneCell", zones);
        Assert.Contains("leftEdge - 1", zones);
        Assert.Contains("rightEdge + 1", zones);
        Assert.Contains("VisibleDepthOffset = 0.12f", stock);
        Assert.Contains("RenderBay", zones);
        Assert.DoesNotContain("FrontDepthOffset", stock + zones);
        Assert.DoesNotContain("building.WorkPositionX", stock);
        Assert.Contains("RenderProductionWorkbench(building, view.ShowWorkbench)", zones);
        Assert.Contains("building.WorkPositionX", zones);
        Assert.Contains("Destroy(collider)", zones);
        Assert.Contains("Storage tray", bay);
        Assert.DoesNotContain("Storage back rail", bay);
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

    private static string SourceRoot()
    {
        return Path.Combine(FindRepositoryRoot(), "src");
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
