using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
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

    [Test]
    public void Direct_movement_overrides_self_defense_while_enemy_aggro_remains()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        runtime.Terrain.BindManualMovementSource(
            runtime.Residents.HasManualTunnelMovement);
        runtime.Residents.BindDirectCommandPrioritySource(_ => false);
        runtime.Terrain.BindDirectCommandCombatDisengage(
            runtime.Residents.BeginResidentDirectCommand);
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
        runtime.Residents.Repository.Save(resident);

        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);

        Assert.That(runtime.Residents.GetCombatIntent(resident.Id), Is.Not.Null);
        Assert.That(runtime.Residents.GetCombatIntent(enemyId), Is.Not.Null);

        CellId start = runtime.Residents.Repository.Get(resident.Id)!.Position;
        CellId destination = runtime.Residents.TunnelVolume.SupportedCells
            .Where(cell => cell != start)
            .Select(cell => new
            {
                Cell = cell,
                Path = runtime.Residents.TunnelVolume.FindPath(start, cell),
            })
            .Where(candidate => candidate.Path.Succeeded
                && candidate.Path.Path != null
                && candidate.Path.Path.Cells.Count > 1)
            .OrderBy(candidate => candidate.Path.Path!.Cells.Count)
            .ThenBy(candidate => candidate.Cell)
            .First()
            .Cell;

        Assert.That(
            runtime.Terrain.PrepareResidentsForDirectCommand(
                new[] { resident.Id.ToString() },
                runtime.Residents.Tick).IsSuccess,
            Is.True);
        var route = runtime.Residents.MoveResidentThroughTunnel(
            resident.Id.ToString(),
            destination);
        Assert.That(route.Result.IsSuccess, Is.True, route.Result.Error?.ToString());
        Assert.That(runtime.Residents.GetCombatIntent(resident.Id), Is.Null);
        Assert.That(runtime.Residents.GetCombatIntent(enemyId), Is.Not.Null);

        bool moved = false;
        for (int iteration = 0; iteration < 20; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            AgentState current = runtime.Residents.Repository.Get(resident.Id)!;
            moved |= current.Position != start;
            Assert.That(
                runtime.Residents.GetCombatIntent(enemyId),
                Is.Not.Null,
                "The enemy keeps persistent aggro while the resident obeys the order.");
            Assert.That(
                runtime.Residents.GetCombatIntent(resident.Id),
                Is.Null,
                "Self-defense must not replace an active direct movement order.");
            if (moved)
            {
                break;
            }
        }

        Assert.That(moved, Is.True, "The direct movement order never advanced.");

        bool selfDefenseResumed = false;
        for (int iteration = 0; iteration < 4; iteration++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            selfDefenseResumed = runtime.Residents.GetCombatIntent(resident.Id) != null;
            if (selfDefenseResumed)
            {
                break;
            }
        }

        Assert.That(
            selfDefenseResumed,
            Is.True,
            "A still-valid threat should restore self-defense after the order ends.");
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
