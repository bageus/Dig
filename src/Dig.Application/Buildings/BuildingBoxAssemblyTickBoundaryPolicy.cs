using System;

namespace Dig.Application.Buildings
{

public enum BuildingBoxAssemblyTickDisposition
{
    ContinueCurrentTick = 0,
    StopCurrentTick = 1,
    Completed = 2,
}

public static class BuildingBoxAssemblyTickBoundaryPolicy
{
    public static BuildingBoxAssemblyTickDisposition AfterSuccessfulStep(
        BuildingBoxAssemblyExecutionStepKind step)
    {
        return step switch
        {
            BuildingBoxAssemblyExecutionStepKind.CommitBoxToSite =>
                BuildingBoxAssemblyTickDisposition.StopCurrentTick,
            BuildingBoxAssemblyExecutionStepKind.AddWork =>
                BuildingBoxAssemblyTickDisposition.StopCurrentTick,
            BuildingBoxAssemblyExecutionStepKind.CompleteAssembly =>
                BuildingBoxAssemblyTickDisposition.Completed,
            BuildingBoxAssemblyExecutionStepKind.None =>
                throw new ArgumentOutOfRangeException(nameof(step)),
            _ => BuildingBoxAssemblyTickDisposition.ContinueCurrentTick,
        };
    }
}

}
