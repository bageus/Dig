using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Presentation.Buildings;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private static IReadOnlyDictionary<string, BuildingWorldViewModel>
        IndexPendingBuildingBoxTransformations(
            IReadOnlyList<BuildingWorldViewModel> buildings)
    {
        return buildings
            .Where(value => value.IsPendingBuildingBoxLifecycle)
            .ToDictionary(
                value => value.SourceBuildingBoxStackId!,
                value => value,
                StringComparer.Ordinal);
    }

    private static string FormatBuildingBoxTransformationLabel(
        BuildingWorldViewModel transformation,
        string location)
    {
        return transformation.Name
            + " · " + FormatBuildingBoxTransformationStatus(transformation)
            + " · " + location
            + $" · Cell {transformation.OriginX},{transformation.OriginY}, Z"
            + transformation.OriginZ;
    }

    private static string FormatBuildingBoxTransformationStatus(
        BuildingWorldViewModel transformation)
    {
        return transformation.BuildingBoxCommitState switch
        {
            BuildingBoxCommitState.Reserved => "Planned",
            BuildingBoxCommitState.AtSite =>
                $"Unpacking {transformation.CompletedWork}/{transformation.RequiredWork}",
            _ => transformation.Status.ToString(),
        };
    }
}

}
