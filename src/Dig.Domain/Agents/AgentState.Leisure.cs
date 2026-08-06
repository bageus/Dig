using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public readonly struct LeisureVarietyId : IEquatable<LeisureVarietyId>
{
    private readonly string? _value;

    public LeisureVarietyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Leisure variety id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);
    public bool Equals(LeisureVarietyId other) =>
        string.Equals(_value, other._value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is LeisureVarietyId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    public override string ToString() => _value ?? string.Empty;
}

public sealed class LeisureActivityDefinition
{
    public LeisureActivityDefinition(
        LeisureVarietyId id,
        int moodPerInterval,
        int intervalTicks,
        bool isSocial)
    {
        if (id.IsEmpty || moodPerInterval <= 0 || intervalTicks <= 0)
        {
            throw new ArgumentException("Leisure definition values must be positive.");
        }

        Id = id;
        MoodPerInterval = moodPerInterval;
        IntervalTicks = intervalTicks;
        IsSocial = isSocial;
    }

    public LeisureVarietyId Id { get; }
    public int MoodPerInterval { get; }
    public int IntervalTicks { get; }
    public bool IsSocial { get; }
}

public sealed class LeisureRuntimeSnapshot
{
    public LeisureRuntimeSnapshot(
        IEnumerable<LeisureVarietyId> history,
        LeisureVarietyId? activeVariety,
        EntityId? partnerId,
        long nextEffectTick,
        bool historyCommitted,
        int moodGainPercent)
    {
        LeisureVarietyId[] values = (history ?? throw new ArgumentNullException(nameof(history)))
            .ToArray();
        if (values.Length > 10 || values.Any(value => value.IsEmpty))
        {
            throw new ArgumentException("Leisure history is invalid.", nameof(history));
        }

        if (activeVariety.HasValue && nextEffectTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextEffectTick));
        }

        History = new ReadOnlyCollection<LeisureVarietyId>(values);
        ActiveVariety = activeVariety;
        PartnerId = partnerId;
        NextEffectTick = nextEffectTick;
        HistoryCommitted = historyCommitted;
        MoodGainPercent = moodGainPercent;
    }

    public IReadOnlyList<LeisureVarietyId> History { get; }
    public LeisureVarietyId? ActiveVariety { get; }
    public EntityId? PartnerId { get; }
    public long NextEffectTick { get; }
    public bool HistoryCommitted { get; }
    public int MoodGainPercent { get; }
}

public sealed partial class AgentState
{
    private readonly List<LeisureVarietyId> _leisureHistory = new List<LeisureVarietyId>();
    private LeisureVarietyId? _activeLeisureVariety;
    private EntityId? _leisurePartnerId;
    private long _nextLeisureEffectTick = -1;
    private bool _leisureHistoryCommitted;
    private int _leisureMoodGainPercent = 100;

    public LeisureRuntimeSnapshot CreateLeisureRuntimeSnapshot() =>
        new LeisureRuntimeSnapshot(
            _leisureHistory,
            _activeLeisureVariety,
            _leisurePartnerId,
            _nextLeisureEffectTick,
            _leisureHistoryCommitted,
            _leisureMoodGainPercent);

    public Result BeginLeisure(
        LeisureActivityDefinition definition,
        EntityId? partnerId,
        long tick)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        ValidateTick(tick);
        if (definition.IsSocial != partnerId.HasValue || partnerId == Id)
        {
            return Result.Failure(AgentErrors.InvalidLeisureReservation);
        }

        int repeated = _leisureHistory.Count(value => value.Equals(definition.Id));
        _activeLeisureVariety = definition.Id;
        _leisurePartnerId = partnerId;
        _nextLeisureEffectTick = checked(tick + definition.IntervalTicks);
        _leisureHistoryCommitted = false;
        _leisureMoodGainPercent = repeated >= 5 ? 50 : 100;
        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result AdvanceLeisure(LeisureActivityDefinition definition, long tick)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        ValidateTick(tick);
        if (!_activeLeisureVariety.HasValue
            || !_activeLeisureVariety.Value.Equals(definition.Id)
            || tick < _nextLeisureEffectTick)
        {
            return Result.Success();
        }

        int gain = checked(definition.MoodPerInterval * _leisureMoodGainPercent / 100);
        Result applied = ApplyExternalNeedDelta(
            new NeedDelta(0, 0, gain, 0),
            "leisure:" + definition.Id,
            tick);
        if (applied.IsFailure) return applied;
        if (!_leisureHistoryCommitted)
        {
            _leisureHistory.Add(definition.Id);
            if (_leisureHistory.Count > 10) _leisureHistory.RemoveAt(0);
            _leisureHistoryCommitted = true;
        }

        _nextLeisureEffectTick = checked(tick + definition.IntervalTicks);
        Version = checked(Version + 1);
        return Result.Success();
    }

    public void RestoreLeisureRuntime(LeisureRuntimeSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        _leisureHistory.Clear();
        _leisureHistory.AddRange(snapshot.History);
        _activeLeisureVariety = snapshot.ActiveVariety;
        _leisurePartnerId = snapshot.PartnerId;
        _nextLeisureEffectTick = snapshot.NextEffectTick;
        _leisureHistoryCommitted = snapshot.HistoryCommitted;
        _leisureMoodGainPercent = snapshot.MoodGainPercent;
    }

    public void CancelLeisure()
    {
        _activeLeisureVariety = null;
        _leisurePartnerId = null;
        _nextLeisureEffectTick = -1;
        _leisureHistoryCommitted = false;
        _leisureMoodGainPercent = 100;
    }
}

}
