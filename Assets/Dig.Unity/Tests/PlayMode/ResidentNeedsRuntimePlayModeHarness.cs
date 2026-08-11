using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

internal static class ResidentNeedsRuntimePlayModeHarness
{
    internal static Runtime CreateRuntime(long materialDurationTicks = 1)
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession residents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            residents.LoadView(),
            world.Journal,
            residents.SkillGrants);
        terrain.InitializeHauling(world.Journal);
        terrain.InitializeBuildingDemo(world.Journal);
        terrain.InitializeBuildingProductionDemo(
            residents.Repository,
            world.Journal,
            materialDurationTicks);
        return new Runtime(world, residents, terrain);
    }

    internal static void RunTick(Runtime runtime)
    {
        AgentViewModel[] before = runtime.Residents.LoadView().ToArray();
        long nextTick = runtime.Residents.Tick + 1;
        runtime.Terrain.SynchronizeHauling(nextTick, before);
        runtime.Terrain.SynchronizeBuildingProduction(nextTick, before);
        IReadOnlyDictionary<string, CellId> movement =
            runtime.Terrain.PlanMovement(before, nextTick);
        Assert.That(runtime.Residents.Advance(movement).IsSuccess, Is.True);
        AgentViewModel[] after = runtime.Residents.LoadView().ToArray();
        Assert.That(
            runtime.Terrain.AdvanceProductionPackages(
                runtime.Residents.Tick,
                after).IsSuccess,
            Is.True);
        Assert.That(
            runtime.Terrain.AdvanceBuildingProduction(
                runtime.Residents.Tick,
                after).IsSuccess,
            Is.True);
        Assert.That(
            runtime.Terrain.AdvanceWorldItemPickup(
                runtime.Residents.Tick,
                after).IsSuccess,
            Is.True);
        Assert.That(
            runtime.Terrain.SettleWorldItems(runtime.Residents.Tick).IsSuccess,
            Is.True);
    }

    internal static void AddCompletedTent(
        WorldViewModel world,
        DigTerrainWorkSession terrain)
    {
        InMemoryBuildingsRepository repository =
            GetField<InMemoryBuildingsRepository>(terrain, "_buildingsRepository");
        BuildingDefinition definition = CampfireProductionContent.CreateBuildings()
            .Single(value => value.Id == CampfireProductionContent.TentBuildingId);
        Dictionary<CellId, WorldCellViewModel> cells = world.Chunks
            .SelectMany(value => value.Cells)
            .ToDictionary(value => new CellId(value.X, value.Y, value.Z));
        HashSet<CellId> occupied = repository.Get().GetAll()
            .SelectMany(value => value.Footprint)
            .ToHashSet();
        CellId origin = default;
        CellId workPosition = default;
        CellId[] footprint = Array.Empty<CellId>();
        bool found = false;
        foreach (CellId candidate in cells.Keys.OrderBy(value => value))
        {
            CellId[] proposed = definition.ResolveFootprint(
                candidate,
                BuildingOrientation.North).ToArray();
            if (!proposed.All(value => IsSupportedOpen(value, cells, occupied)))
            {
                continue;
            }

            CellId? work = definition.ResolveWorkPositions(
                    candidate,
                    BuildingOrientation.North)
                .Where(value => !proposed.Contains(value))
                .Where(value => IsSupportedOpen(value, cells, occupied))
                .Select(value => (CellId?)value)
                .FirstOrDefault();
            if (!work.HasValue)
            {
                continue;
            }

            origin = candidate;
            footprint = proposed;
            workPosition = work.Value;
            found = true;
            break;
        }

        Assert.That(found, Is.True, "No supported 2x2 Tent placement was found.");
        BuildingSnapshot tent = new BuildingSnapshot(
            EntityId.Parse("ad000000000000000000000000000001"),
            definition,
            origin,
            BuildingOrientation.North,
            footprint,
            workPosition,
            BuildingStatus.Completed,
            definition.RequiredWork,
            definition.MaximumDurability,
            version: 1,
            diagnosticReason: null);
        BuildingsState restored = BuildingsState.RestoreWithPacking(
            repository.Get().GetAll().Append(tent)).Value;
        repository.Save(restored);
    }

    private static bool IsSupportedOpen(
        CellId cell,
        IReadOnlyDictionary<CellId, WorldCellViewModel> cells,
        ISet<CellId> occupied)
    {
        return !occupied.Contains(cell)
            && cells.TryGetValue(cell, out WorldCellViewModel open)
            && !open.IsSolid
            && cells.TryGetValue(
                new CellId(cell.X, cell.Y + 1, cell.Z),
                out WorldCellViewModel support)
            && support.IsSolid;
    }

    internal static BuildingRuntime AddCampfireStock(DigTerrainWorkSession terrain)
    {
        InMemoryInventoryRepository inventory =
            GetField<InMemoryInventoryRepository>(terrain, "_inventoryRepository");
        InMemoryProductionRepository production =
            GetField<InMemoryProductionRepository>(terrain, "_productionRepository");
        EntityId buildingId = terrain.LoadBuildings().Single(value =>
            value.DefinitionId
                == CampfireBuildingBoxContent.CampfireBuildingId.ToString())
            .Functions.BuildingId;
        InventoryState state = inventory.Get();
        AddStock(state, buildingId, CampfireProductionContent.MushroomCapItemId, 4, 1);
        AddStock(state, buildingId, CampfireProductionContent.MushroomLegItemId, 4, 2);
        AddStock(state, buildingId, CampfireProductionContent.StoneItemId, 4, 3);
        inventory.Save(state);
        return new BuildingRuntime(buildingId, inventory, production);
    }

    private static void AddStock(
        InventoryState inventory,
        EntityId buildingId,
        ItemId itemId,
        int quantity,
        int suffix)
    {
        Result result = inventory.AddStack(
            EntityId.Parse((9700 + suffix).ToString("x32")),
            itemId,
            quantity,
            ItemLocation.InBuilding(buildingId),
            tick: 0);
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    internal static void SetNeeds(
        AgentState resident,
        int nutrition,
        int alertness,
        int mood,
        long tick)
    {
        AgentNeedsSnapshot current = resident.CreateSnapshot(tick).Needs;
        Result result = resident.ApplyExternalNeedDelta(
            new NeedDelta(
                nutrition - current.Nutrition.Points,
                alertness - current.Alertness.Points,
                mood - current.Mood.Points,
                0),
            "test.runtime.needs",
            tick);
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    internal static T GetField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    internal readonly struct Runtime
    {
        internal Runtime(
            DigWorldSession world,
            DigAgentSession residents,
            DigTerrainWorkSession terrain)
        {
            World = world;
            Residents = residents;
            Terrain = terrain;
        }

        internal DigWorldSession World { get; }
        internal DigAgentSession Residents { get; }
        internal DigTerrainWorkSession Terrain { get; }
    }

    internal readonly struct BuildingRuntime
    {
        internal BuildingRuntime(
            EntityId buildingId,
            InMemoryInventoryRepository inventory,
            InMemoryProductionRepository production)
        {
            BuildingId = buildingId;
            Inventory = inventory;
            Production = production;
        }

        internal EntityId BuildingId { get; }
        internal InMemoryInventoryRepository Inventory { get; }
        internal InMemoryProductionRepository Production { get; }
    }
}

}
