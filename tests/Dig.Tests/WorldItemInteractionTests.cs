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
    public void Presenter_marks_only_explicit_BuildingBox_projection_as_interactive()
    {
        ItemId itemId = new ItemId("test.building_box");
        EntityId stackId = EntityId.Parse("10000000000000000000000000000001");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                itemId,
                "Test BuildingBox",
                maximumStackSize: 1,
                isTool: false),
        }));
        Assert.True(inventory.AddStack(
            stackId,
            itemId,
            quantity: 1,
            location: ItemLocation.InWorld(new CellId(3, 4)),
            tick: 0).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);
        GetInventorySnapshotQueryHandler query =
            new GetInventorySnapshotQueryHandler(repository);

        WorldItemViewModel generic = new InventoryWorldPresenter(query).Load()[0];
        WorldItemViewModel box = new InventoryWorldPresenter(
            query,
            WorldItemInteractionKind.BuildingBox).Load()[0];

        Assert.False(generic.IsBuildingBox);
        Assert.Equal(WorldItemInteractionKind.None, generic.InteractionKind);
        Assert.True(box.IsBuildingBox);
        Assert.Equal(WorldItemInteractionKind.BuildingBox, box.InteractionKind);
        Assert.Equal(stackId.ToString(), box.StackId);
        Assert.Equal(3, box.CellX);
        Assert.Equal(4, box.CellY);
    }
}

}
