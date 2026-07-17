using System;
using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState : AggregateRoot
{
    private readonly AgentNeedsState _needs;
    private readonly AgentSkillSet _skills;
    private readonly AgentTraitSet _traits;
    private ActiveAgentAction? _activeAction;
    private PlayerOrder? _playerOrder;
    private long _lastNeedsTick = -1;

    public AgentState(
        EntityId id,
        string name,
        AgentNeedsSnapshot initialNeeds,
        DailySchedule schedule,
        IEnumerable<AgentSkillValue>? skills = null,
        IEnumerable<AgentTraitId>? traits = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Agent name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _needs = new AgentNeedsState(
            initialNeeds.Nutrition,
            initialNeeds.Alertness,
            initialNeeds.Mood,
            initialNeeds.Health);
        _skills = new AgentSkillSet(skills ?? Array.Empty<AgentSkillValue>());
        _traits = new AgentTraitSet(traits ?? Array.Empty<AgentTraitId>());
    }

    public EntityId Id { get; }

    public string Name { get; }

    public DailySchedule Schedule { get; }

    public long Version { get; private set; }

    public bool IsAlive => _needs.Health.Points > NeedValue.Minimum;

    public long LastActionSwitchTick { get; private set; } = -1;

    public AgentDecision? LastDecision { get; private set; }

    public string? LastActionBlockReason { get; private set; }

    public Result SetSkillLevel(AgentSkillId skillId, int level)
    {
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        _skills.SetLevel(skillId, level);
        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result SetPlayerOrder(PlayerOrder order, long tick)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        if (!order.IsActiveAt(tick))
        {
            return Result.Failure(AgentErrors.PlayerOrderInactive);
        }

        _playerOrder = order;
        Version = checked(Version + 1);
        Raise(new AgentPlayerOrderChanged(tick, Id, order.Id));
        return Result.Success();
    }

    public Result ClearPlayerOrder(long tick)
    {
        ValidateTick(tick);
        if (_playerOrder is null)
        {
            return Result.Success();
        }

        _playerOrder = null;
        Version = checked(Version + 1);
        Raise(new AgentPlayerOrderChanged(tick, Id, null));
        return Result.Success();
    }

    public Result AdvanceNeeds(AgentBehaviorPolicy policy, long tick)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        ValidateTick(tick);
        if (tick <= _lastNeedsTick)
        {
            return Result.Failure(AgentErrors.TickNotIncreasing);
        }

        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        ExpirePlayerOrder(tick);
        _needs.AdvancePassive(policy.Needs);
        _lastNeedsTick = tick;
        Version = checked(Version + 1);
        HandleDeath(tick);
        return Result.Success();
    }

    public AgentSnapshot CreateSnapshot(long tick)
    {
        ValidateTick(tick);
        PlayerOrder? activeOrder = _playerOrder is not null && _playerOrder.IsActiveAt(tick)
            ? _playerOrder
            : null;
        AgentActionSnapshot? action = _activeAction?.CreateSnapshot();

        return AgentSnapshot.FromNormalizedCapabilities(
            Id,
            Name,
            Version,
            IsAlive,
            _needs.CreateSnapshot(),
            Schedule.GetActivity(tick),
            action,
            activeOrder,
            LastActionSwitchTick,
            LastDecision,
            _skills.CreateSnapshot(),
            _traits.CreateSnapshot(),
            SpatialPosition);
    }

    private void ExpirePlayerOrder(long tick)
    {
        if (_playerOrder is not null && !_playerOrder.IsActiveAt(tick))
        {
            _playerOrder = null;
            Raise(new AgentPlayerOrderChanged(tick, Id, null));
        }
    }

    private void HandleDeath(long tick)
    {
        if (IsAlive)
        {
            return;
        }

        _activeAction = null;
        _playerOrder = null;
        Raise(new AgentDied(tick, Id));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

}
