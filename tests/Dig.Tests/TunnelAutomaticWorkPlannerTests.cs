using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelAutomaticWorkPlannerTests
{
    private static readonly ItemId MushroomLeg = new ItemId("material.mushroom_leg");

    [Fact]
    public void Completed_building_range_is_inclusive_at_thirty_xyz_cells()
    {
        CellId target = new CellId(10, 10, 1);

        Assert.True(TunnelAutomaticWorkPlanner.IsWithinCompletedBuildingRange(
            target,
            new[] { new CellId(38, 10, 3) }));
        Assert.False(TunnelAutomaticWorkPlanner.IsWithinCompletedBuildingRange(
            target,
            new[] { new CellId(39, 10, 3) }));
    }

    [Fact]
    public void Source_selection_is_distance_then_cell_then_stack_id()
    {
        InventoryState inventory = CreateInventory();
        EntityId farther = Id(20);
        EntityId higherCell = Id(21);
        EntityId lowerCell = Id(22);
        RequireSuccess(inventory.AddUnit(
            farther,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(1, 0, 0)),
            tick: 0));
        RequireSuccess(inventory.AddUnit(
            higherCell,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(8, 1, 0)),
            tick: 0));
        RequireSuccess(inventory.AddUnit(
            lowerCell,
            MushroomLeg,
            ItemLocation.InWorld(new CellId(8, -1, 0)),
            tick: 0));
        CellId[] visible =
        {
            new CellId(1, 0, 0),
            new CellId(8, 1, 0),
            new CellId(8, -1, 0),
        };

        TunnelAutomaticWorkSource? source = TunnelAutomaticWorkPlanner.SelectSource(
            MushroomLeg,
            new CellId(10, 0, 0),
            inventory.GetAvailableWorldStacks(),
            visible,
            visible);

        Assert.True(source.HasValue);
        Assert.Equal(lowerCell, source.Value.StackId);
    }

    [Fact]
    public void Hidden_unreachable_or_reserved_source_is_not_selected()
    {
        InventoryState inventory = CreateInventory();
        EntityId sourceId = Id(30);
        EntityId otherJobId = Id(31);
        CellId sourceCell = new CellId(2, 0, 0);
        RequireSuccess(inventory.AddUnit(
            sourceId,
            MushroomLeg,
            ItemLocation.InWorld(sourceCell),
            tick: 0));

        Assert.Null(TunnelAutomaticWorkPlanner.SelectSource(
            MushroomLeg,
            new CellId(10, 0, 0),
            inventory.GetAvailableWorldStacks(),
            Array.Empty<CellId>(),
            new[] { sourceCell }));
        Assert.Null(TunnelAutomaticWorkPlanner.SelectSource(
            MushroomLeg,
            new CellId(10, 0, 0),
            inventory.GetAvailableWorldStacks(),
            new[] { sourceCell },
            Array.Empty<CellId>()));

        RequireSuccess(inventory.ReserveQuantity(sourceId, otherJobId, 1, tick: 1));
        Assert.Null(TunnelAutomaticWorkPlanner.SelectSource(
            MushroomLeg,
            new CellId(10, 0, 0),
            inventory.GetAvailableWorldStacks(),
            new[] { sourceCell },
            new[] { sourceCell }));
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(MushroomLeg, "Mushroom leg", 100, isTool: false),
        }));
    }

    private static void RequireSuccess(Result result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
