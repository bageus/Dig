using System.Collections;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Presentation.Creatures;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class VukerReproductionPlayModeTests
{
    [UnityTest]
    public IEnumerator PairBirthsChildChildDoesNotFightAndAltKidnapTamesIt()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession agents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        var residents = agents.LoadView();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            residents,
            world.Journal,
            agents.SkillGrants);
        agents.BindCombatInventory(terrain.InventoryRepository);

        VukerEcologySnapshot initial = agents.LoadVukerEcology();
        Assert.That(initial.Individuals.Count, Is.EqualTo(2));
        Assert.That(initial.Pairs.Count, Is.EqualTo(1));
        Assert.That(initial.Pairs[0].NextBirthTick,
            Is.EqualTo(VukerEcologyProfile.ReproductionCooldownTicks));

        for (int tick = 0;
            tick < VukerEcologyProfile.ReproductionCooldownTicks;
            tick++)
        {
            Assert.That(agents.Advance().IsSuccess, Is.True);
        }

        VukerEcologySnapshot afterBirth = agents.LoadVukerEcology();
        VukerIndividualSnapshot child = afterBirth.Individuals.Single(value =>
            value.Lifecycle == VukerLifecycleStage.Child);
        CreatureVisualSnapshot childVisual = agents.LoadEnemyCreatures().Single(value =>
            value.CreatureId == child.EntityId.ToString());
        Assert.That(childVisual.Lifecycle, Is.EqualTo(CreatureLifecycleVisualStage.Child));
        Assert.That(childVisual.Disposition, Is.EqualTo(CreatureDisposition.Hostile));
        Assert.That(childVisual.IsGrowing, Is.True);
        Assert.That(agents.GetCombatIntent(child.EntityId), Is.Null);

        for (int tick = 0; tick < 4; tick++)
        {
            Assert.That(agents.Advance().IsSuccess, Is.True);
            Assert.That(agents.GetCombatIntent(child.EntityId), Is.Null);
        }

        EntityId residentId = EntityId.Parse(residents[0].Id);
        AgentState resident = agents.Repository.Get(residentId)!;
        Dig.Domain.World.CellId residentDeployment = resident.Position;
        Assert.That(resident.MoveTo(child.Position, agents.Tick).IsSuccess, Is.True);
        agents.Repository.Save(resident);
        terrain.BindDirectCommandCombatDisengage(
            agents.DisengageResidentForDirectOrder);
        Assert.That(terrain.PrepareResidentsForDirectCommand(
            new[] { residentId.ToString() },
            agents.Tick).IsSuccess, Is.True);

        Result requested = agents.RequestVukerKidnap(residentId, child.EntityId);
        Assert.That(requested.IsSuccess, Is.True, requested.Error?.ToString());
        VukerIndividualSnapshot tamed = agents.LoadVukerEcology().Individuals
            .Single(value => value.EntityId == child.EntityId);
        Assert.That(tamed.Disposition, Is.EqualTo(VukerDisposition.Tamed));
        Assert.That(tamed.TamedByResidentId, Is.EqualTo(residentId));
        Assert.That(agents.IsTamedVuker(child.EntityId), Is.True);
        Assert.That(agents.GetCombatIntent(child.EntityId), Is.Null);

        Dig.Application.Agents.PlanAgentTunnelRouteReport move =
            agents.MoveTamedVukerThroughTunnel(
                child.EntityId,
                residentDeployment);
        Assert.That(move.Result.IsSuccess, Is.True, move.Result.Error?.ToString());
        for (int tick = 0; tick < 500; tick++)
        {
            if (agents.Repository.Get(child.EntityId)!.Position == residentDeployment)
            {
                break;
            }

            Assert.That(agents.Advance().IsSuccess, Is.True);
        }
        Assert.That(agents.Repository.Get(child.EntityId)!.Position,
            Is.EqualTo(residentDeployment));

        long remainingGrowth = tamed.MaturityTick - agents.Tick;
        for (long tick = 0; tick <= remainingGrowth; tick++)
        {
            Assert.That(agents.Advance().IsSuccess, Is.True);
        }

        VukerIndividualSnapshot mature = agents.LoadVukerEcology().Individuals
            .Single(value => value.EntityId == child.EntityId);
        Assert.That(mature.Lifecycle, Is.EqualTo(VukerLifecycleStage.Adult));
        Assert.That(mature.Disposition, Is.EqualTo(VukerDisposition.Tamed));
        Assert.That(agents.GetCombatIntent(child.EntityId), Is.Null);
        yield return null;
    }
}

}
