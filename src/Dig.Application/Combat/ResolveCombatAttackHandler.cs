using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;

namespace Dig.Application.Combat
{

public sealed class ResolveCombatAttackHandler
    : ICommandHandler<ResolveCombatAttackCommand, Result<CombatAttackResolution>>
{
    private readonly IAgentRepository _agents;
    private readonly ICombatRepository _combat;
    private readonly IFactionRepository _factions;
    private readonly IEventSink _eventSink;
    private readonly IAgentSkillGrantService _skillGrants;

    public ResolveCombatAttackHandler(
        IAgentRepository agents,
        ICombatRepository combat,
        IFactionRepository factions,
        IEventSink eventSink,
        IAgentSkillGrantService skillGrants)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _factions = factions ?? throw new ArgumentNullException(nameof(factions));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
        _skillGrants = skillGrants
            ?? throw new ArgumentNullException(nameof(skillGrants));
    }

    public Result<CombatAttackResolution> Handle(ResolveCombatAttackCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        AgentState? attacker = _agents.Get(command.AttackerId);
        AgentState? target = _agents.Get(command.TargetId);
        if (attacker is null || target is null)
        {
            return Result<CombatAttackResolution>.Failure(
                CombatApplicationErrors.AgentNotFound);
        }

        FactionState factions = _factions.Get();
        FactionId? attackerFaction = factions.GetMemberFaction(attacker.Id);
        FactionId? targetFaction = factions.GetMemberFaction(target.Id);
        if (!attackerFaction.HasValue || !targetFaction.HasValue)
        {
            return Result<CombatAttackResolution>.Failure(
                CombatApplicationErrors.FactionMembershipMissing);
        }

        AgentSnapshot attackerSnapshot = attacker.CreateSnapshot(command.Tick);
        AgentSnapshot targetSnapshot = target.CreateSnapshot(command.Tick);
        CombatantSnapshot attackerCombatant = CreateCombatant(
            attackerSnapshot,
            attackerFaction.Value,
            command.AttackerModifiers);
        CombatantSnapshot targetCombatant = CreateCombatant(
            targetSnapshot,
            targetFaction.Value,
            command.TargetModifiers);
        CombatAttackRequest request = new CombatAttackRequest(
            command.ActionId,
            attacker.Id,
            target.Id,
            command.WeaponProfileId,
            command.WorldSeed,
            command.Tick);

        CombatState combat = _combat.Get();
        bool isReplay = combat.HasResolvedAttack(command.ActionId);
        Result intentValidation = ValidateSourceIntent(command, combat);
        if (intentValidation.IsFailure)
        {
            return Result<CombatAttackResolution>.Failure(intentValidation.Error!);
        }

        if (!isReplay)
        {
            Result skillPreflight = ValidatePotentialSkillGrants(
                command,
                combat,
                attacker,
                target);
            if (skillPreflight.IsFailure)
            {
                return Result<CombatAttackResolution>.Failure(skillPreflight.Error!);
            }
        }

        Result<CombatAttackResolution> resolved = combat.ResolveAttack(
            request,
            attackerCombatant,
            targetCombatant,
            factions);
        if (resolved.IsFailure)
        {
            return resolved;
        }

        CombatAttackResolution resolution = resolved.Value;
        if (!resolution.WasAlreadyProcessed && command.SourceIntentId.HasValue)
        {
            Result completedIntent = combat.CompleteIntent(
                command.SourceIntentId.Value,
                command.Tick);
            if (completedIntent.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Validated combat intent could not complete: {completedIntent.Error}");
            }
        }

        IReadOnlyList<SkillGrantBundle> skillBundles = BuildSkillBundles(
            command,
            resolution,
            combat,
            attacker,
            target);
        ApplyConfirmedSkillResults(skillBundles);
        if (!resolution.WasAlreadyProcessed && resolution.Damage > 0)
        {
            Result damageApplied = target.ApplyExternalNeedDelta(
                new NeedDelta(0, 0, 0, -resolution.Damage),
                "combat:" + resolution.ActionId,
                command.Tick);
            if (damageApplied.IsFailure)
            {
                return Result<CombatAttackResolution>.Failure(
                    CombatApplicationErrors.DamageRejected);
            }

            _agents.Save(target);
        }

        _combat.Save(combat);
        _eventSink.Append(combat.DequeueUncommittedEvents());
        _eventSink.Append(target.DequeueUncommittedEvents());
        return resolved;
    }

