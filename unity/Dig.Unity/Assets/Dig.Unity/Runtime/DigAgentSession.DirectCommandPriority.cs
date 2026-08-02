using System;
using System.Collections.Generic;
using Dig.Domain.Combat;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private readonly Dictionary<EntityId, long> _directCommandPriorityStartedTicks =
        new Dictionary<EntityId, long>();
    private Func<EntityId, bool>? _hasActiveTerrainDirectCommand;

    internal void BindDirectCommandPrioritySource(
        Func<EntityId, bool> hasActiveTerrainDirectCommand)
    {
        _hasActiveTerrainDirectCommand = hasActiveTerrainDirectCommand
            ?? throw new ArgumentNullException(nameof(hasActiveTerrainDirectCommand));
    }

    internal Result BeginResidentDirectCommand(EntityId residentId, long tick)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Result suppressed = SuppressResidentCombatForDirectCommand(
            residentId,
            tick);
        if (suppressed.IsFailure)
        {
            return suppressed;
        }

        _directCommandPriorityStartedTicks[residentId] = tick;
        return Result.Success();
    }

    internal bool HasResidentDirectCommandPriority(EntityId residentId, long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (!_directCommandPriorityStartedTicks.TryGetValue(
                residentId,
                out long startedTick))
        {
            return false;
        }

        if (_manualTunnelMovements.ContainsKey(residentId)
            || (_hasActiveTerrainDirectCommand?.Invoke(residentId) ?? false)
            || tick <= checked(startedTick + 1))
        {
            return true;
        }

        _directCommandPriorityStartedTicks.Remove(residentId);
        return false;
    }

    internal Result SuppressResidentCombatForDirectCommand(
        EntityId residentId,
        long tick)
    {
        Result disengaged = DisengageResidentForDirectOrder(residentId, tick);
        if (disengaged.IsFailure
            || _combatOnlyActors.Contains(residentId)
            || _combatRepository == null)
        {
            return disengaged;
        }

        CombatState combat = _combatRepository.Get();
        CombatIntentSnapshot? intent = combat.GetActiveIntent(residentId);
        bool changed = false;
        if (intent != null)
        {
            Result cancelled = combat.CancelIntent(
                intent.IntentId,
                "resident_direct_command_overrode_combat",
                tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            changed = true;
        }
        else
        {
            CombatExecutionSnapshot? execution = combat.GetActiveExecution(residentId);
            if (execution != null)
            {
                Result cancelled = combat.CancelExecution(
                    execution.ExecutionId,
                    tick,
                    "resident_direct_command_overrode_combat");
                if (cancelled.IsFailure)
                {
                    return cancelled;
                }

                changed = true;
            }
        }

        if (!changed)
        {
            return Result.Success();
        }

        _combatRepository.Save(combat);
        _combatJournal?.Append(combat.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
