using System;

namespace Dig.Presentation.Input
{

public enum GameplayHudContextMode
{
    Excavation = 0,
    ResidentInventory = 1,
    BuildingFunctions = 2,
    BuildingPlacement = 3,
    HiddenForResidentGroup = 4,
}

public readonly struct GameplayHudContextState
{
    public GameplayHudContextState(
        int selectedResidentCount,
        bool completedBuildingSelected,
        bool buildingPlacementActive)
    {
        if (selectedResidentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedResidentCount));
        }

        SelectedResidentCount = selectedResidentCount;
        CompletedBuildingSelected = completedBuildingSelected;
        BuildingPlacementActive = buildingPlacementActive;
    }

    public int SelectedResidentCount { get; }
    public bool CompletedBuildingSelected { get; }
    public bool BuildingPlacementActive { get; }
}

public static class GameplayHudContextResolver
{
    public static GameplayHudContextMode Resolve(GameplayHudContextState state)
    {
        if (state.BuildingPlacementActive)
        {
            return GameplayHudContextMode.BuildingPlacement;
        }

        if (state.CompletedBuildingSelected)
        {
            return GameplayHudContextMode.BuildingFunctions;
        }

        if (state.SelectedResidentCount > 1)
        {
            return GameplayHudContextMode.HiddenForResidentGroup;
        }

        return state.SelectedResidentCount == 1
            ? GameplayHudContextMode.ResidentInventory
            : GameplayHudContextMode.Excavation;
    }
}

}