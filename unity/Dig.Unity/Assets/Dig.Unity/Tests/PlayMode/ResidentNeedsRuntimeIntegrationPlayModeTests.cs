using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ResidentNeedsRuntimeIntegrationPlayModeTests
{
    [Test]
    public void Critical_sleep_walks_to_a_completed_tent_slot_before_recovery()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        ResidentNeedsRuntimePlayModeHarness.AddCompletedTent(
            runtime.World.LoadView(),
            runtime.Terrain);
        runtime.Residents.BindResidentNeedsRuntime(runtime.Terrain);
        runtime.Terrain.InitializeResidentNeedsRuntime(
            runtime.Residents.Tick,
            runtime.Residents.LoadView());

        InMemoryBuildingFacilitiesRepository facilities =
            ResidentNeedsRuntimePlayModeHarness.GetField<
                InMemoryBuildingFacilitiesRepository>(
                    runtime.Terrain,
                    "_residentFacilities");
        BuildingFacilitySnapshot[] slots = facilities.Get().GetAllFacilities()
            .Where(value => value.Definition.Kind == BuildingFacilityKind.Bed)
            .ToArray();
        Assert.That(slots, Has.Length.EqualTo(2));

        AgentState resident = runtime.Residents.Repository.GetAll()
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .First();
        ResidentNeedsRuntimePlayModeHarness.SetNeeds(
            resident,
            nutrition: 9_000,
            alertness: 100,
            mood: 9_000,
            tick: 0);
        runtime.Residents.Repository.Save(resident);
        int initialAlertness = resident.CreateSnapshot(0).Needs.Alertness.Points;

        bool targetedTent = false;
        bool reachedTent = false;
        bool recoveredAtTent = false;
        for (int iteration = 0; iteration < 80; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            AgentState current = runtime.Residents.Repository.Get(resident.Id)!;
            AgentSnapshot snapshot = current.CreateSnapshot(runtime.Residents.Tick);
            AgentActivityTarget? target = snapshot.ActiveAction?.Target;
            BuildingFacilitySnapshot? slot = target.HasValue
                && target.Value.Kind == AgentActivityTargetKind.Bed
                    ? facilities.Get().Get(target.Value.EntityId)
                    : null;
            targetedTent |= slot != null;
            reachedTent |= slot != null && current.Position == slot.Definition.Position;
            recoveredAtTent |= reachedTent
                && snapshot.Needs.Alertness.Points > initialAlertness;
            if (recoveredAtTent)
            {
                break;
            }
        }

        Assert.That(targetedTent, Is.True);
        Assert.That(reachedTent, Is.True);
        Assert.That(recoveredAtTent, Is.True);
    }

    [Test]
    public void Free_time_hunger_breaks_a_food_package_then_picks_up_and_eats_food()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime(materialDurationTicks: 1);
        ResidentNeedsRuntimePlayModeHarness.BuildingRuntime building =
            ResidentNeedsRuntimePlayModeHarness.AddCampfireStock(runtime.Terrain);
        Result queued = runtime.Terrain.EnqueueBuildingProduction(
            building.BuildingId.ToString(),
            CampfireProductionContent.GrilledMushroomRecipeId.ToString(),
            tick: 1);
        Assert.That(queued.IsSuccess, Is.True, queued.Error?.ToString());

        ProductionOutputPackageSnapshot? package = null;
        for (int iteration = 0; iteration < 200; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            package = building.Production.Get().GetOutputPackages()
                .SingleOrDefault(value => value.IsClosed);
            if (package != null)
            {
                break;
            }
        }

        Assert.That(package, Is.Not.Null);
        while ((runtime.Residents.Tick + 1) % 24 != 12)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
        }

        runtime.Residents.BindResidentNeedsRuntime(runtime.Terrain);
        runtime.Terrain.InitializeResidentNeedsRuntime(
            runtime.Residents.Tick,
            runtime.Residents.LoadView());
        AgentState hungry = runtime.Residents.Repository.GetAll()
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .First();
        foreach (AgentState resident in runtime.Residents.Repository.GetAll())
        {
            ResidentNeedsRuntimePlayModeHarness.SetNeeds(
                resident,
                nutrition: resident.Id == hungry.Id ? 100 : 10_000,
                alertness: 10_000,
                mood: 10_000,
                tick: runtime.Residents.Tick);
            runtime.Residents.Repository.Save(resident);
        }

        bool sawPackageUse = false;
        bool sawFoodPickup = false;
        bool nutritionRecovered = false;
        for (int iteration = 0; iteration < 120; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            JobSnapshot[] jobs = runtime.Terrain.LoadJobSnapshots().ToArray();
            sawPackageUse |= jobs.Any(value =>
                value.Definition is ProductionPackageUseJobDefinition);
            sawFoodPickup |= jobs.Any(value =>
                value.Definition is WorldItemPickupJobDefinition pickup
                && pickup.CompletionAction
                    == WorldItemPickupCompletionAction.UseConsumable);
            nutritionRecovered = runtime.Residents.Repository.Get(hungry.Id)!
                .CreateSnapshot(runtime.Residents.Tick)
                .Needs.Nutrition.Points > 100;
            if (sawPackageUse && sawFoodPickup && nutritionRecovered)
            {
                break;
            }
        }

        Assert.That(sawPackageUse, Is.True);
        Assert.That(sawFoodPickup, Is.True);
        Assert.That(nutritionRecovered, Is.True);
        Assert.That(
            building.Inventory.Get().GetStack(package!.StackId),
            Is.Null);
    }

}

}
