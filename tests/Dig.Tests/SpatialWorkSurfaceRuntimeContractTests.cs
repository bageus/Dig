using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class SpatialWorkSurfaceRuntimeContractTests
{
    [Fact]
    public void Spatial_excavation_routes_and_gates_work_by_exact_surface_pose()
    {
        string root = FindRepositoryRoot();
        string spatial = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.Movement.cs")));
        string movement = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigAgentSession.SpatialWorkMovement.cs")));

        Assert.Contains("PlanPreciseWorkMovement", spatial);
        Assert.Contains("IReadOnlyDictionary<string,SurfacePose>", spatial);
        Assert.Contains("WorkSurfacePositioning.Resolve", spatial);
        Assert.Contains("WorkSurfacePositioning.IsAt(ToSurfacePose(agent),workPose)", spatial);
        Assert.Contains("MoveOnReservedSurface(agent,destination)", movement);
        Assert.Contains("SaveAutomaticSurfaceProgress(agent)", movement);
        Assert.Contains("!_tunnelVolume.HasFullActorSupport(destination.Cell)", movement);
        Assert.Contains("VerticalSurfaceSteering.TryAttachToWall", movement);
        Assert.Contains("actual.IsVertical", spatial);
        Assert.Contains("!HasFullStandingSupport(required.Cell)", spatial);
    }

    [Fact]
    public void Mushroom_requires_precise_pose_while_pickup_accepts_supported_source_floor()
    {
        string root = FindRepositoryRoot();
        string planner = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.Movement.cs")));
        string mushrooms = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Mushrooms.cs")));
        string pickup = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigWorldItemPickupExecution.cs")));

        Assert.Contains("MushroomChopJobDefinition", planner);
        Assert.Contains("WorldItemPickupJobDefinition", planner);
        Assert.Contains("WorkSurfacePositioning.IsAt(ToSurfacePose(worker),required)", mushrooms);
        Assert.Contains("IsAtPreciseWorkPose(job,agent)", pickup);
        Assert.Contains("job.DefinitionisWorldItemPickupJobDefinition", planner);
        Assert.Contains("actual.Cell==required.Cell", planner);
        Assert.Contains("actual.Face==SurfaceFace.Floor", planner);
        Assert.Contains("HasFullStandingSupport(required.Cell)", planner);
    }

    [Fact]
    public void Construction_and_hauling_resolve_each_phase_and_gate_actions_by_pose()
    {
        string root = FindRepositoryRoot();
        string planner = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.Movement.cs")));
        string assembly = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingBoxAssemblyExecution.cs")));
        string assemblyDrain = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingBoxAssemblyTickDrain.cs")));
        string packing = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingPackingExecution.cs")));
        string pickup = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingBoxPickupExecution.cs")));
        string relocation = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingBoxRelocationExecution.cs")));
        string hauling = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainHauling.cs")));

        Assert.Contains("BuildingBoxAssemblyJobDefinition", planner);
        Assert.Contains("BuildingBoxPackingJobDefinition", planner);
        Assert.Contains("BuildingBoxPickupJobDefinition", planner);
        Assert.Contains("HaulJobDefinition", planner);
        Assert.Contains("ResolveHaulingTarget(job,hauling)", planner);
        Assert.Contains("IsAtPreciseWorkPose(job,agent)", assembly);
        Assert.Contains("IsAtPreciseWorkPose(currentJob,agent)", assemblyDrain);
        Assert.Contains("IsAtPreciseWorkPose(job,agent)", packing);
        Assert.Contains("IsAtPreciseWorkPose(job,agent)", pickup);
        Assert.Contains("IsAtPreciseWorkPose(current,agent)", relocation);
        Assert.Contains("IsAtPreciseWorkPose(job,agent)", hauling);
    }

    [Fact]
    public void Production_and_supply_resolve_phase_poses_and_gate_inventory_mutations()
    {
        string root = FindRepositoryRoot();
        string planner = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.Movement.cs"))
            + File.ReadAllText(Path.Combine(
                root,
                "Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.ProductionMovement.cs")));
        string production = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingProductionZones.cs"))
            + File.ReadAllText(Path.Combine(
                root,
                "Assets/Dig.Unity/Runtime/DigBuildingProductionMaterialLifecycle.cs")));
        string supply = Normalize(File.ReadAllText(Path.Combine(
            root,
            "Assets/Dig.Unity/Runtime/DigBuildingProductionRuntime.cs"))
            + File.ReadAllText(Path.Combine(
                root,
                "Assets/Dig.Unity/Runtime/DigBuildingProductionSupplyCheck.cs")));

        Assert.Contains("ProductionWorkJobDefinition", planner);
        Assert.Contains("BuildingSupplyJobDefinition", planner);
        Assert.Contains("ProductionMaterialStepPhase.AwaitingMaterial", planner);
        Assert.Contains("ProductionMaterialStepPhase.ProcessedAwaitingPackage", planner);
        Assert.Contains("FindPendingSupplyAllocation(job.Id,supply)", planner);
        Assert.Contains("IsAtPreciseWorkPose(job,worker)", production);
        Assert.Contains("IsAtPreciseWorkPose(current,worker)", production);
        Assert.Contains("IsAtPreciseWorkPose(job,worker)", supply);
        Assert.Contains("IsAtPreciseWorkPose(current,worker)", supply);
    }

    private static string Normalize(string value)
    {
        return value.Replace(" ", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
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
