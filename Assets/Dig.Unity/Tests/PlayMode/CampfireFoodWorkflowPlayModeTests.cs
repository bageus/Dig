using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Production;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CampfireFoodWorkflowPlayModeTests
{
    [Test]
    public void Missing_cap_creates_large_mushroom_dependency_then_world_supply()
    {
        Runtime runtime = Runtime.Create();
        Invoke(runtime.Terrain, "InitializeBuildingDemo", runtime.Journal);
        Invoke(
            runtime.Terrain,
            "InitializeBuildingProductionDemo",
            GetProperty(runtime.Residents, "Repository"),
            runtime.Journal);
        Invoke(runtime.Terrain, "InitializeMushroomDemo", 0L);
        AssertSuccess(Invoke(runtime.Terrain, "AdvanceMushrooms", 3L, runtime.Agents));
        BuildingProductionViewModel campfire = ((IEnumerable)Invoke(
                runtime.Terrain,
                "LoadAllBuildingProduction"))
            .Cast<BuildingProductionViewModel>()
            .Single();
        AssertSuccess(Invoke(
            runtime.Terrain,
            "EnqueueBuildingProduction",
            campfire.BuildingId.ToString(),
            CampfireProductionContent.GrilledMushroomRecipeId.ToString(),
            4L));

        Invoke(runtime.Terrain, "SynchronizeBuildingProduction", 4L, runtime.Agents);

        JobSnapshot chopJob = ActiveJobs(runtime.Terrain)
            .Single(value => value.Definition is MushroomChopJobDefinition);
        MushroomChopJobDefinition chop = (MushroomChopJobDefinition)chopJob.Definition;
        MushroomSiteSnapshot site = ((IEnumerable)Invoke(
                runtime.Terrain,
                "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .Single(value => value.SiteId == chop.SiteId);
        object arrive = GetField(runtime.Terrain, "_arriveAtMushroom");
        object swing = GetField(runtime.Terrain, "_completeMushroomSwing");
        object complete = GetField(runtime.Terrain, "_completeMushroomChop");
        Assembly application = arrive.GetType().Assembly;
        AssertSuccess(Invoke(
            arrive,
            "Handle",
            Create(application, "Dig.Application.Ecology.ArriveAtMushroomCommand", chopJob.Id, 5L)));
        for (int index = 0; index < site.RequiredSwings; index++)
        {
            AssertSuccess(Invoke(
                swing,
                "Handle",
                Create(
                    application,
                    "Dig.Application.Ecology.CompleteMushroomSwingCommand",
                    chopJob.Id,
                    6L + index)));
        }

        long completedTick = 7L + site.RequiredSwings;
        AssertSuccess(Invoke(
            complete,
            "Handle",
            Create(
                application,
                "Dig.Application.Ecology.CompleteMushroomChopCommand",
                chopJob.Id,
                Id(900),
                completedTick)));
        runtime.RefreshAgents();
        Invoke(
            runtime.Terrain,
            "SynchronizeBuildingProduction",
            completedTick + 1L,
            runtime.Agents);

        Assert.That(
            ActiveJobs(runtime.Terrain).Count(value =>
                value.Definition is BuildingSupplyJobDefinition),
            Is.EqualTo(1));
        Assert.That(
            runtime.Inventory.Get().CreateSnapshot().Stacks.Count(value =>
                value.ItemId == CampfireProductionContent.MushroomCapItemId
                && value.Location.Kind == ItemLocationKind.World),
            Is.EqualTo(2));
    }

    [Test]
    public void Pickup_then_use_starts_one_eat_action_and_finishes_three_bites()
    {
        Runtime runtime = Runtime.Create();
        Invoke(runtime.Terrain, "InitializeBuildingDemo", runtime.Journal);
        Invoke(
            runtime.Terrain,
            "InitializeBuildingProductionDemo",
            GetProperty(runtime.Residents, "Repository"),
            runtime.Journal);
        AgentViewModel worker = runtime.Agents[0];
        CellId cell = new CellId(worker.CellX, worker.CellY, worker.CellZ);
        EntityId stackId = Id(901);
        Assert.That(runtime.Inventory.Get().AddStack(
            stackId,
            CampfireProductionContent.GrilledMushroomItemId,
            1,
            ItemLocation.InWorld(cell),
            tick: 1).IsSuccess,
            Is.True);
        EntityId residentId = EntityId.Parse(worker.Id);
        AgentState resident = runtime.AgentRepository.Get(residentId)!;
        int nutritionBefore = resident.CreateSnapshot(1).Needs.Nutrition.Points;

        AssertSuccess(Invoke(
            runtime.Terrain,
            "CreateWorldItemPickup",
            stackId.ToString(),
            worker.Id,
            cell,
            2L,
            true));
        JobSnapshot pickup = ActiveJobs(runtime.Terrain)
            .Single(value => value.Definition is WorldItemPickupJobDefinition);
        Assert.That(
            ((WorldItemPickupJobDefinition)pickup.Definition).CompletionAction,
            Is.EqualTo(WorldItemPickupCompletionAction.UseConsumable));

        long pickupTick = 3;
        while (!resident.HasActiveFoodMeal && pickupTick <= 6)
        {
            AssertSuccess(Invoke(
                runtime.Terrain,
                "AdvanceWorldItemPickup",
                pickupTick,
                runtime.Agents));
            pickupTick++;
        }

        Assert.That(resident.HasActiveFoodMeal, Is.True);
        Assert.That(
            resident.CreateSnapshot(pickupTick).ActiveAction!.Value.IntentKind,
            Is.EqualTo(AgentIntentKind.Eat));
        Assert.That(runtime.Inventory.Get().GetStack(stackId), Is.Null);
        Assert.That(resident.AdvanceFoodMealBite(pickupTick).Value, Is.False);
        Assert.That(resident.AdvanceFoodMealBite(pickupTick + 1).Value, Is.False);
        Assert.That(resident.AdvanceFoodMealBite(pickupTick + 2).Value, Is.True);
        Assert.That(
            resident.CreateSnapshot(pickupTick + 2).Needs.Nutrition.Points,
            Is.EqualTo(Math.Min(NeedValue.Maximum, nutritionBefore + 1_500)));
        Assert.That(resident.HasActiveFoodMeal, Is.False);
        Assert.That(
            AllJobs(runtime.Terrain).Single(value => value.Id == pickup.Id).Status,
            Is.EqualTo(JobStatus.Completed));
    }

    private static JobSnapshot[] ActiveJobs(object terrain)
    {
        return AllJobs(terrain).Where(value => !value.IsTerminal).ToArray();
    }

    private static JobSnapshot[] AllJobs(object terrain)
    {
        object repository = GetField(terrain, "_jobRepository");
        object jobs = Invoke(repository, "Get");
        return ((IEnumerable)Invoke(jobs, "GetAll"))
            .Cast<JobSnapshot>()
            .ToArray();
    }

    private sealed class Runtime
    {
        private Runtime(
            object residents,
            object terrain,
            object journal,
            InMemoryInventoryRepository inventory,
            InMemoryAgentRepository agentRepository,
            IReadOnlyList<AgentViewModel> agents)
        {
            Residents = residents;
            Terrain = terrain;
            Journal = journal;
            Inventory = inventory;
            AgentRepository = agentRepository;
            Agents = agents;
        }

        internal object Residents { get; }
        internal object Terrain { get; }
        internal object Journal { get; }
        internal InMemoryInventoryRepository Inventory { get; }
        internal InMemoryAgentRepository AgentRepository { get; }
        internal IReadOnlyList<AgentViewModel> Agents { get; private set; }

        internal static Runtime Create()
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
            IReadOnlyList<AgentViewModel> agents = ((IEnumerable)Invoke(
                    residents,
                    "LoadView"))
                .Cast<AgentViewModel>()
                .ToArray();
            object terrain = InvokeStatic(
                RequireType(assembly, "Dig.Unity.DigTerrainWorkSession"),
                "CreateDemo",
                world,
                agents,
                journal,
                GetProperty(residents, "SkillGrants"));
            return new Runtime(
                residents,
                terrain,
                journal,
                (InMemoryInventoryRepository)GetField(terrain, "_inventoryRepository"),
                (InMemoryAgentRepository)GetProperty(residents, "Repository"),
                agents);
        }

        internal void RefreshAgents()
        {
            Agents = ((IEnumerable)Invoke(Residents, "LoadView"))
                .Cast<AgentViewModel>()
                .ToArray();
        }
    }

    private static object Create(Assembly assembly, string typeName, params object[] args)
    {
        object? value = Activator.CreateInstance(RequireType(assembly, typeName), args);
        Assert.That(value, Is.Not.Null, typeName);
        return value!;
    }

    private static void AssertSuccess(object result)
    {
        PropertyInfo property = result.GetType().GetProperty("IsSuccess")!;
        Assert.That((bool)property.GetValue(result)!, Is.True, result.ToString());
    }

    private static object GetField(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field!.GetValue(target)!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return property!.GetValue(target)!;
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        return RequireMethod(target.GetType(), name, args.Length, false)
            .Invoke(target, args)!;
    }

    private static object InvokeStatic(Type type, string name, params object[] args)
    {
        return RequireMethod(type, name, args.Length, true).Invoke(null, args)!;
    }

    private static MethodInfo RequireMethod(
        Type type,
        string name,
        int count,
        bool isStatic)
    {
        MethodInfo? method = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance))
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == count);
        Assert.That(method, Is.Not.Null, name);
        return method!;
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
