using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public Result ApplyDecision(
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        long tick)
    {
        return ApplyDecision(decision, policy, target: null, tick);
    }

    public Result ApplyDecision(
        AgentDecision decision,
        AgentBehaviorPolicy policy,
        AgentActivityTarget? target,
        long tick)
    {
        if (decision is null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (decision.Tick != tick)
        {
            return Result.Failure(AgentErrors.DecisionTickMismatch);
        }

        LastDecision = decision;
        bool sameAction = _activeAction is not null
            && _activeAction.IntentKind == decision.SelectedIntent
            && string.Equals(
                _activeAction.PlayerOrderId,
                decision.SelectedPlayerOrderId,
                StringComparison.Ordinal)
            && Nullable.Equals(_activeAction.Target, target);
        if (sameAction)
        {
            Version = checked(Version + 1);
            return Result.Success();
        }

        if (_activeAction is not null)
        {
            Raise(new AgentActionInterrupted(
                tick,
                Id,
                _activeAction.IntentKind,
                decision.SelectedIntent));
        }

        AgentActionEffect effect = policy.Actions.Get(decision.SelectedIntent);
        _activeAction = new ActiveAgentAction(
            decision.SelectedIntent,
            decision.SelectedPlayerOrderId,
            tick,
            effect.DurationTicks,
            target);
        LastActionSwitchTick = tick;
        LastActionBlockReason = null;
        Version = checked(Version + 1);
        Raise(new AgentActionStarted(
            tick,
            Id,
            decision.SelectedIntent,
            decision.SelectedPlayerOrderId));
        return Result.Success();
    }

    public Result AdvanceAction(AgentBehaviorPolicy policy, long tick)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (_activeAction is null)
        {
            return Result.Success();
        }

        if (_activeAction.Target.HasValue)
        {
            return Result.Failure(AgentErrors.TargetedActionRequired);
        }

        AgentIntentKind completedIntent = _activeAction.IntentKind;
        AgentActionEffect effect = policy.Actions.Get(completedIntent);
        ApplyNeedDelta(effect.NeedDelta, tick);
        bool completed = _activeAction.Advance();
        Version = checked(Version + 1);
        HandleDeath(tick);

        if (IsAlive && completed)
        {
            _activeAction = null;
            Raise(new AgentActionCompleted(tick, Id, completedIntent));
        }

        return Result.Success();
    }

    public Result<bool> AdvanceTargetedAction(long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result<bool>.Failure(AgentErrors.AgentDead);
        }

        if (_activeAction is null || !_activeAction.Target.HasValue)
        {
            return Result<bool>.Failure(AgentErrors.TargetedActionRequired);
        }

        if (_activeAction.IsReadyToComplete)
        {
            return Result<bool>.Failure(AgentErrors.TargetedActionAlreadyReady);
        }

        bool ready = _activeAction.Advance();
        Version = checked(Version + 1);
        return Result<bool>.Success(ready);
    }

    public Result CompleteTargetedAction(AgentBehaviorPolicy policy, long tick)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (_activeAction is null || !_activeAction.Target.HasValue)
        {
            return Result.Failure(AgentErrors.TargetedActionRequired);
        }

        if (!_activeAction.IsReadyToComplete)
        {
            return Result.Failure(AgentErrors.TargetedActionNotReady);
        }

        AgentIntentKind completedIntent = _activeAction.IntentKind;
        AgentActionEffect effect = policy.Actions.Get(completedIntent);
        ApplyNeedDelta(effect.NeedDelta, tick);
        _activeAction = null;
        LastActionBlockReason = null;
        Version = checked(Version + 1);
        HandleDeath(tick);
        if (IsAlive)
        {
            Raise(new AgentActionCompleted(tick, Id, completedIntent));
        }

        return Result.Success();
    }

    public Result BlockTargetedAction(string reason, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Action block reason is required.", nameof(reason));
        }

        if (_activeAction is null || !_activeAction.Target.HasValue)
        {
            return Result.Failure(AgentErrors.TargetedActionRequired);
        }

        AgentIntentKind interrupted = _activeAction.IntentKind;
        _activeAction = null;
        LastActionBlockReason = reason.Trim();
        LastActionSwitchTick = tick;
        Version = checked(Version + 1);
        Raise(new AgentActionBlocked(tick, Id, interrupted, LastActionBlockReason));
        return Result.Success();
    }
}
}
