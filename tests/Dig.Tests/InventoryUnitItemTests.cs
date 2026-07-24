using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class InventoryUnitItemTests
{
    private static readonly ItemId Coal = new ItemId("resource.coal");

    [Fact]
    public void World_batch_creates_one_entity_per_physical_unit()
    {
        InventoryState inventory = CreateInventory();
        EntityId[] ids = UnitIds(10);
        ItemLocation location = ItemLocation.InWorld(new CellId(4, 6, 0));

        Result result = inventory.AddUnits(ids, Coal, location, tick: 0);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(10, inventory.GetTotal(Coal));
        Assert.All(ids, id =>
        {
            ItemStackSnapshot unit = inventory.GetStack(id)!;
            Assert.Equal(1, unit.Quantity);
            Assert.Equal(location, unit.Location);
        });
        Assert.Equal(10, inventory.CreateSnapshot().Stacks.Count);
    }

    [Fact]
    public void Duplicate_unit_id_rejects_entire_batch_without_partial_creation()
    {
        InventoryState inventory = CreateInventory();
        EntityId id = UnitIds(1)[0];

        Result result = inventory.AddUnits(
            new[] { id, id },
            Coal,
            ItemLocation.InWorld(new CellId(1, 2, 0)),
            tick: 0);

        Assert.Equal(InventoryErrors.StackAlreadyExists, result.Error);
        Assert.Empty(inventory.CreateSnapshot().Stacks);
        Assert.Equal(0, inventory.GetTotal(Coal));
    }

    [Fact]
    public void Resident_location_accepts_exactly_one_unit_per_slot_operation()
    {
        InventoryState inventory = CreateInventory();
        EntityId residentId = EntityId.Parse("71000000000000000000000000000001");
        EntityId[] ids = UnitIds(2);

        Result result = inventory.AddUnits(
            ids,
            Coal,
            ItemLocation.InAgent(residentId),
            tick: 0);

        Assert.Equal(InventoryErrors.ResidentInventoryCapacityExceeded, result.Error);
        Assert.All(ids, id => Assert.Null(inventory.GetStack(id)));
    }

    [Fact]
    public void Add_unit_is_the_canonical_single_entity_creation_path()
    {
        InventoryState inventory = CreateInventory();
        EntityId id = UnitIds(1).Single();

        Result result = inventory.AddUnit(
            id,
            Coal,
            ItemLocation.InWorld(new CellId(0, 0, 0)),
            tick: 0);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(1, inventory.GetStack(id)!.Quantity);
    }

    private static InventoryState CreateInventory()
    {
        ItemCategoryId raw = new ItemCategoryId("raw");
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Coal, "Coal", 100, isTool: false, new[] { raw }),
        }));
    }

    private static EntityId[] UnitIds(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => EntityId.Parse($"7000000000000000000000000000{index:D4}"))
            .ToArray();
    }
}

}