    private static IReadOnlyList<SkillGrantBundle> BuildSkillBundles(
        ResolveCombatAttackCommand command,
        CombatAttackResolution resolution,
        CombatState combat,
        AgentState attacker,
        AgentState target)
    {
        List<SkillGrantBundle> bundles = new List<SkillGrantBundle>(2);
        if (resolution.WasAlreadyProcessed
            || resolution.Outcome == CombatAttackOutcome.Miss)
        {
            return bundles;
        }

        CombatSkillProfile? weapon = combat.Weapons
            .Get(resolution.WeaponProfileId)
            .SkillProfile;
        if (weapon is not null)
        {
            bundles.Add(new SkillGrantBundle(
                attacker.Id,
                SkillGrantSourceKind.CombatHit,
                resolution.ActionId + ":weapon:" + resolution.WeaponProfileId,
                command.Tick,
                new[] { new SkillGrant(weapon.SkillId, weapon.HitGrantUnits) }));
        }

        ShieldSkillProfile? shield = command.TargetModifiers.ShieldSkillProfile;
        if (resolution.Outcome == CombatAttackOutcome.Blocked
            && shield is not null)
        {
            bundles.Add(new SkillGrantBundle(
                target.Id,
                SkillGrantSourceKind.ShieldDefense,
                resolution.ActionId + ":shield:"
                    + shield.ProfileId,
                command.Tick,
                new[]
                {
                    new SkillGrant(
                        AgentSkillCatalog.Defense,
                        shield.DefenseGrantUnits),
                }));
        }

        return bundles;
    }

    private Result ValidatePotentialSkillGrants(
        ResolveCombatAttackCommand command,
        CombatState combat,
        AgentState attacker,
        AgentState target)
    {
        CombatSkillProfile? weapon = combat.Weapons
            .Get(command.WeaponProfileId)
            .SkillProfile;
        if (weapon is not null)
        {
            Result validation = _skillGrants.Validate(new SkillGrantBundle(
                attacker.Id,
                SkillGrantSourceKind.CombatHit,
                command.ActionId + ":weapon:" + command.WeaponProfileId,
                command.Tick,
                new[] { new SkillGrant(weapon.SkillId, weapon.HitGrantUnits) }));
            if (validation.IsFailure)
            {
                return validation;
            }
        }

        ShieldSkillProfile? shield = command.TargetModifiers.ShieldSkillProfile;
        if (shield is null)
        {
            return Result.Success();
        }

        return _skillGrants.Validate(new SkillGrantBundle(
            target.Id,
            SkillGrantSourceKind.ShieldDefense,
            command.ActionId + ":shield:" + shield.ProfileId,
            command.Tick,
            new[]
            {
                new SkillGrant(
                    AgentSkillCatalog.Defense,
                    shield.DefenseGrantUnits),
            }));
    }

    private static Result ValidateSourceIntent(
        ResolveCombatAttackCommand command,
        CombatState combat)
    {
        if (!command.SourceIntentId.HasValue
            || combat.HasResolvedAttack(command.ActionId))
        {
            return Result.Success();
        }

        CombatIntentSnapshot? intent = combat.GetActiveIntent(command.AttackerId);
        return intent is not null
            && intent.IntentId == command.SourceIntentId.Value
            && intent.Kind == CombatIntentKind.Attack
            && intent.TargetEntityId == command.TargetId
            && command.Tick < intent.ExpiresTick
                ? Result.Success()
                : Result.Failure(CombatApplicationErrors.CombatIntentInactive);
    }

    private void ApplyConfirmedSkillResults(IReadOnlyList<SkillGrantBundle> bundles)
    {
        for (int index = 0; index < bundles.Count; index++)
        {
            Result<SkillRedistributionReport> applied = _skillGrants.ApplyConfirmed(
                bundles[index]);
            if (applied.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Confirmed combat skill grant failed: {applied.Error}");
            }
        }
    }

    private static CombatantSnapshot CreateCombatant(
        AgentSnapshot agent,
        FactionId factionId,
        CombatantModifiers modifiers)
    {
        return new CombatantSnapshot(
            agent.Id,
            factionId,
            agent.Position,
            agent.IsAlive,
            agent.Needs.Health,
            modifiers.AccuracyModifier,
            modifiers.Evasion,
            modifiers.Armor,
            modifiers.BlockChance,
            modifiers.BlockValue);
    }
}
}
