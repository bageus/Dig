using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldItemInteractionTests
{
    [Fact]
    public void Presenter_marks_only_configured_BuildingBox_item_as_interactive()
    {
        ItemId boxItemId = new ItemId("test.building_box");
        ItemId toolItemId = new ItemId("test.tool");
        EntityId boxStackId = Id(1);
        EntityId toolStackId = Id(2);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                boxItemId,
                "Test BuildingBox",
                maximumStackSize: 1,
                isTool: false),
            new ItemDefinition(
                toolItemId,
                "Test tool",
                maximumStackSize: 1,
                isTool: true),
        }));
        Assert.True(inventory.AddStack(
            boxStackId,
            boxItemId,
            quantity: 1,
            location: ItemLocation.InWorld(new CellId(3, 4)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            toolStackId,
            toolItemId,
            quantity: 1,
            location: ItemLocation.InWorld(new CellId(4, 4)),
            tick: 1).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);
        GetInventorySnapshotQueryHandler query =
            new GetInventorySnapshotQueryHandler(repository);

        WorldItemViewModel[] projected = new InventoryWorldPresenter(
            query,
            WorldItemInteractionKind.BuildingBox,
            boxItemId).Load().ToArray();
        WorldItemViewModel box = projected.Single(item => item.StackId == boxStackId.ToString());
        WorldItemViewModel tool = projected.Single(item => item.StackId == toolStackId.ToString());

        Assert.True(box.IsBuildingBox);
        Assert.Equal(WorldItemInteractionKind.BuildingBox, box.InteractionKind);
        Assert.False(tool.IsBuildingBox);
        Assert.Equal(WorldItemInteractionKind.None, tool.InteractionKind);
    }

    [Fact]
    public void Presenter_projects_two_box_types_once_and_keeps_other_items_as_pickups()
    {
        ItemId workshopBox = new ItemId("test.box.workshop");
        ItemId campfireBox = new ItemId("test.box.campfire");
        ItemId stone = new ItemId("test.stone");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(workshopBox, "Workshop box", 1, false),
            new ItemDefinition(campfireBox, "Campfire box", 1, false),
            new ItemDefinition(stone, "Stone", 20, false),
        }));
        Assert.True(inventory.AddStack(
            Id(11), workshopBox, 1, ItemLocation.InWorld(new CellId(1, 1)), 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(12), campfireBox, 1, ItemLocation.InWorld(new CellId(2, 1)), 0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(13), stone, 3, ItemLocation.InWorld(new CellId(3, 1)), 0).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);

        WorldItemViewModel[] projected = new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(repository),
            new Dictionary<ItemId, WorldItemInteractionKind>
            {
                [workshopBox] = WorldItemInteractionKind.BuildingBox,
                [campfireBox] = WorldItemInteractionKind.BuildingBox,
            },
            WorldItemInteractionKind.Pickup).Load().ToArray();

        Assert.Equal(3, projected.Length);
        Assert.Equal(3, projected.Select(item => item.StackId).Distinct().Count());
        Assert.All(
            projected.Where(item => item.ItemId != stone.ToString()),
            item => Assert.True(item.IsBuildingBox));
        Assert.True(projected.Single(item => item.ItemId == stone.ToString()).CanPickup);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
