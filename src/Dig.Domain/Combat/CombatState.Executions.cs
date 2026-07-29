using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public sealed partial class CombatState
{
    private readonly Dictionary<CombatExecutionId, CombatExecutionRecord> _executions =
        new Dictionary<CombatExecutionId, CombatExecutionRecord>();
    private readonly Dictionary<EntityId, CombatExecutionId> _activeExecutions =
        new Dictionary<EntityId, CombatExecutionId>();

    public Result<CombatExecutionSnapshot> StartExecution(CombatExecutionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_executions.TryGetValue(request.ExecutionId, out CombatExecutionRecord? existing))
        {
            return Result<CombatExecutionSnapshot>.Success(existing.CreateSnapshot());
        }

        CombatIntentSnapshot? intent = GetActiveIntent(request.ActorId);
        if (intent is null || intent.IntentId != request.IntentId)
        {
            return Result<CombatExecutionSnapshot>.Failure(new DomainError(
                "combat.execution.intent_inactive",
                "A spatial combat execution requires the actor's active combat intent."));
        }

        CombatExecutionSnapshot? previous = CancelActiveExecutionForActor(
            request.ActorId,
            request.Tick,
            "replaced_by_new_execution");
        CombatExecutionRecord created = new CombatExecutionRecord(
            request,
            intent.TargetEntityId,
            intent.TargetCell);
        _executions.Add(request.ExecutionId, created);
        _activeExecutions[request.ActorId] = request.ExecutionId;
        Version = checked(Version + 1);
        CombatExecutionSnapshot current = created.CreateSnapshot();
        Raise(new CombatExecutionChanged(request.Tick, previous, current));
        return Result<CombatExecutionSnapshot>.Success(current);
    }

    public CombatExecutionSnapshot? GetActiveExecution(EntityId actorId)
    {
        return _activeExecutions.TryGetValue(actorId, out CombatExecutionId executionId)
            ? _executions[executionId].CreateSnapshot()
            : null;
    }

    public CombatExecutionSnapshot? GetExecution(CombatExecutionId executionId)
    {
        return _executions.TryGetValue(executionId, out CombatExecutionRecord? execution)
            ? execution.CreateSnapshot()
            : null;
    }

    public IReadOnlyList<CombatExecutionSnapshot> CreateExecutionSnapshot()
    {
        CombatExecutionSnapshot[] values = _executions.Values
            .OrderBy(value => value.ExecutionId)
            .Select(value => value.CreateSnapshot())
            .ToArray();
        return new ReadOnlyCollection<CombatExecutionSnapshot>(values);
    }

    public int GetSoftClaimCount(CellId cell, EntityId excludingActor)
    {
        return _activeExecutions.Values
            .Select(id => _executions[id])
            .Count(execution => execution.ActorId != excludingActor
                && execution.EngagementCell.HasValue
                && execution.EngagementCell.Value == cell);
    }

    public Result SetExecutionTarget(
        CombatExecutionId executionId,
        EntityId targetEntityId,
        CellId lastKnownTargetCell,
        long tick,
        string reasonCode)
    {
        if (targetEntityId.IsEmpty)
        {
            throw new ArgumentException("Target id cannot be empty.", nameof(targetEntityId));
        }

        return MutateExecution(executionId, tick, execution =>
        {
            execution.SetTarget(targetEntityId, lastKnownTargetCell, reasonCode);
        });
    }

    public Result SetExecutionEquipment(
        CombatExecutionId executionId,
        WeaponProfileId weaponProfileId,
        long tick,
        string reasonCode)
    {
        if (weaponProfileId.IsEmpty)
        {
            throw new ArgumentException("Weapon profile id cannot be empty.", nameof(weaponProfileId));
        }

        return MutateExecution(executionId, tick, execution =>
        {
            execution.SetEquipment(weaponProfileId, reasonCode);
        });
    }

    public Result SetExecutionEngagement(
        CombatExecutionId executionId,
        CellId? engagementCell,
        long tick,
        string reasonCode)
    {
        return MutateExecution(executionId, tick, execution =>
        {
            execution.SetEngagement(engagementCell, reasonCode);
        });
    }

    public Result AdvanceExecutionStage(
        CombatExecutionId executionId,
        CombatExecutionStage stage,
        long nextStageTick,
        long tick,
        string reasonCode)
    {
        if (!Enum.IsDefined(typeof(CombatExecutionStage), stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }

        if (nextStageTick < tick)
        {
            throw new ArgumentOutOfRangeException(nameof(nextStageTick));
        }

        return MutateExecution(executionId, tick, execution =>
        {
            execution.Advance(stage, nextStageTick, reasonCode);
        });
    }

    public Result RecordExecutionAttack(
        CombatExecutionId executionId,
        CombatActionId actionId,
        long nextStageTick,
        long tick)
    {
        if (actionId.IsEmpty)
        {
            throw new ArgumentException("Action id cannot be empty.", nameof(actionId));
        }

        return MutateExecution(executionId, tick, execution =>
        {
            execution.RecordAttack(actionId, nextStageTick);
        });
    }

    public Result IncrementExecutionRetry(
        CombatExecutionId executionId,
        long nextStageTick,
        long tick,
        string reasonCode)
    {
        return MutateExecution(executionId, tick, execution =>
        {
            execution.IncrementRetry(nextStageTick, reasonCode);
        });
    }

    public Result CompleteExecution(
        CombatExecutionId executionId,
        long tick,
        string reasonCode)
    {
        return FinishExecution(
            executionId,
            CombatExecutionStage.Completed,
            tick,
            reasonCode);
    }

    public Result CancelExecution(
        CombatExecutionId executionId,
        long tick,
        string reasonCode)
    {
        return FinishExecution(
            executionId,
            CombatExecutionStage.Cancelled,
            tick,
            reasonCode);
    }

    public void PublishAlarm(CombatAlarmStimulus stimulus)
    {
        if (stimulus is null)
        {
            throw new ArgumentNullException(nameof(stimulus));
        }

        Version = checked(Version + 1);
        Raise(new CombatAlarmPublished(stimulus));
    }

    private Result MutateExecution(
        CombatExecutionId executionId,
        long tick,
        Action<CombatExecutionRecord> mutation)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (mutation is null)
        {
            throw new ArgumentNullException(nameof(mutation));
        }

        if (!_executions.TryGetValue(executionId, out CombatExecutionRecord? execution))
        {
            return Result.Failure(new DomainError(
                "combat.execution.unknown",
                "The spatial combat execution is not registered."));
        }

        if (execution.IsTerminal)
        {
            return Result.Failure(new DomainError(
                "combat.execution.terminal",
                "A terminal spatial combat execution cannot advance."));
        }

        CombatExecutionSnapshot previous = execution.CreateSnapshot();
        mutation(execution);
        Version = checked(Version + 1);
        Raise(new CombatExecutionChanged(tick, previous, execution.CreateSnapshot()));
        return Result.Success();
    }

    private Result FinishExecution(
        CombatExecutionId executionId,
        CombatExecutionStage terminalStage,
        long tick,
        string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Finish reason is required.", nameof(reasonCode));
        }

        if (!_executions.TryGetValue(executionId, out CombatExecutionRecord? execution))
        {
            return Result.Failure(new DomainError(
                "combat.execution.unknown",
                "The spatial combat execution is not registered."));
        }

        if (execution.IsTerminal)
        {
            return Result.Success();
        }

        CombatExecutionSnapshot previous = execution.CreateSnapshot();
        execution.Advance(terminalStage, tick, reasonCode.Trim());
        _activeExecutions.Remove(execution.ActorId);
        Version = checked(Version + 1);
        Raise(new CombatExecutionChanged(tick, previous, execution.CreateSnapshot()));
        return Result.Success();
    }

    private CombatExecutionSnapshot? CancelActiveExecutionForActor(
        EntityId actorId,
        long tick,
        string reasonCode)
    {
        if (!_activeExecutions.TryGetValue(actorId, out CombatExecutionId executionId))
        {
            return null;
        }

        CombatExecutionRecord execution = _executions[executionId];
        if (execution.IsTerminal)
        {
            _activeExecutions.Remove(actorId);
            return execution.CreateSnapshot();
        }

        CombatExecutionSnapshot previous = execution.CreateSnapshot();
        execution.Advance(CombatExecutionStage.Cancelled, tick, reasonCode);
        _activeExecutions.Remove(actorId);
        Raise(new CombatExecutionChanged(tick, previous, execution.CreateSnapshot()));
        return execution.CreateSnapshot();
    }
}
}
