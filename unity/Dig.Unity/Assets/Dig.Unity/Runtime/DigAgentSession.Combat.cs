using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Application.Combat;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
internal sealed partial class DigAgentSession
{
    private const long PlayerAttackIntentLifetimeTicks = 24;
    private static readonly FactionId ResidentFaction = new FactionId("faction.residents");
    private static readonly FactionId HostileFaction = new FactionId("faction.hostiles");
    private static readonly WeaponProfileId DemoUnarmedProfile =
        new WeaponProfileId("weapon.demo.unarmed");
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
    private readonly HashSet<EntityId> _combatOnlyActors = new HashSet<EntityId>();

    private void InitializeCombat(
        InMemoryExecutionJournal journal,
        TunnelNavigationVolume tunnelVolume)
    {
        if (journal == null) throw new ArgumentNullException(nameof(journal));
        if (tunnelVolume == null) throw new ArgumentNullException(nameof(tunnelVolume));

        WeaponProfile unarmed = new WeaponProfile(
            DemoUnarmedProfile,
            minimumRange: 1,
            maximumRange: 1,
            accuracy: 7_000,
            baseDamage: 500,
            armorPenetration: 0,
            cooldownTicks: 2,
            skillProfile: new CombatSkillProfile(
                AgentSkillCatalog.UnarmedCombat,
                hitGrantUnits: 25),
            spatialMode: CombatAttackSpatialMode.Melee);
        CombatState combat = new CombatState(new WeaponCatalog(new[] { unarmed }));
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

        _combatRepository = new InMemoryCombatRepository(combat);
        _combatFactions = new InMemoryFactionRepository(factions);
        _issueCombatIntent = new IssueCombatIntentHandler(_combatRepository, journal);
        _combatExecution = new CombatSpatialExecutionHandler(
            _repository,
            _combatRepository,
            _combatFactions,
            tunnelVolume,
            new DemoCombatEquipmentProvider(),
            journal,
            _skillGrants,
            new CombatSpatialPolicy(
                sightRange: 8,
                alarmRadius: 4,
                windUpTicks: 1,
                recoveryTicks: 1,
                retryDelayTicks: 1,
                maximumRetries: 3,
                new CombatTacticalPolicy(
                    retreatHealthThreshold: 2_000,
                    retreatThreatRatio: 1_500,
                    defendDistance: 0)));
    }

    internal Result RegisterHostileCombatant(
        EntityId targetId,
        CellId cell,
        int health = NeedValue.Maximum)
    {
        if (targetId.IsEmpty || health <= 0 || health > NeedValue.Maximum)
        {
            return Result.Failure(AttackTargetInvalid);
        }

        AgentState? existing = _repository.Get(targetId);
        if (existing == null)
        {
            AgentState target = new AgentState(
                targetId,
                "Hostile combatant",
                new AgentNeedsSnapshot(
                    new NeedValue(8_000),
                    new NeedValue(8_000),
                    new NeedValue(8_000),
                    new NeedValue(health)),
                DailySchedule.CreateBalanced(24),
                skills: null,
                traits: null,
                initialPosition: cell);
            Result added = _repository.Add(target);
            if (added.IsFailure)
            {
                return added;
            }
        }
        else
        {
            Result moved = existing.MoveTo(cell, _tick);
            if (moved.IsFailure)
            {
                return moved;
            }
            _repository.Save(existing);
        }

        if (_combatFactions == null)
        {
            throw new InvalidOperationException("Combat input is not initialized.");
        }

        _combatOnlyActors.Add(targetId);
        FactionState factions = _combatFactions.Get();
        factions.AssignMember(targetId, HostileFaction);
        _combatFactions.Save(factions);
        return Result.Success();
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
            && (target == null || target.IsAlive)
            && targetCell.X >= 0
            && targetCell.Y >= 0
            && targetCell.Z >= 0;
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
        return Result<CombatIntentSnapshot>.Success(intent);
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

    internal CombatExecutionSnapshot? GetCombatExecution(EntityId actorId)
    {
        return _combatRepository?.Get().GetActiveExecution(actorId);
    }

    private string BuildAttackIntentId(EntityId actorId, EntityId targetId)
    {
        return "player.attack." + actorId + "." + targetId + "." + _tick;
    }

    private sealed class DemoCombatEquipmentProvider : ICombatEquipmentProvider
    {
        public Result<CombatEquipmentSelection> Select(EntityId actorId, EntityId targetId)
        {
            return Result<CombatEquipmentSelection>.Success(
                new CombatEquipmentSelection(
                    DemoUnarmedProfile,
                    new CombatantModifiers(0, 0, 0, 0, 0),
                    new CombatantModifiers(0, 0, 0, 0, 0)));
        }
    }
}
}
