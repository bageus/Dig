using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Application.Combat;
using Dig.Domain.Combat;
using Dig.Domain.Content;
using Dig.Domain.Ecology;
using Dig.Application.Ecology;
using Dig.Presentation.Combat;
using Dig.Presentation.Creatures;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
internal sealed partial class DigAgentSession
{
    private const long PlayerAttackIntentLifetimeTicks = 240;
    private static readonly FactionId ResidentFaction = new FactionId("faction.residents");
    private static readonly FactionId HostileFaction = new FactionId("faction.hostiles");
    private static readonly DomainError AttackActorUnavailable = new DomainError(
        "combat.input.actor_unavailable",
        "The selected resident is missing or no longer alive.");
    private static readonly DomainError AttackTargetInvalid = new DomainError(
        "combat.input.target_invalid",
        "The hostile target is invalid.");
    private InMemoryCombatRepository? _combatRepository;
    private InMemoryFactionRepository? _combatFactions;
    private IssueCombatIntentHandler? _issueCombatIntent;
    private CombatSpatialExecutionHandler? _combatExecution;
    private DemoCombatEquipmentProvider? _combatEquipmentProvider;
    private InMemoryExecutionJournal? _combatJournal;
    private readonly HashSet<EntityId> _combatOnlyActors = new HashSet<EntityId>();
    private readonly Dictionary<EntityId, EnemyCombatDefinition> _enemyDefinitions =
        new Dictionary<EntityId, EnemyCombatDefinition>();
    private readonly Dictionary<EntityId, long> _lastCombatImpactTicks =
        new Dictionary<EntityId, long>();

