using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class LivingMaterialEcologyPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void FreshDemoSeedsTwoHamstersAndOneGrubAwayFromResidentsExactlyOnce()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agentSession = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        IReadOnlyList<AgentViewModel> residents = agentSession.LoadView();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            residents,
            world.Journal,
            agentSession.SkillGrants);
        terrain.InitializeBuildingDemo(world.Journal);
        terrain.InitializeBuildingProductionDemo(
            agentSession.Repository,
            world.Journal);

        terrain.InitializeLivingMaterials(agentSession.Tick, residents);
        IReadOnlyList<CreatureVisualSnapshot> first =
            terrain.LoadLivingMaterialCreatures();
        terrain.InitializeLivingMaterials(agentSession.Tick, residents);
        IReadOnlyList<CreatureVisualSnapshot> repeated =
            terrain.LoadLivingMaterialCreatures();
        terrain.SynchronizeBuildingProduction(agentSession.Tick + 1, residents);

        Assert.That(first, Has.Count.EqualTo(3));
        Assert.That(first.Count(value => value.SpeciesId == "creature.hamster"),
            Is.EqualTo(2));
        Assert.That(first.Count(value => value.SpeciesId == "creature.grub"),
            Is.EqualTo(1));
        Assert.That(first.Select(value => value.CreatureId).Distinct().Count(),
            Is.EqualTo(3));
        Assert.That(repeated.Select(value => value.CreatureId),
            Is.EqualTo(first.Select(value => value.CreatureId)));

        CellId[] residentCells = residents
            .Where(value => value.IsAlive)
            .Select(value => new CellId(value.CellX, value.CellY, value.CellZ))
            .ToArray();
        Assert.That(first.Select(value => new CellId(
            value.CellX,
            value.CellY,
            value.CellZ)),
            Has.None.Matches<CellId>(cell => residentCells.Contains(cell)));

        var hamsterStock = terrain.LoadAllBuildingProduction()
            .SelectMany(value => value.Stocks)
            .Single(value =>
                value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.That(hamsterStock.Capacity, Is.EqualTo(2));
        Assert.That(hamsterStock.DeliveryEnabled, Is.False);

        var worldHamsters = terrain.LoadAllWorldItems()
            .Where(value => value.ItemId ==
                CampfireProductionContent.HamsterItemId.ToString())
            .ToArray();
        Assert.That(worldHamsters, Has.Length.EqualTo(2));
        Assert.That(worldHamsters, Has.All.Matches<object>(value =>
            ((Dig.Presentation.Inventory.WorldItemViewModel)value).ReservedQuantity == 0));
        Assert.That(terrain.LoadJobSnapshots()
            .Where(value => !value.IsTerminal
                && value.Definition is BuildingSupplyJobDefinition)
            .SelectMany(value =>
                ((BuildingSupplyJobDefinition)value.Definition).RequestedItems)
            .Select(value => value.ItemId),
            Has.None.EqualTo(CampfireProductionContent.HamsterItemId));

        foreach (AgentViewModel resident in residents)
        {
            Assert.That(terrain.LoadResidentInventoryLayout(resident.Id).Slots
                .Where(value => !value.IsEmpty)
                .Select(value => value.ItemId),
                Has.None.EqualTo("creature.hamster"));
            Assert.That(terrain.LoadResidentInventoryLayout(resident.Id).Slots
                .Where(value => !value.IsEmpty)
                .Select(value => value.ItemId),
                Has.None.EqualTo("creature.grub"));
        }
    }

    [UnityTest]
    public IEnumerator FreeRendererUsesApprovedScaleAndDormantCrawlPoses()
    {
        _root = new GameObject("Living material renderer test");
        DigCreatureRenderer renderer = _root.AddComponent<DigCreatureRenderer>();
        LivingMaterialCreatureVisualProjector projector =
            new LivingMaterialCreatureVisualProjector();
        LivingMaterialSnapshot hamster = Snapshot(
            Id(1),
            LivingMaterialSpecies.Hamster,
            LivingMaterialActivity.ReleaseDormant,
            new CellId(4, 3, 0),
            remaining: 1,
            version: 1);
        LivingMaterialSnapshot grub = Snapshot(
            Id(2),
            LivingMaterialSpecies.Grub,
            LivingMaterialActivity.Moving,
            new CellId(6, 3, 0),
            remaining: 0,
            version: 2);

        renderer.Render(
            projector.Project(new[] { hamster, grub }),
            camera: null,
            movementDuration: 0f);
        yield return null;

        DigCreatureVisual[] visuals = _root
            .GetComponentsInChildren<DigCreatureVisual>(true)
            .OrderBy(value => value.Model.SpeciesId, StringComparer.Ordinal)
            .ToArray();
        Assert.That(visuals, Has.Length.EqualTo(2));
        DigCreatureVisual grubVisual = visuals.Single(
            value => value.Model.SpeciesId == "creature.grub");
        DigCreatureVisual hamsterVisual = visuals.Single(
            value => value.Model.SpeciesId == "creature.hamster");
        DigCreatureRig grubRig = grubVisual.GetComponentInChildren<DigCreatureRig>(true);
        DigCreatureRig hamsterRig = hamsterVisual.GetComponentInChildren<DigCreatureRig>(true);

        Assert.That(grubRig.transform.localScale.x, Is.EqualTo(0.20f).Within(0.001f));
        Assert.That(hamsterRig.transform.localScale.x, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(
            Mathf.DeltaAngle(hamsterRig.transform.localEulerAngles.z, 84f),
            Is.EqualTo(0f).Within(0.5f));
        Assert.That(grubVisual.Model.ActivityVariantId, Is.EqualTo("grub.crawling"));
    }

    [Test]
    public void DropDormancyAndFlatMovementRejectVerticalTunnelStep()
    {
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(42);
        EntityId hamsterId = Id(10);
        CellId dropped = new CellId(5, 3, 0);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(
            new CellId(2, 3, 0));
        Assert.That(state.Register(
            hamsterId,
            hamsterId,
            LivingMaterialSpecies.Hamster,
            worldCell: null,
            plane,
            tick: 0).IsSuccess, Is.True);
        Assert.That(state.Release(hamsterId, dropped, plane, tick: 0).IsSuccess, Is.True);
        Assert.That(state.Get(hamsterId)!.Activity,
            Is.EqualTo(LivingMaterialActivity.ReleaseDormant));

        Assert.That(state.AdvanceOneEcologyStep(1).IsSuccess, Is.True);
        Assert.That(state.Get(hamsterId)!.Activity,
            Is.EqualTo(LivingMaterialActivity.Moving));
        for (int index = 0; index < 5; index++)
        {
            Assert.That(state.AdvanceOneEcologyStep(index + 2).IsSuccess, Is.True);
        }

        Result vertical = state.CommitMovement(
            hamsterId,
            new CellId(6, 2, 0),
            plane,
            direction: 1,
            tick: 8);
        Result flat = state.CommitMovement(
            hamsterId,
            new CellId(6, 3, 0),
            plane,
            direction: 1,
            tick: 8);

        Assert.That(vertical.IsFailure, Is.True);
        Assert.That(vertical.Error, Is.EqualTo(LivingMaterialErrors.InvalidMovement));
        Assert.That(flat.IsSuccess, Is.True);
        Assert.That(state.Get(hamsterId)!.Cell, Is.EqualTo(new CellId(6, 3, 0)));
    }

    [UnityTest]
    public IEnumerator CampfireRendererShowsTwoStableIdentityTethers()
    {
        _root = new GameObject("Campfire hamster tether test");
        DigBuildingInternalStockRenderer renderer =
            _root.AddComponent<DigBuildingInternalStockRenderer>();
        EntityId campfireId = Id(90);
        BuildingWorldViewModel building = Building(campfireId);
        LivingMaterialCampfireTetherViewModel[] tethers =
        {
            new LivingMaterialCampfireTetherViewModel(Id(1).ToString(), campfireId.ToString(), 0, 1),
            new LivingMaterialCampfireTetherViewModel(Id(2).ToString(), campfireId.ToString(), 1, 1),
        };

        renderer.RenderLivingMaterialTethers(tethers, new[] { building });
        yield return null;

        Assert.That(renderer.ActiveLivingMaterialTetherCount, Is.EqualTo(2));
        DigLivingMaterialTetherVisual[] visuals = _root
            .GetComponentsInChildren<DigLivingMaterialTetherVisual>(true);
        Assert.That(visuals.Select(value => value.CreatureId),
            Is.EquivalentTo(tethers.Select(value => value.CreatureId)));
        Assert.That(_root.GetComponentsInChildren<Collider>(true)
            .All(value => !value.enabled), Is.True);
    }

    private static LivingMaterialSnapshot Snapshot(
        EntityId id,
        LivingMaterialSpecies species,
        LivingMaterialActivity activity,
        CellId cell,
        int remaining,
        long version)
    {
        return new LivingMaterialSnapshot(
            id,
            id,
            species,
            LivingMaterialContainment.Free,
            cell,
            cell,
            new LivingMaterialPlaneKey(cell),
            direction: 1,
            activity: activity,
            activityStepsRemaining: remaining,
            movementCredit: 0,
            successfulMovementSteps: 0,
            nextSearchAtStep: 4,
            nextSleepAtStep: 16,
            reproductionCyclesCompleted: 0,
            nextReproductionStep: 96,
            deterministicSequence: 0,
            blockedReason: null,
            version: version);
    }

    private static BuildingWorldViewModel Building(EntityId id)
    {
        BuildingFunctionsViewModel functions = new BuildingFunctionsViewModel(
            id,
            CampfireBuildingBoxContent.CampfireBuildingId,
            BuildingStatus.Completed,
            durability: 100,
            maximumDurability: 100,
            isPacking: false,
            packingCompletedWork: 0,
            packingRequiredWork: 0,
            Array.Empty<BuildingFunctionActionViewModel>());
        return new BuildingWorldViewModel(
            id.ToString(),
            CampfireBuildingBoxContent.CampfireBuildingId.ToString(),
            "Campfire",
            originX: 5,
            originY: 3,
            originZ: 0,
            orientation: BuildingOrientation.North,
            workPositionX: 4,
            workPositionY: 3,
            workPositionZ: 0,
            status: BuildingStatus.Completed,
            completedWork: 1,
            requiredWork: 1,
            version: 1,
            footprint: new[] { new BuildingFootprintCellViewModel(5, 3, 0) },
            functions: functions);
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "7500000000000000000000000000" + suffix.ToString("D4"));
}

}
