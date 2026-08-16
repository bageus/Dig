using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmUnityRuntimeContractTests
{
    [Fact]
    public void Runtime_projects_authoritative_farm_snapshots_after_buildings_exist()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigTerrainWorkSession.Farming.cs");
        string renderer = Read(runtime, "DigBuildingRenderer.Farming.cs");
        string loop = Read(runtime, "DigAgentSimulationDriverBase.Loop.cs");
        string bootstrap = Read(runtime, "DigUnityBootstrap.cs");

        Assert.Contains("LoadAllFarmSnapshots()", session);
        Assert.Contains("CreateSnapshot()", session);
        Assert.Contains("GetComponent<DigFarmVisualDecoration>()", renderer);
        Assert.Contains("decoration.SetState(snapshot)", renderer);
        Assert.True(
            loop.IndexOf("BuildingRenderer.Render(buildings)", StringComparison.Ordinal)
            < loop.IndexOf("BuildingRenderer.RenderFarmContents(", StringComparison.Ordinal));
        Assert.True(
            bootstrap.IndexOf("buildingRenderer.Render(buildings)", StringComparison.Ordinal)
            < bootstrap.IndexOf("buildingRenderer.RenderFarmContents(", StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_composition_accepts_loaded_farm_repository()
    {
        string runtime = RuntimeRoot();
        string composition = Read(runtime, "DigTerrainWorkSession.Composition.cs");
        string session = Read(runtime, "DigTerrainWorkSession.cs");
        string farming = Read(runtime, "DigTerrainWorkSession.Farming.cs");

        Assert.Contains("IFarmRepository? farms = null", composition);
        Assert.Contains(
            "miningOutputCommits,\n            farms,\n            farmLogisticsReservations);",
            composition);
        Assert.Contains("_farmRepository = farms ?? new InMemoryFarmRepository();", session);
        Assert.Contains("internal IFarmRepository FarmRepository => _farmRepository;", farming);
        Assert.Contains(
            "internal FarmLogisticsReservations FarmLogisticsReservations",
            Read(runtime, "DigTerrainFarmLogistics.cs"));
        Assert.DoesNotContain(
            "_farmRepository = new InMemoryFarmRepository()",
            farming);
    }

    [Fact]
    public void Reloaded_runtime_ids_skip_restored_farm_jobs_and_stacks()
    {
        string logistics = Read(RuntimeRoot(), "DigTerrainFarmLogistics.cs");

        Assert.Contains("while (true)", logistics);
        Assert.Contains("_jobRepository.Get().Get(candidate) != null", logistics);
        Assert.Contains("_inventoryRepository.Get().GetStack(candidate) != null", logistics);
        Assert.Contains("if (!exists) return candidate;", logistics);
        Assert.Contains(
            "_farmRuntimeSequence = checked(_farmRuntimeSequence + 1UL)",
            logistics);
    }

    [Fact]
    public void Removed_farm_keeps_logistics_links_until_runtime_reconciliation()
    {
        string farming = Read(RuntimeRoot(), "DigTerrainWorkSession.Farming.cs");
        string logistics = Read(RuntimeRoot(), "DigTerrainFarmLogistics.cs");

        Assert.Contains("_farmRepository.Remove(existing)", farming);
        Assert.DoesNotContain("_farmLogisticsReservations.ReleaseForFarm", farming);
        Assert.Contains("ReconcileReservations(jobs, command.Tick, inventory)",
            ReadApplication("Farming", "FarmLogisticsUseCases.cs"));
        Assert.Contains("SynchronizeFarmLogisticsRuntime(", farming);
        Assert.Contains("SynchronizeFarmLogisticsHandler(", logistics);
    }

    [Fact]
    public void Decoration_visualizes_stock_and_escaping_animals_without_physics()
    {
        string decoration = Read(RuntimeRoot(), "DigFarmVisualDecoration.cs");

        Assert.Contains("new GameObject[3]", decoration);
        Assert.Equal(2, Count(decoration, "new GameObject[8]"));
        Assert.Contains("new GameObject[2]", decoration);
        Assert.Contains("snapshot.MushroomSlotsOccupied + snapshot.ResidualMushrooms", decoration);
        Assert.Contains("snapshot.HamsterCount + snapshot.EscapingHamsterCount", decoration);
        Assert.Contains("snapshot.GrubCount + snapshot.EscapingGrubCount", decoration);
        Assert.Contains("SetVisible(_mushrooms, mushrooms)", decoration);
        Assert.Contains("SetVisible(_hamsters, hamsters)", decoration);
        Assert.Contains("SetVisible(_grubs, grubs)", decoration);
        Assert.Contains("snapshot.FeedCount", decoration);
        Assert.Contains("SetVisible(_feedCaps", decoration);
        Assert.Contains("-0.09f + (index * 0.18f)", decoration);
        Assert.Contains("Destroy(collider)", decoration);
        Assert.DoesNotContain("AddForce", decoration);
        Assert.DoesNotContain("Rigidbody", decoration);
        Assert.Contains("AnimateAnimals(_hamsters", decoration);
        Assert.Contains("AnimateAnimals(_grubs", decoration);
        Assert.Contains("Mathf.Clamp(x, -halfWidth, halfWidth)", decoration);
        Assert.Contains("Mathf.Clamp(z, -halfDepth, halfDepth)", decoration);
        Assert.Contains("Time.time * speed", decoration);
    }

    [Fact]
    public void Farm_mushroom_harvest_creates_physical_cap_and_leg_outputs()
    {
        string runtime = RuntimeRoot();
        string session = Read(runtime, "DigTerrainWorkSession.Farming.cs");
        string hud = Read(runtime, "DigGameHudCanvas.Farming.cs");

        Assert.Contains("HarvestFarmMushroom(string buildingId, long tick)", session);
        Assert.Contains("CollectFarmProductCommand", session);
        Assert.Contains("FarmDeliveryKind.MushroomSeed", session);
        Assert.Contains("_farmItems.MushroomCap", session);
        Assert.Contains("CampfireProductionContent.MushroomLegItemId", session);
        Assert.Equal(2, Count(session, "ItemLocation.InWorld(farm.Origin)"));
        string harvest = Read(runtime, "DigTerrainWorkSession.FarmHarvest.cs");
        Assert.Contains("MushroomChopJobDefinition", harvest);
        Assert.Contains("FarmMushroomRequiredSwings", harvest);
        Assert.Contains("PrepareResidentsForDirectCommand", harvest);
        Assert.Contains("jobs.Claim(jobId, workerId, tick)", harvest);
        Assert.Contains("HarvestFarmMushroom(farmId.ToString(), tick)", harvest);
        Assert.Contains("TryRestoreFarmMushroomHarvest", harvest);
        Assert.Contains("ReconcileFarmMushroomHarvests", harvest);
        Assert.Contains("job.Definition is MushroomChopJobDefinition", harvest);
        Assert.Contains("IsFarmMushroomHarvest(job)", Read(
            runtime,
            "DigTerrainWorkSession.DirectCommands.cs"));
        Assert.Contains("CancelFarmMushroomHarvest(job, tick)", Read(
            runtime,
            "DigTerrainWorkSession.Mushrooms.cs"));
        string priority = Read(
            runtime,
            "DigWorldInteraction.ResidentCommandPriority.cs");
        string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");
        Assert.Contains("CanDirectHarvestFarmMushroom", priority);
        Assert.Contains("new CellId(selected.CellX, selected.CellY, selected.CellZ)",
            priority);
        Assert.Contains("StartFarmMushroomHarvest", priority);
        Assert.Contains("Dwarf ordered to harvest farm mushroom", priority);
        Assert.Contains("CanDirectHarvestFarmMushroom", cursor);
        Assert.Contains("selected.CellX", cursor);
        Assert.Contains("DirectCommandCursorKind.Axe", cursor);
        Assert.DoesNotContain("Order harvest (", hud);
        Assert.Contains("farm.harvest_route_unavailable", harvest);
        Assert.Contains("TryResolveMushroomWorkPosition", harvest);
        Assert.Contains("farm.Origin,", harvest);
        Assert.Contains("workPosition,", harvest);
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

    private static string Read(string root, string file) =>
        File.ReadAllText(Path.Combine(root, file));

    private static string ReadApplication(string folder, string file) =>
        File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Dig.Application",
            folder,
            file));

    private static string RuntimeRoot() => Path.Combine(
        FindRepositoryRoot(),
        "Assets",
        "Dig.Unity",
        "Runtime");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Assets")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
