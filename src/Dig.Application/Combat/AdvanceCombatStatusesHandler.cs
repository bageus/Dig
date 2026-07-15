using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;

namespace Dig.Application.Combat
{

public sealed class AdvanceCombatStatusesHandler
    : ICommandHandler<AdvanceCombatStatusesCommand, Result<CombatStatusAdvanceReport>>
{
    private readonly IAgentRepository _agents;
    private readonly ICombatRepository _combat;
    private readonly IEventSink _eventSink;

    public AdvanceCombatStatusesHandler(
        IAgentRepository agents,
        ICombatRepository combat,
        IEventSink eventSink)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<CombatStatusAdvanceReport> Handle(AdvanceCombatStatusesCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CombatState combat = _combat.Get();
        EntityId[] targetIds = combat.CreateSnapshot().Statuses
            .Select(item => item.TargetId)
            .Distinct()
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        Dictionary<EntityId, AgentState> targets = new Dictionary<EntityId, AgentState>();
        foreach (EntityId targetId in targetIds)
        {
            AgentState? target = _agents.Get(targetId);
            if (target is null)
            {
                return Result<CombatStatusAdvanceReport>.Failure(
                    CombatApplicationErrors.AgentNotFound);
            }

            targets.Add(targetId, target);
        }

        IReadOnlyList<CombatStatusDamage> damages = combat.AdvanceStatuses(command.Tick);
        var grouped = damages
            .GroupBy(item => item.TargetId)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
        int totalDamage = 0;
        foreach (var group in grouped)
        {
            int targetDamage = group.Sum(item => item.Damage);
            totalDamage = checked(totalDamage + targetDamage);
            if (targetDamage == 0)
            {
                continue;
            }

            AgentState target = targets[group.Key];
            Result applied = target.ApplyExternalNeedDelta(
                new NeedDelta(0, 0, 0, -targetDamage),
                "combat-status:" + command.Tick,
                command.Tick);
            if (applied.IsFailure)
            {
                return Result<CombatStatusAdvanceReport>.Failure(
                    CombatApplicationErrors.DamageRejected);
            }

            _agents.Save(target);
        }

        _combat.Save(combat);
        _eventSink.Append(combat.DequeueUncommittedEvents());
        foreach (var group in grouped)
        {
            _eventSink.Append(targets[group.Key].DequeueUncommittedEvents());
        }

        return Result<CombatStatusAdvanceReport>.Success(
            new CombatStatusAdvanceReport(
                command.Tick,
                damages.Count,
                grouped.Length,
                totalDamage));
    }
}
}
