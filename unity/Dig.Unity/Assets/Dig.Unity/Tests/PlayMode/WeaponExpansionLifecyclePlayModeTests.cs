using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class WeaponExpansionLifecyclePlayModeTests
{
    private static readonly EntityId ResidentId = Id(1);
    private readonly InMemoryExecutionJournal _journal = new InMemoryExecutionJournal();
    private long _tick = 1;

    [Test]
    public void Club_pickup_uses_active_weapon_compartment_and_harness_tier()
    {
        ResidentInventoryExpansionContent expansions =
            new ResidentInventoryExpansionContent();
        InventoryState inventory = new InventoryState(new ItemCatalog(
            expansions.Items.Concat(CombatEquipmentContent.CreateItems())));
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        EntityId sheath = Id(2);
        EntityId harness = Id(3);
        EntityId club = Id(4);
        AddWorld(inventory, sheath,
            ResidentInventoryExpansionContent.SheathItemId, new CellId(1, 0));
        AddWorld(inventory, harness,
            ResidentInventoryExpansionContent.WeaponHarnessItemId, new CellId(2, 0));
        AddWorld(inventory, club,
            CombatEquipmentContent.ClubItemId, new CellId(3, 0));

        Assert.That(
            inventory.GetResidentInventoryLayout(ResidentId).WeaponCapacity,
            Is.Zero);
        PickUp(inventories, jobs, sheath, Id(10), new CellId(1, 0));
        Assert.That(
            inventory.GetResidentInventoryLayout(ResidentId).WeaponCapacity,
            Is.EqualTo(2));

        PickUp(inventories, jobs, club, Id(11), new CellId(3, 0));
        ItemStackSnapshot clubStack = inventory.GetStack(club)!;
        Assert.That(
            clubStack.Location.ResidentCompartment,
            Is.EqualTo(ResidentInventoryCompartment.Weapon));
        Assert.That(clubStack.Location.ResidentSlotIndex, Is.Zero);

        PickUp(inventories, jobs, harness, Id(12), new CellId(2, 0));
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.That(layout.WeaponCapacity, Is.EqualTo(4));
        Assert.That(
            layout.ActiveWeaponExpansion!.Value.StackId,
            Is.EqualTo(harness));
        Assert.That(
            layout.ActiveWeaponExpansion.Value.Definition.Tier,
            Is.EqualTo(2));
        Assert.That(
            inventory.GetStack(club)!.Location.ResidentCompartment,
            Is.EqualTo(ResidentInventoryCompartment.Weapon));
        Assert.That(inventory.GetStack(club)!.Location.ResidentSlotIndex, Is.Zero);
    }

    [Test]
    public void Equipment_policy_creates_distinct_sheath_harness_and_club_geometry()
    {
        GameObject root = new GameObject("Equipment Visual Policy Test");
        try
        {
            AssertParts(
                root.transform,
                ResidentInventoryExpansionContent.SheathItemId.ToString(),
                "Sheath Body",
                "Sheath Mouth");
            AssertParts(
                root.transform,
                ResidentInventoryExpansionContent.WeaponHarnessItemId.ToString(),
                "Weapon Harness Belt",
                "Weapon Harness Buckle");
            AssertParts(
                root.transform,
                CombatEquipmentContent.ClubItemId.ToString(),
                "Club Handle",
                "Club Head");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private void PickUp(
        InMemoryInventoryRepository inventories,
        InMemoryJobRepository jobs,
        EntityId stackId,
        EntityId jobId,
        CellId sourceCell)
    {
        Require(new CreateWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CreateWorldItemPickupCommand(
                jobId,
                stackId,
                ResidentId,
                sourceCell,
                priority: 675,
                tick: _tick++)));
        Advance(jobs, jobId);
        Advance(jobs, jobId);
        Require(new CompleteWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CompleteWorldItemPickupCommand(jobId, _tick++)));
    }

    private void Advance(InMemoryJobRepository jobs, EntityId jobId)
    {
        Require(new AdvanceJobHandler(jobs, _journal).Handle(
            new AdvanceJobCommand(jobId, _tick++)));
    }

    private static void AddWorld(
        InventoryState inventory,
        EntityId stackId,
        ItemId itemId,
        CellId cell)
    {
        Require(inventory.AddUnit(
            stackId,
            itemId,
            ItemLocation.InWorld(cell),
            tick: 0));
    }

    private static void AssertParts(
        Transform parent,
        string itemId,
        params string[] expected)
    {
        DigItemVisualResolution resolution =
            DigWorldItemVisualPolicy.Resolve(catalog: null, itemId);
        Assert.That(resolution.Asset.IsFallback, Is.True);
        Assert.That(resolution.Asset.Tint, Is.Not.EqualTo(Color.magenta));
        GameObject instance = DigBasketVisualPolicy.CreateInstance(
            itemId,
            resolution,
            parent,
            itemId);
        string[] names = instance.GetComponentsInChildren<Transform>(true)
            .Select(value => value.name)
            .ToArray();
        foreach (string part in expected)
        {
            Assert.That(names, Does.Contain(part));
        }
    }

    private static void Require(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
