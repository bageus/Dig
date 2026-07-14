using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests;

public sealed class SettlementAllocationQueryTests
{
    [Fact]
    public void Food_query_keeps_location_and_id_tie_break_and_reservation_visibility()
    {
        ItemCategoryId food = new ItemCategoryId("food");
        ItemId meal = new ItemId("food.meal");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(meal, "Meal", 10, isTool: false, new[] { food }),
        }));
        EntityId later = EntityId.Parse("d2000000000000000000000000000002");
        EntityId earlier = EntityId.Parse("d2000000000000000000000000000001");
        EntityId owner = EntityId.Parse("d3000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            later,
            meal,
            quantity: 1,
            ItemLocation.InWorld(new CellId(2, 1)),
            tick: 0).IsSuccess);
        Assert.True(inventory.AddStack(
            earlier,
            meal,
            quantity: 1,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);

        Assert.Equal(earlier, inventory.FindFirstAvailableStackId(food));
        Assert.True(inventory.ReserveQuantity(earlier, owner, quantity: 1, tick: 1).IsSuccess);
        Assert.True(inventory.HasAvailableCategory(food, owner));
        Assert.True(inventory.HasReservation(earlier, owner));
        Assert.Equal(later, inventory.FindFirstAvailableStackId(food));
    }

    [Fact]
    public void Facility_query_keeps_position_building_and_id_tie_break()
    {
        BuildingFacilitiesState facilities = new BuildingFacilitiesState();
        EntityId firstBuilding = EntityId.Parse("d4000000000000000000000000000001");
        EntityId secondBuilding = EntityId.Parse("d4000000000000000000000000000002");
        EntityId firstFacility = EntityId.Parse("d5000000000000000000000000000001");
        EntityId secondFacility = EntityId.Parse("d5000000000000000000000000000002");
        EntityId agent = EntityId.Parse("d6000000000000000000000000000001");
        Assert.True(facilities.Add(new BuildingFacilityDefinition(
            secondFacility,
            secondBuilding,
            BuildingFacilityKind.Bed,
            new CellId(2, 2))).IsSuccess);
        Assert.True(facilities.Add(new BuildingFacilityDefinition(
            firstFacility,
            firstBuilding,
            BuildingFacilityKind.Bed,
            new CellId(2, 2))).IsSuccess);

        Assert.Equal(
            firstFacility,
            facilities.FindFirstAvailableId(BuildingFacilityKind.Bed, agent));
        Assert.True(facilities.Reserve(firstFacility, agent, tick: 1).IsSuccess);
        Assert.True(facilities.HasAvailable(BuildingFacilityKind.Bed, agent));
        Assert.True(facilities.IsReservedBy(
            firstFacility,
            agent,
            BuildingFacilityKind.Bed));
    }
}
