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
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class WeaponExpansionWorkflowTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId SheathStackId = Id(2);
    private static readonly EntityId HarnessStackId = Id(3);
    private static readonly EntityId ClubStackId = Id(4);
    private readonly InMemoryExecutionJournal _journal = new InMemoryExecutionJournal();
    private long _tick = 1;

    [Fact]
    public void Surface_sheath_then_club_uses_weapon_slot_before_main_or_cargo()
    {
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();

        PickUp(inventories, jobs, SheathStackId, Id(10), new CellId(1, 0));

        ResidentInventoryLayoutSnapshot sheathLayout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.Equal(2, sheathLayout.WeaponCapacity);
        Assert.Equal(ResidentInventoryCompartment.Main,
            inventory.GetStack(SheathStackId)!.Location.ResidentCompartment);
        Assert.Equal(0, inventory.GetStack(SheathStackId)!.Location.ResidentSlotIndex);

        PickUp(inventories, jobs, ClubStackId, Id(11), new CellId(3, 0));

        ItemStackSnapshot club = inventory.GetStack(ClubStackId)!;
        Assert.Equal(ResidentInventoryCompartment.Weapon,
            club.Location.ResidentCompartment);
        Assert.Equal(0, club.Location.ResidentSlotIndex);
        Assert.True(sheathLayout.Slots.Single(value =>
            value.Slot.Compartment == ResidentInventoryCompartment.Main
            && value.Slot.Index == 1).IsEmpty);
        Assert.Equal(1d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));
    }

    [Fact]
    public void Harness_tier_expands_to_four_slots_and_spill_reactivates_sheath()
    {
        InventoryState inventory = CreateInventory();
        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();

        PickUp(inventories, jobs, SheathStackId, Id(20), new CellId(1, 0));
        PickUp(inventories, jobs, ClubStackId, Id(21), new CellId(3, 0));
        PickUp(inventories, jobs, HarnessStackId, Id(22), new CellId(2, 0));

        ResidentInventoryLayoutSnapshot harnessLayout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.Equal(4, harnessLayout.WeaponCapacity);
        Assert.Equal(2, harnessLayout.ActiveWeaponExpansion!.Value.Definition.Tier);
        Assert.Equal(HarnessStackId, harnessLayout.ActiveWeaponExpansion.Value.StackId);
        Assert.Equal(ResidentInventoryCompartment.Weapon,
            inventory.GetStack(ClubStackId)!.Location.ResidentCompartment);
        Assert.Equal(0, inventory.GetStack(ClubStackId)!.Location.ResidentSlotIndex);

        ResidentInventoryAttachmentViewModel attachment = Assert.Single(
            new ResidentInventoryAttachmentPresenter().Present(inventory));
        Assert.Equal(
            ResidentInventoryExpansionContent.WeaponHarnessItemId.ToString(),
            attachment.ItemId);

        CellId target = new CellId(5, 0);
        Result dropped = inventory.DropResidentStackWithSpill(
            HarnessStackId,
            ItemLocation.InWorld(target),
            tick: _tick++);

        Assert.True(dropped.IsSuccess, dropped.Error?.ToString());
        ResidentInventoryLayoutSnapshot fallbackLayout =
            inventory.GetResidentInventoryLayout(ResidentId);
        Assert.Equal(2, fallbackLayout.WeaponCapacity);
        Assert.Equal(1, fallbackLayout.ActiveWeaponExpansion!.Value.Definition.Tier);
        Assert.Equal(SheathStackId, fallbackLayout.ActiveWeaponExpansion.Value.StackId);
        Assert.Equal(ItemLocation.InWorld(target),
            inventory.GetStack(HarnessStackId)!.Location);
        Assert.Equal(ItemLocation.InWorld(target),
            inventory.GetStack(ClubStackId)!.Location);
        Assert.Equal(3, inventory.CreateSnapshot().Stacks.Count);

        ResidentInventoryAttachmentViewModel fallbackAttachment = Assert.Single(
            new ResidentInventoryAttachmentPresenter().Present(inventory));
        Assert.Equal(
            ResidentInventoryExpansionContent.SheathItemId.ToString(),
            fallbackAttachment.ItemId);
    }

    private void PickUp(
        InMemoryInventoryRepository inventories,
        InMemoryJobRepository jobs,
        EntityId stackId,
        EntityId jobId,
        CellId sourceCell)
    {
        Result created = new CreateWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CreateWorldItemPickupCommand(
                jobId,
                stackId,
                ResidentId,
                sourceCell,
                priority: 675,
                tick: _tick++));
        Assert.True(created.IsSuccess, created.Error?.ToString());
        Advance(jobs, jobId);
        Advance(jobs, jobId);
        Result completed = new CompleteWorldItemPickupHandler(
            inventories,
            jobs,
            _journal).Handle(new CompleteWorldItemPickupCommand(jobId, _tick++));
        Assert.True(completed.IsSuccess, completed.Error?.ToString());
    }

    private void Advance(InMemoryJobRepository jobs, EntityId jobId)
    {
        Result advanced = new AdvanceJobHandler(jobs, _journal).Handle(
            new AdvanceJobCommand(jobId, _tick++));
        Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
    }

    private static InventoryState CreateInventory()
    {
        ResidentInventoryExpansionContent expansions =
            new ResidentInventoryExpansionContent();
        InventoryState inventory = new InventoryState(new ItemCatalog(
            expansions.Items.Concat(CombatEquipmentContent.CreateItems())));
        Assert.True(inventory.AddUnit(
            SheathStackId,
            ResidentInventoryExpansionContent.SheathItemId,
            ItemLocation.InWorld(new CellId(1, 0)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddUnit(
            HarnessStackId,
            ResidentInventoryExpansionContent.WeaponHarnessItemId,
            ItemLocation.InWorld(new CellId(2, 0)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddUnit(
            ClubStackId,
            CombatEquipmentContent.ClubItemId,
            ItemLocation.InWorld(new CellId(3, 0)),
            tick: 0).IsSuccess);
        return inventory;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
