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

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
