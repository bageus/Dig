using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

internal static class BuildingBoxRuntimeLifecyclePlayModeHarness
{
    internal static Runtime CreateRuntime()
    {
        Assembly assembly = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(assembly, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            20,
            14,
            5);
        object view = Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        object tunnel = Invoke(world, "CreateTunnelNavigationVolume");
        object residents = InvokeStatic(
            RequireType(assembly, "Dig.Unity.DigAgentSession"),
            "CreateDemo",
            view,
            tunnel,
            journal);
        AgentViewModel[] agents = ((IEnumerable)Invoke(residents, "LoadView"))
            .Cast<AgentViewModel>()
            .ToArray();
        object terrain = InvokeStatic(
            RequireType(assembly, "Dig.Unity.DigTerrainWorkSession"),
            "CreateDemo",
            world,
            agents,
            journal,
            GetProperty(residents, "SkillGrants"));
        Invoke(terrain, "InitializeBuildingDemo", journal);
        return new Runtime(
            world,
            residents,
            terrain,
            agents,
            GetField<InMemoryInventoryRepository>(
                terrain,
                "_buildingInventoryRepository"),
            GetField<InMemoryJobRepository>(terrain, "_jobRepository"));
    }

    internal static ItemStackSnapshot MoveCampfireBoxToResident(
        Runtime runtime,
        AgentViewModel resident)
    {
        InventoryState inventory = runtime.Inventory;
        ItemStackSnapshot box = inventory.CreateSnapshot().Stacks.Single(
            value => value.ItemId == CampfireBuildingBoxContent.CampfireBoxItemId);
        EntityId residentId = EntityId.Parse(resident.Id);
        Assert.That(
            inventory.NormalizeResidentInventory(residentId, tick: 1).IsSuccess,
            Is.True);
        ResidentInventorySlot slot = inventory.GetResidentInventoryLayout(residentId)
            .Slots
            .First(value => value.Slot.Compartment == ResidentInventoryCompartment.Main
                && value.IsEmpty)
            .Slot;
        Result moved = inventory.MoveAvailableToResidentSlot(
            box.StackId,
            quantity: 1,
            residentId,
            slot,
            splitStackId: default,
            tick: 2);
        Assert.That(moved.IsSuccess, Is.True, moved.Error?.ToString());
        runtime.InventoryRepository.Save(inventory);
        return inventory.GetStack(box.StackId)!;
    }

    internal static AgentViewModel[] AdvanceAssemblyTick(Runtime runtime, long tick)
    {
        AgentViewModel[] before = ((IEnumerable)Invoke(runtime.Residents, "LoadView"))
            .Cast<AgentViewModel>()
            .ToArray();
        Invoke(runtime.Terrain, "SynchronizeBuildingBoxAssembly", tick, before);
        object movement = Invoke(runtime.Terrain, "PlanMovement", before, tick);
        AssertSuccess(Invoke(runtime.Residents, "Advance", movement));
        AgentViewModel[] after = ((IEnumerable)Invoke(runtime.Residents, "LoadView"))
            .Cast<AgentViewModel>()
            .ToArray();
        AssertSuccess(Invoke(runtime.Terrain, "AdvanceBuildingBoxAssembly", tick, after));
        return after;
    }

    internal static BuildingBoxGhostViewModel FindValidPreview(
        Runtime runtime,
        EntityId stackId,
        BuildingBoxPlacementKind kind)
    {
        object started = Invoke(
            runtime.Terrain,
            "BeginBuildingBoxPlacement",
            stackId.ToString());
        AssertSuccess(started);
        object mode = GetProperty(started, "Value");
        int firstDepth = kind == BuildingBoxPlacementKind.RelocateBox ? 0 : 1;
        int lastDepth = kind == BuildingBoxPlacementKind.RelocateBox ? 0 : 3;
        for (int z = firstDepth; z <= lastDepth; z++)
        {
            for (int y = 1; y < 13; y++)
            {
                for (int x = 1; x < 19; x++)
                {
                    BuildingBoxGhostViewModel preview =
                        (BuildingBoxGhostViewModel)Invoke(
                            runtime.Terrain,
                            "PreviewBuildingBoxPlacement",
                            mode,
                            new CellId(x, y, z),
                            runtime.Agents);
                    if (preview.IsValid && preview.PlacementKind == kind)
                    {
                        return preview;
                    }
                }
            }
        }

        Assert.Fail("No valid BuildingBox preview was found for " + kind + ".");
        throw new InvalidOperationException();
    }

