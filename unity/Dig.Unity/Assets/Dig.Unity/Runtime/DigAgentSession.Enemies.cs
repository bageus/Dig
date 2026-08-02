using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Combat;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Combat;
using Dig.Presentation.Creatures;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private const long AutonomousIntentLifetimeTicks = 240;
    private readonly EnemyPatrolPlanner _enemyPatrolPlanner =
        new EnemyPatrolPlanner();
    private readonly Dictionary<EntityId, CellId> _enemyPatrolAnchors =
        new Dictionary<EntityId, CellId>();
    private readonly Dictionary<EntityId, long> _lastEnemyPatrolMoveTicks =
        new Dictionary<EntityId, long>();
    private static readonly EntityId CaveMonsterOneId =
        EntityId.Parse("e1000000000000000000000000000001");
    private static readonly EntityId CaveMonsterTwoId =
        EntityId.Parse("e1000000000000000000000000000002");

    internal IReadOnlyList<CreatureVisualSnapshot> LoadEnemyCreatures()
    {
        CombatState? combat = _combatRepository?.Get();
        List<CreatureVisualSnapshot> result = new List<CreatureVisualSnapshot>(
            _enemyDefinitions.Count);
        foreach (KeyValuePair<EntityId, EnemyCombatDefinition> pair
            in _enemyDefinitions.OrderBy(value => value.Key.ToString(), StringComparer.Ordinal))
        {
            AgentState? actor = _repository.Get(pair.Key);
            if (actor == null)
            {
                continue;
            }

            AgentSnapshot snapshot = actor.CreateSnapshot(_tick);
            CombatExecutionSnapshot? execution = combat?.GetActiveExecution(actor.Id);
            bool engaged = combat != null && IsCombatEngaged(combat, actor.Id);
            bool moving = execution != null
                && (execution.Stage == CombatExecutionStage.Approach
                    || execution.Stage == CombatExecutionStage.Retreat);
            moving |= _lastEnemyPatrolMoveTicks.TryGetValue(
                actor.Id,
                out long patrolTick)
                && patrolTick == _tick;
            bool attacking = execution != null
                && (execution.Stage == CombatExecutionStage.WindUp
                    || execution.Stage == CombatExecutionStage.ResolveAttack);
            bool impact = _lastCombatImpactTicks.TryGetValue(actor.Id, out long impactTick)
                && _tick - impactTick <= 1;
            result.Add(new CreatureVisualSnapshot(
                actor.Id.ToString(),
                pair.Value.SpeciesId,
                CreatureLifecycleVisualStage.Adult,
                CreatureDisposition.Hostile,
                snapshot.IsAlive,
                snapshot.Position.X,
                snapshot.Position.Y,
                snapshot.Position.Z,
                moving,
                attacking,
                impact,
                isGrowing: false,
                isSpecialAction: false,
                actionProgress: 0d,
                version: checked(snapshot.Version + (combat?.Version ?? 0)),
                activityVariantId: string.Empty,
                currentHealth: snapshot.Needs.Health.Points,
                maximumHealth: pair.Value.MaximumHealth,
                showHealthBar: engaged));
        }
        return result;
    }

    internal IReadOnlyList<CreatureVisualSnapshot> LoadCreatures(
        IReadOnlyList<CreatureVisualSnapshot> livingMaterials)
    {
        if (livingMaterials == null)
        {
            throw new ArgumentNullException(nameof(livingMaterials));
        }

        return livingMaterials
            .Concat(LoadEnemyCreatures())
            .OrderBy(value => value.CreatureId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<CombatantHealthBarViewModel> LoadResidentCombatHealthBars()
    {
        CombatState? combat = _combatRepository?.Get();
        List<CombatantHealthBarViewModel> result = new List<CombatantHealthBarViewModel>();
        foreach (AgentState resident in _repository.GetAll()
            .Where(value => !_combatOnlyActors.Contains(value.Id))
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            AgentSnapshot snapshot = resident.CreateSnapshot(_tick);
            bool engaged = combat != null && IsCombatEngaged(combat, resident.Id);
            result.Add(new CombatantHealthBarViewModel(
                resident.Id.ToString(),
                snapshot.Needs.Health.Points,
                NeedValue.Maximum,
                engaged && snapshot.IsAlive,
                engaged ? "active_combat" : "not_in_combat"));
        }
        return result;
    }

    private void SeedCaveMonsterPair(
        TunnelNavigationVolume volume,
        FactionState factions)
    {
        TunnelDemoLayout layout = volume.DemoLayout
            ?? throw new InvalidOperationException("Cave monster seed requires demo layout.");
        EnemyCombatDefinition definition = CaveEncounterCombatContent.CaveMonster;
        CellId[] cells = volume.SupportedCells
            .Where(cell => cell.Y == layout.CaveFloorY)
            .Where(cell => cell.X >= layout.CaveMinX && cell.X <= layout.CaveMaxX)
            .OrderByDescending(cell => Math.Abs(cell.X - layout.ShaftX))
            .ThenBy(cell => cell.Z)
            .ThenBy(cell => cell)
            .Take(definition.MaximumGroupSize)
            .ToArray();
        if (cells.Length != definition.MaximumGroupSize)
        {
            throw new InvalidOperationException(
                "The lower cave does not contain two supported monster spawn cells.");
        }

        AddEnemy(CaveMonsterOneId, definition, cells[0], factions);
        AddEnemy(CaveMonsterTwoId, definition, cells[1], factions);
    }

    private void AddEnemy(
        EntityId id,
        EnemyCombatDefinition definition,
        CellId position,
        FactionState factions)
    {
        AgentState enemy = new AgentState(
            id,
            definition.DisplayName,
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(definition.MaximumHealth)),
            DailySchedule.CreateBalanced(24),
            skills: null,
            traits: null,
            initialPosition: position);
        Result added = _repository.Add(enemy);
        if (added.IsFailure)
        {
            throw new InvalidOperationException(added.Error!.ToString());
        }

        _combatOnlyActors.Add(id);
        _enemyDefinitions.Add(id, definition);
        _enemyPatrolAnchors.Add(id, position);
        factions.AssignMember(id, HostileFaction);
    }

    private void EnsureAutonomousEnemyIntent(AgentState enemy)
    {
        if (_issueCombatIntent == null || _combatRepository == null
            || !enemy.IsAlive
            || _combatRepository.Get().GetActiveIntent(enemy.Id) != null)
        {
            return;
        }

        AgentState? target = FindNearestVisibleResident(enemy);
        if (target == null)
        {
            return;
        }

        IssueAutomaticAttack(
            enemy.Id,
            target.Id,
            CombatIntentSource.Autonomous,
            "autonomous");
        EnsureResidentSelfDefense(target.Id, enemy.Id);
    }

    private void EnsureEnemyRetaliation(EntityId enemyId, EntityId attackerId)
    {
        if (!_combatOnlyActors.Contains(enemyId))
        {
            return;
        }

        IssueAutomaticAttack(
            enemyId,
            attackerId,
            CombatIntentSource.Autonomous,
            "retaliation");
    }

    private void EnsureResidentSelfDefense(EntityId residentId, EntityId enemyId)
    {
        if (_combatOnlyActors.Contains(residentId))
        {
            return;
        }

        IssueAutomaticAttack(
            residentId,
            enemyId,
            CombatIntentSource.Alarm,
            "self_defense");
    }

    private void IssueAutomaticAttack(
        EntityId actorId,
        EntityId targetId,
        CombatIntentSource source,
        string reason)
    {
        if (_issueCombatIntent == null || _combatRepository == null)
        {
            return;
        }

        CombatState combat = _combatRepository.Get();
        if (combat.GetActiveIntent(actorId) != null)
        {
            return;
        }

        AgentState? actor = _repository.Get(actorId);
        AgentState? target = _repository.Get(targetId);
        if (actor == null || target == null || !actor.IsAlive || !target.IsAlive)
        {
            return;
        }

        _issueCombatIntent.Handle(new IssueCombatIntentCommand(
            new CombatIntentRequest(
                new CombatIntentId(
                    reason + ":" + actorId + ":" + targetId + ":" + _tick),
                actorId,
                CombatIntentKind.Attack,
                source,
                _tick,
                RetainsEnemyAggro(actorId)
                    ? long.MaxValue
                    : checked(_tick + AutonomousIntentLifetimeTicks),
                targetId,
                target.Position)));
    }

    private AgentState? FindNearestVisibleResident(AgentState enemy)
    {
        return _repository.GetAll()
            .Where(candidate => candidate.IsAlive
                && !_combatOnlyActors.Contains(candidate.Id))
            .Where(candidate => CombatSpatialMath.Distance3D(
                enemy.Position,
                candidate.Position) <= ResolveEnemySightRange(enemy.Id))
            .Where(candidate => CombatLineOfSightResolver.HasLineOfSight(
                enemy.Position,
                candidate.Position,
                cell => !TunnelVolume.IsOpen(cell)))
            .OrderBy(candidate => CombatSpatialMath.Distance3D(
                enemy.Position,
                candidate.Position))
            .ThenBy(candidate => candidate.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private bool TryAdvanceEnemyIdle(AgentState enemy)
    {
        if (!_combatOnlyActors.Contains(enemy.Id))
        {
            return false;
        }

        if (TryAdvanceEnemyPatrol(enemy, out Result patrol)
            && patrol.IsFailure)
        {
            CancelManualMovementWithWarning(enemy.Id, patrol.Error!);
        }
        return true;
    }

    private bool TryAdvanceEnemyPatrol(AgentState enemy, out Result result)
    {
        result = Result.Success();
        if (!_enemyDefinitions.TryGetValue(
                enemy.Id,
                out EnemyCombatDefinition? definition)
            || !_enemyPatrolAnchors.TryGetValue(enemy.Id, out CellId anchor))
        {
            return false;
        }

        EnemyPatrolDecision decision = _enemyPatrolPlanner.Plan(
            definition,
            enemy.Id,
            anchor,
            enemy.Position,
            TunnelVolume,
            DemoIdentitySeed,
            _tick);
        if (!decision.ShouldMove)
        {
            return false;
        }

        result = MoveThroughTunnelTraffic(enemy, decision.Target);
        if (result.IsSuccess && enemy.Position == decision.Target)
        {
            _lastEnemyPatrolMoveTicks[enemy.Id] = _tick;
        }
        return true;
    }

    private int ResolveEnemySightRange(EntityId enemyId)
    {
        return _enemyDefinitions.TryGetValue(
            enemyId,
            out EnemyCombatDefinition? definition)
            ? definition.SightRange
            : 0;
    }

    private static bool IsCombatEngaged(CombatState combat, EntityId entityId)
    {
        return combat.CreateIntentSnapshot().Any(intent => intent.IsActive
            && (intent.ActorId == entityId || intent.TargetEntityId == entityId));
    }
}

}
