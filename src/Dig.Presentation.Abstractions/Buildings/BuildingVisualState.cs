using Dig.Domain.Buildings;

namespace Dig.Presentation.Buildings
{

public enum BuildingVisualState
{
    BuildingBox = 0,
    Assembly = 1,
    Completed = 2,
    Damaged = 3,
    Packing = 4,
}

public static class BuildingVisualStateResolver
{
    public static BuildingVisualState Resolve(
        BuildingStatus status,
        bool isPacking)
    {
        if (isPacking)
        {
            return BuildingVisualState.Packing;
        }

        return status switch
        {
            BuildingStatus.AwaitingBox => BuildingVisualState.BuildingBox,
            BuildingStatus.AwaitingMaterials => BuildingVisualState.Assembly,
            BuildingStatus.ReadyToBuild => BuildingVisualState.Assembly,
            BuildingStatus.UnderConstruction => BuildingVisualState.Assembly,
            BuildingStatus.ReadyToComplete => BuildingVisualState.Assembly,
            BuildingStatus.Damaged => BuildingVisualState.Damaged,
            _ => BuildingVisualState.Completed,
        };
    }
}

}