    internal static BuildingWorldViewModel PendingTransformation(
        Runtime runtime,
        EntityId sourceStackId)
    {
        return Buildings(runtime).Single(value =>
            value.SourceBuildingBoxStackId == sourceStackId.ToString()
            && value.IsPendingBuildingBoxLifecycle);
    }

    internal static BuildingWorldViewModel[] Buildings(Runtime runtime)
    {
        return ((IEnumerable)Invoke(runtime.Terrain, "LoadBuildings"))
            .Cast<BuildingWorldViewModel>()
            .ToArray();
    }

    internal static JobSnapshot Job(Runtime runtime, EntityId id)
    {
        return runtime.Jobs.Get(id)!;
    }

    internal static CellId FindSupportedAdjacentCell(Runtime runtime, CellId target)
    {
        WorldViewModel view = (WorldViewModel)Invoke(runtime.World, "LoadView");
        Dictionary<CellId, WorldCellViewModel> cells = view.Chunks
            .SelectMany(value => value.Cells)
            .ToDictionary(value => new CellId(value.X, value.Y, value.Z));
        CellId[] candidates =
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
            new CellId(target.X, target.Y - 1, target.Z),
            new CellId(target.X, target.Y + 1, target.Z),
        };
        return candidates.First(candidate =>
            cells.TryGetValue(candidate, out WorldCellViewModel open)
            && !open.IsSolid
            && cells.TryGetValue(
                new CellId(candidate.X, candidate.Y + 1, candidate.Z),
                out WorldCellViewModel support)
            && support.HasFullActorSupport);
    }

    internal static AgentViewModel AtCell(AgentViewModel source, CellId cell)
    {
        return new AgentViewModel(
            source.Id, source.Name, source.Version, source.IsAlive,
            cell.X, cell.Y, source.Nutrition, source.Alertness, source.Mood,
            source.Health, source.ScheduledActivity, source.ActiveIntent,
            source.ActionElapsedTicks, source.ActionRequiredTicks,
            source.DecisionReason, source.DecisionExplanation, source.UtilityOptions,
            cell.Z, source.AutomaticPlanningEnabled);
    }

    internal static object Invoke(
        object target,
        string name,
        params object[] arguments)
    {
        return RequireMethod(
            target.GetType(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length).Invoke(target, arguments)!;
    }

    internal static void AssertSuccess(object result)
    {
        PropertyInfo? property = result.GetType().GetProperty("IsSuccess");
        Assert.That(property, Is.Not.Null, result.GetType().FullName);
        Assert.That((bool)property!.GetValue(result)!, Is.True, result.ToString());
    }

    private static object InvokeStatic(
        Type type,
        string name,
        params object[] arguments)
    {
        return RequireMethod(
            type,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length).Invoke(null, arguments)!;
    }

    private static MethodInfo RequireMethod(
        Type type,
        BindingFlags flags,
        string name,
        int argumentCount)
    {
        MethodInfo? method = type.GetMethods(flags).SingleOrDefault(value =>
            value.Name == name && value.GetParameters().Length == argumentCount);
        Assert.That(method, Is.Not.Null, type.FullName + "." + name);
        return method!;
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return property!.GetValue(target)!;
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    internal sealed class Runtime
    {
        internal Runtime(
            object world,
            object residents,
            object terrain,
            AgentViewModel[] agents,
            InMemoryInventoryRepository inventoryRepository,
            InMemoryJobRepository jobRepository)
        {
            World = world;
            Residents = residents;
            Terrain = terrain;
            Agents = agents;
            InventoryRepository = inventoryRepository;
            JobRepository = jobRepository;
        }

        internal object World { get; }
        internal object Residents { get; }
        internal object Terrain { get; }
        internal AgentViewModel[] Agents { get; }
        internal InMemoryInventoryRepository InventoryRepository { get; }
        internal InMemoryJobRepository JobRepository { get; }
        internal InventoryState Inventory => InventoryRepository.Get();
        internal JobSystem Jobs => JobRepository.Get();
    }
}

}