    private void InitializeCombat(
        InMemoryExecutionJournal journal,
        TunnelNavigationVolume tunnelVolume)
    {
        if (journal == null) throw new ArgumentNullException(nameof(journal));
        if (tunnelVolume == null) throw new ArgumentNullException(nameof(tunnelVolume));

        _combatJournal = journal;
        CombatState combat = new CombatState(new WeaponCatalog(
            CaveEncounterCombatContent.CreateWeaponProfiles()));
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(ResidentFaction, "Residents", -10_000),
                new FactionDefinition(HostileFaction, "Hostiles", -10_000),
            }),
            new FactionDiplomacyPolicy(
                hostileThreshold: -5_000,
                friendlyThreshold: 3_000,
                alliedThreshold: 8_000,
                territoryViolationPenalty: 1_000));
        foreach (AgentState resident in _repository.GetAll())
        {
            factions.AssignMember(resident.Id, ResidentFaction);
        }
        InitializeVukerEcology(tunnelVolume);
        SeedCaveMonsterPair(tunnelVolume, factions);
        FormInitialVukerPair();

        _combatRepository = new InMemoryCombatRepository(combat);
        _combatFactions = new InMemoryFactionRepository(factions);
        _issueCombatIntent = new IssueCombatIntentHandler(_combatRepository, journal);
        _combatEquipmentProvider = new DemoCombatEquipmentProvider(this, combat.Weapons);
        _combatExecution = new CombatSpatialExecutionHandler(
            _repository,
            _combatRepository,
            _combatFactions,
            tunnelVolume,
            _combatEquipmentProvider,
            journal,
            _skillGrants,
            new CombatSpatialPolicy(
                sightRange: CaveEncounterCombatContent.CaveMonster.SightRange,
                alarmRadius: 4,
                windUpTicks: 1,
                recoveryTicks: 1,
                retryDelayTicks: 1,
                maximumRetries: 3,
                new CombatTacticalPolicy(
                    retreatHealthThreshold: 2_000,
                    retreatThreatRatio: 1_500,
                    defendDistance: 0),
                retainsAggro: RetainsEnemyAggro));
    }

    internal bool CanIssuePlayerAttackOrder(
        EntityId actorId,
        EntityId targetId,
        CellId targetCell)
    {
        if (actorId.IsEmpty || targetId.IsEmpty || actorId == targetId)
        {
            return false;
        }

        AgentState? actor = _repository.Get(actorId);
        AgentState? target = _repository.Get(targetId);
        return actor != null
            && actor.IsAlive
            && target != null
            && target.IsAlive
            && target.Position == targetCell
            && CanIssuePlayerAttackOrder(actorId, targetId);
    }

    internal bool CanIssuePlayerAttackOrder(EntityId actorId, EntityId targetId)
    {
        if (actorId.IsEmpty || targetId.IsEmpty || actorId == targetId
            || _combatFactions == null)
        {
            return false;
        }

        AgentState? actor = _repository.Get(actorId);
        AgentState? target = _repository.Get(targetId);
        if (actor == null || target == null || !actor.IsAlive || !target.IsAlive)
        {
            return false;
        }

        FactionState factions = _combatFactions.Get();
        FactionId? actorFaction = factions.GetMemberFaction(actorId);
        FactionId? targetFaction = factions.GetMemberFaction(targetId);
        return actorFaction.HasValue
            && targetFaction.HasValue
            && factions.AreHostile(actorFaction.Value, targetFaction.Value);
    }

    internal Result<CombatIntentSnapshot> IssuePlayerAttackOrder(
        EntityId actorId,
        EntityId targetId)
    {
        if (!CanIssuePlayerAttackOrder(actorId, targetId))
        {
            AgentState? actor = _repository.Get(actorId);
            return Result<CombatIntentSnapshot>.Failure(
                actor == null || !actor.IsAlive
                    ? AttackActorUnavailable
                    : AttackTargetInvalid);
        }

        if (_issueCombatIntent == null)
        {
            throw new InvalidOperationException("Combat input is not initialized.");
        }

        CombatIntentRequest request = new CombatIntentRequest(
            new CombatIntentId(BuildAttackIntentId(actorId, targetId)),
            actorId,
            CombatIntentKind.Attack,
            CombatIntentSource.PlayerOrder,
            createdTick: _tick,
            expiresTick: checked(_tick + PlayerAttackIntentLifetimeTicks),
            targetEntityId: targetId,
            targetCell: _repository.Get(targetId)!.Position);
        CombatIntentSnapshot intent = _issueCombatIntent.Handle(
            new IssueCombatIntentCommand(request));
        EnsureEnemyRetaliation(targetId, actorId);
        return Result<CombatIntentSnapshot>.Success(intent);
    }

    internal Result DisengageResidentForDirectOrder(EntityId actorId, long tick)
    {
        if (actorId.IsEmpty)
        {
            throw new ArgumentException("Actor id is required.", nameof(actorId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (_combatOnlyActors.Contains(actorId) || _combatRepository == null)
        {
            return Result.Success();
        }

        CombatState combat = _combatRepository.Get();
        CombatIntentSnapshot? intent = combat.GetActiveIntent(actorId);
        if (intent == null || intent.Kind != CombatIntentKind.Attack)
        {
            return Result.Success();
        }

        Result cancelled = combat.CancelIntent(
            intent.IntentId,
            "resident_direct_order_disengaged",
            tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        _combatRepository.Save(combat);
        _combatJournal?.Append(combat.DequeueUncommittedEvents());
        return Result.Success();
    }

    internal CombatIntentSnapshot? GetCombatIntent(EntityId actorId)
    {
        return _combatRepository?.Get().GetActiveIntent(actorId);
    }

    private bool RetainsEnemyAggro(EntityId actorId)
    {
        return _enemyDefinitions.TryGetValue(
            actorId,
            out EnemyCombatDefinition? definition)
            && definition.RetainsAggroUntilTargetUnavailable;
    }

    internal Result CancelPlayerAttackOrder(EntityId actorId)
    {
        if (_combatRepository == null)
        {
            return Result.Success();
        }

        CombatState combat = _combatRepository.Get();
        CombatIntentSnapshot? intent = combat.GetActiveIntent(actorId);
        if (intent == null || intent.Source != CombatIntentSource.PlayerOrder)
        {
            return Result.Success();
        }

        Result cancelled = combat.CancelIntent(
            intent.IntentId,
            "player_cancelled",
            _tick);
        if (cancelled.IsSuccess)
        {
            _combatRepository.Save(combat);
        }
        return cancelled;
    }

    private bool TryAdvanceCombat(AgentState actor, out Result result)
    {
        result = Result.Success();
        if (_combatOnlyActors.Contains(actor.Id))
        {
            EnsureAutonomousEnemyIntent(actor);
        }

        if (_combatRepository == null || _combatExecution == null
            || _combatRepository.Get().GetActiveIntent(actor.Id) == null)
        {
            return false;
        }

        Result<CombatSpatialExecutionReport> advanced = _combatExecution.Handle(
            new AdvanceCombatSpatialExecutionCommand(
                actor.Id,
                DemoIdentitySeed,
                _tick));
        if (advanced.IsSuccess && advanced.Value.Attack != null)
        {
            _lastCombatImpactTicks[advanced.Value.Attack.TargetId] = _tick;
        }
        result = advanced.IsSuccess
            ? Result.Success()
            : Result.Failure(advanced.Error!);
        return true;
    }

    private IReadOnlyList<Dig.Presentation.Agents.AgentViewModel> LoadResidentView()
    {
        return _presenter.Load(_tick)
            .Where(value => !_combatOnlyActors.Contains(EntityId.Parse(value.Id)))
            .ToArray();
    }

    private bool SkipNormalMovement(AgentState actor)
    {
        return _combatOnlyActors.Contains(actor.Id);
    }

    private void SynchronizeCombatDeath(EntityId actorId)
    {
        if (_combatRepository == null || _combatJournal == null)
        {
            return;
        }

        CombatState combat = _combatRepository.Get();
        CombatIntentSnapshot? intent = combat.GetActiveIntent(actorId);
        if (intent != null)
        {
            combat.CancelIntent(intent.IntentId, "actor_dead", _tick);
        }
        else
        {
            CombatExecutionSnapshot? execution = combat.GetActiveExecution(actorId);
            if (execution != null)
            {
                combat.CancelExecution(
                    execution.ExecutionId,
                    _tick,
                    "actor_dead");
            }
        }

        _combatRepository.Save(combat);
        _combatJournal.Append(combat.DequeueUncommittedEvents());
    }

    internal CombatExecutionSnapshot? GetCombatExecution(EntityId actorId)
    {
        return _combatRepository?.Get().GetActiveExecution(actorId);
    }

    private string BuildAttackIntentId(EntityId actorId, EntityId targetId)
    {
        return "player.attack." + actorId + "." + targetId + "." + _tick;
    }

}
}
