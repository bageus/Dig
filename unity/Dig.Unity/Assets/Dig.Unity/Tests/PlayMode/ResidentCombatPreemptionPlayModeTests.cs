using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ResidentCombatPreemptionPlayModeTests
{
    [Test]
    public void Incoming_enemy_intent_preempts_work_and_sleep_before_their_next_interval()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        runtime.Residents.BindResidentNeedsRuntime(runtime.Terrain);
        runtime.Terrain.InitializeResidentNeedsRuntime(
            runtime.Residents.Tick,
            runtime.Residents.LoadView());

        AgentState resident = runtime.Residents.Repository.GetAll()
            .Where(value => runtime.Residents.LoadView()
                .Any(view => view.Id == value.Id.ToString()))
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .First();
        EntityId enemyId = EntityId.Parse(runtime.Residents.LoadEnemyCreatures()
            .OrderBy(value => value.CreatureId, StringComparer.Ordinal)
            .First()
            .CreatureId);
        AgentState enemy = runtime.Residents.Repository.Get(enemyId)!;
        Assert.That(
            resident.MoveTo(enemy.Position, runtime.Residents.Tick).IsSuccess,
            Is.True);

        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        Assert.That(
            resident.ApplyDecision(
                CreateDecision(AgentIntentKind.Work, runtime.Residents.Tick),
                policy,
                runtime.Residents.Tick).IsSuccess,
            Is.True);
        runtime.Residents.Repository.Save(resident);

        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);

        AgentState fighting = runtime.Residents.Repository.Get(resident.Id)!;
        Assert.That(runtime.Residents.GetCombatIntent(resident.Id), Is.Not.Null);
        Assert.That(fighting.CreateSnapshot(runtime.Residents.Tick).ActiveAction, Is.Null);
        Assert.That(fighting.LastActionBlockReason, Is.EqualTo("combat_preempted"));

        int alertnessBeforeSleepAttempt = fighting.CreateSnapshot(runtime.Residents.Tick)
            .Needs.Alertness.Points;
        Assert.That(
            fighting.ApplyDecision(
                CreateDecision(AgentIntentKind.Sleep, runtime.Residents.Tick),
                policy,
                runtime.Residents.Tick).IsSuccess,
            Is.True);
        runtime.Residents.Repository.Save(fighting);

        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);

        AgentSnapshot after = runtime.Residents.Repository.Get(resident.Id)!
            .CreateSnapshot(runtime.Residents.Tick);
        Assert.That(after.ActiveAction, Is.Null);
        Assert.That(runtime.Residents.GetCombatIntent(resident.Id), Is.Not.Null);
        Assert.That(
            after.Needs.Alertness.Points,
            Is.LessThanOrEqualTo(alertnessBeforeSleepAttempt),
            "Combat must not grant a Sleep recovery interval.");
    }

    private static AgentDecision CreateDecision(AgentIntentKind intent, long tick)
    {
        return new AgentDecision(
            tick,
            intent,
            selectedPlayerOrderId: null,
            selectedScore: 10_000,
            critical: false,
            reasonCode: "test.combat_preemption",
            explanation: "Forced action for combat preemption regression.",
            options: Array.Empty<UtilityOptionDiagnostic>());
    }
}

}