using System;
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

    public ResolveCombatAttackHandler(
        IAgentRepository agents,
        ICombatRepository combat,
        IFactionRepository factions,
        IEventSink eventSink)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _factions = factions ?? throw new ArgumentNullException(nameof(factions));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
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
