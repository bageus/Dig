using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Ecology;
using Dig.Domain.Jobs;
using Dig.Presentation.Agents;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CampfireSupplyDependencyPlayModeTests
{
    [Test]
    public void Enabled_internal_stock_creates_mushroom_dependency_without_recipe_order()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession residents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        AgentViewModel[] agents = residents.LoadView().ToArray();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            agents,
            world.Journal,
            residents.SkillGrants);
        terrain.InitializeBuildingDemo(world.Journal);
        terrain.InitializeBuildingProductionDemo(residents.Repository, world.Journal);
        terrain.InitializeMushroomDemo(0L);
        Assert.That(terrain.AdvanceMushrooms(3L, agents).IsSuccess, Is.True);

        terrain.SynchronizeBuildingProduction(4L, agents);

        JobSnapshot chopJob = terrain.LoadJobSnapshots()
            .Single(value => !value.IsTerminal
                && value.Definition is MushroomChopJobDefinition);
        JobSnapshot supplyJob = terrain.LoadJobSnapshots()
            .Single(value => !value.IsTerminal
                && value.Definition is BuildingSupplyJobDefinition);
        BuildingSupplyJobDefinition supply =
            (BuildingSupplyJobDefinition)supplyJob.Definition;
        Assert.That(supply.IsSourceResolved, Is.False);
        Assert.That(supply.Dependencies, Has.Count.EqualTo(1));
        Assert.That(supply.Dependencies[0], Is.EqualTo(chopJob.Id));
        Assert.That(supply.RequestedItems, Has.Count.EqualTo(1));
        Assert.That(
            supply.RequestedItems[0].ItemId,
            Is.EqualTo(CampfireProductionContent.MushroomCapItemId));
    }
}

}
