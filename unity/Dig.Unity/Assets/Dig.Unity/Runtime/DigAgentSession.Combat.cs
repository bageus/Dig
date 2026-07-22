using System;
using Dig.Domain.Agents;
using Dig.Application.Combat;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
internal sealed partial class DigAgentSession
{
    private const long PlayerAttackIntentLifetimeTicks = 24;
    private static readonly DomainError AttackActorUnavailable = new DomainError(
        "combat.input.actor_unavailable",
        "The selected resident is missing or no longer alive.");
    private static readonly DomainError AttackTargetInvalid = new DomainError(
        "combat.input.target_invalid",
        "The hostile target is invalid.");
    private IssueCombatIntentHandler? _issueCombatIntent;

    private void InitializeCombat(InMemoryExecutionJournal journal)
    {
        if (journal == null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        WeaponProfile unarmed = new WeaponProfile(
            new WeaponProfileId("weapon.demo.unarmed"),
            minimumRange: 1,
            maximumRange: 1,
            accuracy: 7_000,
            baseDamage: 500,
            armorPenetration: 0,
            cooldownTicks: 2,
            skillProfile: new CombatSkillProfile(
                AgentSkillCatalog.UnarmedCombat,
                hitGrantUnits: 25));
        _issueCombatIntent = new IssueCombatIntentHandler(
            new InMemoryCombatRepository(
                new CombatState(new WeaponCatalog(new[] { unarmed }))),
            journal);
    }

    internal Result<CombatIntentSnapshot> IssuePlayerAttackOrder(
        EntityId actorId,
        EntityId targetId)
    {
        if (actorId.IsEmpty || targetId.IsEmpty || actorId == targetId)
        {
            return Result<CombatIntentSnapshot>.Failure(AttackTargetInvalid);
        }

        AgentState? actor = _repository.Get(actorId);
        if (actor == null || !actor.IsAlive)
        {
            return Result<CombatIntentSnapshot>.Failure(AttackActorUnavailable);
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
            targetEntityId: targetId);
        CombatIntentSnapshot intent = _issueCombatIntent.Handle(
            new IssueCombatIntentCommand(request));
        return Result<CombatIntentSnapshot>.Success(intent);
    }

    private string BuildAttackIntentId(EntityId actorId, EntityId targetId)
    {
        return "player.attack."
            + actorId + "." + targetId + "." + _tick;
    }
}
}
