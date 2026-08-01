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
public sealed class BasketInventoryPlacementTests
{
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId BasketStackId = Id(2);
    private static readonly EntityId CargoStackId = Id(3);
    private static readonly EntityId PlacementJobId = Id(4);
    private static readonly ItemId CargoItemId = new ItemId("material.basket-placement");
    private static readonly ItemId FillerItemId = new ItemId("material.basket-filler");
    private static readonly CellId Destination = new CellId(4, 4, 0);

    [Fact]
    public void Planned_active_basket_drop_spills_cargo_and_removes_capacity_atomically()
    {
        ResidentInventoryExpansionContent content = new ResidentInventoryExpansionContent();
        InventoryState inventory = new InventoryState(new ItemCatalog(content.Items.Concat(new[]
        {
            new ItemDefinition(
                CargoItemId,
                "Cargo",
                maximumStackSize: 100,
                isTool: false,
                new[] { ResidentInventoryExpansionContent.RawMaterialCategoryId }),
            new ItemDefinition(
                FillerItemId,
                "Filler",
                maximumStackSize: 1,
                isTool: false,
                new[] { ResidentInventoryExpansionContent.GeneralItemCategoryId }),
        })));
        Assert.True(inventory.AddUnit(
            BasketStackId,
            ResidentInventoryExpansionContent.BasketItemId,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Main,
                0),
            tick: 0).IsSuccess);
        for (int slot = 1; slot < ResidentInventoryLayoutSnapshot.MainSlotCount; slot++)
        {
            Assert.True(inventory.AddUnit(
                Id(10 + slot),
                FillerItemId,
                ItemLocation.InResidentSlot(
                    ResidentId,
                    ResidentInventoryCompartment.Main,
                    slot),
                tick: slot).IsSuccess);
        }
        Assert.True(inventory.AddStack(
            CargoStackId,
            CargoItemId,
            quantity: 7,
            ItemLocation.InResidentSlot(
                ResidentId,
                ResidentInventoryCompartment.Cargo,
                0),
            tick: 7).IsSuccess);

        InMemoryInventoryRepository inventories = new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        InMemoryWorldRepository world = new InMemoryWorldRepository(
            BuildingBoxPlacementTestWorld.SupportedState(new[] { Destination }));
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        Result created = new CreateResidentInventoryPlacementHandler(
            world,
            inventories,
            jobs,
            journal).Handle(new CreateResidentInventoryPlacementCommand(
                PlacementJobId,
                ResidentId,
                BasketStackId,
                quantity: 1,
                Destination,
                new[] { Destination },
                priority: 700,
                tick: 10));
        Assert.True(created.IsSuccess, created.Error?.ToString());
        Assert.Equal(1, inventory.GetStack(BasketStackId)!.ReservedQuantity);

        AdvanceJobHandler advance = new AdvanceJobHandler(jobs, journal);
        Assert.True(advance.Handle(new AdvanceJobCommand(PlacementJobId, tick: 11)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(PlacementJobId, tick: 12)).IsSuccess);
        Result completed = new CompleteResidentInventoryPlacementHandler(
            world,
            inventories,
            jobs,
            journal).Handle(new CompleteResidentInventoryPlacementCommand(
                PlacementJobId,
                Destination,
                tick: 13));

        Assert.True(completed.IsSuccess, completed.Error?.ToString());
        Assert.Equal(JobStatus.Completed, jobs.Get().Get(PlacementJobId)!.Status);
        Assert.Equal(ItemLocation.InWorld(Destination), inventory.GetStack(BasketStackId)!.Location);
        Assert.Equal(ItemLocation.InWorld(Destination), inventory.GetStack(CargoStackId)!.Location);
        Assert.Equal(0, inventory.GetStack(BasketStackId)!.ReservedQuantity);
        Assert.Equal(7, inventory.GetTotal(CargoItemId));
        Assert.Equal(0, new ResidentInventoryLayoutPresenter()
            .Present(inventory, ResidentId).CargoCapacity);
        Assert.Equal(1d, inventory.GetResidentMoveSpeedMultiplier(ResidentId));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
