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
using UnityEngine;

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
    public void Active_floor_sleeper_survives_and_remains_rostered_and_selectable()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        runtime.Residents.BindResidentNeedsRuntime(runtime.Terrain);
        runtime.Terrain.InitializeResidentNeedsRuntime(
            runtime.Residents.Tick,
            runtime.Residents.LoadView());

        AgentState sleeper = runtime.Residents.Repository.GetAll()
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .First();
        foreach (AgentState resident in runtime.Residents.Repository.GetAll())
        {
            AgentNeedsSnapshot current = resident.CreateSnapshot(0).Needs;
            int health = resident.Id == sleeper.Id ? 1_100 : current.Health.Points;
            Result applied = resident.ApplyExternalNeedDelta(
                new NeedDelta(
                    9_000 - current.Nutrition.Points,
                    (resident.Id == sleeper.Id ? 100 : 10_000)
                        - current.Alertness.Points,
                    9_000 - current.Mood.Points,
                    health - current.Health.Points),
                "test.runtime.sleep_visibility",
                tick: 0);
            Assert.That(applied.IsSuccess, Is.True, applied.Error?.ToString());
            runtime.Residents.Repository.Save(resident);
        }

        AgentSnapshot? activeSleep = null;
        for (int iteration = 0; iteration < 12; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            AgentSnapshot snapshot = runtime.Residents.Repository.Get(sleeper.Id)!
                .CreateSnapshot(runtime.Residents.Tick);
            if (snapshot.IsAlive
                && snapshot.ActiveAction.HasValue
                && snapshot.ActiveAction.Value.IntentKind == AgentIntentKind.Sleep
                && snapshot.ActiveAction.Value.ElapsedTicks > 0)
            {
                activeSleep = snapshot;
                break;
            }
        }

        Assert.That(activeSleep, Is.Not.Null);
        AgentSnapshot sleepingSnapshot = activeSleep!;
        Assert.That(sleepingSnapshot.IsAlive, Is.True);
        Assert.That(sleepingSnapshot.ActiveAction.HasValue, Is.True);
        AgentActionSnapshot sleepingAction = sleepingSnapshot.ActiveAction!.Value;
        Assert.That(sleepingAction.Target.HasValue, Is.True);
        Assert.That(sleepingAction.Target!.Value.Kind,
            Is.EqualTo(AgentActivityTargetKind.FloorSleep));

        string sleeperId = sleeper.Id.ToString();
        ResidentRosterViewModel roster = runtime.Residents.LoadResidentRoster(
            runtime.Terrain.LoadJobSnapshots().ToArray(),
            sleeperId);
        ResidentRosterRowViewModel row = roster.Rows.Single(value =>
            value.Id == sleeperId);
        Assert.That(row.IsAlive, Is.True);
        Assert.That(row.IsExpanded, Is.True);
        Assert.That(row.Activity.Kind, Is.EqualTo(ResidentActivityKind.Sleep));

        GameObject rendererObject = new GameObject("SleepingResidentRenderer");
        try
        {
            DigAgentRenderer renderer = rendererObject.AddComponent<DigAgentRenderer>();
            renderer.Render(runtime.Residents.LoadView().ToArray(), movementDuration: 0f);

            Assert.That(renderer.GetHudModels().Any(value => value.Id == sleeperId),
                Is.True);
            Assert.That(renderer.SelectById(sleeperId), Is.Not.Null);
            Assert.That(renderer.SelectedAgentId, Is.EqualTo(sleeperId));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rendererObject);
        }

        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
        AgentSnapshot afterRecovery = runtime.Residents.Repository.Get(sleeper.Id)!
            .CreateSnapshot(runtime.Residents.Tick);

        Assert.That(afterRecovery.IsAlive, Is.True);
        Assert.That(afterRecovery.Needs.Health.Points, Is.GreaterThan(0));
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
