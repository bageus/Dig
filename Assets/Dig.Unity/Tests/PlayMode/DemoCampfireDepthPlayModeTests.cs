using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Navigation;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class DemoCampfireDepthPlayModeTests
{
    [Test]
    public void Fresh_demo_completed_campfire_uses_Z1_building_layer()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            agents.LoadView(),
            world.Journal,
            agents.SkillGrants);

        terrain.InitializeBuildingDemo(world.Journal);

        TunnelDemoLayout layout = world.CreateTunnelNavigationVolume().DemoLayout
            ?? throw new AssertionException("Demo layout is required.");
        BuildingWorldViewModel campfire = terrain.LoadBuildings().Single(value =>
            value.DefinitionId ==
                CampfireBuildingBoxContent.CampfireBuildingId.ToString());

        Assert.That(campfire.Status, Is.EqualTo(BuildingStatus.Completed));
        Assert.That(campfire.OriginX, Is.EqualTo(layout.ShaftX - 2));
        Assert.That(campfire.OriginY, Is.EqualTo(layout.SurfaceY));
        Assert.That(campfire.OriginZ, Is.EqualTo(1));
        Assert.That(campfire.OriginZ, Is.Not.EqualTo(layout.ShaftZ));
        Assert.That(campfire.WorkPositionZ, Is.EqualTo(1));
        Assert.That(campfire.Footprint, Has.All.Matches<BuildingFootprintCellViewModel>(
            value => value.Z == 1));
    }
}

}
