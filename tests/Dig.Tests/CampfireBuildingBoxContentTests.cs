using System;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class CampfireBuildingBoxContentTests
{
    [Fact]
    public void Campfire_uses_existing_building_box_policy()
    {
        PackableBuildingContentDefinition content =
            CampfireBuildingBoxContent.Definition;

        Assert.Equal(
            BuildingConstructionPolicyKind.BuildingBox,
            content.Building.ConstructionPolicy);
        Assert.Equal(
            CampfireBuildingBoxContent.CampfireBoxItemId,
            content.Building.BoxPolicy!.BoxItemId);
        Assert.Equal(1, content.BoxItem.MaximumStackSize);
        Assert.True(content.BoxItem.HasCategory(
            CampfireBuildingBoxContent.BuildingBoxCategoryId));
    }

    [Fact]
    public void Campfire_pack_and_unpack_are_three_ten_minute_iterations()
    {
        BuildingBoxWorkProfile work = CampfireBuildingBoxContent.Definition.Work;

        Assert.Equal(3, work.AssemblyIterations);
        Assert.Equal(3, work.PackingIterations);
        Assert.Equal(10m, work.BaseMinutesPerIteration);
        Assert.Equal(0.7m, work.LogisticsPerCompletedIteration);
        Assert.Equal(2.1m, work.TotalAssemblyLogistics);
        Assert.Equal(2.1m, work.TotalPackingLogistics);
        Assert.Equal(
            work.AssemblyIterations,
            CampfireBuildingBoxContent.Definition.Building.RequiredWork);
        Assert.Equal(
            work.PackingIterations,
            CampfireBuildingBoxContent.Definition.Building.BoxPolicy!.PackingWork);
    }

    [Fact]
    public void Campfire_placement_profile_is_flat_outdoor_and_not_tunnel()
    {
        PackableBuildingPlacementProfile placement =
            CampfireBuildingBoxContent.Definition.Placement;

        Assert.Equal(1.5m, placement.WidthCells);
        Assert.Equal(1.5m, placement.DepthCells);
        Assert.True(placement.RequiresFlatSurface);
        Assert.True(placement.OutdoorOnly);
        Assert.False(placement.AllowsTunnel);
    }

    [Fact]
    public void Catalog_resolves_the_same_content_by_building_and_box_ids()
    {
        PackableBuildingContentCatalog catalog = CampfireBuildingBoxContent.Catalog;

        Assert.Same(
            CampfireBuildingBoxContent.Definition,
            catalog.Get(CampfireBuildingBoxContent.CampfireBuildingId));
        Assert.Same(
            CampfireBuildingBoxContent.Definition,
            catalog.GetByBoxItem(CampfireBuildingBoxContent.CampfireBoxItemId));
    }

    [Fact]
    public void Content_rejects_a_box_that_does_not_match_the_building_policy()
    {
        BuildingDefinition building = new BuildingDefinition(
            new BuildingDefinitionId("building.invalid"),
            "Invalid",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 1,
            boxPolicy: new BuildingBoxPolicy(
                new ItemId("building_box.expected"),
                packingWork: 3));
        ItemDefinition actualBox = new ItemDefinition(
            new ItemId("building_box.actual"),
            "Actual",
            maximumStackSize: 1,
            isTool: false);

        Assert.Throws<ArgumentException>(() => new PackableBuildingContentDefinition(
            building,
            actualBox,
            new BuildingBoxWorkProfile(3, 3, 10m, 0.7m),
            new PackableBuildingPlacementProfile(1.5m, 1.5m, true, true, false)));
    }

    [Fact]
    public void Outdoor_only_profile_cannot_allow_tunnel_placement()
    {
        Assert.Throws<ArgumentException>(() =>
            new PackableBuildingPlacementProfile(
                widthCells: 1m,
                depthCells: 1m,
                requiresFlatSurface: true,
                outdoorOnly: true,
                allowsTunnel: true));
    }
}

}
