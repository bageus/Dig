using System;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class CampfireBuildingBoxContent
{
    public static readonly BuildingDefinitionId CampfireBuildingId =
        new BuildingDefinitionId("building.campfire");

    public static readonly ItemId CampfireBoxItemId =
        new ItemId("building_box.campfire");

    public static readonly ItemCategoryId BuildingBoxCategoryId =
        new ItemCategoryId("building.box");

    public static PackableBuildingContentDefinition Definition { get; } = Create();

    public static PackableBuildingContentCatalog Catalog { get; } =
        new PackableBuildingContentCatalog(new[] { Definition });

    private static PackableBuildingContentDefinition Create()
    {
        BuildingBoxWorkProfile work = new BuildingBoxWorkProfile(
            assemblyIterations: 3,
            packingIterations: 3,
            baseMinutesPerIteration: 10m,
            logisticsPerCompletedIteration: 0.7m);
        ItemDefinition boxItem = new ItemDefinition(
            CampfireBoxItemId,
            "Packed campfire",
            maximumStackSize: 1,
            isTool: false,
            new[] { BuildingBoxCategoryId });
        BuildingDefinition building = new BuildingDefinition(
            CampfireBuildingId,
            "Campfire",
            new[] { new CellOffset(0, 0) },
            new[]
            {
                new CellOffset(0, -1),
                new CellOffset(-1, 0),
                new CellOffset(1, 0),
                new CellOffset(0, 1),
            },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: work.AssemblyIterations,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(
                CampfireBoxItemId,
                packingWork: work.PackingIterations));
        PackableBuildingPlacementProfile placement =
            new PackableBuildingPlacementProfile(
                widthCells: 1.5m,
                depthCells: 1.5m,
                requiresFlatSurface: true,
                outdoorOnly: true,
                allowsTunnel: false);
        return new PackableBuildingContentDefinition(
            building,
            boxItem,
            work,
            placement);
    }
}

}
