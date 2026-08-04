using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Generation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class FullDepthEatingTentBehaviorTests
{
    private static readonly MaterialId Air = new MaterialId("terrain.test.air");
    private static readonly MaterialId Unmineable =
        new MaterialId("terrain.test.unmineable");
    private static readonly MaterialId Ore = new MaterialId("terrain.test.ore");

    [Fact]
    public void Generated_unmineable_front_cells_own_the_complete_depth_column()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(
                Unmineable,
                "Unmineable",
                isSolid: true,
                hardness: 999,
                isMineable: false,
                outputProfile: null),
            new MaterialDefinition(Ore, isSolid: true, hardness: 120),
        });
        WorldGenerationProfile profile = new WorldGenerationProfile(
            "test.unmineable-columns",
            WorldGenerator.CurrentGeneratorVersion,
            new WorldSize(28, 20),
            chunkSize: 4,
            Air,
            new[]
            {
                new WorldGenerationBiomeDefinition(
                    "unmineable",
                    Unmineable,
                    Ore),
            },
            zoneCount: 4,
            startRoomRadius: 2,
            zoneRoomRadius: 2,
            corridorHalfWidth: 0,
            minimumStartingResources: 4,
            pointOfInterestCount: 2,
            layerCount: 2);

        GeneratedWorld generated = new WorldGenerator().Generate(
            new WorldGenerationRequest(626UL, profile, materials)).Value;
        CellSnapshot[] cells = generated.World.CreateSnapshot()
            .Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToArray();
        CellSnapshot[] front = cells
            .Where(cell => cell.Id.Z == CellId.MinimumDepth)
            .Where(cell => cell.State.MaterialId == Unmineable)
            .ToArray();

        Assert.NotEmpty(front);
        foreach (CellSnapshot cell in front)
        {
            for (int z = CellId.MinimumDepth + 1; z <= CellId.MaximumDepth; z++)
            {
                CellId expected = new CellId(cell.Id.X, cell.Id.Y, z);
                CellSnapshot deep = Assert.Single(
                    cells,
                    value => value.Id == expected);
                Assert.True(deep.IsSolid);
                Assert.Equal(Unmineable, deep.State.MaterialId);
            }
        }
    }

    [Fact]
    public void Active_eat_intent_projects_looping_eating_action_with_bite_progress()
    {
        AgentViewModel model = new AgentViewModel(
            "resident.eater",
            "Eater",
            version: 7,
            isAlive: true,
            cellX: 3,
            cellY: 4,
            nutrition: 30,
            alertness: 80,
            mood: 70,
            health: 100,
            scheduledActivity: "FreeTime",
            activeIntent: "Eat",
            actionElapsedTicks: 1,
            actionRequiredTicks: 3,
            decisionReason: "agents.eat.active",
            decisionExplanation: "Resident is eating.",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());

        ResidentActionVisualViewModel action =
            new ResidentVisualPresenter().PresentAction(
                model,
                isMoving: false,
                isCarrying: false);

        Assert.Equal(ResidentActionVisualState.Eat, action.State);
        Assert.True(action.IsLooping);
        Assert.Equal(1d / 3d, action.NormalizedProgress, precision: 6);
    }
}

}
