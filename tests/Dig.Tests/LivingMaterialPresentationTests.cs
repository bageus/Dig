using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialPresentationTests
{
    [Theory]
    [InlineData(LivingMaterialActivity.ReleaseDormant, "hamster.release_dormant", true, false)]
    [InlineData(LivingMaterialActivity.HamsterSearching, "hamster.searching", true, false)]
    [InlineData(LivingMaterialActivity.HamsterSleeping, "hamster.sleeping", true, false)]
    [InlineData(LivingMaterialActivity.Moving, "hamster.moving", false, true)]
    public void HamsterActivityProjectsExactVisualVariant(
        LivingMaterialActivity activity,
        string expectedVariant,
        bool expectedSpecial,
        bool expectedMoving)
    {
        LivingMaterialSnapshot hamster = Snapshot(
            LivingMaterialSpecies.Hamster,
            activity,
            activity == LivingMaterialActivity.Moving ? 0 : 2);

        CreatureVisualSnapshot visual = Assert.Single(
            new LivingMaterialCreatureVisualProjector().Project(new[] { hamster }));

        Assert.Equal(expectedVariant, visual.ActivityVariantId);
        Assert.Equal(expectedSpecial, visual.IsSpecialAction);
        Assert.Equal(expectedMoving, visual.IsMoving);
        Assert.Equal("creature.hamster", visual.SpeciesId);
    }

    [Fact]
    public void GrubProjectsContinuousCrawlVariant()
    {
        CreatureVisualSnapshot visual = Assert.Single(
            new LivingMaterialCreatureVisualProjector().Project(new[]
            {
                Snapshot(LivingMaterialSpecies.Grub, LivingMaterialActivity.Moving, 0),
            }));

        Assert.Equal("creature.grub", visual.SpeciesId);
        Assert.Equal("grub.crawling", visual.ActivityVariantId);
        Assert.True(visual.IsMoving);
        Assert.False(visual.IsSpecialAction);
    }

    [Fact]
    public void CampfireTethersUseStableUnitIdentityAndStopAtTwo()
    {
        EntityId campfireId = Id(90);
        EntityId otherBuildingId = Id(91);
        InventoryState inventory = new InventoryState(new ItemCatalog(
            LivingMaterialContent.CreateItems()));
        Assert.True(inventory.AddUnit(
            Id(1),
            LivingMaterialEcologyProfiles.HamsterItemId,
            ItemLocation.InBuilding(campfireId),
            0).IsSuccess);
        Assert.True(inventory.AddUnit(
            Id(2),
            LivingMaterialEcologyProfiles.HamsterItemId,
            ItemLocation.InBuilding(campfireId),
            0).IsSuccess);
        Assert.True(inventory.AddUnit(
            Id(3),
            LivingMaterialEcologyProfiles.HamsterItemId,
            ItemLocation.InBuilding(campfireId),
            0).IsSuccess);
        Assert.True(inventory.AddUnit(
            Id(4),
            LivingMaterialEcologyProfiles.GrubItemId,
            ItemLocation.InBuilding(campfireId),
            0).IsSuccess);
        Assert.True(inventory.AddUnit(
            Id(5),
            LivingMaterialEcologyProfiles.HamsterItemId,
            ItemLocation.InBuilding(otherBuildingId),
            0).IsSuccess);

        LivingMaterialCampfireTetherViewModel[] projected =
            new LivingMaterialCampfireTetherProjector()
                .Project(
                    inventory.CreateSnapshot(),
                    new[]
                    {
                        Building(campfireId, CampfireBuildingBoxContent.CampfireBuildingId),
                        Building(
                            otherBuildingId,
                            new BuildingDefinitionId("building.workshop")),
                    })
                .ToArray();

        Assert.Equal(2, projected.Length);
        Assert.Equal(new[] { Id(1).ToString(), Id(2).ToString() },
            projected.Select(value => value.CreatureId));
        Assert.Equal(new[] { 0, 1 }, projected.Select(value => value.SlotIndex));
        Assert.All(projected, value => Assert.Equal(campfireId.ToString(), value.BuildingId));
    }

    [Fact]
    public void CanonicalHamsterContentIsQuantityOneEverywhere()
    {
        ItemDefinition living = Assert.Single(
            LivingMaterialContent.CreateItems(),
            value => value.Id == LivingMaterialEcologyProfiles.HamsterItemId);
        ItemDefinition production = Assert.Single(
            CampfireProductionContent.CreateItems(),
            value => value.Id == LivingMaterialEcologyProfiles.HamsterItemId);

        Assert.Equal(1, living.MaximumStackSize);
        Assert.Equal(1, production.MaximumStackSize);
    }

    private static LivingMaterialSnapshot Snapshot(
        LivingMaterialSpecies species,
        LivingMaterialActivity activity,
        int remaining)
    {
        EntityId id = Id(species == LivingMaterialSpecies.Hamster ? 10 : 20);
        CellId cell = new CellId(5, 3, 0);
        return new LivingMaterialSnapshot(
            id,
            id,
            species,
            LivingMaterialContainment.Free,
            cell,
            cell,
            new LivingMaterialPlaneKey(cell),
            direction: 1,
            activity: activity,
            activityStepsRemaining: remaining,
            movementCredit: 0,
            successfulMovementSteps: 0,
            nextSearchAtStep: 4,
            nextSleepAtStep: 16,
            reproductionCyclesCompleted: 0,
            nextReproductionStep: 96,
            deterministicSequence: 0,
            blockedReason: null,
            version: 4);
    }

    private static BuildingWorldViewModel Building(
        EntityId id,
        BuildingDefinitionId definitionId)
    {
        BuildingFunctionsViewModel functions = new BuildingFunctionsViewModel(
            id,
            definitionId,
            BuildingStatus.Completed,
            durability: 100,
            maximumDurability: 100,
            isPacking: false,
            packingCompletedWork: 0,
            packingRequiredWork: 0,
            Array.Empty<BuildingFunctionActionViewModel>());
        return new BuildingWorldViewModel(
            id.ToString(),
            definitionId.ToString(),
            "Building",
            originX: 5,
            originY: 3,
            originZ: 0,
            orientation: BuildingOrientation.North,
            workPositionX: 4,
            workPositionY: 3,
            workPositionZ: 0,
            status: BuildingStatus.Completed,
            completedWork: 1,
            requiredWork: 1,
            version: 1,
            footprint: new[] { new BuildingFootprintCellViewModel(5, 3, 0) },
            functions: functions);
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "7400000000000000000000000000" + suffix.ToString("D4"));
}

}
