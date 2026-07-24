using System;
using Dig.Domain.Buildings;

namespace Dig.Domain.Content
{

public static class PackableBuildingPlacementPolicyMapping
{
    public static PackableBuildingSurfacePolicy ToSurfacePolicy(
        this PackableBuildingPlacementProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        return new PackableBuildingSurfacePolicy(
            profile.WidthCells,
            profile.DepthCells,
            profile.RequiresFlatSurface,
            profile.OutdoorOnly,
            profile.AllowsTunnel);
    }
}

}