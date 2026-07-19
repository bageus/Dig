using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{

public sealed class GameplayHudContextResolverTests
{
    [Fact]
    public void Placement_has_priority_over_building_and_resident_selection()
    {
        GameplayHudContextMode mode = GameplayHudContextResolver.Resolve(
            new GameplayHudContextState(
                selectedResidentCount: 1,
                completedBuildingSelected: true,
                buildingPlacementActive: true));

        Assert.Equal(GameplayHudContextMode.BuildingPlacement, mode);
    }

    [Fact]
    public void Completed_building_replaces_resident_inventory()
    {
        GameplayHudContextMode mode = GameplayHudContextResolver.Resolve(
            new GameplayHudContextState(
                selectedResidentCount: 1,
                completedBuildingSelected: true,
                buildingPlacementActive: false));

        Assert.Equal(GameplayHudContextMode.BuildingFunctions, mode);
    }

    [Fact]
    public void Exactly_one_resident_shows_inventory()
    {
        GameplayHudContextMode mode = GameplayHudContextResolver.Resolve(
            new GameplayHudContextState(
                selectedResidentCount: 1,
                completedBuildingSelected: false,
                buildingPlacementActive: false));

        Assert.Equal(GameplayHudContextMode.ResidentInventory, mode);
    }

    [Fact]
    public void Resident_group_hides_bottom_context_panel()
    {
        GameplayHudContextMode mode = GameplayHudContextResolver.Resolve(
            new GameplayHudContextState(
                selectedResidentCount: 4,
                completedBuildingSelected: false,
                buildingPlacementActive: false));

        Assert.Equal(GameplayHudContextMode.HiddenForResidentGroup, mode);
    }

    [Fact]
    public void No_context_selection_shows_excavation_palette()
    {
        GameplayHudContextMode mode = GameplayHudContextResolver.Resolve(
            new GameplayHudContextState(
                selectedResidentCount: 0,
                completedBuildingSelected: false,
                buildingPlacementActive: false));

        Assert.Equal(GameplayHudContextMode.Excavation, mode);
    }
}

}